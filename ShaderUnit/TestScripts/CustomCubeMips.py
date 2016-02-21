# Test custom cubemap mipmap generation.
import utils

#utils.TestSetting('sampler', 'linear', globals())
utils.TestSetting('mip', 1, globals())
filename = 'CustomDownsample_Cube.hlsl'
tex = ri.LoadTexture('Assets/Textures/Cubemap.dds', generateMips = filename)
utils.TestCubemapLevel(ri, tex, mip)
