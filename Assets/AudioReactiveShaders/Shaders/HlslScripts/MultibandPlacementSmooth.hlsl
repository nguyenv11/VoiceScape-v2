#ifndef MultibandPlacementSmooth_HLSL

#define MultibandPlacementSmooth_HLSL
#define EPSILON 0.01
float _FreqLevels[64];

void MultibandPlacementSmooth_float(float Bands,  float MasterLevel, float LowsSmooter, float UvXPosition, float UvYPosition, float OffsetY, float BandsWidth, out float Out) {
	float segmentsOut = 0;
	float OutVertical;
	float OutLeft;
	float OutRight;
	float Center = .1;

	//0 is fixed in the start point
	//OutLeft = clamp(1 - Bands / (Bands - ( - BandsWidth)) * (1 - UvXPosition), 0, 1);
	//OutRight = clamp(1 - (Bands / (BandsWidth) * UvXPosition ) , 0, 1);
	//OutVertical = 1 - Bands / Center / /*MasterLevel **/ 10*(UvYPosition - OffsetY);
	//segmentsOut += clamp(OutLeft* OutRight, 0, 1) * OutVertical;

	//0 and Bands are excluded of the loop
	for (int i = 0; i < Bands; i++) {

		OutLeft = clamp(1 - Bands / (Bands - (i - BandsWidth)) * (1 - UvXPosition),0,1);
		OutRight = clamp(1- (Bands / ((i + BandsWidth) + 1) * UvXPosition),0,1);

		if (abs(_FreqLevels[i]) > EPSILON)
			OutVertical =  1- Bands / _FreqLevels[i] / (MasterLevel ) * (UvYPosition - OffsetY);
		else
			OutVertical = 1 - Bands / Center / /*MasterLevel **/ 10*(UvYPosition - OffsetY);

		segmentsOut += clamp(OutLeft * OutRight, 0, 1) * OutVertical;
	}

	//Bands is fixed in the end point
	//OutLeft = clamp(1 - Bands / (Bands - (Bands - BandsWidth)) * (1 - UvXPosition), 0, 1);
	//OutRight = clamp(1 - (Bands / (Bands + BandsWidth)*UvXPosition), 0, 1);
	//OutVertical = 1 - Bands / Center / /*MasterLevel **/ 10*(UvYPosition - OffsetY);
	//segmentsOut += clamp(OutLeft* OutRight, 0, 1) * OutVertical;

	Out = segmentsOut;
}
#endif