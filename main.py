import numpy as np
import cv2

cap = cv2.VideoCapture('~/Documents/')

R = 10
X = 0
Y = 0

cv2.createTrackbar("CircleRadius", "frame", R, 1000)
cv2.createTrackbar("CircleX", "frame", X, 1000)
cv2.createTrackbar("CircleY", "frame", Y, 1000)

while cap.isOpened():
    ret, frame = cap.read()

    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)

    cv2.imshow('frame', gray)
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()