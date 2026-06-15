
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RedLockNet;

namespace TurfBooking.Infrastructure.Services
{
    public class DummyRedLock : IRedLock
    {
        public string Resource => "dummy";
        public string LockId => "dummy";
        public bool IsAcquired => true;
        public RedLockStatus Status => RedLockStatus.Acquired;
        public RedLockInstanceSummary InstanceSummary => new RedLockInstanceSummary(1, 0, 0);
        public int ExtendCount => 0;
        public void Dispose() {}
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public class DummyLockFactory : IDistributedLockFactory
    {
        public IRedLock CreateLock(string resource, TimeSpan expiryTime) => new DummyRedLock();
        public IRedLock CreateLock(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null) => new DummyRedLock();
        
        public Task<IRedLock> CreateLockAsync(string resource, TimeSpan expiryTime) => Task.FromResult<IRedLock>(new DummyRedLock());
        public Task<IRedLock> CreateLockAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null) => Task.FromResult<IRedLock>(new DummyRedLock());
        
        public void Dispose() {}
    }
}
