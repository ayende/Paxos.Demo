using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using log4net;
using Paxos.Messages;

namespace Paxos
{
	public class Agent
	{
		private static readonly Dictionary<Type,int> counters = new Dictionary<Type, int>();

		protected ILog log;
		readonly string name;

		public Agent()
		{
			int value;
			counters.TryGetValue(GetType(), out value);
			value += 1;
			counters[GetType()] = value;
			name = GetType().Name + " #" + (value);
			log = LogManager.GetLogger(name);
		}

		private readonly ConcurrentQueue<Message> queue = new ConcurrentQueue<Message>();
		private readonly Random random = new Random();

		private readonly IDictionary<Type, Action<object>> registrations = new Dictionary<Type, Action<object>>();
		private readonly ConcurrentBag<Message> unordered = new ConcurrentBag<Message>();

		private readonly SemaphoreSlim waitForMessages = new SemaphoreSlim(0);

		public bool ExecuteWork { get; set; }

		[DebuggerNonUserCode]
		public void SendMessage(Message message)
		{
			//DispatchMessage(message);
			EnqueueMessage(message);

			//switch (random.Next(1, 10)) // simulate strangeness in message passing
			//{
			//    default:
			//        EnqueueMessage(message);
			//        break;
			//    case 2: // message lost 10% of the time
			//        break;
			//    case 3: // message arrive multiple times 10% of the time
			//        for (int i = 0; i < random.Next(2, 5); i++)
			//        {
			//            EnqueueMessage(message);
			//        }
			//        break;
			//    case 4: // message will arrive out of order 10% of the time
			//        unordered.Add(message);
			//        break;
			//}

			//Message result; // send message out of order
			//if (unordered.TryTake(out result))
			//    EnqueueMessage(result);
		}


		public void ExecuteMultiThreaded()
		{
			while (ExecuteWork)
			{
				ConsumeAllMessages();
				ProcessTimeouts();
				waitForMessages.Wait();
			}
		}

		public bool ConsumeAllMessages()
		{
			var hadMessages = false;
			Message result;
			while (queue.TryDequeue(out result))
			{
				hadMessages = true;
				DispatchMessage(result);
			}
			return hadMessages;
		}

		[DebuggerNonUserCode]
		private void DispatchMessage(Message result)
		{
			Action<object> value;
			if (registrations.TryGetValue(result.GetType(), out value))
			{
				log.DebugFormat("Processing {0}: {1}", result.GetType().Name, result);
				value(result);
			}
		}

		[DebuggerNonUserCode]
		public void Register<T>(Action<T> action)
		{
			registrations[typeof (T)] = new InvokerWithoutDebugger<T>(action).Invoke;
		}

		private void EnqueueMessage(Message message)
		{
			queue.Enqueue(message);
			waitForMessages.Release(1);
		}

		public virtual bool ProcessTimeouts()
		{
			return false;
		}

		public override string ToString()
		{
			return name;
		}

		#region Nested type: InvokerWithoutDebugger

		public class InvokerWithoutDebugger<T>
		{
			private readonly Action<T> action;

			public InvokerWithoutDebugger(Action<T> action)
			{
				this.action = action;
			}

			[DebuggerNonUserCode]
			public void Invoke(object obj)
			{
				action((T) obj);
			}
		}

		#endregion
	}
}