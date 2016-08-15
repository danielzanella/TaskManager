using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
	/// <summary>
	/// Simple IStatsData implementation that keeps statistic data in memory.
	/// </summary>
    [DebuggerDisplay("{RawValue}")]
    public class MemoryStatsData : IStatsData
    {
		#region Fields
        private object _lock = new object();
        private long _rawValue;
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets the raw value.
		/// </summary>
		/// <value>The raw value.</value>
        public long RawValue
        {
            get
            {
                return _rawValue;
            }

            set
            {
                lock(_lock)
                {
                    _rawValue = value;
                }
            }
        }
     
		/// <summary>
		/// Increment the stats data raw value.
		/// </summary>
        public void Increment()
        {
            lock (_lock)
            {
                _rawValue++;
            }
        }

		/// <summary>
		/// Increments the stats data raw value by the value specified.
		/// </summary>
		/// <param name="value">The value.</param>
        public void IncrementBy(long value)
        {
            lock (_lock)
            {
                _rawValue += value;
            }
        }

		/// <summary>
		/// Decrement the stats data raw value.
		/// </summary>
        public void Decrement()
        {
            lock (_lock)
            {
                _rawValue--;
            }
        }
		#endregion
    }
}