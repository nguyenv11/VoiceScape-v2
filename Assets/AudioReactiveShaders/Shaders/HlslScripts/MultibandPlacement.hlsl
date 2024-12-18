#ifndef MultibandPlacement_HLSL

#define MultibandPlacement_HLSL
#define EPSILON 0.0001
float _FreqLevels[64];

void MultibandPlacement_float(float Bands,float HighsIntesifyer,float MasterLevel, float UvXPosition,float UvYPosition,float OffsetY, out float Out) {
	float segmentsOut = 0;
	float OutVertical;
	float OutLeft;
	float OutRight;
	float baseLine = floor(UvYPosition - OffsetY + 1);
	for (int i = 0; i < Bands; i++) {

		if (i > 0)
			OutLeft = floor(Bands / i * UvXPosition);
		else
			OutLeft = 1;

		OutRight = 1 - floor(Bands / (i + 1) * UvXPosition);

		if (abs(_FreqLevels[i]) > EPSILON)
			OutVertical = baseLine*(1 - floor(Bands / _FreqLevels[i] / (MasterLevel *i+ HighsIntesifyer) * (UvYPosition- OffsetY)))* baseLine;
		else
			OutVertical = 0.0;

		segmentsOut += clamp(OutLeft * OutRight, 0, 1) * OutVertical;
	}
	Out = segmentsOut;
}
#endif