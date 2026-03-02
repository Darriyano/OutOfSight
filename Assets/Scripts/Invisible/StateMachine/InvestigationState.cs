using UnityEngine;

public class InvestigationState : IState
{
    private StateController controller;
    public InvestigationState(StateController controller)
    {
        this.controller = controller;
    }
    public void Enter()
    {
        controller.Agent.speed = InvisibleParameters.Instance.investigationSpeed;
        Debug.Log("Investigation");
    }
    public void Update()
    {
        if (Vector3.Distance(controller.transform.position, controller.Target.position) < 10f)
        {
            controller.ChangeState(new ChasingState(controller));
        }
    }
    public void Exit()
    {

    }
}

