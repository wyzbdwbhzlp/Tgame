using TGame.Data;
using UnityEngine;

namespace TGame.Battle
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class VFXPlayer : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private VFXDataSO _data;

        private int _currentFrame = 0;
        private float _timer = 0f;
        private float _frameDuration;

        public void Play(VFXDataSO data)
        {
            _sr = GetComponent<SpriteRenderer>();
            _data = data;
            _currentFrame = 0;
            _timer = 0f;

            if (_data != null && _data.frames != null && _data.frames.Length > 0)
            {
                _frameDuration = 1f / _data.frameRate;
                _sr.sprite = _data.frames[0];
                transform.localScale = Vector3.one * _data.scale;

                // 【??核心】动态层级：尝试获取父节点（受击角色）的层级，并永远比它高 10 级，确保特效盖在角色身上
                SpriteRenderer parentSr = transform.parent != null ? transform.parent.GetComponentInChildren<SpriteRenderer>() : null;
                _sr.sortingOrder = parentSr != null ? parentSr.sortingOrder + 10 : 3500;
            }
            else
            {
                Debug.LogWarning("[VFXPlayer] 特效数据为空，直接销毁。");
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (_data == null || _data.frames == null || _data.frames.Length == 0) return;

            _timer += Time.deltaTime;

            // 达到一帧的时间，切换下一张图
            if (_timer >= _frameDuration)
            {
                _timer -= _frameDuration;
                _currentFrame++;

                if (_currentFrame >= _data.frames.Length)
                {
                    if (_data.loop)
                    {
                        _currentFrame = 0;
                    }
                    else
                    {
                        // 播放完毕，销毁自身
                        Destroy(gameObject);
                        return;
                    }
                }

                _sr.sprite = _data.frames[_currentFrame];
            }
        }
    }
}