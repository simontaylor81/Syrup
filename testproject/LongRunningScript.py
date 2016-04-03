from SRPScripting import *
import time

vs = ri.CompileShader("BasicShaders.hlsl", "BasicVS", "vs_4_0")
ps = ri.CompileShader("BasicShaders.hlsl", "SolidColourPS", "ps_4_0")

ps.FindConstantVariable("SolidColour").BindToMaterial("DiffuseColour")

# Insert long delay to test async script execution.
time.sleep(5)

def RenderFrame(context):
	context.DrawScene(vs, ps)

ri.SetFrameCallback(RenderFrame)
