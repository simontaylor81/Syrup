// DirectXTexSlim.h

#pragma once

using namespace System;

// Forward decls.
namespace DirectX
{
	class ScratchImage;
}

namespace DirectXTexSlim
{

// Mirror of DirectXTex ScratchImage class.
public ref class ScratchImage
{
public:
	// Create a SlimDX D3D11 texture from the image.
	SlimDX::Direct3D11::Texture2D^ CreateTexture(SlimDX::Direct3D11::Device^ device);

	// Generate mipmaps for this image.
	void GenerateMipMaps();

internal:
	ScratchImage();
	~ScratchImage();

	DirectX::ScratchImage* GetScratchImage() { return scratchImage_; }

private:
	DirectX::ScratchImage* scratchImage_;
};

// Static class containing the global functions for file handling.
public ref class DirectXTex
{
public:

	static ScratchImage^ LoadFromDDSFile(String^ filename);
	static ScratchImage^ LoadFromWICFile(String^ filename);
	static ScratchImage^ LoadFromTGAFile(String^ filename);
};

}
