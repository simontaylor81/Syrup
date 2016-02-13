# Test custom mipmap generation.
import utils

tex = ri.LoadTexture('Assets/Textures/ThisIsATest.png', generateMips = 'CustomDownsample.hlsl')
utils.TestTextureLevel(ri, tex, 1)
