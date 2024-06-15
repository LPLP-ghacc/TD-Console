using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoDanger;

public class TDAction
{
    private Func<string, string> Action { get; set; }
    private Func<string>? ActionWithoutArgs { get; set; }

    public TDAction(string? actionName, string? actionDescription, Func<string, string> action)
    {
        ActionName = actionName;
        ActionDescription = actionDescription;
        Action = action;
    }

    public TDAction(string actionName, string actionDescription, Func<string> actionWithoutArgs)
    {
        ActionName = actionName;
        ActionDescription = actionDescription;
        ActionWithoutArgs = actionWithoutArgs;
    }

    public string? ActionName { get; set; }
    public string? ActionDescription { get; set; }

    public string Execute(string args)
    {
        if (Action != null)
        {
            return Action(args);
        }
        else if (ActionWithoutArgs != null)
        {
            return ActionWithoutArgs();
        }
        else
        {
            return "No action defined.";
        }
    }
}
