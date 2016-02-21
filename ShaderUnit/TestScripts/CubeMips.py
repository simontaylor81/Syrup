# Test cubemap mipmap generation
import utils
utils.TestSetting('mip', 3, globals())
utils.TestCubemapFileLevel(ri, 'Assets/Textures/Cubemap.dds', mip)
