import cv2
import numpy as np
import pygetwindow as gw
from PIL import ImageGrab
import win32gui
import win32api
import time

# Find the window of the running process by its name or title
window_title = "Toontown Rewritten"
handle = win32gui.FindWindow(None, window_title)

# Bring the window to the foreground
win32gui.SetForegroundWindow(handle)

while True:
    # Wait for 0.1 seconds to give the window time to update
    time.sleep(0.1)

    # Get the size and position of the window
    x, y, w, h = win32gui.GetClientRect(handle)

    # Use the cv2 library to capture the screen within the bounds of the window
    screenshot = np.array(ImageGrab.grab(bbox=(x, y, x+w, y+h)))
    screenshot = cv2.cvtColor(screenshot, cv2.COLOR_BGR2RGB)  # Convert color space to RGB

    # Resize the captured image to fit inside the resized window
    resized_screenshot = cv2.resize(screenshot, (800, 600))

    # Display the captured screen
    cv2.imshow('Window Capture', resized_screenshot)

    if cv2.waitKey(1) == ord('q'):  # Exit on 'q' key press
        break

cv2.destroyAllWindows()
