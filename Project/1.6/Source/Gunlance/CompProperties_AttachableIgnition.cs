using UnityEngine;
using Verse;

namespace NewRatkin
{
	public class CompProperties_AttachableIgnition : CompProperties
	{
		// 위치 오프셋
		public Vector3 positionOffset = new Vector3(0, 0, 0.2f);
		
		// PreIgnition 전용 설정
		public float powerIncreasePerTick = 0.02f;
		public float maxPower = 1f;
		
		// AfterIgnition 전용 설정
		public float powerDecreasePerTick = 0.1f;
		public float initialPower = 1f;
		
		// 렌더링 설정
		public float directionOffsetDistance = 1.1f;
		public float preIgnitionBaseSize = 3f;
		public float preIgnitionSizeMultiplier = 4f;
		public float preIgnitionMaxSize = 2.5f;
		public float preIgnitionSizeScale = 1f; // 외부에서 크기를 조절하는 배율 (기본값 1.0)
		public float preIgnitionHeightBase = 1.1f;
		public float preIgnitionHeightReduction = 0.2f;
		public float afterIgnitionSizeMultiplier = 4f;
		public float afterIgnitionSizeScale = 1f; // 외부에서 크기를 조절하는 배율 (기본값 1.0)
		public float afterIgnitionHeightMultiplier = 2f;
		
		// 색상 설정 (AfterIgnition)
		public Color afterIgnitionBaseColor = new Color(1, 0.75f, 0.75f);
		public float afterIgnitionAlphaMultiplier = 1.25f;
		
		public CompProperties_AttachableIgnition()
		{
			this.compClass = typeof(CompAttachableIgnition);
		}
	}
	
	public class CompAttachableIgnition : ThingComp
	{
		public CompProperties_AttachableIgnition Props
		{
			get
			{
				return (CompProperties_AttachableIgnition)this.props;
			}
		}
	}
}
