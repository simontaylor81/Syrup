# Test texture mipmap generation
import utils
utils.TestSetting('mip', 1, globals())
utils.TestTextureFileLevel(ri, 'Assets/Textures/ThisIsATest.png', mip)
