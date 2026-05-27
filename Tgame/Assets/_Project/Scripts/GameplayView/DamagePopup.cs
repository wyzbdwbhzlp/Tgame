using UnityEngine;
using TMPro;

namespace TGame.Battle
{
    public class DamagePopup : MonoBehaviour
    {
        private TextMeshPro _textMesh;
        private float _disappearTimer = 0.8f;
        private float _fadeSpeed = 3f;
        private Color _textColor;
        private Vector3 _moveSpeed;

        public void Setup(int damageAmount, bool isCrit)
        {
            // 【🔥核心修复】使用 GetComponentInChildren，无论组件在自己身上还是子物体上，都能精准抓到！
            _textMesh = GetComponentInChildren<TextMeshPro>();

            // 【🔥防爆措施】如果还是没抓到，直接在控制台精准提示你该怎么做！
            if (_textMesh == null)
            {
                Debug.LogError("<color=red>[DamagePopup] 致命错误：预制体上找不到 3D 版的 TextMeshPro 组件！</color>\n请检查预制体：必须是通过右键 -> 3D Object -> Text - TextMeshPro 创建的，绝不能是 UI 里的 Text！");
                return;
            }

            _textMesh.sortingOrder = 5000;

            float randomX = UnityEngine.Random.Range(-0.5f, 0.5f);
            _moveSpeed = new Vector3(randomX, 2f, 0);

            if (isCrit)
            {
                _textMesh.text = damageAmount.ToString() + "!";
                _textMesh.fontSize = 7;
                _textMesh.color = new Color(1f, 0.5f, 0f);
            }
            else
            {
                _textMesh.text = damageAmount.ToString();
                _textMesh.fontSize = 5;
                _textMesh.color = new Color(1f, 0.2f, 0.2f);
            }

            _textColor = _textMesh.color;
        }

        private void Update()
        {
            // 如果没抓到组件，就不要执行 Update 了，防止疯狂报错
            if (_textMesh == null) return;

            transform.position += _moveSpeed * Time.deltaTime;

            _disappearTimer -= Time.deltaTime;
            if (_disappearTimer < 0)
            {
                _textColor.a -= _fadeSpeed * Time.deltaTime;
                _textMesh.color = _textColor;

                if (_textColor.a < 0)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}