using UnityEngine;
using System.Collections;
using TGame.Battle; // 引入命名空间以访问总台

[RequireComponent(typeof(Animator))]
public class UnitAnimator : MonoBehaviour
{
    private Animator _animator;

    private readonly int _hashIdle = Animator.StringToHash("Idle");
    private readonly int _hashMove = Animator.StringToHash("Move");
    private readonly int _hashAttack = Animator.StringToHash("Attack");
    private readonly int _hashSkill = Animator.StringToHash("Skill");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void SetFacingDirection(bool faceRight)
    {
        Vector3 scale = transform.localScale;
        scale.x = faceRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    public void PlayIdle()
    {
        StopAllCoroutines();
        _animator.Play(_hashIdle);
        transform.localPosition = Vector3.zero;
    }

    public void PlayMove()
    {
        StopAllCoroutines();
        _animator.Play(_hashMove);
    }

    // ==========================================
    // 攻击与技能：从全局总台获取停顿时间
    // ==========================================
    public void PlayAttack()
    {
        StopAllCoroutines();
        _animator.Play(_hashAttack, -1, 0f);
        StartCoroutine(WaitAndReturnToIdle(GetHoldDuration()));
    }

    public void PlaySkill()
    {
        StopAllCoroutines();
        _animator.Play(_hashSkill, -1, 0f);
        StartCoroutine(WaitAndReturnToIdle(GetHoldDuration()));
    }

    // 【🔥新增】智能获取全局参数的方法
    private float GetHoldDuration()
    {
        // 如果场景里有总台，就听总台的；否则给一个默认的 0.3f 保底
        if (BattleVisualConfig.Instance != null)
        {
            return BattleVisualConfig.Instance.globalActionHoldDuration;
        }
        return 0.3f;
    }

    private IEnumerator WaitAndReturnToIdle(float holdTime)
    {
        yield return null;

        float animDuration = _animator.GetCurrentAnimatorStateInfo(0).length;

        yield return new WaitForSeconds(animDuration + holdTime);

        PlayIdle();
    }
}