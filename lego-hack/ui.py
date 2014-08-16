import cv2
import math
import json

class UiElement:
    def unserialize(self, data):
        pass

    def draw(self, image):
        pass

    def on_mouse_down(self, x, y):
        pass

    def on_mouse_move(self, x, y):
        pass

    def on_mouse_up(self, x, y):
        pass
        
class Square:
    def __init__(self, x, y, width, height, color, colors_to_look_for):
        self.x = x
        self.y = y
        self.width = width
        self.height = height
        self.color = color
        self.colors_to_look_for = colors_to_look_for
        
    def draw(self, image):
        thickness = 3
        cv2.line(image, (self.x, self.y), (self.x+self.width, self.y), self.color, thickness)
        cv2.line(image, (self.x+self.width, self.y), (self.x+self.width, self.y+self.height), self.color, thickness)
        cv2.line(image, (self.x+self.width, self.y+self.height), (self.x, self.y+self.height), self.color, thickness)
        cv2.line(image, (self.x, self.y+self.height), (self.x, self.y), self.color, thickness)

class UI:
    def __init__(self, ui):
        self.ui = ui

    def draw(self, image):
        for element in self.ui:
            element.draw(image)