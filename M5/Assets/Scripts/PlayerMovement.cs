using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
    public float forwardSpeed = 8f;
    float sideSpeed = 6f;

    Rigidbody rb;

    Vector2 moveInput;

    float currentSideInput;
    float sideVelocity;

    float originalForwardSpeed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalForwardSpeed = forwardSpeed;
    }

    public void ActivateSpeedBoost(float boostSpeed, float duration)
    {
        StartCoroutine(SpeedBoostRoutine(boostSpeed, duration));
    }

    private IEnumerator SpeedBoostRoutine(float boostSpeed, float duration)
    {
        forwardSpeed = boostSpeed;
        yield return new WaitForSeconds(duration);
        forwardSpeed = originalForwardSpeed;
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnRestart()
    {
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm != null && gm.IsGameOver())
        {
            gm.RestartGame();
        }
    }

    void OnMenu()
    {
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm != null && gm.IsGameOver())
        {
            gm.ReturnToMainMenu();
        }
    }

    void FixedUpdate()
    {
        currentSideInput = Mathf.SmoothDamp(currentSideInput, moveInput.x, ref sideVelocity, 0.1f);

        Vector3 movement = new(
            currentSideInput * sideSpeed,
            rb.linearVelocity.y,
            forwardSpeed
            );

        rb.linearVelocity = movement;
    }

    public void ApplySlow(float speedFactor, float duration)
    {
        StartCoroutine(SlowRoutine(speedFactor, duration));
    }

    private IEnumerator SlowRoutine(float speedFactor, float duration)
    {
        forwardSpeed *= speedFactor;
        yield return new WaitForSeconds(duration);
        forwardSpeed /= speedFactor;
    }

    public void ApplyKnockback(float force)
    {
        StartCoroutine(KnockbackRoutine(force));
    }

    private IEnumerator KnockbackRoutine(float force)
    {
        Vector3 originalVelocity = rb.linearVelocity;
        int direction = Random.Range(0, 2) * 2 - 1; // -1 or 1
        rb.AddForce(direction * force * Vector3.right, ForceMode.Impulse);
        yield return _waitForSeconds0_5;
        rb.linearVelocity = originalVelocity;
    }

    public void PenalizeScore(float penaltyAmount)
    {
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            gm.PenalizeScore(penaltyAmount);
        }
    }
}
