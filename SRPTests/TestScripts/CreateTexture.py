# Test texture generation

from SRPScripting import *
import utils

# Create the texture.
ramp = [((i % 16) / 15.0, (i / 16) / 15.0, 0, 1) for i in xrange(0, 16*16)]
tex = ri.CreateTexture2D(16, 16, Format.R8G8B8A8_UNorm, ramp)

utils.TestTexture(ri, tex)
