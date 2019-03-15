using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * A standard and convenient way to create a timer that expires.
*/

namespace gilligames
{
	public class GameTimer
	{
		private float startTime;            // Time used for starting time.
		private float timeSeconds;			// Duration of timer.
		
		private float timeElapsedWhenPaused = -1.0f;
		
		private static System.DateTime systemStartTime;

		public GameTimer(int seconds) 
		{
			startTimer((float)seconds);
		}

		public GameTimer(float seconds) 
		{
			startTimer(seconds);
		}

		// Returns the "System Seconds Since Startup", which works in actual realtime while the app is suspended.
		public static float SSSS
		{
			get
			{
				init();	// Just in case it hasn't been initialized before calling this.

				// Time calculation uses System.TimeSpans instead of Time.realtimeSinceStartup because timers are getting desynced on device
				// due to closing and resuming the app. Unity was not taking the time in between into account. Confirmed with Jon that this is
				// the desired solution, and even though they can manipulate the system time, they would crash if they attempted to...so its ok. I suppose.
				System.TimeSpan timeDuration = System.DateTime.UtcNow.Subtract(systemStartTime);
				return (float)timeDuration.TotalSeconds;
			}
		}
		
		// The ability to pause and resume the timer.
		public bool isPaused
		{
			set
			{
				if (value)
				{
					// Pausing
					timeElapsedWhenPaused = GameTimer.SSSS - startTime;
				}
				else
				{
					// Resuming
					startTimer(timeSeconds - timeElapsedWhenPaused);
				}
			}
			
			get { return timeElapsedWhenPaused >= 0.0f; }
		}
		
		// Initialize the "System Seconds Since Startup" variable at startup.
		public static void init()
		{
			if (systemStartTime == System.DateTime.MinValue)
			{
				systemStartTime = System.DateTime.UtcNow;
			}
		}
		
		// Called on creation as well as from an event through static updateTimer()
		public void startTimer(float seconds)
		{
			startTime = SSSS;
			timeSeconds = seconds;
			timeElapsedWhenPaused = -1.0f;
		}
		
		// Restarts the timer for the same amount of time.
		public void reset()
		{
			startTimer(timeSeconds);
		}
				
		// Returns the amount of time (in seconds) that has elapsed since starting the timer.
		public float timeElapsed
		{
			get
			{
				if (isPaused)
				{
					return timeElapsedWhenPaused;
				}
				return SSSS - startTime;
			}
		}

		// Returns 0.0-1.0, where 0.0 is no time elapsed and 1.0 if all time elapsed.
		public float timeElapsedNormalized
		{
			get
			{
				return timeElapsed / timeSeconds;
			}
		}
		
		// Returns the amount of time (in seconds) remaining on the timer before it expires.
		public float timeRemaining
		{
			get
			{
				float timeLeft = (timeSeconds - timeElapsed);
				return ((timeLeft < 0.0f) ? 0.0f : timeLeft);
			}
		}
		
		public string timeRemainingFormatted
		{
			get { return Common.secondsFormatted(Mathf.CeilToInt(timeRemaining)); }
		}
		
		// Add some seconds from the time remaining.
		public void addSeconds(float timeDiff)
		{
			timeSeconds += timeDiff;
		}

		// Remove some seconds from the time remaining.
		public void removeSeconds(float timeDiff)
		{
			timeSeconds -= timeDiff;
		}

		public void forceExpire()
		{
			startTime = SSSS - timeSeconds;
		}
			
		// Is the timer expired and ready to do something?
		public bool isExpired
		{
			get { return timeRemaining <= 0; }
		}
	}
}