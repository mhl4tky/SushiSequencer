import numpy as np
import cv2

video_file = "/Users/bram.dejong/Documents/sushi seq scout 0729 short seq.avi"

circle_min_r = 55
circle_max_r = 65
bounding_top_left = (560, 308)
bounding_bottom_right = (770, 480)

original_frame_name = "original"
draw_frame_name = "with_drawing"
masked_frame_name = "masked"

cv2.namedWindow(original_frame_name, cv2.WINDOW_NORMAL)
cv2.namedWindow(draw_frame_name, cv2.WINDOW_NORMAL)
cv2.namedWindow(draw_frame_name, cv2.WINDOW_NORMAL)

mask_width = bounding_bottom_right[0] - bounding_top_left[0]
mask_height = bounding_bottom_right[1] - bounding_top_left[1]


class Variables:
    def __init__(self):
        self.min_dist = 4000
        self.dp = 1.7
        self.blur = 0
        self.outer_circle_size = 64
        self.inner_circle_size = 50
        self.X = 0
        self.Y = -200

    def set_outer_circle_size(self, R):
        self.outer_circle_size = R

    def set_inner_circle_size(self, R):
        self.inner_circle_size = R

    def set_x(self, X):
        self.X = X

    def set_y(self, Y):
        self.Y = Y

    def set_blur(self, x):
        self.blur = x

    def set_dp(self, param):
        self.dp = param

    def set_min_dist(self, x):
        self.min_dist = x


variables = Variables()

cv2.createTrackbar("Inner circ", original_frame_name, variables.inner_circle_size, 90,
                   lambda x: variables.set_inner_circle_size(x))
cv2.createTrackbar("Outer circ", original_frame_name, variables.outer_circle_size, 90,
                   lambda x: variables.set_outer_circle_size(x))
cv2.createTrackbar("X", original_frame_name, variables.X + 200, 400, lambda x: variables.set_x(x - 200))
cv2.createTrackbar("Y", original_frame_name, variables.Y + 200, 400, lambda x: variables.set_y(x - 200))

cv2.createTrackbar("BLUR", original_frame_name, variables.blur, 3, lambda x: variables.set_blur(x))
cv2.createTrackbar("DP", original_frame_name, int((variables.dp - 1) * 100), 100,
                   lambda x: variables.set_dp(1.0 + x / 100.0))
cv2.createTrackbar("MINDIST", original_frame_name, variables.min_dist, 4000, lambda x: variables.set_min_dist(x))

color_lookup = {
    "red": (0, 0, 0xFF),
    "yellow": (0, 0xFF, 0xFF),
    "orange": (0, 0xA5, 0xFF),
    "green": (0x57, 0x8B, 0x2E),
    "blue": (0xCD, 0x5A, 0x6A)
}

cap = cv2.VideoCapture(video_file)

total_frames = cap.get(cv2.cv.CV_CAP_PROP_FRAME_COUNT) - 5


def offset_xy(point, x, y):
    return point[0] + x, point[1] + y


previous_y_value = -20000

while cap.isOpened():

    ret, frame = cap.read()

    br = offset_xy(bounding_bottom_right, variables.X, variables.Y)
    tl = offset_xy(bounding_top_left, variables.X, variables.Y)

    to_process = frame.copy()[tl[1]:br[1], tl[0]:br[0]]
    to_draw = to_process.copy()

    # original
    cv2.circle(frame, (variables.X, variables.Y), variables.outer_circle_size, color_lookup["red"], 2)
    cv2.rectangle(frame, br, tl, color_lookup["green"], 2)
    cv2.imshow(original_frame_name, frame)

    to_process = cv2.cvtColor(to_process, cv2.COLOR_BGR2GRAY)
    to_process = cv2.medianBlur(to_process, variables.blur * 2 + 1)

    circles = cv2.HoughCircles(to_process, cv2.cv.CV_HOUGH_GRADIENT, variables.dp, variables.min_dist,
                               minRadius=circle_min_r, maxRadius=circle_max_r)

    if circles is not None:
        a, b, c = circles.shape
        circles = np.uint16(np.around(circles))
        for i in range(b):
            y_value = circles[0][i][1]

            if y_value + 20 < previous_y_value:
                mask = np.zeros((mask_height, mask_width), np.uint8)
                cv2.circle(mask, (circles[0][i][0], circles[0][i][1]), variables.outer_circle_size, (255, 255, 255), -1)
                cv2.circle(mask, (circles[0][i][0], circles[0][i][1]), variables.inner_circle_size, (0, 0, 0), -1)

                masked_image = cv2.bitwise_and(to_draw, to_draw, mask=mask)

                hsv_image = cv2.cvtColor(to_draw, cv2.COLOR_BGR2HSV)

                h_hist = cv2.calcHist([hsv_image], [0], mask, [256], [0, 255])

                hsv_max = np.uint8([[[h_hist.argmax(), 255, 255]]])
                bgr = map(int, tuple(cv2.cvtColor(hsv_max, cv2.COLOR_HSV2BGR)[0][0]))
                print bgr, type(bgr[0])

                cv2.rectangle(masked_image, (0, 0), (20, 20), bgr, -1)

                cv2.imshow(masked_frame_name, masked_image)

                h = np.zeros((300, 256, 3))
                bins = np.arange(256).reshape(256, 1)
                hist_item = cv2.calcHist([masked_image], [0], mask, [256], [0, 255])
                cv2.normalize(hist_item, hist_item, 0, 255, cv2.NORM_MINMAX)
                hist = np.int32(np.around(hist_item))
                pts = np.column_stack((bins, hist))
                cv2.polylines(h, [pts], False, (255, 255, 255))
                h = np.flipud(h)
                cv2.imshow('colorhist', h)

            previous_y_value = y_value

            cv2.circle(to_draw, (circles[0][i][0], circles[0][i][1]), variables.outer_circle_size,
                       color_lookup["red"], 2)
            cv2.circle(to_draw, (circles[0][i][0], circles[0][i][1]), variables.inner_circle_size, color_lookup["red"],
                       2)

            cv2.circle(to_draw, (circles[0][i][0], circles[0][i][1]), 2, color_lookup["red"], 2)

    cv2.imshow(draw_frame_name, to_draw)

    current_pos = cap.get(cv2.cv.CV_CAP_PROP_POS_FRAMES)

    if current_pos >= total_frames:
        cap.set(cv2.cv.CV_CAP_PROP_POS_MSEC, 0)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

if cap:
    cap.release()

cv2.destroyAllWindows()