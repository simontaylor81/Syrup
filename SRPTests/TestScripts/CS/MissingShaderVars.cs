// Test the attempting to bind missing shader variables silently fails
// (this simulates variables that are compiled out).

var ps = ri.CompileShader("ConstantColour.hlsl", "PS", "ps_4_0");

ps.FindConstantVariable("NonExistant").Set(0);
ps.FindResourceVariable("NonExistant").Set(null);
ps.FindSamplerVariable("NonExistant").Set(SamplerState.PointClamp);
ps.FindUavVariable("NonExistant").Set(null);

// Don't actually render anything, we're just testing it doesn't crash.
