using System.Collections;
using System.Collections.Generic;

namespace Tools.FactKeyRegistry
{
    /// <summary>
    /// this is for all the places that contribute fact keys. if you want a key to
    /// be contributed from somewhere, implement this
    /// and use [FactKeySource] on top of class that gives the constants
    /// </summary>
    public interface IFactKeySource
    {
        IEnumerable<string> GetFactKeys();
    }
}