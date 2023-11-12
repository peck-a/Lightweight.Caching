﻿using System;

namespace BitFaster.Caching
{
    /// <summary>
    /// Represents a fixed time based cache policy.
    /// </summary>
    public interface ITimePolicy
    {
        /// <summary>
        /// Gets the time to live for items in the cache.
        /// </summary>
        TimeSpan TimeToLive { get; }

        /// <summary>
        /// Remove all expired items from the cache.
        /// </summary>
        void TrimExpired();
    }

    /// <summary>
    /// Represents a variable time based cache policy.
    /// </summary>
    /// backcompat: this should be generic in terms of Key to avoid object arg.
    public interface IVariableTimePolicy
    {
        /// <summary>
        /// Gets the time to live for an item in the cache.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// <param name="timeToLive">If the key exists, the time to live for the item with the specified key.</param>
        /// <returns>True if the key exists, otherwise false.</returns>
        bool TryGetTimeToLive(object key, out TimeSpan timeToLive);

        /// <summary>
        /// Remove all expired items from the cache.
        /// </summary>
        void TrimExpired();
    }
}
