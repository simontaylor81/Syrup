# Test PNG texture load
import utils
utils.TestSetting('extension', 'png', globals())
utils.TestTextureFile(ri, 'Assets/Textures/ThisIsATest.' + extension)
