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
	auto metaData = scratchImage_->GetMetadata();
	
	// Skip images that already have mipmaps.
	if (metaData.mipLevels > 1)
	{
		return;
	}

	// We can't generate mips for compressed formats, so skip those.
	if (DirectX::IsCompressed(metaData.format))
	{
		return;
	}

	auto newScratchImage = new DirectX::ScratchImage();
	try
	{
		auto hr = DirectX::GenerateMipMaps(scratchImage_->GetImages(), scratchImage_->GetImageCount(),
			metaData, (DWORD)DirectX::TEX_FILTER_FANT, 0, *newScratchImage);

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

void ScratchImage::CreateEmptyMipChain()
{
	auto metaData = scratchImage_->GetMetadata();

	// Skip images that already have mipmaps.
	if (metaData.mipLevels > 1)
	{
		return;
	}

	// We can't generate mips for compressed formats, so skip those.
	if (DirectX::IsCompressed(metaData.format))
	{
		return;
	}

	auto newScratchImage = new DirectX::ScratchImage();
	try
	{
		HRESULT hr;
		if (metaData.miscFlags & DirectX::TEX_MISC_TEXTURECUBE)
		{
			hr = newScratchImage->InitializeCube(metaData.format, metaData.width, metaData.height, metaData.arraySize / 6, 0);
		}
		else
		{
			hr = newScratchImage->Initialize2D(metaData.format, metaData.width, metaData.height, metaData.arraySize, 0);
		}
			
		Marshal::ThrowExceptionForHR(hr);

		// Copy each array entry.
		for (size_t item = 0; item < metaData.arraySize; item++)
		{
			hr = DirectX::CopyRectangle(
				*scratchImage_->GetImage(0, item, 0),
				DirectX::Rect(0, 0, metaData.width, metaData.height),
				*newScratchImage->GetImage(0, item, 0),
				0,	// Filter -- not needed since source and dest format are the same.
				0, 0);
			Marshal::ThrowExceptionForHR(hr);

			// Zero out mipchain.
			for (size_t mip = 1; mip < newScratchImage->GetMetadata().mipLevels; mip++)
			{
				auto* image = newScratchImage->GetImage(mip, item, 0);
				auto* data = image->pixels;
				auto size = image->rowPitch * image->height;
				ZeroMemory(data, size);
			}
		}

		// Replace existing scratch image with the new one (the one with mips).
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

//--------------------------------------------------------------------------------------------------
// Create a 2D texture from raw pixel data.
//--------------------------------------------------------------------------------------------------
ScratchImage^ DirectXTexSlim::DirectXTex::Create2D(SlimDX::DataRectangle^ data, int width, int height, SlimDX::DXGI::Format format)
{
	// Construct Image representation.
	DirectX::Image image;
	image.width = width;
	image.height = height;
	image.format = static_cast<DXGI_FORMAT>(format);
	image.rowPitch = data->Pitch;
	image.slicePitch = 0;
	image.pixels = static_cast<uint8_t*>(data->Data->DataPointer.ToPointer());

	// Create scrath image from the Image.
	auto result = gcnew ScratchImage();
	auto hr = result->GetScratchImage()->InitializeFromImage(image);

	// Throw on failure.
	Marshal::ThrowExceptionForHR(hr);

	return result;
}

}
