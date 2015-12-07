# Test custom mipmap generation.
import utils

tex = ri.LoadTexture('Assets/Textures/ThisIsATest.png', generateMips = '')
utils.TestTextureLevel(ri, tex, 2)
