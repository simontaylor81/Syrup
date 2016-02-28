// DirectXTexSharp.h

#pragma once

using namespace System;

// Forward decls.
namespace DirectX
{
	class ScratchImage;
}

namespace DirectXTexSharp
{

// Mirror of DirectXTex ScratchImage class.
public ref class ScratchImage
{
public:
	// Create a SlimDX D3D11 texture from the image.
	SharpDX::Direct3D11::Texture2D^ CreateTexture(SharpDX::Direct3D11::Device^ device);

	// Generate mipmaps for this image.
	void GenerateMipMaps();

	// Create an empty mip chain for this image.
	void CreateEmptyMipChain();

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

	static ScratchImage^ Create2D(SharpDX::DataRectangle^ data, int width, int height, SharpDX::DXGI::Format format);
};

}
