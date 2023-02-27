from PIL import ImageGrab, Image, ImageDraw
import win32api, win32gui, pyautogui
import sys, time
import os
import matplotlib.pyplot as plt
import matplotlib.patches as patches



# Iterating through all the running processes 
# for process in f.Win32_Process(): 
	
#     if(process.Name == 'TTREngine.exe'):
# 	    # Displaying the P_ID and P_Name of the process 
# 	    print(f"{process.ProcessId:<10} {process.Name}") 


def screenshot(window_title=None):
    if window_title:
        hwnd = win32gui.FindWindow(None, window_title)
        if hwnd:
            win32gui.SetForegroundWindow(hwnd)
            x, y, x1, y1 = win32gui.GetClientRect(hwnd)
            x, y = win32gui.ClientToScreen(hwnd, (x, y))
            x1, y1 = win32gui.ClientToScreen(hwnd, (x1 - x, y1 - y))
            time.sleep(1)
            img = pyautogui.screenshot(region=(x, y, x1, y1))
            return img
        else:
            print('Window not found!')
    else:
        img = pyautogui.screenshot()
        return img

def findImage():
    #adjust confidence to make it harder or easier to find. Adjust with screen sizes etc
    #current working directory + file name of the image to locate
    filePath = os.getcwd() + '\\' + sys.argv[1]
    #print("Path: " + filePath.replace('\\','/'))
    try:
        global regionOnScreen
        global centerOfBox
        regionOnScreen = pyautogui.locateOnScreen(filePath, confidence=sys.argv[2])
        centerOfBox = pyautogui.center(regionOnScreen)
    except TypeError:
        print('Unable to find image')

def getRGBAtCoords():
    x = int(centerOfBox.x)
    y = int(centerOfBox.y)
    print('RGB', img.getpixel((x,y)))

def showDebugView():
    # Create figure and axes
    fig,ax = plt.subplots(1)
    # Display the image
    ax.imshow(img)
    # Create a Rectangle patch
    rect = patches.Rectangle((regionOnScreen.left,regionOnScreen.top),regionOnScreen.width,regionOnScreen.height,linewidth=1,edgecolor='r',facecolor='none')
    # Add the patch to the Axes
    ax.add_patch(rect)
    print("Code will not continue until the plot is closed.")
    plt.show()

#returns the image of the screenshot
img = screenshot('Toontown Rewritten')
findImage() 

if hasattr(regionOnScreen, 'left'):
    #print(regionOnScreen)
    #print(centerOfBox)
    print('x=',centerOfBox.x)
    print('y=',centerOfBox.y)
    #pyautogui.moveTo(centerOfBox)

    #display debug view if
    if(sys.argv[3] == 'True'):
        showDebugView()
    #pyautogui.screenshot(region=(regionOnScreen)).show()
    #time.sleep(1)
    #if img:
        #img.show()

#argv[0] will be the this file's name
#argv[1] image to find
#argv[2] confidence value
#argv[3] debug view bool