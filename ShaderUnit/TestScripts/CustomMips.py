# Test custom mipmap generation.
import utils

utils.TestSetting('sampler', 'linear', globals())
utils.TestSetting('mip', 1, globals())
filename = 'CustomDownsample_2D_' + sampler + '.hlsl'
tex = ri.LoadTexture('Assets/Textures/ThisIsATest.png', generateMips = filename)
utils.TestTextureLevel(ri, tex, mip)
