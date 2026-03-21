using TMPro;
using UnityEngine;
using System.Collections;

public class TileVisual : MonoBehaviour
{
    public char character;
    public TextMeshPro textMesh;

    private Vector3 targetScale = Vector3.one;

    public void SetLetter(char letter)
    {
        textMesh.text = letter.ToString().ToUpper();
    }

    public void InitializeVisual(char letter)
    {
        character = letter;
        textMesh.text = letter.ToString().ToUpper();
        StartCoroutine(PopInAnimation(0.15f));
    }

    private IEnumerator PopInAnimation(float duration)
    {
        float timer = 0f;
        transform.localScale = Vector3.zero;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            t = 1f - Mathf.Pow(1f - t, 3);
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
    }
}
