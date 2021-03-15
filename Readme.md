interface IEventEmiter
{
	void EmitEvent(string event, Guid itemId);
	IEnumerable<IEvent> GetEvents(int? fromOrder = null);
	MarkEventAsFired(Guid eventId);
	SubmitEvents();
}

Entity Event
{
	ShortString Name;
	Guid ItemId;
	DateTime Timestamp;
	ShortString Username;
	Integer Order;
}

Entity Predmet
{
	EmitCrudEvents;
	AfterSave 'EmitEvent'
	'
		repository.RhetosJobs.AsyncAction.Enqeue(new MojAction());
		....
		foreach(var item in inserted)
			eventEmiter.EmitEvent("PredmetInserted", item.ID);
		foreach(var item in updated)
			eventEmiter.EmitEvent("PredmetUpdated", item.ID);
		foreach(var item in deleted)
			eventEmiter.EmitEvent("PredmetDeleted", item.ID);
	';
}

Entity PredmetElasticQueue
{
	AfterSave 'EmitEvent'
	'
		eventEmiter.EmitEvent("PredmetElasticQueuePopulated");
	';
}

PredmetInserted
PredmetElasticQueuePopulated
PredmetElasticQueuePopulated
PredmetUpdated
PredmetElasticQueuePopulated



------------------------------------------
class TaskEmiter : ITaskEmiter
{
	public void GenerateTasksFromEvents()
	{
		var event = eventEmiter.GetNext(GetLastConsumedEventOrder());
		while (event != null)
		{
			var subscriptions = GetSubscriptions(event.Event);
			var tasks = subscriptions.Select(x => new Task
			{
				Event = event,
				Subscriber = x.Subscriber
			}
			QueueTasks(tasks);
			SaveLastConsumendEventOrder(event.Order);
			event = eventEmiter.GetNext(GetLastConsumedEventOrder());
		}
	}
	
	public int GetLastConsumedEventOrder()
	{
		...
	}
	
	public void SaveLastConsumendEventOrder(int order)
	{
		...
	}
	
	public IEnumerable<Subscription> GetSubscriptions(string eventName)
	{
		...
	}
	
	public bool QueueTasks(IEnumerable<Task> tasks)
	{
		...
	}
}

public class HangfireTasks : IEventEmiter, ITaskEmiter
{
	private readonly List<Event> _events;
	
	public void EmitEvent(Event event)
	{
		_events.Add	(event);
	}
	
	public void GenerateTasksFromEvents()
	{
		
	}
}