
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;
using TMPro;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public class MenuTyping : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap highlightTilemap;
    public Tilemap wordTilemap;
    public Tilemap selectedTilemap;
    
    public Tilemap bonusTilemap;

    [Header("Tiles")]
    public Tile highlightTile;

    public Tile hoverTile;
    public Tile transparentTile;

    public Tile midhighlightTile;

    public Tile letterTile;

    public Tile lockedLetterTile;
    

    public Color spawnLockedTileColor;
    public Color lockedTileColor;
    
	public Tile multiwordScoreTile;
	public Tile multiletterScoreTile;

    [Header("Other References")]
    [SerializeField] public Camera cam;

    [SerializeField] public Transform selectedTileTransform;


    [SerializeField] public StringToTile stringToTileScript;
    
    private Vector3Int previousMousePos;
    private Vector3Int selectedTile;
    private bool tileIsSelected = false;

    private bool horizontalInput = true;

    public List<Vector3Int> temporaryLetterTiles = new List<Vector3Int>();
    private List<string> currentTempChars = new List<string>();

    private List<GameObject> temporaryTextTiles = new List<GameObject>();

    private List<Vector3Int> lockedPosList = new List<Vector3Int>(); 
    private Dictionary<Vector3Int, string> lockedStrDictionary = new Dictionary<Vector3Int, string>();

    [SerializeField] public Dictionary<Vector3Int, int> multiwordScorePos = new Dictionary<Vector3Int, int>();
    [SerializeField] public Dictionary<Vector3Int, int> multiletterScorePos = new Dictionary<Vector3Int, int>();
    
	[SerializeField] public Dictionary<char, int> letterValues = new Dictionary<char, int>();

    [SerializeField] public List<string> letterBank = new List<string>();
    private List<GameObject> letterBankGameobjects = new List<GameObject>();

    public GameObject letterBankPrefab;
    public GameObject letterBankPanel;

    private Vector3Int highlightOrigin;
    private int previewLength = 18; // length of column + row highlighted tiles

    [SerializeField] private int currentLetterIndex; //for tracking where we are in the word
    
    [Header("Score UI")] 
    public int score;
    public TextMeshProUGUI ScoreUI;
    public GameObject newWordText;
    public GameObject newWordLayoutGroup;

    [Header("Settings")] 
    [SerializeField] public int boardSize;

    [SerializeField] public bool useLetterBank;
    [SerializeField] public int letterBankSize = 7;
    [SerializeField] public int randomBackgroundModifer;

    [SerializeField] public bool onlyBuildOffWords;
    [SerializeField] public bool randomBackgroundLetters;

    private Dictionary<Vector3Int, string> spawnLockedDictionary = new Dictionary<Vector3Int, string>();
    
    [SerializeField] public bool createStartingWord;
    [SerializeField] public string startingWord;

    public bool refreshEveryTime;
    public bool refreshAfterThreshold;
    public int refreshThreshold;

    public HashSet<string> createdWords = new HashSet<string>();

    private bool firstWord = true;


    [Header("Menu Settings")] 
    public GameObject settingsPanel;

    public FadeTo fadeScript;

    public CinemachineImpulseSource impulseSource;

    private bool settingsWasOpen = false;
    
    void Start()
    {
		
        InitializeLetterValues();
        
        if (useLetterBank)
        {
            AddLetters(letterBankSize);
        }
        else
        {
            letterBankPanel.SetActive(false);
        }

        if (randomBackgroundLetters)
        {
            InitializeRandomLetters();
        }
        
		SwitchColumnRow();
        FakeClick();
      
    }

    void AddLetters(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            letterBank.Add(DrawRandomLetter().ToString().ToUpper());
        }

        InitializeLetterBank();
        
    }

    void InitializeRandomLetters()
    {
        for (int x = -boardSize + 1; x < boardSize; x++)
        {
            for (int y = -boardSize + 1; y < boardSize; y++)
            {
                int r = Random.Range(0, randomBackgroundModifer);
                
                    if (r == 1)
                    {
                        if (!lockedStrDictionary.ContainsKey(new Vector3Int(x, y, 0)) && !CheckForNeighborsIndividual(new Vector3Int(x, y, 0)))
                        {
                            string s1 = DrawRandomLetter().ToString();
                            
                            Instantiate(stringToTileScript.StringTile(s1),
                                Vector3Int.RoundToInt(wordTilemap.CellToWorld(new Vector3Int(x, y, 0))),
                                Quaternion.identity);
                            
                            wordTilemap.SetTile(new Vector3Int(x, y, 0), lockedLetterTile);
                            
                            wordTilemap.SetTileFlags(new Vector3Int(x, y, 0), TileFlags.None);
                            wordTilemap.SetColor(new Vector3Int(x, y, 0), spawnLockedTileColor);
                            
                            lockedStrDictionary.Add(new Vector3Int(x, y, 0), s1);
                            spawnLockedDictionary.Add(new Vector3Int(x, y, 0), s1);
                            
                        }
                    }
                
            }
        }
    }
    
    bool CheckForNeighborsIndividual(Vector3Int pos)
    {
        Vector3Int above = new Vector3Int(pos.x, pos.y + 1, pos.z);
        Vector3Int below = new Vector3Int(pos.x, pos.y - 1, pos.z);
        Vector3Int left = new Vector3Int(pos.x - 1, pos.y, pos.z);
        Vector3Int right = new Vector3Int(pos.x + 1, pos.y, pos.z);

        if (lockedStrDictionary.ContainsKey(above) || lockedStrDictionary.ContainsKey(below) ||
            lockedStrDictionary.ContainsKey(left) || lockedStrDictionary.ContainsKey(right))
        {
            return true;
        }
        else return false;
    }

    void InitializeLetterBank()
    {
        for (int i = 0; i < letterBankGameobjects.Count; i++)
        {
            Destroy(letterBankGameobjects[i]);
        }

        letterBankGameobjects.Clear();
        
        for (int i = 0; i < letterBank.Count; i++)
        {
            GameObject g5 = Instantiate(letterBankPrefab, letterBankPanel.transform);
            letterBankGameobjects.Add(g5);

            TextMeshProUGUI tmp = g5.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = "" + (letterBank[i])[0];

            TextMeshProUGUI tmp2 = tmp.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            tmp2.text = "" + (letterValues[(letterBank[i])[0]]);
        }
    }
    
	void InitializeLetterValues()
	{
		letterValues.Add('a', 1);
		letterValues.Add('b', 3);
		letterValues.Add('c', 3);
		letterValues.Add('d', 2);
		letterValues.Add('e', 1);
		letterValues.Add('f', 4);
		letterValues.Add('g', 2);
		letterValues.Add('h', 4);
		letterValues.Add('i', 1);
		letterValues.Add('j', 8);
		letterValues.Add('k', 5);
		letterValues.Add('l', 1);
		letterValues.Add('m', 3);
		letterValues.Add('n', 1);
		letterValues.Add('o', 1);
		letterValues.Add('p', 3);
		letterValues.Add('q', 10);
		letterValues.Add('r', 1);
		letterValues.Add('s', 1);
		letterValues.Add('t', 1);
		letterValues.Add('u', 1);
		letterValues.Add('v', 4);
		letterValues.Add('w', 4);
		letterValues.Add('x', 8);
		letterValues.Add('y', 4);
		letterValues.Add('z', 10);
        
        letterValues.Add('A', 1);
        letterValues.Add('B', 3);
        letterValues.Add('C', 3);
        letterValues.Add('D', 2);
        letterValues.Add('E', 1);
        letterValues.Add('F', 4);
        letterValues.Add('G', 2);
        letterValues.Add('H', 4);
        letterValues.Add('I', 1);
        letterValues.Add('J', 8);
        letterValues.Add('K', 5);
        letterValues.Add('L', 1);
        letterValues.Add('M', 3);
        letterValues.Add('N', 1);
        letterValues.Add('O', 1);
        letterValues.Add('P', 3);
        letterValues.Add('Q', 10);
        letterValues.Add('R', 1);
        letterValues.Add('S', 1);
        letterValues.Add('T', 1);
        letterValues.Add('U', 1);
        letterValues.Add('V', 4);
        letterValues.Add('W', 4);
        letterValues.Add('X', 8);
        letterValues.Add('Y', 4);
        letterValues.Add('Z', 10);
		
	}

    
    char DrawRandomLetter()
    {
        float totalWeight = 0f;

        foreach (var kvp in letterValues)
        {
            totalWeight += 1f / kvp.Value;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float runningTotal = 0f;

        foreach (var kvp in letterValues)
        {
            runningTotal += 1f / kvp.Value;

            if (randomValue <= runningTotal)
            {
                return kvp.Key;
            }
        }
        
        return 'A'; //just in case
    }
    
    void InstantiateBonusScores()
    {
        foreach (KeyValuePair<Vector3Int, int> pair in multiwordScorePos)
        {
            if (!spawnLockedDictionary.ContainsKey(pair.Key))
            {
                bonusTilemap.SetTile(pair.Key, multiwordScoreTile); 
            }
        }    
        
        foreach (KeyValuePair<Vector3Int, int> pair in multiletterScorePos)
        {
            if (!spawnLockedDictionary.ContainsKey(pair.Key))
            {
                bonusTilemap.SetTile(pair.Key, multiletterScoreTile); 
            }
        }    
    }
    
    void InitializeBonusScores()
    {
        for (int x = -boardSize + 1; x < boardSize; x++)
        {
            for (int y = -boardSize + 1; y < boardSize; y++)
            {
                if (x % 10 == 0 && y % 10 == 0)
                {
                    multiwordScorePos.Add(new Vector3Int(x, y, 0), 2);
                }
                
                if ((x + 5) % 10 == 0 && (y + 5) % 10 == 0)
                {
                    multiletterScorePos.Add(new Vector3Int(x, y, 0), 2);
                }
            }
        }
        
        InstantiateBonusScores();
    }
    

    void RemoveColumnRow()
    {
        if (horizontalInput)
        {
            for (int i = -previewLength; i < previewLength; i++)
            {
                highlightTilemap.SetTile(new Vector3Int(highlightOrigin.x + i, highlightOrigin.y, highlightOrigin.z), transparentTile);
            }
        }
        else
        {
            for (int i = -previewLength; i < previewLength; i++)
            {
                highlightTilemap.SetTile(new Vector3Int(highlightOrigin.x, highlightOrigin.y + i, highlightOrigin.z), transparentTile);
            }
        }
    
    }

    void CreateColumnRow()
    {
        if (horizontalInput)
        {
            for (int i = -previewLength; i < previewLength; i++)
            {
                highlightTilemap.SetTile(new Vector3Int(selectedTile.x + i, selectedTile.y, selectedTile.z),
                    midhighlightTile);
            }

            highlightOrigin = selectedTile;
        }
        else
        {
            for (int i = -previewLength; i < previewLength; i++)
            {
                highlightTilemap.SetTile(new Vector3Int(selectedTile.x, selectedTile.y + i, selectedTile.z),
                    midhighlightTile);
            }

            highlightOrigin = selectedTile;
        }
    }

    void ClearTempLetters()
    {
        
        if (useLetterBank)
        {
            for (int i = 0; i < currentTempChars.Count; i++)
            {
                if (!lockedStrDictionary.ContainsKey(temporaryLetterTiles[i]))
                {
                    letterBank.Add(currentTempChars[i].ToUpper());
                }
            }
            
            InitializeLetterBank();
        }
        
        
        //clear temp letters from tilemap AND empty list
        currentTempChars.Clear();
        
        for (int i = 0; i < temporaryLetterTiles.Count; i++)
        {
            if (wordTilemap.GetTile(temporaryLetterTiles[i]) != lockedLetterTile)
            {
                wordTilemap.SetTile(temporaryLetterTiles[i], transparentTile);
            }
        }

        for (int i = 0; i < temporaryTextTiles.Count; i++)
        {
            Destroy(temporaryTextTiles[i]);
        }
        
        currentTempChars.Clear();
        temporaryTextTiles.Clear();

        temporaryLetterTiles.Clear();
        
        currentLetterIndex = 0;

    }


    string CheckHeadTail(string baseString)
    {
        Debug.Log(baseString);
        if (horizontalInput) //VERTICAL (CHECKING TOP & BOTTOM)
        {
            Vector3Int headTile = temporaryLetterTiles[0];
            Vector3Int tailTile = temporaryLetterTiles[temporaryLetterTiles.Count - 1];

            string temp1 = (ReverseString(CheckForNeighbors(headTile, 1, 0))
                            + baseString
                            + CheckForNeighbors(tailTile, -1, 0));
            return temp1;
        }
        else //HORIZONTAL (CHECKING LEFT AND RIGHT)
        {
            Vector3Int headTile = temporaryLetterTiles[0];
            Vector3Int tailTile = temporaryLetterTiles[temporaryLetterTiles.Count - 1];

            string temp1 = (ReverseString(CheckForNeighbors(headTile, 0, -1))
                            + baseString
                            + CheckForNeighbors(tailTile, 0, 1));
            return temp1;
        }
        
    }
    
    int CheckHeadTailAmount(string baseString)
    {
        Debug.Log(baseString);
        if (horizontalInput) //VERTICAL (CHECKING TOP & BOTTOM)
        {
            Vector3Int headTile = temporaryLetterTiles[0];
            Vector3Int tailTile = temporaryLetterTiles[temporaryLetterTiles.Count - 1];

            string temp1 = (ReverseString(CheckForNeighbors(headTile, 1, 0))
                            + baseString
                            + CheckForNeighbors(tailTile, -1, 0));
            return temp1.Length - baseString.Length;
        }
        else //HORIZONTAL (CHECKING LEFT AND RIGHT)
        {
            Vector3Int headTile = temporaryLetterTiles[0];
            Vector3Int tailTile = temporaryLetterTiles[temporaryLetterTiles.Count - 1];

            string temp1 = (ReverseString(CheckForNeighbors(headTile, 0, -1))
                            + baseString
                            + CheckForNeighbors(tailTile, 0, 1));
            return temp1.Length - baseString.Length;
        }
        
    }
    
    List<Vector3Int> CheckHeadTailPos()
    {
        if (horizontalInput) //VERTICAL (CHECKING TOP & BOTTOM)
        {
            Vector3Int headTile = temporaryLetterTiles[0];
            Vector3Int tailTile = temporaryLetterTiles[temporaryLetterTiles.Count - 1];

            List<Vector3Int> returnList = new List<Vector3Int>();
            List<Vector3Int> headlist = new List<Vector3Int>();
            List<Vector3Int> taillist = new List<Vector3Int>();

            returnList.AddRange(CheckForNeighborsPos(headTile, 1, 0, headlist));
            returnList.AddRange(CheckForNeighborsPos(tailTile, -1, 0, taillist));

            return returnList;
            //1, 0
            //-1, 0
        }
        else //HORIZONTAL (CHECKING LEFT AND RIGHT)
        {
            Vector3Int headTile = temporaryLetterTiles[0];
            Vector3Int tailTile = temporaryLetterTiles[temporaryLetterTiles.Count - 1];

            List<Vector3Int> returnList = new List<Vector3Int>();
            List<Vector3Int> headlist = new List<Vector3Int>();
            List<Vector3Int> taillist = new List<Vector3Int>();

            returnList.AddRange(CheckForNeighborsPos(headTile, 0, -1, headlist));
            returnList.AddRange(CheckForNeighborsPos(tailTile, 0, 1, taillist));

            return returnList;
        }
        
    }
    
    
    void LockTempLetters()
    {
        
        for (int i = 0; i < temporaryLetterTiles.Count; i++)
        {
            if (!lockedStrDictionary.ContainsKey(temporaryLetterTiles[i]))
            {
                lockedStrDictionary.Add(temporaryLetterTiles[i], currentTempChars[i]);
                spawnLockedDictionary.Remove(temporaryLetterTiles[i]);
                wordTilemap.SetTile(temporaryLetterTiles[i], lockedLetterTile);
                
            }
            
			StartCoroutine(PopTile(temporaryTextTiles[i].GetComponentInChildren<RectTransform>()));
            
        }



		impulseSource.GenerateImpulse();	

        currentLetterIndex = 0;
        
        currentTempChars.Clear();
        temporaryTextTiles.Clear();

        temporaryLetterTiles.Clear();
    }

    bool AtLeastOneOriginalLetter()
    {
        for (int i = 0; i < temporaryLetterTiles.Count; i++)
        {
            if (lockedStrDictionary.ContainsKey(temporaryLetterTiles[i]))
            {
                continue;
            }
            else
            {
                return true;
            }
        }

        return false;
    }
    
	IEnumerator PopTile(RectTransform tileVisual)
	{
    	Vector3 originalScale = tileVisual.localScale;
    	Vector3 popScale = originalScale * 1.15f;
    	float duration = 0.12f;
    	float t = 0;

    	while(t < duration) //grow
    	{
        	t += Time.deltaTime;
       		tileVisual.localScale = Vector3.Lerp(originalScale, popScale, t / duration);
        	yield return null;
    	}

    	t = 0;

    	while(t < duration) //shrink
    	{
        	t += Time.deltaTime;
        	tileVisual.localScale = Vector3.Lerp(popScale, originalScale, t / duration);
        	yield return null;
    	}
	}

	int CheckForWordMultiplier()
	{
		int multi = 1;
        
		for (int i = 0; i < temporaryLetterTiles.Count; i++)
		{
			if (multiwordScorePos.ContainsKey(temporaryLetterTiles[i]))
			{
				multi *= multiwordScorePos[temporaryLetterTiles[i]];
                bonusTilemap.SetTile(temporaryLetterTiles[i], transparentTile);
			}
		}

		return multi;
	}

	List<Vector2> CheckForLetterMultiplier()
	{
		List<Vector2> multi = new List<Vector2>();

		for (int i = 0; i < temporaryLetterTiles.Count; i++)
		{
			if (multiletterScorePos.ContainsKey(temporaryLetterTiles[i]))
			{
				multi.Add(new Vector2(multiletterScorePos[temporaryLetterTiles[i]], i));
                bonusTilemap.SetTile(temporaryLetterTiles[i], transparentTile);
            }
		}

		return multi;
	}
    
    void SwitchColumnRow()
    {
        if (horizontalInput)
        {

            //repaint horizontal inputs (using origin)
            for (int i = -previewLength; i < previewLength; i++)
            {
                highlightTilemap.SetTile(new Vector3Int(highlightOrigin.x + i, highlightOrigin.y, highlightOrigin.z),
                    transparentTile);
            }

            //paint vertical inputs (using selectedtile, store origin by setting it to (currently) selected tile)
            for (int i = -previewLength; i < previewLength; i++)
            {
                highlightTilemap.SetTile(new Vector3Int(selectedTile.x, selectedTile.y + i, selectedTile.z),
                    midhighlightTile);


            }
            
            highlightOrigin = selectedTile;
            horizontalInput = false;
        }
        
        else
        {
            
            //repaint vertical inputs (using origin)
            for (int i = -previewLength; i < previewLength; i++)
            {
                highlightTilemap.SetTile(new Vector3Int(highlightOrigin.x, highlightOrigin.y + i, highlightOrigin.z),
                    transparentTile);
            }

            //paint horizontal inputs (using selectedtile, store origin by setting it to (currently) selected tile)
            for (int i = -previewLength; i < previewLength; i++)
            {
                highlightTilemap.SetTile(new Vector3Int(selectedTile.x + i, selectedTile.y, selectedTile.z),
                    midhighlightTile);
            }
            
            highlightOrigin = selectedTile;
            horizontalInput = true;
        }

    }
    
    
    void HandleWordVerification()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            string acc = "";
            
            for (int i = 0; i < currentTempChars.Count; i++)
            {   
                acc = acc + currentTempChars[i];
            }

            if (acc == "settings" || acc == "SETTINGS")
            {
                OpenSettings();
            }

            if (acc == "play" || acc == "PLAY")
            {
                fadeScript.Fade();
                LockTempLetters();
            }

            if (acc == "exit" || acc == "EXIT")
            {
				Application.Quit();
            }
        }
        
    }

    int CheckNotFloating() //return the number of elements that have a neighbor that is locked in
    {
        int count = 0;
        
        for (int i = 0; i < temporaryLetterTiles.Count; i++)
        {

                if (!horizontalInput) //horizontal
                {
                    Vector3Int aboveTile = new Vector3Int(temporaryLetterTiles[i].x + 1, temporaryLetterTiles[i].y,
                        temporaryLetterTiles[i].z);

                    Vector3Int belowTile = new Vector3Int(temporaryLetterTiles[i].x - 1, temporaryLetterTiles[i].y,
                        temporaryLetterTiles[i].z);

                    if (lockedStrDictionary.ContainsKey(aboveTile) && !spawnLockedDictionary.ContainsKey(aboveTile))
                    {
                        Debug.Log("Above Tile: " + aboveTile);
                        count += 1;
                    }
                    else if (lockedStrDictionary.ContainsKey(belowTile) && !spawnLockedDictionary.ContainsKey(belowTile))
                    {
                        count += 1;
                        Debug.Log("Below Tile: " + belowTile);
                    }
                    
                }
                else
                {
                    Vector3Int leftTile = new Vector3Int(temporaryLetterTiles[i].x, temporaryLetterTiles[i].y - 1,
                        temporaryLetterTiles[i].z);

                    Vector3Int rightTile = new Vector3Int(temporaryLetterTiles[i].x, temporaryLetterTiles[i].y + 1,
                        temporaryLetterTiles[i].z);

                    if (lockedStrDictionary.ContainsKey(leftTile) && !spawnLockedDictionary.ContainsKey(leftTile))
                    {
                        Debug.Log("Left Tile: " + leftTile);
                        count += 1;
                    }
                    else if (lockedStrDictionary.ContainsKey(rightTile) && !spawnLockedDictionary.ContainsKey(rightTile))
                    {
                        Debug.Log("Right Tile: " + rightTile);
                        count += 1;
                    }
                }
            
        }
        
        return count;
    }
    
    void AddScore(string stringToAdd, int wordMultiplier, List<Vector2> letterMultiplier)
    {
        int scoreToAdd = 0;

		for (int i = 0; i < stringToAdd.Length; i++)
		{
			int letterScore = 0;
            
			letterScore = letterValues[stringToAdd[i]] * wordMultiplier;

			for (int j = 0; j < letterMultiplier.Count; j++)
			{
				if (letterMultiplier[j].x == i)
				{
					int letterMulti = (int)letterMultiplier[j].y;

					letterScore *= letterMulti;
				}
			}

			scoreToAdd += letterScore;
		}


        score += scoreToAdd;
        ScoreUI.text = "" + score;

        GameObject g2 = Instantiate(newWordText, newWordLayoutGroup.transform);

        
        Destroy(g2, 4f);
        TextMeshProUGUI tempW = g2.GetComponent<TextMeshProUGUI>();
        tempW.CrossFadeAlpha(0f, 4f, false);
        
        tempW.text = "" + stringToAdd.ToUpper() + " +" + scoreToAdd;
        
    }
    
    private List<Vector3Int> GetCrossWordPositions()
    {
        List<Vector3Int> acc = new List<Vector3Int>();
        
        for (int i = 0; i < currentTempChars.Count; i++)
        {
            if (lockedStrDictionary.ContainsKey(temporaryLetterTiles[i])) continue;

            if (horizontalInput) // VERTICAL WORD
            {
                List<Vector3Int> headlist = new List<Vector3Int>();
                List<Vector3Int> taillist = new List<Vector3Int>();

                List<Vector3Int> returnList = new List<Vector3Int>();

                returnList.AddRange(CheckForNeighborsPos(temporaryLetterTiles[i], 0, -1, headlist));
                returnList.AddRange(CheckForNeighborsPos(temporaryLetterTiles[i], 0, 1, taillist));
                
                if (returnList.Count > 0)
                {
                    acc.AddRange(returnList);
                }
            }
            else //HORIZONTAL WORD
            {
                List<Vector3Int> headlist = new List<Vector3Int>();
                List<Vector3Int> taillist = new List<Vector3Int>();
                
                List<Vector3Int> returnList = new List<Vector3Int>();

                returnList.AddRange(CheckForNeighborsPos(temporaryLetterTiles[i], 1, 0, headlist));
                returnList.AddRange(CheckForNeighborsPos(temporaryLetterTiles[i], -1, 0, taillist));
                
                if (returnList.Count > 0)
                {
                    acc.AddRange(returnList);
                }
            }
        }
        
        return acc;
    }
    
    private List<string> GetCrossWords()
    {
        List<string> acc = new List<string>();
        
        for (int i = 0; i < currentTempChars.Count; i++)
        {
            if (lockedStrDictionary.ContainsKey(temporaryLetterTiles[i])) continue;
            
            string startLetter = currentTempChars[i];

            if (horizontalInput) // VERTICAL WORD
            {

                string temps = ReverseString(CheckForNeighbors(temporaryLetterTiles[i], 0, -1)) + startLetter +
                               CheckForNeighbors(temporaryLetterTiles[i], 0, 1);

                Debug.Log(temps);
                
                if (temps.Length > 1)
                {
                    if (ValidWord.IsValidWord(temps))
                    {
                        acc.Add(temps);
                    }
                }
            }
            else //HORIZONTAL WORD
            {
                string temps = ReverseString(CheckForNeighbors(temporaryLetterTiles[i], 1, 0)) + startLetter +
                               CheckForNeighbors(temporaryLetterTiles[i], -1, 0);

                Debug.Log(temps);
                
                if (temps.Length > 1)
                {
                    if (ValidWord.IsValidWord(temps))
                    {
                        acc.Add(temps);
                    }
                }
            }
        }

        for (int i = 0; i < acc.Count; i++)
        {
            Debug.Log(acc[i]);
        }
        
        return acc;
    }
    
    bool CheckNeighborWords()
    {
        
        for (int i = 0; i < currentTempChars.Count; i++)
        {
            string startLetter = currentTempChars[i];
            
            if (horizontalInput) // VERTICAL WORD
            {

                string temps = ReverseString(CheckForNeighbors(temporaryLetterTiles[i], 0, -1)) + startLetter +
                               CheckForNeighbors(temporaryLetterTiles[i], 0, 1);
                
                if (temps.Length > 1)
                {
                    if (!ValidWord.IsValidWord(temps))
                    {
                        return false;
                    }
                }
            }
            else //HORIZONTAL WORD
            {
                string temps = ReverseString(CheckForNeighbors(temporaryLetterTiles[i], 1, 0)) + startLetter +
                               CheckForNeighbors(temporaryLetterTiles[i], -1, 0);
                
                if (temps.Length > 1)
                {
                    if (!ValidWord.IsValidWord(temps))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
    
    public static string ReverseString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return new string(input.Reverse().ToArray());
    }
    
    private string CheckForNeighbors(Vector3Int startPos, int xDir, int yDir)
    {
        if (lockedStrDictionary.ContainsKey(new Vector3Int(startPos.x + xDir, startPos.y + yDir, startPos.z)))
        {
            return lockedStrDictionary[new Vector3Int(startPos.x + xDir, startPos.y + yDir, startPos.z)] + CheckForNeighbors(new Vector3Int(startPos.x + xDir, startPos.y + yDir, startPos.z), xDir, yDir);
        }
        else return "";

    }
    
    private List<Vector3Int> CheckForNeighborsPos(Vector3Int startPos, int xDir, int yDir, List<Vector3Int> neighborsList)
    {
        if (lockedStrDictionary.ContainsKey(new Vector3Int(startPos.x + xDir, startPos.y + yDir, startPos.z)))
        {
            List<Vector3Int> newNeighborsList = neighborsList;
            newNeighborsList.Add(new Vector3Int(startPos.x + xDir, startPos.y + yDir, startPos.z));
                
             return CheckForNeighborsPos(new Vector3Int(startPos.x + xDir, startPos.y + yDir, startPos.z), xDir, yDir, newNeighborsList);
        }
        else return neighborsList;
    }
    
    void HandleMovementInput()
    {
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (currentTempChars.Count > 0 && currentLetterIndex == currentTempChars.Count)
            {
                selectedTilemap.SetTile(selectedTile, transparentTile);

                currentTempChars.RemoveAt(currentTempChars.Count - 1);
                

                GameObject g0 = temporaryTextTiles[temporaryTextTiles.Count - 1];
                
                temporaryTextTiles.RemoveAt(temporaryTextTiles.Count - 1); // destroy

                Destroy(g0);

                if (wordTilemap.GetTile(temporaryLetterTiles[temporaryLetterTiles.Count - 1]) != lockedLetterTile)
                {
                    wordTilemap.SetTile(temporaryLetterTiles[temporaryLetterTiles.Count - 1], transparentTile);
                }
            
                temporaryLetterTiles.RemoveAt(temporaryLetterTiles.Count - 1); //set
            
                if (horizontalInput)
                {
                    selectedTile.x += 1;
                }
                else
                {
                    selectedTile.y -= 1;
                }

                currentLetterIndex--;
                
                UpdateSelectedTile();
            }
            else
            { 
                
            }

        }
    }
    
    
    // Update is called once per frame
    void Update()
    {
        if (settingsPanel != null && settingsPanel.activeInHierarchy)
        {
            settingsWasOpen = true;
            return;
        }

        if (settingsWasOpen)
        {
            settingsWasOpen = false;
            ResetMenuTypingPosition();
        }

        HandleMovementInput();
        
        HandleWordVerification();

        if (settingsPanel != null && settingsPanel.activeInHierarchy)
        {
            return;
        }
        
        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.anyKeyDown)
        {
            Debug.Log(Input.inputString);
            
            string s = Input.inputString;

            if (!string.IsNullOrEmpty(s))
            {

                if (lockedStrDictionary.ContainsKey(selectedTile))
                {
                    Debug.Log("Dictionary value: " + lockedStrDictionary[selectedTile] +
                              " Input string value.ToLower(): " + s.ToLower());
                }
                else
                {
                    Debug.Log("selected Tile: " + selectedTile + " not contained in dictionary.");
                }
                    
                    if (wordTilemap.GetTile(selectedTile) != lockedLetterTile || lockedStrDictionary[selectedTile] == s.ToLower() || lockedStrDictionary[selectedTile] == s.ToUpper()) //THIS IS THE PROBLEM WE ARENT PASSIN GHERE
                    {
                        char c = s[0];

                        c = char.ToUpperInvariant(c);
                        
                        
                        string curs = c.ToString();
                        bool inBank = true;
                
                        if (useLetterBank)
                        {
                            inBank = false;
                    
                            for (int i = 0; i < letterBank.Count; i++)
                            {
                                if (letterBank[i] == curs)
                                {
                                    inBank = true;
                                    break;
                                }
                            }
                        }

                        Debug.Log(selectedTile);

                        Debug.Log("getting here");
                        if (lockedStrDictionary.ContainsKey(selectedTile)) Debug.Log("Dictionary Value for SelectedTile: " + lockedStrDictionary[selectedTile]);
                        
                        
                        Debug.Log(c.ToString());
                        
                        
                        
                        if (char.IsLetter(c) && inBank || char.IsLetter(c) && lockedStrDictionary.ContainsKey(selectedTile) && lockedStrDictionary[selectedTile] == c.ToString().ToLower())
                        {

                            
                            selectedTilemap.SetTile(selectedTile, transparentTile);
                            if (currentLetterIndex < currentTempChars.Count)
                            {
                                //inserting in word OR before word

                                //inserting before word
                                if (currentLetterIndex < 0)
                                {

                                    currentTempChars.Insert(0, Input.inputString);
                                    temporaryLetterTiles.Insert(0, selectedTile);
                                    
                                    GameObject g1 = Instantiate(stringToTileScript.StringTile(s),
                                        Vector3Int.RoundToInt(selectedTilemap.CellToWorld(selectedTile)),
                                        Quaternion.identity);

                                    temporaryTextTiles.Insert(0, g1);

                                    //temporaryLetterTiles
                                    //temporaryTextTiles
                                    //currenttempchars

                                }
                                else
                                {
                                    currentTempChars.RemoveAt(currentLetterIndex);
                                    currentTempChars.Insert(currentLetterIndex, Input.inputString);

                                    temporaryLetterTiles.RemoveAt(currentLetterIndex);
                                    temporaryLetterTiles.Insert(currentLetterIndex, selectedTile);

                                    GameObject g1 = Instantiate(stringToTileScript.StringTile(s),
                                        Vector3Int.RoundToInt(selectedTilemap.CellToWorld(selectedTile)),
                                        Quaternion.identity);

                                    GameObject g2 = temporaryTextTiles[currentLetterIndex];

                                    temporaryTextTiles.RemoveAt(currentLetterIndex);
                                    Destroy(g2);

                                    temporaryTextTiles.Insert(currentLetterIndex, g1);
                                }

                                //inserting mid word
                            }
                            else
                            {
                                //typing at end of word
                                currentTempChars.Add(Input.inputString);
                                temporaryLetterTiles.Add(selectedTile);

                                GameObject g = Instantiate(stringToTileScript.StringTile(s),
                                    Vector3Int.RoundToInt(selectedTilemap.CellToWorld(selectedTile)),
                                    Quaternion.identity);

                                temporaryTextTiles.Add(g);
                            }

                            if (useLetterBank && !lockedStrDictionary.ContainsKey(selectedTile))
                            {

                                for (int i = 0; i < letterBank.Count; i++)
                                {
                                    if (letterBank[i] == curs)
                                    {
                                        letterBank.RemoveAt(i);

                                        GameObject g8 = letterBankGameobjects[i];
                                        letterBankGameobjects.RemoveAt(i);
                                        Destroy(g8);

                                        InitializeLetterBank();

                                        break;
                                    }
                                }
                            }
                            
                            if (wordTilemap.GetTile(selectedTile) != lockedLetterTile)
                            {
                                wordTilemap.SetTile(selectedTile, letterTile);
                            }


                            if (!horizontalInput)
                            {
                                selectedTile.y += 1;
                            }
                            else
                            {
                                selectedTile.x -= 1;
                            }
                            

                            currentLetterIndex++;

                            UpdateSelectedTile();
                        }

                    }
                
            }
            

        }
        
        
        /*
        if (Input.GetMouseButtonDown(0))
        {
            
            //Vector3Int clickedTile = highlightTilemap.WorldToCell(mousePos);

            Vector3Int clickedTile = new Vector3Int(1, -4, 0);

            bool clickedCurrentTemp = false;
            horizontalInput = false;
            
            for (int i = 0; i < temporaryLetterTiles.Count; i++)
            {
                if (clickedTile == temporaryLetterTiles[i])
                {
                    currentLetterIndex = i;
                    clickedCurrentTemp = true;
                    break;
                }
            }
            
            if (!clickedCurrentTemp)
            {
                ClearTempLetters();
            }
            
            if (selectedTile == clickedTile)
            {
                //SwitchColumnRow();
            }
            else
            {
                RemoveColumnRow();
                
                selectedTilemap.SetTile(selectedTile, transparentTile);
                
                selectedTile = clickedTile;
                
                UpdateSelectedTile();
                
                CreateColumnRow();
            }
            
            tileIsSelected = true;
        }

		*/

        
            Vector3Int hoveredTilePos = highlightTilemap.WorldToCell(mousePos);

            if (previousMousePos != hoveredTilePos)
            {
                if (previousMousePos == selectedTile)
                {
                    selectedTilemap.SetTile(previousMousePos, highlightTile); 
                    selectedTilemap.SetTile(hoveredTilePos, hoverTile);
                    previousMousePos = hoveredTilePos;
                }
                else
                {
                    if (hoveredTilePos != selectedTile)
                    {
                        selectedTilemap.SetTile(previousMousePos, transparentTile); 
                        selectedTilemap.SetTile(hoveredTilePos, hoverTile);
                        previousMousePos = hoveredTilePos;
                    }
                    else
                    {
                        selectedTilemap.SetTile(previousMousePos, transparentTile); 
                        previousMousePos = hoveredTilePos;
                    }

                }

            }
        
            //make selected tile either be the currently clicked tile, or the hovering, and check in hover logic whether previous is selected tile

    }

    bool IsLetter(char c)
    {
        if (c >= 'A' && c <= 'Z')
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void UpdateSelectedTile()
    {
        selectedTilemap.SetTile(selectedTile, highlightTile);
        
        selectedTileTransform.position = selectedTilemap.CellToWorld(selectedTile);

    }

    public void FakeClick()
    {
        Vector2 mousePos = new Vector2(-3, 2);
        horizontalInput = false;
        Vector3Int clickedTile = highlightTilemap.WorldToCell(mousePos);

            bool clickedCurrentTemp = false;
            
            for (int i = 0; i < temporaryLetterTiles.Count; i++)
            {
                if (clickedTile == temporaryLetterTiles[i])
                {
                    currentLetterIndex = i;
                    clickedCurrentTemp = true;
                    break;
                }
            }

            if (!clickedCurrentTemp)
            {
                ClearTempLetters();
            }
            
            if (selectedTile == clickedTile)
            {
                SwitchColumnRow();
            }
            else
            {
                RemoveColumnRow();
                
                selectedTilemap.SetTile(selectedTile, transparentTile);
                
                selectedTile = clickedTile;
                
                UpdateSelectedTile();
                
                CreateColumnRow();
            }
            
            tileIsSelected = true;
        UpdateSelectedTile();
    }

    void OpenSettings()
    {
        ResetMenuTypingPosition();
        settingsWasOpen = true;

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    void ResetMenuTypingPosition()
    {
        Vector2 mousePos = new Vector2(-3, 2);
        Vector3Int clickedTile = highlightTilemap.WorldToCell(mousePos);

        ClearTempLetters();
        RemoveColumnRow();
        selectedTilemap.SetTile(previousMousePos, transparentTile);
        selectedTilemap.SetTile(selectedTile, transparentTile);

        horizontalInput = false;
        selectedTile = clickedTile;
        previousMousePos = clickedTile;
        currentLetterIndex = 0;

        UpdateSelectedTile();
        CreateColumnRow();
        tileIsSelected = true;
    }

	}
