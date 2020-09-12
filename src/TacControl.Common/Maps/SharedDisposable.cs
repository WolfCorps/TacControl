using System;
using System.Collections.Generic;
using System.Text;

namespace TacControl.Common.Maps
{
    public sealed class SharedDisposable<T> where T : IDisposable
    {
        public sealed class Reference : IDisposable
        {
            public Reference(SharedDisposable<T> owner)
            {
                mOwner = owner;
            }

            public void Dispose()
            {
                if (mIsDisposed) return;
                mIsDisposed = true;

                mOwner.Release();
            }

            public T Value => mOwner.mValue;

            private readonly SharedDisposable<T> mOwner;
            private bool mIsDisposed;
        }

        public SharedDisposable(T value)
        {
            mValue = value;
        }

        public Reference Acquire()
        {
            lock (mLock)
            {
                if (mRefCount < 0) throw new ObjectDisposedException(typeof(T).FullName);
                mRefCount++;
                return new Reference(this);
            }
        }

        private void Release()
        {
            lock (mLock)
            {
                mRefCount--;
                if (mRefCount <= 0)
                {
                    mValue.Dispose();
                    mRefCount = -1;
                }
            }
        }

        private readonly T mValue;
        private readonly object mLock = new object();
        private int mRefCount;
    }
}
