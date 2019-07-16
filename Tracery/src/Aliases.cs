using System;
using System.Collections.Generic;


namespace Tracery {

    public class RuleSet : Dictionary<string, List<string>> {}
    
    public class ActionCache : Dictionary<string, List<List<string>>> {}
    
    public class ModifierTable : Dictionary<string, Func<string, string>> {}

}