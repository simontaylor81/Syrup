# Test textures don't get mips when we don't ask for them.

from SRPScripting import *
import utils

col1 = (0, 0, 0, 1)
col2 = (1, 1, 1, 1)

w = 16
h = 16

# Create the texture.
def getPixel(x, y):
	set = x+1 == y or (w - x - 1) == y
	return col1 if set else col2
	
tex = ri.CreateTexture2D(w, h, Format.R8G8B8A8_UNorm, getPixel, False)

utils.TestTextureLevel(ri, tex, 1)
