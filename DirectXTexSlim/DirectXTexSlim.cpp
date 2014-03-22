// This is the main DLL file.

#include "stdafx.h"
#include "DirectXTexSlim.h"

using namespace System::Runtime::InteropServices;
using namespace SlimDX::Direct3D11;

namespace DirectXTexSlim
{

//--------------------------------------------------------------------------------------------------
// ScratchImage implementation.
//--------------------------------------------------------------------------------------------------

ScratchImage::ScratchImage()
{
	scratchImage_ = new DirectX::ScratchImage();
}

ScratchImage::~ScratchImage()
{
	delete scratchImage_;
}

Texture2D^ ScratchImage::CreateTexture(Device^ device)
{
	// Get internal device pointer.
	auto deviceRaw = static_cast<ID3D11Device*>(device->ComPointer.ToPointer());

	// Create the texture.
	ID3D11Resource* texture = nullptr;
	HRESULT hr = DirectX::CreateTexture(
		deviceRaw,
		scratchImage_->GetImages(),
		scratchImage_->GetImageCount(),
		scratchImage_->GetMetadata(),
		&texture);

	// Throw on failure.
	Marshal::ThrowExceptionForHR(hr);

	// Convert back to SlimDX type.
	return Texture2D::FromPointer(System::IntPtr(texture));
}


//--------------------------------------------------------------------------------------------------
// Load an image from a DDS file.
//--------------------------------------------------------------------------------------------------
ScratchImage^ DirectXTex::LoadFromDDSFile(String^ filename)
{
	// Get C rep of filename string.
	pin_ptr<const wchar_t> filenameCStr = PtrToStringChars(filename);

	auto image = gcnew ScratchImage();
	auto hr = DirectX::LoadFromDDSFile(filenameCStr, DirectX::DDS_FLAGS_NONE, nullptr, *image->GetScratchImage());

	// Throw on failure.
	Marshal::ThrowExceptionForHR(hr);

	return image;
}


//--------------------------------------------------------------------------------------------------
// Load an image from a file using WIC.
//--------------------------------------------------------------------------------------------------
ScratchImage^ DirectXTex::LoadFromWICFile(String^ filename)
{
	// Get C rep of filename string.
	pin_ptr<const wchar_t> filenameCStr = PtrToStringChars(filename);

	auto image = gcnew ScratchImage();
	auto hr = DirectX::LoadFromWICFile(filenameCStr, DirectX::WIC_FLAGS_NONE, nullptr, *image->GetScratchImage());

	// Throw on failure.
	Marshal::ThrowExceptionForHR(hr);

	return image;
}


//--------------------------------------------------------------------------------------------------
// Load an image from a TGA file.
//--------------------------------------------------------------------------------------------------
ScratchImage^ DirectXTex::LoadFromTGAFile(String^ filename)
{
	// Get C rep of filename string.
	pin_ptr<const wchar_t> filenameCStr = PtrToStringChars(filename);

	auto image = gcnew ScratchImage();
	auto hr = DirectX::LoadFromTGAFile(filenameCStr, nullptr, *image->GetScratchImage());

	// Throw on failure.
	Marshal::ThrowExceptionForHR(hr);

	return image;
}

}
