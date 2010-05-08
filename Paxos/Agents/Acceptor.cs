using System.Collections.Generic;
using System.Linq;
using Paxos.Commands;
using Paxos.Messages;

namespace Paxos.Agents
{
	public class Acceptor : Agent
	{
		private readonly Dictionary<int, AcceptState> acceptorState = new Dictionary<int, AcceptState>();
		private readonly Learner[] learners;

		public Acceptor(Learner[] learners)
		{
			this.learners = learners;
			Register<Propose>(OnPropose);
			Register<Accept>(OnAccept);
			Register<AcceptedValueQuery>(OnAcceptedValueQuery);
		}

		public int AcceptedProposalNumber
		{
			get
			{
				if (acceptorState.Any(x => x.Value.AcceptedValue != null) == false)
					return 0;
				return acceptorState.Where(x => x.Value.AcceptedValue != null).Max(x => x.Key);
			}
		}

		private void OnAcceptedValueQuery(AcceptedValueQuery acceptedValueQuery)
		{
			AcceptState state;
			if (acceptorState.TryGetValue(acceptedValueQuery.ProposalNumber, out state) == false)
				return;
			if (state.AcceptedValue == null)
				return;
			acceptedValueQuery.Originator.SendMessage(new Accepted
			{
				BallotNumber = state.BallotNumber,
				Originator = this,
				ProposalNumber = state.ProposalNumber,
				Value = state.AcceptedValue
			});
		}

		private void OnAccept(Accept accept)
		{
			AcceptState state;
			if (acceptorState.TryGetValue(accept.ProposalNumber, out state) == false)
				return; // trying to accept without a propsal?
			if (accept.BallotNumber < state.BallotNumber)
			{
				accept.Originator.SendMessage(new ProposalSubsumed
				{
					BallotNumber = state.BallotNumber,
					Originator = this,
					ProposalNumber = state.ProposalNumber
				});
				return;
			}
			state.AcceptedValue = accept.Value;
			state.BallotNumber = accept.BallotNumber;
			accept.Originator.SendMessage(new Accepted
			{
				Originator = this,
				ProposalNumber = accept.ProposalNumber,
				BallotNumber = accept.BallotNumber,
				Value = accept.Value
			});
			foreach (var learner in learners)
			{
				learner.SendMessage(new Accepted
				{
					Originator = this,
					ProposalNumber = accept.ProposalNumber,
					BallotNumber = accept.BallotNumber,
					Value = accept.Value
				});
			}
		}

		private void OnPropose(Propose propose)
		{
			AcceptState state;
			if (acceptorState.TryGetValue(propose.ProposalNumber, out state))
			{
				if (propose.BallotNumber <= state.BallotNumber)
				{
					propose.Originator.SendMessage(new ProposalSubsumed
					{
						Originator = this,
						BallotNumber = propose.BallotNumber,
						ProposalNumber = propose.ProposalNumber
					});
				}
				else
				{
					state.BallotNumber = propose.BallotNumber;
					propose.Originator.SendMessage(new Promise
					{
						Originator = this,
						AcceptedValue = state.AcceptedValue,
						BallotNumber = propose.BallotNumber,
						ProposalNumber = propose.ProposalNumber
					});
				}
			}
			else
			{
				acceptorState[propose.ProposalNumber] = new AcceptState
				{
					BallotNumber = propose.BallotNumber,
					ProposalNumber = propose.ProposalNumber
				};
				propose.Originator.SendMessage(new Promise
				{
					AcceptedValue = null,
					BallotNumber = propose.BallotNumber,
					ProposalNumber = propose.ProposalNumber,
					Originator = this
				});
			}
		}

		#region Nested type: AcceptState

		private class AcceptState
		{
			public int ProposalNumber { get; set; }
			public int BallotNumber { get; set; }
			public ICommand AcceptedValue { get; set; }
		}

		#endregion
	}
}