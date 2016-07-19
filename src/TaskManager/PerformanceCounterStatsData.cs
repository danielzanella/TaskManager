using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
	/// <summary>
	/// An IStatsData's implementation that use  PerformanceCounter to hold stats data.
	/// </summary>
	public sealed class PerformanceCounterStatsData : IStatsData, IDisposable
    {
		#region Fields
        private PerformanceCounter _counter;
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TaskManager.PerformanceCounterStatsData"/> class.
		/// </summary>
		/// <param name="counter">The underlying PerformanceCounter.</param>
        public PerformanceCounterStatsData(PerformanceCounter counter)
        {
            _counter = counter;
        }
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
                return _counter.RawValue;
            }
            set
            {
                _counter.RawValue = value;
            }
        }
		#endregion

		#region Methods
		/// <summary>
		/// Increment the stats data raw value.
		/// </summary>
        public void Increment()
        {
            _counter.Increment();
        }

		/// <summary>
		/// Increments the stats data raw value by the value specified.
		/// </summary>
		/// <param name="value">The value.</param>
        public void IncrementBy(long value)
        {
            _counter.IncrementBy(value);
        }

		/// <summary>
		/// Decrement the stats data raw value.
		/// </summary>
        public void Decrement()
        {
            _counter.Decrement();
        }

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="TaskManager.PerformanceCounterStatsData"/>.
		/// The <see cref="Dispose"/> method leaves the <see cref="TaskManager.PerformanceCounterStatsData"/> in an unusable
		/// state. After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="TaskManager.PerformanceCounterStatsData"/> so the garbage collector can reclaim the memory that the
		/// <see cref="TaskManager.PerformanceCounterStatsData"/> was occupying.</remarks>
		public void Dispose ()
		{
			_counter.Close ();
		}
		#endregion
    }
}