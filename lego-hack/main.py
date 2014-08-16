import cv2
import numpy as np
import collections
import math
import utils
import ui

cap = cv2.VideoCapture(0)

#cv2.namedWindow("mask", cv2.WINDOW_NORMAL)
cv2.namedWindow("frame", cv2.WINDOW_NORMAL)
cv2.namedWindow('perspective_transformed', cv2.WINDOW_NORMAL)

# OpenCV HSV values... H: 0 - 180, S: 0 - 255, V: 0 - 255
bounds = [
    utils.HSVBounds(label="red", V_max=175, range=24, H=4, S_min=36, S_max=255, V_min=0),
    utils.HSVBounds(label="yellow", V_max=255, range=15, H=22, S_min=183, S_max=255, V_min=0),
    utils.HSVBounds(label="orange", V_max=255, range=13, H=6, S_min=216, S_max=255, V_min=0),
    utils.HSVBounds(label="brightgreen", V_max=255, range=45, H=50, S_min=87, S_max=236, V_min=0)
]

hands = utils.HSVBounds(label="hand", V_max=255, range=7, H=13, S_min=74, S_max=250, V_min=0)

ui_bounds_index = 0

def setH(x):
    bounds[ui_bounds_index].setH(x)

def setH_range(x):
    bounds[ui_bounds_index].setH_range(x)

def setS_min(x):
    bounds[ui_bounds_index].setS_min(x)

def setS_max(x):
    bounds[ui_bounds_index].setS_max(x)
    
def setV_min(x):
    bounds[ui_bounds_index].setS_max(x)
    
def setV_max(x):
    bounds[ui_bounds_index].setV_max(x)
            
cv2.createTrackbar("H", "frame", bounds[ui_bounds_index].H, 180, lambda x: setH(x))
cv2.createTrackbar("H range", "frame", bounds[ui_bounds_index].range, 90, lambda x: setH_range(x))
cv2.createTrackbar("S min", "frame", bounds[ui_bounds_index].S_min, 255, lambda x: setS_min(x))
cv2.createTrackbar("S max", "frame", bounds[ui_bounds_index].S_max, 255, lambda x: setS_max(x))
cv2.createTrackbar("V min", "frame", bounds[ui_bounds_index].V_min, 255, lambda x: setV_min(x))
cv2.createTrackbar("V max", "frame", bounds[ui_bounds_index].V_max, 255, lambda x: setV_max(x))
    
first = True

gui = ui.UI([
            ui.Square(251, 352, 1378-251, 982-352, (255, 0, 0), ["red"]),
            ui.Square(100, 100, 200, 200, (255, 0, 0), ["red"]),
        ])

def perspective_transform(ui, frame):
    width = frame.shape[1]
    height = frame.shape[0]

    transform = cv2.getPerspectiveTransform(
                    np.array(list(ui.get_points()),np.float32),
                    np.array([(0,0), (width,0), (width,height), (0,height)],np.float32)
                )
    
    perspective_transformed = cv2.warpPerspective(frame, transform, (width, height))
    return perspective_transformed

def onMouse(event, x, y, flags, ui):
    if event == cv2.EVENT_LBUTTONDOWN:
        print x, y, hsv[y,x]

cv2.setMouseCallback("frame", onMouse, param=ui)

color_lookup = {
    "red": (0, 0, 0xFF),
    "yellow": (0, 0xFF, 0xFF),
    "orange": (0, 0xA5, 0xFF),
    "brightgreen": (0x57, 0x8B, 0x2E),
    "blue": (0xCD, 0x5A, 0x6A)
}

sequence_points = []
offsetX, mulX = gui.ui[0].x + 24, 72.8
offsetY, mulY = gui.ui[0].y + 24, 72.1
for x in range(17):
    for y in range(9):
        X, Y = int(x*mulX + offsetX), int(y*mulY + offsetY)
        sequence_points.append([X,Y,x,y])

sequence = []
hits_history = []

