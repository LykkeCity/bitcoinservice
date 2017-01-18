using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LkeServices.Triggers.Attributes
{
	public class QueueTriggerAttribute : Attribute
	{
		public string Queue { get; }
	    public int MaxPollingIntervalMs { get; }
	    public bool Notify { get; }

	    public QueueTriggerAttribute(string queue, int maxPollingIntervalMs = -1, bool notify = false)
	    {
	        Queue = queue;
	        MaxPollingIntervalMs = maxPollingIntervalMs;
	        Notify = notify;
	    }
	}
}
