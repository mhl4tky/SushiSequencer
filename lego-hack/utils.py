import cv2
import numpy as np
from functools import partial
import math
import json

class HSVBounds:
    def __init__(self, H, range, S_min, S_max, V_min, V_max, label):
        self.H = H
        self.range = range
        self.S_min = S_min
        self.S_max = S_max
        self.V_min = V_min
        self.V_max = V_max
        self.label = label
            
    def serialize(self):
        return dict(H=self.H, range=self.range, S_min=self.S_min, S_max=self.S_max, V_min=self.V_min, V_max=self.V_max)
    
    def print_serialized(self):
        print "label=\"%s\", V_max=%d, range=%d, H=%d, S_min=%d, S_max=%d, V_min=%d" % (self.label, self.V_max, self.range, self.H, self.S_min, self.S_max, self.V_min)
    
    def setH(self, value):
        self.H = value
        self.print_serialized()
        
    def setH_range(self, value):
        self.range = value
        self.print_serialized()
        
    def setS_min(self, value):
        self.S_min = value
        self.print_serialized()
        
    def setS_max(self, value):
        self.S_max = value
        self.print_serialized()
        
    def setV_min(self, value):
        self.V_min = value
        self.print_serialized()
        
    def setV_max(self, value):
        self.V_max = value
        self.print_serialized()

    def has_double_range(self):
        return self.H < self.range/2 or 180 - self.H < self.range/2
    
    def get_range(self, H_min, H_max):
        return [np.array([H_min, self.S_min, self.V_min]), np.array([H_max, self.S_max, self.V_max])]

    def ranges(self):
        if self.has_double_range():
            if self.H < self.range/2:
                return [self.get_range(0, self.H + self.range/2), self.get_range(180 - self.H - self.range/2, 180)]
            else:
                return [self.get_range(self.H - self.range/2, 180), self.get_range(0, self.H + self.range/2 - 180)]
        else:
            return [self.get_range(self.H - self.range/2, self.H + self.range/2)]


kernel = np.ones((3,3),np.uint8)

def recognize(bounds, hsv):
    # Threshold the HSV image to get only certain colors
    ranges = bounds.ranges()
    
    mask = cv2.inRange(hsv, ranges[0][0], ranges[0][1])
    
    if len(ranges) == 2:
        mask2 = cv2.inRange(hsv, ranges[1][0], ranges[1][1])
        mask = cv2.bitwise_or(mask, mask2)
        
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, kernel)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel)
    
    contours,hierarchy = cv2.findContours(mask, cv2.cv.CV_RETR_LIST, cv2.cv.CV_CHAIN_APPROX_SIMPLE)
    
    filtered_contours = []
    
    for index, contour in enumerate(contours):
        moments = cv2.moments(contour)
        area = moments['m00']
        
        if area <= 1000:
            continue
        
        contour_x, contour_y = int(moments['m10']/area), int(moments['m01']/area)
        
        filtered_contours.append(dict(x=contour_x, y=contour_y, area=area, contour=contour))
            
    return filtered_contours