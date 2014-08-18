import cv2

video_file = "sushi.avi"


cap = cv2.VideoCapture(0)

while True:
    #cap.isOpened():

    _, frame = cap.read()



    cv2.imshow("dude", frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()