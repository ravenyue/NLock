using System;
using System.Collections.Generic;

namespace NLock.Core
{
    public class NLockOptions
    {
        public NLockOptions()
        {
            Extensions = new List<INLockOptionsExtension>();
            //DefaultLockTimeout = 10000;
        }

        //public int DefaultLockTimeout { get; set; }

        internal IList<INLockOptionsExtension> Extensions { get; }

        public void RegisterExtension(INLockOptionsExtension extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            Extensions.Add(extension);
        }
    }
}