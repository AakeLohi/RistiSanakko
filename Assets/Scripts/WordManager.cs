using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class WordManager : MonoBehaviour
{
    public static WordManager Instance { get; private set; }

    [System.Serializable]
    public class Term
    {
        public string word;
        public string explanation;
        public int difficultyLevel;
        public Term(string w, string e, int dif) { word = w; explanation = e; difficultyLevel = dif; }
    }

    private List<Term> terms = new List<Term>();
    private List<Term> usedTerms = new List<Term>();
    private Term currentTerm;
    private Term nextTerm;
    public WordDisplay.PlacedWord currentPlanned;
    private WordDisplay.PlacedWord nextPlanned;

    public Vector2Int startOrigin = Vector2Int.zero;
    public Vector2Int dir;

    public UnityEvent<int> onScoreChange;
    public UnityEvent<int> onWordGuessed;

    public int wordsGuessed;
    public int currentScore;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LoadTerms();
        if (terms.Count > 0) InitializeChain();
    }

    void LoadTerms()
    {
        TextAsset txt = Resources.Load<TextAsset>("SanatSelitykset");
        if (txt == null) { Debug.LogError("SanatSelitykset.txt missing!"); return; }
        foreach (var line in txt.text.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var word_and_dif = line.Trim().Split('<');
            var parts = word_and_dif[0].Trim().Split(';');

            if (parts.Length >= 2)
            {
                int dif = 0;
                if (word_and_dif.Length >= 2) int.TryParse(word_and_dif[1].Trim(), out dif);
                terms.Add(new Term(parts[0].Trim(), parts[1].Trim(), dif));
            }
        }
    }

    void InitializeChain()
    {
        var valid = terms.Where(t => !string.IsNullOrEmpty(t.word)).ToList();
        currentTerm = WeightedChoice(valid);
        usedTerms.Add(currentTerm);
        currentPlanned = new WordDisplay.PlacedWord(currentTerm.word, currentTerm.explanation, startOrigin.x, startOrigin.y, 1, 0);
        PlanNextTerm();
        WordDisplay.Instance?.ShowExplanationAt(currentPlanned, currentPlanned.explanation);
        WordDisplay.Instance?.ShowExplanationAt(nextPlanned, nextTerm.explanation);
        WordDisplay.Instance.PlaceCameraTo(currentPlanned);
    }

    void PlanNextTerm()
    {
        int maxIndex = Mathf.Max(0, currentTerm.word.Length - 1);
        var unused = terms.Where(t => !usedTerms.Contains(t) && !string.IsNullOrEmpty(t.word)).ToList();

        if (maxIndex == 0)
        {
            nextTerm = (unused.Count > 0) ? WeightedChoice(unused) : WeightedChoice(terms.Where(t => !string.IsNullOrEmpty(t.word)).ToList());
            if (!usedTerms.Contains(nextTerm)) usedTerms.Add(nextTerm);

            Vector2Int dir0 = currentPlanned.dir.x != 0 ? Vector2Int.down : Vector2Int.right;
            nextPlanned = new WordDisplay.PlacedWord(nextTerm.word, nextTerm.explanation,
                currentPlanned.startX, currentPlanned.startY, dir0.x, dir0.y);
            return;
        }

        var indexTerms = new List<List<Term>>(maxIndex + 1);
        for (int i = 0; i <= maxIndex; i++) indexTerms.Add(new List<Term>());

        bool any = false;
        for (int i = 1; i <= maxIndex; i++)
        {
            char want = char.ToLowerInvariant(currentTerm.word[i]);
            for (int j = 0; j < terms.Count; j++)
            {
                var term = terms[j];
                if (string.IsNullOrEmpty(term.word)) continue;
                if (!usedTerms.Contains(term) && char.ToLowerInvariant(term.word[0]) == want) indexTerms[i].Add(term);
            }
            if (indexTerms[i].Count > 0) any = true;
        }

        if (!any)
        {
            any = false;
            for (int i = 1; i <= maxIndex; i++)
            {
                indexTerms[i].Clear();
                char want = char.ToLowerInvariant(currentTerm.word[i]);
                for (int j = 0; j < terms.Count; j++)
                {
                    var term = terms[j];
                    if (string.IsNullOrEmpty(term.word)) continue;
                    if (char.ToLowerInvariant(term.word[0]) == want) indexTerms[i].Add(term);
                }
                if (indexTerms[i].Count > 0) any = true;
            }
        }

        if (maxIndex > 1 && indexTerms[2].Count > 0) indexTerms[1].Clear();

        int bestI = 0;
        int bestV = indexTerms[0].Count;
        for (int i = 1; i <= maxIndex; i++)
        {
            int cnt = indexTerms[i].Count;
            if (cnt > bestV || (cnt == bestV && i > bestI))
            {
                bestV = cnt;
                bestI = i;
            }
        }

        int chosenIndex = bestI;
        if (chosenIndex < 0 || indexTerms[chosenIndex].Count == 0)
        {
            chosenIndex = -1;
            for (int i = maxIndex; i >= 1; i--) if (indexTerms[i].Count > 0) { chosenIndex = i; break; }
        }

        if (chosenIndex == -1)
        {
            var pool = terms.Where(t => !string.IsNullOrEmpty(t.word) && !usedTerms.Contains(t)).ToList();
            if (pool.Count == 0) pool = terms.Where(t => !string.IsNullOrEmpty(t.word)).ToList();
            nextTerm = WeightedChoice(pool);
            if (!usedTerms.Contains(nextTerm)) usedTerms.Add(nextTerm);
            Vector2Int dirX = currentPlanned.dir.x != 0 ? Vector2Int.down : Vector2Int.right;
            nextPlanned = new WordDisplay.PlacedWord(nextTerm.word, nextTerm.explanation,
                currentPlanned.startX + currentPlanned.dir.x * 1,
                currentPlanned.startY + currentPlanned.dir.y * 1,
                dirX.x, dirX.y);
            return;
        }

        var bucket = indexTerms[chosenIndex];

        var useCount = new Dictionary<Term, int>();
        for (int i = 0; i < usedTerms.Count; i++)
        {
            var u = usedTerms[i];
            if (u == null) continue;
            useCount[u] = useCount.TryGetValue(u, out var cc) ? cc + 1 : 1;
        }

        int minUse = int.MaxValue;
        for (int i = 0; i < bucket.Count; i++)
        {
            var t = bucket[i];
            int c = useCount.TryGetValue(t, out var v) ? v : 0;
            if (c < minUse) minUse = c;
        }

        var leastUsed = new List<Term>();
        for (int i = 0; i < bucket.Count; i++)
        {
            var t = bucket[i];
            int c = useCount.TryGetValue(t, out var v) ? v : 0;
            if (c == minUse) leastUsed.Add(t);
        }

        nextTerm = (leastUsed.Count > 0) ? WeightedChoice(leastUsed) : WeightedChoice(bucket);
        if (!usedTerms.Contains(nextTerm)) usedTerms.Add(nextTerm);

        Vector2Int dir = currentPlanned.dir.x != 0 ? Vector2Int.down : Vector2Int.right;
        nextPlanned = new WordDisplay.PlacedWord(nextTerm.word, nextTerm.explanation,
            currentPlanned.startX + currentPlanned.dir.x * chosenIndex,
            currentPlanned.startY + currentPlanned.dir.y * chosenIndex,
            dir.x, dir.y);

        currentScore = currentPlanned.text.Length;
        Debug.Log("Next planned words difficulty: " + nextTerm.difficultyLevel);
    }

    Term WeightedChoice(List<Term> list)
    {
        if (list == null || list.Count == 0) return null;

        // Sort list by difficulty ascending
        list.Sort((a, b) => a.difficultyLevel.CompareTo(b.difficultyLevel));

        float randomValue = UnityEngine.Random.value;
        float biasExponent = Mathf.Lerp(5f, 0.01f, wordsGuessed/300);
        int index = Mathf.Clamp(Mathf.RoundToInt(Mathf.Pow(randomValue, biasExponent) * (list.Count - 1)), 0, list.Count - 1);

        return list[index];
    }

    public void SubmitWord(string guess)
    {
        if (currentTerm == null || currentPlanned == null) return;

        string target = currentTerm.word.Trim();
        string trimmed = guess.Trim();

        if (string.Equals(trimmed, target, System.StringComparison.OrdinalIgnoreCase))
        {
            WordDisplay.Instance?.AddWord(currentPlanned);

            wordsGuessed++;
            GameManager.Instance.SetWordCount(wordsGuessed);

            int increase = 1 + (wordsGuessed / 5);
            currentTerm.difficultyLevel = Mathf.Max(currentTerm.difficultyLevel, 0) + increase;

            GameManager.Instance.AddScore(currentScore, currentPlanned.text.Length);
            AdvanceChain();
        }
        else
        {
            GameManager.Instance.HintUsed(currentScore, currentPlanned.text.Length);
            WordDisplay.Instance.RevealLetters(currentPlanned, wordsGuessed != 0);
            if (currentScore > 0) currentScore -= 1;
        }
    }

    void AdvanceChain()
    {
        currentTerm = nextTerm;
        currentPlanned = nextPlanned;
        PlanNextTerm();
        WordDisplay.Instance?.ShowExplanationAt(nextPlanned, nextTerm.explanation);
        WordDisplay.Instance?.PlaceCameraTo(currentPlanned);
    }
}
