using System;

namespace LkeServices.Triggers.Attributes
{
	public class TimerTriggerAttribute : Attribute
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="period">Period's format is HH:mm:ss</param>
		public TimerTriggerAttribute(string period)
		{
			TimeSpan value;
			if (!TimeSpan.TryParse(period, out value))
				throw new ArgumentException("Can't parse to timespan. Expected format is HH:mm:ss", nameof(period));
			Period = value;		    
		}

		public TimeSpan Period { get; }        
	}
}
