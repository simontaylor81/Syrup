# Test texture generation

from SRPScripting import *
import utils

# Create the texture.
def getPixel(x, y):
	return (x / 15.0, y / 15.0, 0, 1)
	
tex = ri.CreateTexture2D(16, 16, Format.R8G8B8A8_UNorm, getPixel)

utils.TestTexture(ri, tex)
