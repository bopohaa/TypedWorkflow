using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TypedWorkflow
{
    public interface ICache<Tk, Tv>
    {
        void Set(Tk key, ICacheEntry<Tv> value, TimeSpan expiration_time);
        bool TryGet(Tk key, out ICacheEntry<Tv> value);
    }

    public interface ICacheEntry<Tv>
    {
        ValueTask<Tv> GetValue();
    }
}
