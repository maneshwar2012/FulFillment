﻿#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2012
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion
using System;
using System.Threading;

namespace DotNetNuke.Collections.Internal
{
    public class ReaderWriterLockStrategy : IDisposable, ILockStrategy
    {
        private ReaderWriterLockSlim _lock;

        public ReaderWriterLockStrategy() : this(LockRecursionPolicy.NoRecursion)
        {
        }

        public ReaderWriterLockStrategy(LockRecursionPolicy recursionPolicy)
        {
            _lock = new ReaderWriterLockSlim(recursionPolicy);
        }

        #region ILockStrategy Members

        public ISharedCollectionLock GetReadLock()
        {
            return GetReadLock(TimeSpan.FromMilliseconds(-1));
        }

        public ISharedCollectionLock GetReadLock(TimeSpan timeout)
        {
            EnsureNotDisposed();
            if (_lock.TryEnterReadLock(timeout))
            {
                return new ReaderWriterSlimLock(_lock);
            }
            else
            {
                throw new ApplicationException("ReaderWriterLockStrategy.GetReadLock timed out");
            }
        }

        public ISharedCollectionLock GetWriteLock()
        {
            return GetWriteLock(TimeSpan.FromMilliseconds(-1));
        }

        public ISharedCollectionLock GetWriteLock(TimeSpan timeout)
        {
            EnsureNotDisposed();
            if (_lock.TryEnterWriteLock(timeout))
            {
                return new ReaderWriterSlimLock(_lock);
            }
            else
            {
                throw new ApplicationException("ReaderWriterLockStrategy.GetWriteLock timed out");
            }
        }

        public bool ThreadCanRead
        {
            get
            {
                EnsureNotDisposed();
                return _lock.IsReadLockHeld || _lock.IsWriteLockHeld;
                //todo uncomment if upgradelock is used OrElse _lock.IsUpgradeableReadLockHeld
            }
        }

        public bool ThreadCanWrite
        {
            get
            {
                EnsureNotDisposed();
                return _lock.IsWriteLockHeld;
            }
        }

        public bool SupportsConcurrentReads
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region "IDisposable Support"

        private bool _isDisposed;

        public void Dispose()
        {
            // Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("ReaderWriterLockStrategy");
            }
        }

        // To detect redundant calls

        // IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    //dispose managed state (managed objects).
                }

                _lock.Dispose();
                _lock = null;
            }
            _isDisposed = true;
        }

        ~ReaderWriterLockStrategy()
        {
            // Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(false);
        }

        // This code added by Visual Basic to correctly implement the disposable pattern.

        #endregion
    }
}