using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UISpriteAnimation : MonoBehaviour
{
    [SerializeField] private Image m_Image;
    [SerializeField] private Sprite[] m_SpriteArray;
    [SerializeField] private float m_Speed = 0.02f;

    private int m_IndexSprite;
    private Coroutine m_AnimationCoroutine;

    private void OnEnable()
    {
        m_IndexSprite = 0;

        if (m_AnimationCoroutine != null)
        {
            StopCoroutine(m_AnimationCoroutine);
        }

        m_AnimationCoroutine = StartCoroutine(PlayAnimUI());
    }

    private void OnDisable()
    {
        if (m_AnimationCoroutine != null)
        {
            StopCoroutine(m_AnimationCoroutine);
            m_AnimationCoroutine = null;
        }
    }

    private IEnumerator PlayAnimUI()
    {
        while (true)
        {
            if (m_SpriteArray == null || m_SpriteArray.Length == 0)
                yield break;

            m_Image.sprite = m_SpriteArray[m_IndexSprite];

            m_IndexSprite++;
            if (m_IndexSprite >= m_SpriteArray.Length)
            {
                m_IndexSprite = 0;
            }

            yield return new WaitForSeconds(m_Speed);
        }
    }
}