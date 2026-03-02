using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StateController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform target;
    [SerializeField] private InvisibleBehaviour behaviour;
    //[SerializeField] private List<Transform> roomEntrances = new List<Transform>();
    [SerializeField] private List<Room> rooms;

    private IState currentState;

    public NavMeshAgent Agent => agent;
    public Transform Target => target;
    public InvisibleBehaviour Behaviour => behaviour;
    //public List<Transform> RoomEntrances => roomEntrances;
    public List<Room> Rooms => rooms;
    private void Start()
    {
        ChangeState(new PatrollingState(this));
    }
    private void Update()
    {
        currentState?.Update();
    }
    public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
}

public interface IState
{
    void Enter();
    void Update();
    void Exit();
}

[System.Serializable]
public class Room
{
    public Transform entrance;
    public List<Transform> inspectionPoints;
}