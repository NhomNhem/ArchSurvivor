//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	class TextureUtils
	{
		private static void ReadTexture(RenderTexture sourceTexture, Texture2D rwTexture)
		{
			// Assumptions
			Debug.Assert(sourceTexture != null);
			Debug.Assert(rwTexture != null);
			Debug.Assert(rwTexture.width == sourceTexture.width && rwTexture.height == sourceTexture.height);

			// Read pixels from GPU to CPU
			RenderTexture prevTexture = RenderTexture.active;
			RenderTexture.active = sourceTexture;
			rwTexture.ReadPixels(new Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0, recalculateMipMaps:false);
			rwTexture.Apply(updateMipmaps:false, makeNoLongerReadable:false);
			RenderTexture.active = prevTexture;

			rwTexture.IncrementUpdateCount();
		}

		internal static bool WriteToPNG(RenderTexture sourceTexture, Texture2D rwTexture, string outputPath)
		{
			ReadTexture(sourceTexture, rwTexture);

			// Write PNG
			byte[] data = ImageConversion.EncodeToPNG(rwTexture);
			string directoryPath = System.IO.Path.GetDirectoryName(outputPath);
			if (!System.IO.Directory.Exists(directoryPath))
			{
				System.IO.Directory.CreateDirectory(directoryPath);
			}
			System.IO.File.WriteAllBytes(outputPath, data);

			return true;
		}

		internal static bool WriteToEXR(RenderTexture sourceTexture, Texture2D rwTexture, string outputPath)
		{
			ReadTexture(sourceTexture, rwTexture);

			// Write EXR
			byte[] data = ImageConversion.EncodeToEXR(rwTexture, Texture2D.EXRFlags.CompressZIP);
			string directoryPath = System.IO.Path.GetDirectoryName(outputPath);
			if (!System.IO.Directory.Exists(directoryPath))
			{
				System.IO.Directory.CreateDirectory(directoryPath);
			}
			System.IO.File.WriteAllBytes(outputPath, data);

			return true;
		}
	}
}