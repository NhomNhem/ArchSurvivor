//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChocDino.UIFX.Demos
{
	internal class AskForReview : MonoBehaviour, IPointerClickHandler 
	{
		enum Product
		{
			Unknown,
			Bundle,
			BlurFilter,
			DropShadowFilter,
			ExtrudeFilter,
			FrameFilter,
			GlowFilter,
			MotionBlur,
			OutlineFilter,
			Trail,
		}

		[SerializeField] Product _product = Product.Unknown;

		public void OnPointerClick(PointerEventData eventData) 
		{
			string url = GetBestReviewUrl();
			if (!string.IsNullOrEmpty(url))
			{
				Application.OpenURL(url);
			}
		}

		private readonly static string BundleId = "266945";
		private readonly static string BlurFilterId = "268262";

		private readonly static string DropShadowFilterId = "272733";
		private readonly static string ExtrudeFilterId = "276742";
		private readonly static string FrameFilterId = "301228";
		private readonly static string GlowFilterId = "274847";
		private readonly static string MotionBlurId = "260687";
		private readonly static string OutlineFilterId = "273578";
		private readonly static string TrailId = "260697";

		private static string GetReviewUrl(string assetId)
		{
			return string.Format("https://assetstore.unity.com/packages/slug/{0}?aid=1100lSvNe#reviews", assetId);
		}

		private string GetBestReviewUrl()
		{
			// Try to work out which asset package is being used
			if (GetTypeFromName("ChocDino.UIFX.FillGradientFilter") != null)
			{
				return GetReviewUrl(BundleId);
			}

			switch (_product)
			{
				case Product.Bundle:
				return GetReviewUrl(BundleId);
				case Product.BlurFilter:
				return GetReviewUrl(BlurFilterId);
				case Product.DropShadowFilter:
				return GetReviewUrl(DropShadowFilterId);
				case Product.ExtrudeFilter:
				return GetReviewUrl(ExtrudeFilterId);
				case Product.FrameFilter:
				return GetReviewUrl(FrameFilterId);
				case Product.GlowFilter:
				return GetReviewUrl(GlowFilterId);
				case Product.MotionBlur:
				return GetReviewUrl(MotionBlurId);
				case Product.OutlineFilter:
				return GetReviewUrl(OutlineFilterId);
				case Product.Trail:
				return GetReviewUrl(TrailId);
				default:
				case Product.Unknown:
				break;
			}

			if (GetTypeFromName("ChocDino.UIFX.BlurFilter") != null)
			{
				return GetReviewUrl(BlurFilterId);
			}
			if (GetTypeFromName("ChocDino.UIFX.DropShadowFilter") != null)
			{
				return GetReviewUrl(DropShadowFilterId);
			}
			if (GetTypeFromName("ChocDino.UIFX.ExtrudeFilter") != null)
			{
				return GetReviewUrl(ExtrudeFilterId);
			}
			if (GetTypeFromName("ChocDino.UIFX.FrameFilter") != null)
			{
				return GetReviewUrl(FrameFilterId);
			}
			if (GetTypeFromName("ChocDino.UIFX.GlowFilter") != null)
			{
				return GetReviewUrl(GlowFilterId);
			}
			if (GetTypeFromName("ChocDino.UIFX.OutlineFilter") != null)
			{
				return GetReviewUrl(OutlineFilterId);
			}
			if (GetTypeFromName("ChocDino.UIFX.MotionBlurReal") != null)
			{
				return GetReviewUrl(MotionBlurId);
			}
			if (GetTypeFromName("ChocDino.UIFX.TrailEffect") != null)
			{
				return GetReviewUrl(TrailId);
			}

			return GetReviewUrl(BundleId);
		}

		public static System.Type GetTypeFromName(string name)
		{
			foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies().Reverse())
			{
				var tt = assembly.GetType(name);
				if (tt != null)
				{
					return tt;
				}
			}

			return null;
		}
	}
}