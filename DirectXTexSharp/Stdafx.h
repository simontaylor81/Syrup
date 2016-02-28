// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once


// Disable some annoying warnings.
#pragma warning(disable: 4100)	// Unreferenced formal parameter.

// 'Type': type referenced was expected in unreferenced assembly 'Assembly', type defined in current translation unit used instead
// Don't know what this warning is about or why it occurs. It *appears* to not be causing any harmful effects, but I can't be sure.
#pragma warning(disable: 4691)

#include <vcclr.h>
#include "DirectXTex.h"