history_length = 7
on_length = 3

import OSC
c = OSC.OSCClient()
c.connect(('192.168.0.6', 7000))   # connect to SuperCollider

do_send = True

def send_on(x, y, color):
    oscmsg = OSC.OSCMessage()
    oscmsg.setAddress("/gridon")
    oscmsg.append(x)
    oscmsg.append(y)
    oscmsg.append(color)
    if do_send:
        c.send(oscmsg)
    #print "on", x, y
    

def send_off(x, y):
    oscmsg = OSC.OSCMessage()
    oscmsg.setAddress("/gridoff")
    oscmsg.append(x)
    oscmsg.append(y)
    if do_send:
        c.send(oscmsg)
    #print "off", x, y
    
def send_nrpn(index, value):
    oscmsg = OSC.OSCMessage()
    oscmsg.setAddress("/nrpn")
    oscmsg.append(index)
    oscmsg.append(value)
    if do_send:
        c.send(oscmsg)

nrpn_value = 0
nrpn_target = 0
nrpn = dict(x=1045, y=211, width=1315-1045, height=265-211)

rotary_value = 0
rotary_target = 0
rotary = dict(region=dict(x=695, y=78, width=933-695, height=255-78), center=[821,195])

xy_value_x = 0
xy_value_y = 0
xy_target_value_x = 0
xy_target_value_y = 0
xy = dict(x=1522, y=496, width=1799-1522, height=1035-496)

alpha = 0.8
i = 1
do_draw = True

