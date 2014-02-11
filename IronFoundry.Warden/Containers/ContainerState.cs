namespace IronFoundry.Warden.Containers
{
    using System;
    using System.Linq;

    public class ContainerState : IEquatable<ContainerState>
    {
        // Container object created, but setup not performed
        public static readonly ContainerState Born;

        // Container setup completed
        public static readonly ContainerState Active;

        // Triggered by an error condition in the container (e.g. OOM) or
        // explicitly by the user. All processes have been killed but the
        // container exists for introspection. No new commands may be run.
        public static readonly ContainerState Stopped;

        // All state associated with the container has been destroyed.
        public static readonly ContainerState Destroyed;

        private const string BornState = "Born";
        private const string ActiveState = "Active";
        private const string StoppedState = "Stopped";
        private const string DestroyedState = "Destroyed";

        private static readonly string[] states;

        static ContainerState()
        {
            states = new[] { BornState, ActiveState, StoppedState, DestroyedState };
            Born = new ContainerState(BornState);
            Active = new ContainerState(ActiveState);
            Stopped = new ContainerState(StoppedState);
            Destroyed = new ContainerState(DestroyedState);
        }

        private readonly string state;

        private ContainerState(string state)
        {
            if (state.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("state");
            }
            if (!states.Contains(state))
            {
                throw new ArgumentException("Invalid state.");
            }
            this.state = state;
        }

        public static implicit operator string(ContainerState containerState)
        {
            return containerState.state;
        }

        public static implicit operator ContainerState(string containerState)
        {
            ContainerState state = null;

            if (states.Contains(containerState))
            {
                state = containerState;
            }
            else
            {
                state = new ContainerState(BornState);
            }

            return state;
        }

        public static bool operator ==(ContainerState x, ContainerState y)
        {
            if (Object.ReferenceEquals(x, null))
            {
                return Object.ReferenceEquals(y, null);
            }
            return x.Equals(y);
        }

        public static bool operator !=(ContainerState x, ContainerState y)
        {
            return !(x == y);
        }

        public override string ToString()
        {
            return state.ToString();
        }

        public override int GetHashCode()
        {
            return state.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ContainerState);
        }

        public bool Equals(ContainerState other)
        {
            if (Object.ReferenceEquals(null, other))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.GetHashCode() == other.GetHashCode();
        }
    }
}
