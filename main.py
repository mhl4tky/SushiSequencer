import numpy as np
import cv2
import os
import OSC

video_file = "/Users/bram.dejong/Documents/sushi seq scout 0729.avi"

osc_connection = ('192.168.0.6', 7000)

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

plate_names = ["RED", "YELLOW", "BLUE", "PURPLE"]
plate_colors = [5, 27, 90, 118]


class Variables:
    def __init__(self):
        self.min_dist = 4000
        self.dp = 1.8
        self.blur = 0
        self.outer_circle_size = 64
        self.inner_circle_size = 45
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


def offset_xy(point, x, y):
    return point[0] + x, point[1] + y


previous_y_values = []
look_back = 10
index = 0


def find_circles(framed, variables):
    to_process = cv2.cvtColor(framed, cv2.COLOR_BGR2GRAY)
    to_process = cv2.medianBlur(to_process, variables.blur * 2 + 1)
    return cv2.HoughCircles(to_process, cv2.cv.CV_HOUGH_GRADIENT, variables.dp, variables.min_dist,
                            minRadius=circle_min_r, maxRadius=circle_max_r)


#osc = OSC.OSCClient()
#osc.connect(osc_connection)

do_send = False

def send_color(color):
    oscmsg = OSC.OSCMessage()
    oscmsg.setAddress("/plate")
    oscmsg.append(color)
    if do_send:
        osc.send(oscmsg)


def draw_hls_channels(hls_image, mask):
    width, height, bits = hls_image.shape
    h, l, s = cv2.split(hls_image)

    background = np.zeros((height, width*2), np.uint8)

    background[0:height, 0:width] = cv2.bitwise_and(l, l, mask=mask)
    background[0:height, width:width*2] = cv2.bitwise_and(s, s, mask=mask)

    cv2.putText(background, "L", (width/2, height/2), cv2.cv.CV_FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255))
    cv2.putText(background, "S", (width/2 + width, height/2), cv2.cv.CV_FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255))

    cv2.imshow("split", background)

cap = cv2.VideoCapture(video_file)

while cap.isOpened():
    index += 1

    ret, frame = cap.read()

    if frame is None:
        break

    original = frame.copy()

    frame_br = offset_xy(bounding_bottom_right, variables.X, variables.Y)
    frame_tl = offset_xy(bounding_top_left, variables.X, variables.Y)

    # take the frame out of the original image
    framed = frame[frame_tl[1]:frame_br[1], frame_tl[0]:frame_br[0]].copy()

    cv2.rectangle(frame, frame_br, frame_tl, color_lookup["green"], 3)
    cv2.imshow(original_frame_name, frame)

    # now process this frame to extract the circles
    circles = find_circles(framed, variables)

    if circles is not None:
        a, b, c = circles.shape
        circles = np.uint16(np.around(circles))
        if b:
            circle = circles[0][0]
            x_value = circle[0]
            y_value = circle[1]

            is_new_circle = True

            for look_back_value in previous_y_values:
                if look_back_value < y_value:
                    is_new_circle = False
                    break

            if is_new_circle:
                center_x = x_value + frame_tl[0]
                center_y = y_value + frame_tl[1]
                size = int(variables.outer_circle_size * 1.2)

                circle_tl = (center_x - size, center_y - size)
                circle_br = (center_x + size, center_y + size)
                cut_around_circle = original[circle_tl[1]:circle_br[1], circle_tl[0]:circle_br[0]].copy()

                mask = np.zeros((size * 2, size * 2), np.uint8)
                cv2.circle(mask, (size, size), variables.outer_circle_size, (255, 255, 255), -1)
                cv2.circle(mask, (size, size), variables.inner_circle_size, (0, 0, 0), -1)

                # find the max HSV value
                hls_image = cv2.cvtColor(cut_around_circle, cv2.COLOR_BGR2HLS)

                draw_hls_channels(hls_image, mask)

                h_argmax = cv2.calcHist([hls_image], [0], mask, [256], [0, 255]).argmax()

                min_diff = 20000
                min_index = -1

                for col_index, color in enumerate(plate_colors):
                    diff_1 = abs(color - h_argmax)
                    diff_2 = abs(color + 180 - h_argmax)
                    diff = min(diff_1, diff_2)
                    if diff < min_diff:
                        min_index = col_index
                        min_diff = diff

                send_color(min_index)

                masked_image = cv2.bitwise_and(cut_around_circle, cut_around_circle, mask=mask)
                cv2.putText(masked_image, plate_names[min_index], (size - variables.inner_circle_size + 10, size),
                            cv2.cv.CV_FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255))
                cv2.imshow(masked_frame_name, masked_image)

                if True:
                    try:
                        os.mkdir(plate_names[min_index])
                    except:
                        pass

                    filename = os.path.join(plate_names[min_index], str(index) + ".png")

                    cv2.imwrite(filename, masked_image)

            previous_y_values.append(y_value)
            if len(previous_y_values) >= look_back:
                previous_y_values = previous_y_values[1:]

            cv2.circle(framed, (x_value, y_value), variables.outer_circle_size, color_lookup["blue"], 2)
            cv2.circle(framed, (x_value, y_value), variables.inner_circle_size, color_lookup["blue"], 2)
            cv2.line(framed, (x_value + 2, y_value), (x_value - 2, y_value), color_lookup["blue"], 2)
            cv2.line(framed, (x_value, y_value + 2), (x_value, y_value - 2), color_lookup["blue"], 2)

    cv2.imshow(draw_frame_name, framed)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()