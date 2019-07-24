namespace PooledAwait.Internal
{
    internal readonly struct Nothing // to express ValueTask via PooledState<Nothing>
    {
        public override string ToString() => nameof(Nothing);
        public override int GetHashCode() => 0;
        public override bool Equals(object? obj) => obj is Nothing;
    }
}
