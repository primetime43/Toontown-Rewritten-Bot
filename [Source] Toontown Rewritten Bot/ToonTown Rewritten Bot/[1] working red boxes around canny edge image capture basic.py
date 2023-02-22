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

    # Convert the image to grayscale
    gray = cv2.cvtColor(resized_screenshot, cv2.COLOR_RGB2GRAY)

    # Apply Canny edge detection
    edges = cv2.Canny(gray, 100, 200)

    # Find contours around the edges
    contours, hierarchy = cv2.findContours(edges, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

    # Draw red boxes around the contours
    for contour in contours:
        x, y, w, h = cv2.boundingRect(contour)
        cv2.rectangle(resized_screenshot, (x, y), (x+w, y+h), (0, 0, 255), 2)

    # Display the captured screen with red boxes around the detected edges
    cv2.imshow('Window Capture', resized_screenshot)

    if cv2.waitKey(1) == ord('q'):  # Exit on 'q' key press
        break

cv2.destroyAllWindows()
