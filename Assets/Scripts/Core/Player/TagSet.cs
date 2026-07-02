using System;
using System.Collections.Generic;

 namespace Core.Player
 {
     /// <summary>
     /// List of tags. tags are the states of the actor: fighting, frozen, etc.
     /// </summary>
     [System.Flags]
     public enum Tag
     {
         None        = 0,
         Interacting = 1 << 0,
         Charging = 1 << 1,
         Busy     = 1 << 2,
         
         
         // 1 << N is a bit shift — it places a single 1 bit at    
         // position N in the number, so each tag value has its own
         // unique bit:
         //
         // 1 << 0  =  00000001  =  1
         // 1 << 1  =  00000010  =  2
         // 1 << 2  =  00000100  =  4
         // 1 << 3  =  00001000  =  8
         //
         // The point is that because each tag owns exactly one    
         // bit, the bitwise & and | in HasAny/HasAll can check and
         // combine multiple tags at once without them colliding. 
     }
     
     /// <summary>
     /// this class is for the tags - the state the player is currently in
     /// for example, stunned, frozen, in fighting mode etc. thus the tags
     /// can tell the modules if the modules can execute themselves
     /// </summary>
     public class TagSet
     {
         private readonly Dictionary<Tag, int> currentTags = new();
         private Tag present;

         public event Action<Tag> Added;
         public event Action<Tag> Removed;

         //something with bits aggregation
         public bool HasAny(Tag mask) => (present & mask) != 0;
         public bool HasAll(Tag mask) => (present & mask) == mask;

         public void Add(Tag t)
         {
             foreach (var bit in Bits(t))
             {
                 currentTags.TryGetValue(bit, out int c);
                 currentTags[bit] = c + 1;
                 if (c == 0)
                 {
                     present |= bit;
                     Added?.Invoke(bit);
                 }
             }
         }

         public void Remove(Tag t)
         {
             foreach (var bit in Bits(t))
             {
                 if (!currentTags.TryGetValue(bit, out int c) || c == 0) continue;
                 currentTags[bit] = --c;
                 if (c == 0)
                 {
                     present &= ~bit;
                     Removed?.Invoke(bit);
                 }
             }
         }

         private static IEnumerable<Tag> Bits(Tag t)
         {
             for (Tag b = (Tag)1; (int)b != 0 && b <= t; b = (Tag)((int)b << 1))
             {
                 if ((t & b) != 0) yield return b;
             }
         }
     }
 }