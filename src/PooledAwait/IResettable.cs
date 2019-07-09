namespace PooledAwait
{
    /// <summary>
    /// Indicates that an object can be reset
    /// </summary>
    public interface IResettable
    {
        /// <summary>
        /// Resets this instance
        /// </summary>
        public void Reset();
    }
}
