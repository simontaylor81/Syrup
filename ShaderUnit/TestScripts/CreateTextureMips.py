# Test script-created texture mip generation

from SRPScripting import *
import utils

utils.TestSetting('generateMips', True, globals())

col1 = (0, 0, 0, 1)
col2 = (1, 1, 1, 1)

w = 16
h = 16

# Create the texture.
def getPixel(x, y):
	set = x+1 == y or (w - x - 1) == y
	return col1 if set else col2
	
tex = ri.CreateTexture2D(w, h, Format.R8G8B8A8_UNorm, getPixel, generateMips)

utils.TestTextureLevel(ri, tex, 1)
