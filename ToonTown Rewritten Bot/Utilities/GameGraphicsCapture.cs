using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using WinRT;

namespace ToonTown_Rewritten_Bot.Utilities
{
    // Free-threaded capture reads the game's rendered surface, including when another
    // window covers it. Serialize consumers because the frame pool owns its textures.
    internal sealed class GameGraphicsCapture : IDisposable
    {
        private static readonly object Sync = new();
        private static GameGraphicsCapture _current;
        private readonly IntPtr _window;
        private IDirect3DDevice _device;
        private GraphicsCaptureItem _item;
        private Direct3D11CaptureFramePool _pool;
        private GraphicsCaptureSession _session;
        private Windows.Graphics.SizeInt32 _size;

        internal static Bitmap Capture(IntPtr window, GameWindowGeometry geometry)
        {
            lock (Sync)
            {
                try
                {
                    // Never await a WinRT operation on the WinForms synchronization context.
                    return Task.Run(async () =>
                    {
                        if (_current?._window != window)
                        {
                            _current?.Dispose();
                            _current = null;
                            _current = new GameGraphicsCapture(window);
                        }
                        return await _current.ReadFrameAsync(geometry).ConfigureAwait(false);
                    }).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _current?.Dispose();
                    _current = null;
                    throw new WindowCaptureException($"Could not capture the game in background mode: {ex.Message}", ex);
                }
            }
        }

        internal static void Stop()
        {
            lock (Sync)
            {
                _current?.Dispose();
                _current = null;
            }
        }

