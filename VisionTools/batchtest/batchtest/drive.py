import cv2
import os
import sys
import numpy as np
import struct

def load_dat(filename):
    n = 150528
    with open(filename, 'rb') as f:
        image = struct.unpack('f'*n, f.read(4*n))
        f.close()

    image = np.array(image).reshape(224,224,3)
    cv2.imshow("test", image)

load_dat("d:/temp/test.dat")

while True:
    key = cv2.waitKey(1) & 0xFF
    if key == 27:
        break