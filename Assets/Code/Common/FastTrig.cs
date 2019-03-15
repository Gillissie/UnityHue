using UnityEngine;
using System.Collections;

/**
Implements fast lookups of sine, cosine, and tangent values using a lookup table.
Maybe if someone is so inspired, inverse trig functions can be added later.

The values looked up are not perfect, but fit within the resolution
*/
public static class FastTrig
{
	private const int QUARTER_RESOLUTION = 800;										///< The resolution defines how accurate the returned trig values are
	private const int FULL_RESOLUTION = QUARTER_RESOLUTION * 4;						///< The resolution defines how accurate the returned trig values are
	private const float OVER_TWO_PI = ((float)FULL_RESOLUTION) / (Mathf.PI * 2f);	///< FULL_RESOLUTION / (2 * PI)
	private const float UNDER_TWO_PI = (Mathf.PI * 2f) / ((float)FULL_RESOLUTION);	///< (2 * PI) / FULL_RESOLUTION
	private const float HALF_PI = Mathf.PI * 0.5f;									///<  PI / 2
	
	private static float[] table = null;	///< A lookup table of precalculated sine values
	
	public static bool isInitialized
	{
		get { return (table != null); }
	}
	
	/// Needs to be called once during application startup before calling any of the other FastTrig functions
	public static void init()
	{	
		if (table == null)
		{
			table = new float[QUARTER_RESOLUTION];
			
			float angle;	
			for (int i = 0; i < QUARTER_RESOLUTION; i++)
			{
				angle = UNDER_TWO_PI * (float)i;
				table[i] = Mathf.Sin(angle);
			}
			
			/* spot check test to see how well this matches up with Mathf.Sin
			string tmp = "TABLE:";
			for (angle = -3f * Mathf.PI; angle < 3f * Mathf.PI; angle += 1f)
			{
				tmp += "\n    FastTrig.sin(" + angle + ") = " + sin(angle);
				tmp += "\n    Mathf.Sin(" + angle + ") = " + Mathf.Sin(angle);
				tmp += "\n-------------------------------";
			}
			Debug.Log(tmp);
			*/
		}
	}
	
	/// Fast Sine lookup.
	public static float sin(float angle)
	{
		init();

		int key = ((int)(angle * OVER_TWO_PI)) % FULL_RESOLUTION;
		float sign;
		
		if (key < 0)
		{
			key *= -1;		
			sign = -1f;
		}
		else
		{
			sign = 1f;
		}
		
		int quadrant = key / QUARTER_RESOLUTION;
		int offset = key % QUARTER_RESOLUTION;
		
		switch (quadrant)
		{
			case 0:
				return sign * table[offset];
			case 1:
				return sign * table[QUARTER_RESOLUTION - offset - 1];
			case 2:
				return sign * -1f * table[offset];
			case 3:
				return sign * -1f * table[QUARTER_RESOLUTION - offset - 1];
		}
		
		return 0;
	}
	
	/// Fast Cosine lookup.
	public static float cos(float angle)
	{
		return sin(angle + HALF_PI);
	}
	
	/// Fast Tangent lookup.
	public static float tan(float angle)
	{
		return sin(angle) / sin(angle + HALF_PI);
	}
}
