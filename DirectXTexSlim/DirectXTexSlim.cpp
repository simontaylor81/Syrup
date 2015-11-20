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

void ScratchImage::GenerateMipMaps()
{
	// TODO: Handle cubemaps, arrays, textures that already have mips.
	if (scratchImage_->GetImageCount() != 1)
	{
		return;
	}

	// TODO: Handle block compressed images.
	if (DirectX::IsCompressed(scratchImage_->GetMetadata().format))
	{
		return;
	}

	auto newScratchImage = new DirectX::ScratchImage();
	try
	{
		auto hr = DirectX::GenerateMipMaps(
			*scratchImage_->GetImage(0, 0, 0), (DWORD)DirectX::TEX_FILTER_FANT, 0, *newScratchImage);

		Marshal::ThrowExceptionForHR(hr);

		// Replace existing scratch image with the new one with mips.
		delete scratchImage_;
		scratchImage_ = newScratchImage;
		newScratchImage = nullptr;
	}
	finally
	{
		// Free new scratch image if we didn't use it.
		delete newScratchImage;
	}

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