while(1):

    # Take each frame
    _, frame = cap.read()
    frame = cv2.flip(frame, 1)
    
    frame_original = frame.copy()
    
    gui.draw(frame)
    #perspective_transformed = perspective_transform(ui, frame_original)
    perspective_transformed = frame_original

    hsv = cv2.cvtColor(perspective_transformed, cv2.COLOR_BGR2HSV)

    contours = utils.recognize(bounds[ui_bounds_index], hsv)
    
    hands_contour = utils.recognize(hands, hsv)
    
    r = 8
    
    for contour in contours:
        contour_x = contour["x"]
        contour_y = contour["y"]
        area = contour["area"]
        original_contour = contour["contour"]
        
        if do_draw:
            cv2.drawContours(perspective_transformed, [original_contour], 0, (255,255,0), 2)
            cv2.line(perspective_transformed, (contour_x - r, contour_y), (contour_x + r, contour_y), (255, 255, 255), 1)
            cv2.line(perspective_transformed, (contour_x, contour_y - r), (contour_x, contour_y + r), (255, 255, 255), 1)
    
    if do_draw:
        for point_x, point_y,x,y in sequence_points:
            cv2.line(perspective_transformed, (point_x - r, point_y), (point_x + r, point_y), (255,255,255), 1)
            cv2.line(perspective_transformed, (point_x, point_y - r), (point_x, point_y + r), (255,255,255), 1)
    
    hits_in_frame = []
    
    point_in_hand_lookup = {}
    for point_x, point_y, x,y in sequence_points:
        point_in_contour = cv2.pointPolygonTest(original_contour, (point_x, point_y), False) == 1
        for hc in hands_contour:
            if cv2.pointPolygonTest(hc["contour"], (point_x, point_y), False) == 1:
                point_in_hand_lookup[(point_x, point_y)] = 1
                break
            
    for bound in bounds:
        contours = utils.recognize(bound, hsv)
        
        color = color_lookup[bound.label]
        for contour in contours:
            contour_x = contour["x"]
            contour_y = contour["y"]
            area = contour["area"]
            original_contour = contour["contour"]
            
            for point_x, point_y, x,y in sequence_points:
                point_in_contour = cv2.pointPolygonTest(original_contour, (point_x, point_y), False) == 1
                                
                if point_in_contour and (point_x, point_y) not in point_in_hand_lookup:
                    if do_draw:
                        cv2.line(perspective_transformed, (point_x - r, point_y), (point_x + r, point_y), color, 2)
                        cv2.line(perspective_transformed, (point_x, point_y - r), (point_x, point_y + r), color, 2)
                    
                    hits_in_frame.append((x, y, bound.label))
                elif (point_x, point_y) in point_in_hand_lookup:
                    if do_draw:
                        cv2.line(perspective_transformed, (point_x - r, point_y), (point_x + r, point_y), (0,255,0), 2)
                        cv2.line(perspective_transformed, (point_x, point_y - r), (point_x, point_y + r), (0,255,0) , 2)
                    
            if bound.label == "red" and contour_x >= nrpn["x"] and contour_x < nrpn["x"]+nrpn["width"] and contour_y >= nrpn["y"] and contour_y < nrpn["y"]+nrpn["height"]:
                nrpn_target = (contour_x - nrpn["x"]) / float(nrpn["width"])
                if nrpn_target < 0:
                    nrpn_target = 0
                elif nrpn_target > 1:
                    nrpn_target = 1
                    
            if bound.label == "red" and contour_x >= rotary["region"]["x"] and contour_x < rotary["region"]["x"]+rotary["region"]["width"] and \
                contour_y >= rotary["region"]["y"] and contour_y < rotary["region"]["y"]+rotary["region"]["height"]:
                
                deltax = rotary["center"][0] - contour_x
                deltay = rotary["center"][1] - contour_y

                angle_rad = math.atan2(deltay,deltax)
                angle_deg = angle_rad*180.0/math.pi + 180
                
                rotary_target = (angle_deg - 157)/(313 - 157)
                
            if bound.label == "brightgreen" and contour_x >= xy["x"] and contour_x < xy["x"]+xy["width"] and contour_y >= xy["y"] and contour_y < xy["y"]+xy["height"]:
                xy_target_value_x = (contour_x - xy["x"]) / float(xy["width"])
                xy_target_value_y = (contour_y - xy["y"]) / float(xy["height"])
                
                if xy_target_value_x > 1:
                    xy_target_value_x = 1
                elif xy_target_value_x < 0:
                    xy_target_value_x = 0
                
                if xy_target_value_y > 1:
                    xy_target_value_y = 1
                elif xy_target_value_y < 0:
                    xy_target_value_y = 0
                
                xy_target_value_y = 1 - xy_target_value_y

    nrpn_value = nrpn_target*alpha + nrpn_value*(1-alpha)
    send_nrpn(1, nrpn_value)

    rotary_value = rotary_target*alpha + rotary_value*(1-alpha)
    send_nrpn(2, rotary_value)
    
    xy_value_x = xy_target_value_x*alpha + xy_value_x*(1-alpha)
    send_nrpn(3, xy_value_x)

    xy_value_y = xy_target_value_y*alpha + xy_value_y*(1-alpha)
    send_nrpn(4, xy_value_y)

    hits_history.append(hits_in_frame)
    
    if len(hits_history) > history_length:
        hits_history = hits_history[1:]
        
    multiple_hits = collections.defaultdict(int)
    
    for frame_hits in hits_history:
        for hit in frame_hits:
            multiple_hits[hit] = multiple_hits[hit] + 1
    
    current_sequence = []
    
    for key, value in multiple_hits.items():
        if value >= on_length:
            current_sequence.append(key)

    for key in current_sequence:
        if key not in sequence:
            x, y, label = key
            send_on(x,y,label)
    
    for key in sequence:
        if key not in current_sequence:
            x, y, label = key
            send_off(x,y)
    
    sequence = current_sequence
    
    if do_draw:
        cv2.imshow('frame', frame)
        cv2.imshow('perspective_transformed', perspective_transformed)
    
    first = False
    
    k = cv2.waitKey(1) & 0xFF
    if k == 27:
        break
    if k == 0x20:
        for key in sequence:
            print "ON", key
            x, y, label = key
            send_on(x,y,label)

    #print i    
    i = i + 1

cv2.destroyAllWindows()