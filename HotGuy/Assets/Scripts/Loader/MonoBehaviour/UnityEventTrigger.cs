using System;
using UnityEngine;

public class UnityEventTrigger : MonoBehaviour
{
    private Action<Collision2D> Action_OnCollisionEnter2D;
    private Action<Collision2D> Action_OnCollisionExit2D;
    private Action<Collision2D> Action_OnCollisionStay2D;
    private Action<ControllerColliderHit> Action_OnControllerColliderHit;
    private Action<Collider2D> Action_OnTriggerEnter2D;
    private Action<Collider2D> Action_OnTriggerExit2D;

    public void Register(Action<Collision2D> action_Enter = null, Action<Collision2D> action_Exit = null, Action<Collision2D> action_Stay = null,
        Action<ControllerColliderHit> action_ControllerColliderHit = null, Action<Collider2D> action_OnTriggerEnter2D = null, Action<Collider2D> action_OnTriggerExit2D = null)
    {
        if (action_Enter != null)
            this.Action_OnCollisionEnter2D = action_Enter;
        if (action_Exit != null)
            this.Action_OnCollisionExit2D = action_Exit;
        if (action_Stay != null)
            this.Action_OnCollisionStay2D = action_Stay;
        if (action_ControllerColliderHit != null)
            this.Action_OnControllerColliderHit = action_ControllerColliderHit;
        if (action_OnTriggerEnter2D != null)
            this.Action_OnTriggerEnter2D = action_OnTriggerEnter2D;
        if (action_OnTriggerExit2D != null)
            this.Action_OnTriggerExit2D = action_OnTriggerExit2D;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (this.Action_OnCollisionEnter2D != null)
            this.Action_OnCollisionEnter2D.Invoke(other);
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (this.Action_OnCollisionExit2D != null)
            this.Action_OnCollisionExit2D.Invoke(other);
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (this.Action_OnCollisionStay2D != null)
            this.Action_OnCollisionStay2D.Invoke(other);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (this.Action_OnControllerColliderHit != null)
            this.Action_OnControllerColliderHit.Invoke(hit);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (this.Action_OnTriggerEnter2D != null)
            this.Action_OnTriggerEnter2D.Invoke(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (this.Action_OnTriggerExit2D != null)
            this.Action_OnTriggerExit2D.Invoke(other);
    }
}