        private GameGraphicsCapture(IntPtr window)
        {
            _window = window;
            try
            {
                if (!GraphicsCaptureSession.IsSupported())
                    throw new NotSupportedException("Windows Graphics Capture is not supported on this device.");
                _device = CreateDevice();
                _item = CreateItem(window);
                _size = _item.Size;
                _pool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                    _device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _size);
                _session = _pool.CreateCaptureSession(_item);
                if (Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent(
                    "Windows.Graphics.Capture.GraphicsCaptureSession", "IsCursorCaptureEnabled"))
                    _session.IsCursorCaptureEnabled = false;
                _session.StartCapture();
                Logger.Info("Capture", "Capturing the game with Windows Graphics Capture; covered-window detection is available.");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private async Task<Bitmap> ReadFrameAsync(GameWindowGeometry geometry)
        {
            // Discard queued frames from before this scan, especially after a mouse action.
            for (int i = 0; i < 2; i++)
            {
                using var queued = _pool.TryGetNextFrame();
                if (queued == null) break;
            }
            var timer = Stopwatch.StartNew();
            while (timer.Elapsed < TimeSpan.FromSeconds(3))
            {
                using var frame = _pool.TryGetNextFrame();
                if (frame == null)
                {
                    await Task.Delay(15).ConfigureAwait(false);
                    continue;
                }
                if (frame.ContentSize.Width != _size.Width || frame.ContentSize.Height != _size.Height)
                {
                    _size = frame.ContentSize;
                    frame.Dispose();
                    _pool.Recreate(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _size);
                    continue;
                }
                using var software = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface)
                    .AsTask().ConfigureAwait(false);
                var captureSize = new Size(frame.ContentSize.Width, frame.ContentSize.Height);
                // During resize, queued frames can report the new content size while
                // still using a smaller texture from the previous pool allocation.
                if (software.PixelWidth < captureSize.Width || software.PixelHeight < captureSize.Height)
                {
                    frame.Dispose();
                    _pool.Recreate(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _size);
                    continue;
                }
                Rectangle crop = GetClientCrop(_window, geometry, captureSize);
                byte[] pixels = new byte[checked(software.PixelWidth * software.PixelHeight * 4)];
                software.CopyToBuffer(pixels.AsBuffer());
                var bitmap = new Bitmap(crop.Width, crop.Height, PixelFormat.Format32bppArgb);
                try
                {
                    var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size),
                        ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        for (int y = 0; y < crop.Height; y++)
                            Marshal.Copy(pixels, ((crop.Y + y) * software.PixelWidth + crop.X) * 4,
                                data.Scan0 + y * data.Stride, crop.Width * 4);
                    }
                    finally { bitmap.UnlockBits(data); }
                    return bitmap;
                }
                catch { bitmap.Dispose(); throw; }
            }
            throw new TimeoutException("No fresh game frame arrived. Keep the game restored, not minimized.");
        }

        internal static Rectangle GetClientCrop(IntPtr window, GameWindowGeometry geometry, Size captureSize)
        {
            if (captureSize == geometry.ClientBounds.Size) return new Rectangle(Point.Empty, captureSize);
            if (DwmGetWindowAttribute(window, 9, out NativeRect bounds, Marshal.SizeOf<NativeRect>()) == 0)
            {
                var visibleBounds = Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
                if (captureSize == visibleBounds.Size)
                {
                    var crop = new Rectangle(geometry.ClientBounds.X - visibleBounds.X,
                        geometry.ClientBounds.Y - visibleBounds.Y, geometry.ClientBounds.Width, geometry.ClientBounds.Height);
                    if (new Rectangle(Point.Empty, captureSize).Contains(crop)) return crop;
                }
            }
            if (captureSize == geometry.WindowBounds.Size && new Rectangle(Point.Empty, captureSize).Contains(geometry.ClientCrop))
                return geometry.ClientCrop;
            throw new WindowCaptureException("The game changed size during background capture. Try again once it has settled.");
        }

        private static IDirect3DDevice CreateDevice()
        {
            IntPtr device = IntPtr.Zero, context = IntPtr.Zero, dxgi = IntPtr.Zero, inspectable = IntPtr.Zero;
            try
            {
                Marshal.ThrowExceptionForHR(D3D11CreateDevice(IntPtr.Zero, 1, IntPtr.Zero, 0x20,
                    IntPtr.Zero, 0, 7, out device, out _, out context));
                Guid dxgiId = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(device, in dxgiId, out dxgi));
                Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(dxgi, out inspectable));
                return MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
            }
            finally
            {
                if (inspectable != IntPtr.Zero) Marshal.Release(inspectable);
                if (dxgi != IntPtr.Zero) Marshal.Release(dxgi);
                if (context != IntPtr.Zero) Marshal.Release(context);
                if (device != IntPtr.Zero) Marshal.Release(device);
            }
        }

        private static GraphicsCaptureItem CreateItem(IntPtr window)
        {
            const string className = "Windows.Graphics.Capture.GraphicsCaptureItem";
            IntPtr name = IntPtr.Zero, factory = IntPtr.Zero, item = IntPtr.Zero;
            try
            {
                Marshal.ThrowExceptionForHR(WindowsCreateString(className, className.Length, out name));
                Guid interopId = new("3628e81b-3cac-4c60-b7f4-23ce0e0c3356");
                Marshal.ThrowExceptionForHR(RoGetActivationFactory(name, ref interopId, out factory));
                var create = Marshal.GetDelegateForFunctionPointer<CreateForWindow>(
                    Marshal.ReadIntPtr(Marshal.ReadIntPtr(factory), 3 * IntPtr.Size));
                Guid itemId = new("79c3f95b-31f7-4ec2-a464-632ef5d30760");
                Marshal.ThrowExceptionForHR(create(factory, window, ref itemId, out item));
                return MarshalInspectable<GraphicsCaptureItem>.FromAbi(item);
            }
            finally
            {
                if (item != IntPtr.Zero) Marshal.Release(item);
                if (factory != IntPtr.Zero) Marshal.Release(factory);
                if (name != IntPtr.Zero) WindowsDeleteString(name);
            }
        }

        public void Dispose()
        {
            _session?.Dispose(); _session = null;
            _pool?.Dispose(); _pool = null;
            _device?.Dispose(); _device = null;
            _item = null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left, Top, Right, Bottom; }
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateForWindow(IntPtr factory, IntPtr window, ref Guid iid, out IntPtr item);
        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out NativeRect value, int size);
        [DllImport("d3d11.dll")]
        private static extern int D3D11CreateDevice(IntPtr adapter, int driverType, IntPtr software, uint flags,
            IntPtr levels, uint levelCount, uint sdkVersion, out IntPtr device, out int featureLevel, out IntPtr context);
        [DllImport("d3d11.dll")]
        private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr device, out IntPtr graphicsDevice);
        [DllImport("combase.dll", CharSet = CharSet.Unicode)]
        private static extern int WindowsCreateString(string value, int length, out IntPtr handle);
        [DllImport("combase.dll")]
        private static extern int WindowsDeleteString(IntPtr handle);
        [DllImport("combase.dll")]
        private static extern int RoGetActivationFactory(IntPtr name, ref Guid iid, out IntPtr factory);
    }
}
