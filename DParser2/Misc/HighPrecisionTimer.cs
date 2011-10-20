using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace D_Parser.Misc
{
	public interface ITimer
	{
		long Start();
		long Stop();
		double Duration{get;} 
	}
	
    public class HighPrecisionTimer : ITimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private long startTime;
        private long stopTime;
        private long freq;

        public HighPrecisionTimer()
        {
            startTime = 0;
            stopTime = 0;
            freq = 0;
            if (QueryPerformanceFrequency(out freq) == false)
            {
                throw new Win32Exception(); // timer not supported
            }
        }
        /// <summary>
        /// Start the timer
        /// </summary>
        /// <returns>long - tick count</returns>
        public long Start()
        {
            QueryPerformanceCounter(out startTime);
            return startTime;
        }
        /// <summary>
        /// Stop timer 
        /// </summary>
        /// <returns>long - tick count</returns>
        public long Stop()
        {
            QueryPerformanceCounter(out stopTime);
            return stopTime;
        }
        /// <summary>
        /// Return the duration of the timer (in seconds)
        /// </summary>
        /// <returns>double - duration</returns>
        public double Duration
        {
            get
            {
                return (double)(stopTime - startTime) / (double)freq;
            }
        }
        /// <summary>
        /// Frequency of timer (no counts in one second on this machine)
        /// </summary>
        ///<returns>long - Frequency</returns>
        public long Frequency
        {
            get
            {
                QueryPerformanceFrequency(out freq);
                return freq;
            }
        }
    }
	
    public class BasicTimer : ITimer
    {
		Stopwatch timer;
		
        public BasicTimer()
        {		
			timer = new Stopwatch();
			
			//can check if is HighResolution using
			//Stopwatch.IsHighResolution;
				
			//can get frequency using
			//Stopwatch.Frequency;
        }
        /// <summary>
        /// Start the timer
        /// </summary>
        /// <returns>long - tick count</returns>
        public long Start()
        {
			timer.Reset();
			timer.Start();
            return 0;
        }
        /// <summary>
        /// Stop timer 
        /// </summary>
        /// <returns>long - tick count</returns>
        public long Stop()
        {
            timer.Stop();
            return timer.ElapsedMilliseconds;
        }
        /// <summary>
        /// Return the duration of the timer (in seconds)
        /// </summary>
        /// <returns>double - duration</returns>
        public double Duration
        {
            get
            {
                return (double)timer.Elapsed.TotalMilliseconds / (double)1000;
            }
        }
    }	
}