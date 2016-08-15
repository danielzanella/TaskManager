using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
	/// <summary>
	/// Defines an interface to statistic data.
	/// </summary>
    public interface IStatsData
    {
		/// <summary>
		/// Gets or sets the raw value.
		/// </summary>
		/// <value>The raw value.</value>
        long RawValue { get; set; }

		/// <summary>
		/// Increment the stats data raw value.
		/// </summary>
        void Increment();

		/// <summary>
		/// Increments the stats data raw value by the value specified.
		/// </summary>
		/// <param name="value">The value.</param>
        void IncrementBy(long value);

		/// <summary>
		/// Decrement the stats data raw value.
		/// </summary>
        void Decrement();        
    }
}
