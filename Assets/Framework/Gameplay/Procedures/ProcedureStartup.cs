using Xuch.Framework;
using Xuch.Framework.Utils;
using UnityEngine;

public class ProcedureStartup : ProcedureBase
{
    public override void OnEnter(Fsm<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Log.Debug("Enter ProcedureStartup.");

        ChangeState<ProcedureSplash>(fsm);
    }
}