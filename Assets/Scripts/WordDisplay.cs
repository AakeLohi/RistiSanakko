using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WordDisplay : MonoBehaviour
{
    public static WordDisplay Instance { get; private set; }

    public GameObject tileVisualPrefab;
    public GameObject explanationPrefab;

    public GameObject emptyTile;

    public GameObject previewTile;
    
    public GameObject revealedPreviewTile;
    public float gridTileSize = 1f;
    public float secondsPerLetter = 0.06f;
    [HideInInspector] public Vector3 lastTilePos = Vector3.zero;
    private List<GameObject> tiles = new List<GameObject>();
    private List<GameObject> previewTiles = new List<GameObject>();
    private List<GameObject> explanations = new List<GameObject>();

    private bool isFirst = true;

    private void Awake() { Instance = this; }

    [System.Serializable]
    public class PlacedWord
    {
        public int startX, startY;
        public Vector2Int dir;
        public string text, explanation;
        public PlacedWord(string t, string e, int sx, int sy, int dx, int dy)
        { text = t; explanation = e; startX = sx; startY = sy; dir.x = dx; dir.y = dy; }
    }

    public void AddWord(PlacedWord word) { StartCoroutine(PlaceWord(word)); }

    public void ShowExplanationAt(PlacedWord nextPlanned, string text)
    {
        if (!explanationPrefab) return;
        var expl = Instantiate(explanationPrefab, transform);
        var tm = expl.GetComponentInChildren<TextMeshPro>();
        if (tm != null) tm.text = text;
        expl.transform.localPosition = GridToWorld(new Vector2Int(nextPlanned.startX, nextPlanned.startY)) - new Vector3(nextPlanned.dir.x, nextPlanned.dir.y, 0f) * gridTileSize;
        explanations.Add(expl);
        ShowPreviewText(" ");
    }

    private string lastPreviewText = "";

    private List<int> revealedIndexes = new List<int>();

    public void ShowPreviewText(string text)
    {
        PlacedWord currentPlanned = WordManager.Instance.currentPlanned;
        Vector2Int pos = new Vector2Int(currentPlanned.startX, currentPlanned.startY);

        if (previewTiles == null) previewTiles = new List<GameObject>();

        var candidates = new List<GameObject>(previewTiles);
        var newPreviewList = new List<GameObject>();

        int length = currentPlanned.text.Length;

        // build a StringBuilder for the new preview text
        var sb = new System.Text.StringBuilder(length);
        for (int i = 0; i < length; i++) sb.Append('\0');

        for (int i = 0; i < length; i++)
        {
            Vector3 worldPos = GridToWorld(pos + currentPlanned.dir * i);
            GameObject match = null;

            for (int j = 0; j < candidates.Count; j++)
            {
                var t = candidates[j];
                if (t == null) continue;
                if (t.transform.position != worldPos) continue;

                match = t;
                candidates.RemoveAt(j);
                break;
            }

            char charToShow = (i < text.Length) ? text[i] : '\0';

            if (match != null)
            {
                var tv = match.GetComponent<TileVisual>();
                char prevChar = (i < lastPreviewText.Length) ? lastPreviewText[i] : '\0';

                if (tv != null && charToShow != prevChar)
                {
                    tv.InitializeVisual(charToShow);
                }

                // ensure position and active state are correct
                if (match.transform.position != worldPos) match.transform.position = worldPos;
                if (!match.activeSelf) match.SetActive(true);

                newPreviewList.Add(match);
            }
            else
            {
                GameObject newPreviewTile = Instantiate(previewTile, worldPos, Quaternion.identity);
                var tv = newPreviewTile.GetComponent<TileVisual>();
                if (tv != null) tv.InitializeVisual(charToShow);
                newPreviewList.Add(newPreviewTile);
            }

            // record char for this index into sb
            sb[i] = charToShow;
        }

        // destroy leftover candidates that were not reused
        foreach (var leftover in candidates)
        {
            if (leftover != null) Destroy(leftover);
        }

        previewTiles = newPreviewList;

        // store the new preview text for next comparison
        lastPreviewText = sb.ToString();
    }

    public void RevealLetters(PlacedWord word, bool isFirst)
    {
        if (word == null) return;
        int length = word.text.Length;
        if (length == 0) return;

        if (revealedIndexes == null)
            revealedIndexes = new List<int>();

        if (revealedIndexes.Count >= length)
            return;

        int start = isFirst ? 1 : 0;
        var options = new List<int>();
        for (int i = start; i < length; i++)
            if (!revealedIndexes.Contains(i)) options.Add(i);
        if (options.Count == 0) return;

        int idx = options[UnityEngine.Random.Range(0, options.Count)];
        revealedIndexes.Add(idx);

        Vector2Int pos = new Vector2Int(word.startX, word.startY) + word.dir * idx;

        GameObject tile = Instantiate(revealedPreviewTile, transform);
        tile.transform.localPosition = GridToWorld(pos);
        var tv = tile.GetComponent<TileVisual>();
        if (tv != null) tv.InitializeVisual(word.text[idx]);
        else
        {
            var tm = tile.GetComponentInChildren<TextMeshPro>();
            if (tm != null) tm.text = word.text[idx].ToString();
        }
        tiles.Add(tile);
    }

    Vector3 GridToWorld(Vector2Int pos) => new Vector3(pos.x * gridTileSize, pos.y * gridTileSize, 0);

    IEnumerator PlaceWord(PlacedWord w)
    {
        lastTilePos = GridToWorld(new Vector2Int(w.startX, w.startY));

        for (int i = isFirst? 0 : 1; i < w.text.Length; i++)
        {
            Vector2Int pos = new Vector2Int(w.startX, w.startY) + w.dir * i;
            Vector3 worldPos = GridToWorld(pos);

            if (!revealedIndexes.Contains(i))
            {
                GameObject tile = Instantiate(tileVisualPrefab, transform);
                tile.transform.localPosition = worldPos;
                var tv = tile.GetComponent<TileVisual>();
                if (tv != null) tv.InitializeVisual(w.text[i]);
                else
                {
                    var tm = tile.GetComponentInChildren<TextMeshPro>();
                    if (tm != null) tm.text = w.text[i].ToString();
                }
                tiles.Add(tile);
            }

            lastTilePos = GridToWorld(pos);
            yield return new WaitForSeconds(secondsPerLetter);
        }

        isFirst = false;
        revealedIndexes.Clear();
        DeleteFarAwayTiles();
    }


    public void DeleteFarAwayTiles(float maxDistance = 50f, Vector3 referencePos = default)
    {
        if (referencePos == default) referencePos = Camera.main.transform.position;

        tiles.RemoveAll(t =>
        {
            if (t == null) return true;
            if (Vector3.Distance(t.transform.position, referencePos) > maxDistance)
            {
                Destroy(t);
                return true;
            }
            return false;
        });

        explanations.RemoveAll(e =>
        {
            if (e == null) return true;
            if (Vector3.Distance(e.transform.position, referencePos) > maxDistance)
            {
                Destroy(e);
                return true;
            }
            return false;
        });
    }

    public void PlaceCameraTo(PlacedWord word)
    {
        if(Camera.main.GetComponent<CameraMover>() != null)
        {
            Camera.main.GetComponent<CameraMover>().MoveTo(new Vector3(word.startX-word.dir.x, word.startY-word.dir.y, 0f) * gridTileSize + new Vector3(word.dir.x, word.dir.y, 0f)*(word.text.Length/2f));
        }
        
        if(word.dir == new Vector2Int(1, 0))
        {
            Camera.main.GetComponent<CameraMover>().SetOrthoSize(3+(word.text.Length/4f)*gridTileSize);
        }
        else
        {
            Camera.main.GetComponent<CameraMover>().SetOrthoSize(2+(word.text.Length/2.5f)*gridTileSize);
        }
    }
}
