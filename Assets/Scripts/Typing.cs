
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;
using TMPro;
using System.Linq;
using Unity.Cinemachine;

public class Typing : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap highlightTilemap;
    public Tilemap wordTilemap;
    public Tilemap selectedTilemap;
    
    public Tilemap bonusTilemap;
	[Header("SFX")]
	public AudioSource typeAudio;
	public AudioSource lockAudio;

    [Header("Tiles")]
    public Tile highlightTile;

    public Tile hoverTile;
    public Tile transparentTile;

    public Tile midhighlightTile;

    public Tile[] letterTile;

    public Tile startWordTile;

    public Tile lockedLetterTile;
    

    public Color spawnLockedTileColor;
    public Color lockedTileColor;
    
	public Tile wordMultiplier2x;
    public Tile wordMultiplier3x;
    public Tile wordMultiplier5x;

    public Tile timeMultiplier2x;
    public Tile timeMultiplier3x;
    public Tile timeMultiplier5x;

    [Header("Tile Rotation")]
    [SerializeField] private bool useTileRotation = true;
    [SerializeField] private float maxTileRotation = 2.5f;

    private Dictionary<Vector3Int, float> tileRotations = new Dictionary<Vector3Int, float>();

    [Header("Other References")]
    [SerializeField] public Camera cam;

    [SerializeField] public Transform selectedTileTransform;

    public CinemachineImpulseSource impulseSource;
    [SerializeField] public StringToTile stringToTileScript;
    
    private Vector3Int previousMousePos;
    private Vector3Int selectedTile;
    private bool tileIsSelected = false;

    private bool horizontalInput = true;
    public TimeMaster timeScript;
    

    public List<Vector3Int> temporaryLetterTiles = new List<Vector3Int>();
    public List<string> currentTempChars = new List<string>();

    public List<GameObject> temporaryTextTiles = new List<GameObject>();

    public List<Vector3Int> lockedPosList = new List<Vector3Int>(); 
    private Dictionary<Vector3Int, string> lockedStrDictionary = new Dictionary<Vector3Int, string>();

    [SerializeField] public Dictionary<Vector3Int, BonusType> bonusPos = new Dictionary<Vector3Int, BonusType>();
    
    public struct BonusType
    {
        public int type; // 0 for time multi, 1 for word multi
        public int multiplier;
    };

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
    [SerializeField] private GameOverUI gameOverUI;

    [Header("Settings")] 
    [SerializeField] public int boardSize;
    [SerializeField] public bool useBonus;

    [SerializeField] public bool useLetterBank;
    [SerializeField] public int letterBankSize = 7;
    [SerializeField] public int randomBackgroundModifer;

    [SerializeField] public bool useMaxBoard;
    [SerializeField] public int maxBoardSize;

    [SerializeField] public bool onlyBuildOffWords;
    [SerializeField] public bool randomBackgroundLetters;

    private Dictionary<Vector3Int, string> spawnLockedDictionary = new Dictionary<Vector3Int, string>();
    
    [SerializeField] public bool createStartingWord;
    [SerializeField] public string startingWord;


    public bool refreshEveryTime;
    public bool refreshAfterThreshold;
    public int refreshThreshold;

    public HashSet<string> createdWords = new HashSet<string>();
    private string longestWord = "";

    private bool firstWord = true;
    private bool gameOverShown = false;

	[Header("Effects")]
	public GameObject lockedTileEffect;
    public float health = 3f;
    

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
        
        if (useBonus)
        {
            InitializeBonusScores();
        }

    }

    void AddLetters(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            letterBank.Add(DrawRandomLetter().ToString().ToUpper());
        }   

        InitializeLetterBank();
        
    }

    void AddSpecificLetter(char c){
        letterBank.Add(c.ToString().ToUpper());

        InitializeLetterBank();
    }

    float GetOrCreateTileRotation(Vector3Int cellPos)
    {
        if (tileRotations.TryGetValue(cellPos, out float angle))
        {   
            return angle;
        }

        angle = Random.Range(-maxTileRotation, maxTileRotation);
        tileRotations[cellPos] = angle;
        return angle;
    }

    void ApplyTilemapRotation(Vector3Int cellPos, float angle)
    {
        wordTilemap.SetTileFlags(cellPos, TileFlags.None);
        wordTilemap.SetTransformMatrix(
            cellPos,
            Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, angle), Vector3.one)
        );
    }

    void ClearTilemapRotation(Vector3Int cellPos)
    {
        wordTilemap.SetTileFlags(cellPos, TileFlags.None);
        wordTilemap.SetTransformMatrix(cellPos, Matrix4x4.identity);
        tileRotations.Remove(cellPos);
    }

    void ApplyRotationToVisual(GameObject tileObj, float angle)
    {
        tileObj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }


    public int checkEnclosed(HashSet<Vector3Int> prevSet, Vector3Int startingTile, int index, Vector3Int currentTile)
    {

        List<Vector3Int> curNeighbors = ReturnNeighborsIndividual(currentTile);
        prevSet.Add(currentTile);
         
        for (int i = 0; i < curNeighbors.Count; i++){

            if (curNeighbors[i] == startingTile && index >= 2){
                return index * index;
            }

            if (prevSet.Contains(curNeighbors[i])){
                continue;                
            }

            int result = checkEnclosed(prevSet, startingTile, index + 1, curNeighbors[i]);
                if (result > 0){
                    return result;
                }   
        }
        
        Debug.Log("failed here: " + currentTile.x + ", " + currentTile.y + ", " + currentTile.z);
        return 0;

    }

    public HashSet<Vector3Int> returnEnclosed(HashSet<Vector3Int> prevSet, Vector3Int startingTile, int index, Vector3Int currentTile)
    {

        List<Vector3Int> curNeighbors = ReturnNeighborsIndividual(currentTile);
        prevSet.Add(currentTile);

        HashSet<Vector3Int> blankHashset = new HashSet<Vector3Int>();
         
        for (int i = 0; i < curNeighbors.Count; i++){

            if (curNeighbors[i] == startingTile && index >= 2){
                return prevSet;
            }

            if (prevSet.Contains(curNeighbors[i])){
                continue;                
            }

            HashSet<Vector3Int> result = returnEnclosed(prevSet, startingTile, index + 1, curNeighbors[i]);
                if (result != blankHashset){
                    return result;
                }
        }


        return blankHashset;
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
                            
                            GameObject g7 = Instantiate(stringToTileScript.StringTile(s1),
                                Vector3Int.RoundToInt(wordTilemap.CellToWorld(new Vector3Int(x, y, 0))),
                                Quaternion.identity);
                            
                            /* old code (before startingTile)
							if (wordTilemap.GetTile(new Vector3Int(x, y, 0)) != lockedLetterTile)
							{
                             	wordTilemap.SetTile(new Vector3Int(x, y, 0), lockedLetterTile);
							}
							*/
    
                            wordTilemap.SetTile(new Vector3Int(x, y, 0), startWordTile);
                            
                           // wordTilemap.SetTileFlags(new Vector3Int(x, y, 0), TileFlags.None);
                            // wordTilemap.SetColor(new Vector3Int(x, y, 0), spawnLockedTileColor);
                            
                            lockedStrDictionary.Add(new Vector3Int(x, y, 0), s1);
                            spawnLockedDictionary.Add(new Vector3Int(x, y, 0), s1);
                            
							StartCoroutine(PopTile(g7.GetComponentInChildren<RectTransform>()));

                            float angle = GetOrCreateTileRotation(new Vector3Int(x, y, 0));

                            ApplyTilemapRotation(new Vector3Int(x, y, 0), angle);
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

    List<Vector3Int> ReturnNeighborsIndividual(Vector3Int pos)
    {
        List<Vector3Int> neighborsList = new List<Vector3Int>();

        Vector3Int above = new Vector3Int(pos.x, pos.y + 1, pos.z);
        Vector3Int below = new Vector3Int(pos.x, pos.y - 1, pos.z);
        Vector3Int left = new Vector3Int(pos.x - 1, pos.y, pos.z);
        Vector3Int right = new Vector3Int(pos.x + 1, pos.y, pos.z);

        if (lockedStrDictionary.ContainsKey(above)){
            neighborsList.Add(above);
        }
        if (lockedStrDictionary.ContainsKey(below)){
            neighborsList.Add(below);
        }
        if (lockedStrDictionary.ContainsKey(left)){
            neighborsList.Add(left);
        }
        if (lockedStrDictionary.ContainsKey(right)){
            neighborsList.Add(right);
        }

        return neighborsList;
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
		letterValues.Add('g', 3);
		letterValues.Add('h', 4);
		letterValues.Add('i', 1);
		letterValues.Add('j', 8);
		letterValues.Add('k', 5);
		letterValues.Add('l', 2);
		letterValues.Add('m', 2);
		letterValues.Add('n', 2);
		letterValues.Add('o', 1);
		letterValues.Add('p', 3);
		letterValues.Add('q', 8);
		letterValues.Add('r', 2);
		letterValues.Add('s', 2);
		letterValues.Add('t', 2);
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
        letterValues.Add('G', 3);
        letterValues.Add('H', 4);
        letterValues.Add('I', 1);
        letterValues.Add('J', 8);
        letterValues.Add('K', 5);
        letterValues.Add('L', 2);
        letterValues.Add('M', 2);
        letterValues.Add('N', 2);
        letterValues.Add('O', 1);
        letterValues.Add('P', 3);
        letterValues.Add('Q', 8);
        letterValues.Add('R', 2);
        letterValues.Add('S', 2);
        letterValues.Add('T', 2);
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
        foreach (KeyValuePair<Vector3Int, BonusType> pair in bonusPos)
        {
            if (!spawnLockedDictionary.ContainsKey(pair.Key))
            {
                bonusTilemap.SetTile(pair.Key, returnCorrectTile(pair.Value.type, pair.Value.multiplier)); 
            }
        }     
    }


    Tile returnCorrectTile(int type, int multiplier){
        if (type == 0){
            if (multiplier == 2){
                return timeMultiplier2x;
            }
            if (multiplier == 3){
                return timeMultiplier3x;
            }
            else{
                return timeMultiplier5x;
            }
        }
        else{
            if (multiplier == 2){
                return wordMultiplier2x;    
            }
            if (multiplier == 3){
                return wordMultiplier3x;
            }
            else{
                return wordMultiplier5x;
            }
        }
    }
    
    void InitializeBonusScores()
    {
        for (int x = -boardSize + 1; x < boardSize; x++)
        {
            for (int y = -boardSize + 1; y < boardSize; y++)
            {
                if (x % 6 == 0 && y % 6 == 0)
                {                    
                    BonusType temp;
                    temp.type = 0;
                    temp.multiplier = Random.Range(2, 6);

                    while (temp.multiplier != 2 && temp.multiplier != 3 && temp.multiplier != 5){
                        temp.multiplier = Random.Range(2, 6);
                    }

                    bonusPos.Add(new Vector3Int(x, y, 0), temp);

                }
                
                if ((x + 3) % 6 == 0 && (y + 3) % 6 == 0)
                {
                    BonusType temp;
                    temp.type = 1;
                    temp.multiplier = Random.Range(2, 6);

                    while (temp.multiplier != 2 && temp.multiplier != 3 && temp.multiplier != 5){
                        temp.multiplier = Random.Range(2, 6);
                    }

                    bonusPos.Add(new Vector3Int(x, y, 0), temp);
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
                if (!lockedStrDictionary.ContainsKey(temporaryLetterTiles[i])){
                    wordTilemap.SetTile(temporaryLetterTiles[i], transparentTile);
                }
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
            

			Vector3 newPos = new Vector3(temporaryLetterTiles[i].y + 0.5f, temporaryLetterTiles[i].x + 0.5f, temporaryLetterTiles[i].z);

			Instantiate(lockedTileEffect, newPos, Quaternion.identity); //add 0.5 and flip
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

    bool UsesLockedWordLetter()
    {
        for (int i = 0; i < temporaryLetterTiles.Count; i++)
        {
            Vector3Int tilePos = temporaryLetterTiles[i];

            if (lockedStrDictionary.ContainsKey(tilePos) && !spawnLockedDictionary.ContainsKey(tilePos))
            {
                return true;
            }
        }

        return false;
    }

    int CountNewTemporaryLetters()
    {
        int count = 0;

        for (int i = 0; i < temporaryLetterTiles.Count; i++)
        {
            if (!lockedStrDictionary.ContainsKey(temporaryLetterTiles[i]))
            {
                count += 1;
            }
        }

        return count;
    }
    
	IEnumerator PopTile(RectTransform tileVisual)
	{
    	Vector3 originalScale = tileVisual.localScale;
    	Vector3 popScale = originalScale * 1.15f;
    	float duration = 0.25f;
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
			if (bonusPos.ContainsKey(temporaryLetterTiles[i]))
			{
                if (bonusPos[temporaryLetterTiles[i]].type == 1){

				    multi *= bonusPos[temporaryLetterTiles[i]].multiplier;
                    bonusTilemap.SetTile(temporaryLetterTiles[i], transparentTile);
                }
			}
		}

		return multi;
	}

    int CheckForTimeMultiplier()
	{
		int multi = 1;
        
		for (int i = 0; i < temporaryLetterTiles.Count; i++)
		{
		    if (bonusPos.ContainsKey(temporaryLetterTiles[i]))
			{
                if (bonusPos[temporaryLetterTiles[i]].type == 0){
				    multi *= bonusPos[temporaryLetterTiles[i]].multiplier;
                    bonusTilemap.SetTile(temporaryLetterTiles[i], transparentTile);
                }
		    }
            
		}

		return multi;
	}

    /*
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
    */

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

            int prevWord = 0;
            
            string fullAcc = acc;
            
            if (temporaryLetterTiles.Count != 0)
            {
                fullAcc = CheckHeadTail(acc);
            }
            
            
            int floatingProofs = CheckNotFloating();
            bool usesLockedWordLetter = UsesLockedWordLetter();
            
            /*
            Debug.Log("Amount of Letters with a Locked Neighbor: " + floatingProofs);
            Debug.Log("Original String: " + acc);
            Debug.Log("Lengthened String: " + fullAcc);
            Debug.Log("Difference in lengthenedstring and regular" + (fullAcc.Length - acc.Length));
            */

            List<Vector3Int> headTailPositions = new List<Vector3Int>();
            bool headOrTailLocked = false;

            if (currentTempChars.Count != 0)
            {
                headTailPositions = CheckHeadTailPos();
                
                for (int i = 0; i < headTailPositions.Count; i++)
                {
                    if (lockedStrDictionary.ContainsKey(headTailPositions[i]) && !spawnLockedDictionary.ContainsKey(headTailPositions[i]))
                    {
                        headOrTailLocked = true;
                        break;
                    }
                }
            }
            
            if (floatingProofs > 0 || headOrTailLocked || usesLockedWordLetter || !onlyBuildOffWords || firstWord)
            {
                if (ValidWord.IsValidWord(fullAcc) && CheckNeighborWords() && AtLeastOneOriginalLetter() && !createdWords.Contains(fullAcc))
                {

                    int wordMultiplier = CheckForWordMultiplier();
                    int timeMultiplier = CheckForTimeMultiplier();

                    createdWords.Add(fullAcc);
                    RegisterCompletedWord(fullAcc);
                    
                    //debugging
                    List<Vector3Int> crosswordPositions = new List<Vector3Int>();

                    crosswordPositions = GetCrossWordPositions();

                    List<string> crossStrings = new List<string>();
                    crossStrings = GetCrossWords();
                    

                    for (int i = 0; i < crosswordPositions.Count; i++)
                    {
                        Debug.Log("Crossword neighbors" + crosswordPositions[i]);

						wordTilemap.SetTile(crosswordPositions[i], lockedLetterTile);

                       // wordTilemap.SetTileFlags(crosswordPositions[i], TileFlags.None);
                       // wordTilemap.SetColor(crosswordPositions[i], lockedTileColor);
                        
                        if (!lockedStrDictionary.ContainsKey(crosswordPositions[i]))
                        {

                        }
                    }

                    for (int i = 0; i < crossStrings.Count; i++)
                    {
                        Debug.Log(crossStrings[i]);
                    }
                    //end
                    
                    
                    
                    for (int i = 0; i < headTailPositions.Count; i++)
                    {
                        // wordTilemap.SetTileFlags(headTailPositions[i], TileFlags.None);
                        // wordTilemap.SetColor(headTailPositions[i], lockedTileColor);
                        
                        wordTilemap.SetTile(headTailPositions[i], lockedLetterTile);
                        
                    }

                    for (int i = 0; i < temporaryLetterTiles.Count; i++)
                    {
                        // wordTilemap.SetTileFlags(temporaryLetterTiles[i], TileFlags.None);
                        // wordTilemap.SetColor(temporaryLetterTiles[i], lockedTileColor);
                        
                        wordTilemap.SetTile(temporaryLetterTiles[i], lockedLetterTile);
                        
                    }
                    
                    if (useLetterBank)
                    {
                        AddLetters(CountNewTemporaryLetters());
                    }
                    
                    List<string> crossWords = new List<string>();
                    crossWords = GetCrossWords();

                    LockTempLetters();
                    AddScore(fullAcc, wordMultiplier, timeMultiplier);
					lockAudio.Play();                    

                    for (int i = 0; i < crossWords.Count; i++)
                    {
                        RegisterCompletedWord(crossWords[i]);
                        AddScore(crossWords[i], wordMultiplier, timeMultiplier);
                    }

                    if (refreshEveryTime)
                    {
                        letterBank.Clear();
                    }

                    if (refreshAfterThreshold && fullAcc.Length >= refreshThreshold)
                    {
                        letterBank.Clear();
                    }
                    
                    while (letterBank.Count < letterBankSize)
                    {
                        AddLetters(1);
                    } 


                    if (letterBank.Count > letterBankSize)
                    {
                        GameObject g6 = letterBankGameobjects[letterBankGameobjects.Count - 1];
                        letterBank.RemoveAt(letterBank.Count - 1);
                        Destroy(g6);
                        InitializeLetterBank();

                    }
                    
                    firstWord = false;

                }
            }
        }
    }

    void ResetLetters(){
        letterBank.Clear();
        health -= 1;
        ClearTempLetters();
        
        while (letterBank.Count < letterBankSize)
        {
            AddLetters(1);
        } 

    }

    void ResetTime(){
        timeScript.ResetTime();
    }


    void CheckTime(){
        if (timeScript.timeBelowZero)
        {
            ResetTime();
            ResetLetters();
            timeScript.timeBelowZero = false;
        }
    }

    void ShowGameOver()
    {
        if (gameOverShown)
        {
            return;
        }

        gameOverShown = true;

        if (gameOverUI != null)
        {
            gameOverUI.Show(score, longestWord);
        }

        Debug.Log("Game Over");
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
    
    void AddScore(string stringToAdd, int wordMultiplier, int timeMultiplier)
    {
        int scoreToAdd = 0;

		for (int i = 0; i < stringToAdd.Length; i++)
		{
			int letterScore = 0;
            
			letterScore = letterValues[stringToAdd[i]] * wordMultiplier;

			scoreToAdd += letterScore;
		}


        score += scoreToAdd;

        timeScript.AddTime(stringToAdd.Length, scoreToAdd, 1);
        timeScript.countingDown = true;

        ScoreUI.text = "" + score;

        GameObject g2 = Instantiate(newWordText, newWordLayoutGroup.transform);

        
        Destroy(g2, 4f);
        TextMeshProUGUI tempW = g2.GetComponent<TextMeshProUGUI>();
        tempW.CrossFadeAlpha(0f, 4f, false);
        
        tempW.text = "" + stringToAdd.ToUpper() + " +" + scoreToAdd;
        
    }

    void RegisterCompletedWord(string completedWord)
    {
        if (!string.IsNullOrEmpty(completedWord) && completedWord.Length > longestWord.Length)
        {
            longestWord = completedWord;
        }
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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwitchColumnRow();
            
            if (currentTempChars.Count > 1)
            {
                ClearTempLetters();
            }

        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (currentTempChars.Count > 0 && currentLetterIndex == currentTempChars.Count)
            {
                if (useLetterBank && !lockedStrDictionary.ContainsKey(temporaryLetterTiles[temporaryLetterTiles.Count - 1]))
                {
                    letterBank.Add(currentTempChars[currentTempChars.Count - 1].ToUpper());
                    InitializeLetterBank();
                }

                selectedTilemap.SetTile(selectedTile, transparentTile);

                currentTempChars.RemoveAt(currentTempChars.Count - 1);
                

                GameObject g0 = temporaryTextTiles[temporaryTextTiles.Count - 1];
                
                temporaryTextTiles.RemoveAt(temporaryTextTiles.Count - 1); // destroy

                Destroy(g0);
                
                /* OLD VERSION (SWITCH FROM LOCKEDLETTERTILE)
                if (wordTilemap.GetTile(temporaryLetterTiles[temporaryLetterTiles.Count - 1]) != lockedLetterTile)
                {
                    wordTilemap.SetTile(temporaryLetterTiles[temporaryLetterTiles.Count - 1], transparentTile);
                }
                */
                
                if (!lockedStrDictionary.ContainsKey(temporaryLetterTiles[temporaryLetterTiles.Count - 1]))
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
                selectedTilemap.SetTile(selectedTile, transparentTile);
                
                if (horizontalInput)
                {
                    selectedTile.x += 1;
                }
                else
                {
                    selectedTile.y -= 1;
                }

                if (currentTempChars.Count > 0)
                {
                    currentLetterIndex--;
                }
            
                UpdateSelectedTile();
            }

        }
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            RemoveColumnRow();
            selectedTilemap.SetTile(selectedTile, transparentTile);
            
            if (horizontalInput)
            {
                ClearTempLetters();
            }

            if (currentTempChars.Count > 0)
            {
                currentLetterIndex--;
            }
            
            selectedTile.y -= 1;
            
            UpdateSelectedTile();
            CreateColumnRow();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentLetterIndex++;
            
            RemoveColumnRow();
            selectedTilemap.SetTile(selectedTile, transparentTile);

            if (currentLetterIndex > currentTempChars.Count)
            {
                ClearTempLetters();
            }

            if (horizontalInput)
            {
                ClearTempLetters();
            }
            
            selectedTile.y += 1;
            
            UpdateSelectedTile();
            CreateColumnRow();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            RemoveColumnRow();
            selectedTilemap.SetTile(selectedTile, transparentTile);
            
            if (currentTempChars.Count > 0)
            {
                currentLetterIndex--;
            }
            
            
            if (!horizontalInput)
            {
                ClearTempLetters();
            }
            
            selectedTile.x += 1;
            
            UpdateSelectedTile();
            CreateColumnRow();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentLetterIndex++;
            RemoveColumnRow();
            selectedTilemap.SetTile(selectedTile, transparentTile);
            
            if (currentLetterIndex > currentTempChars.Count)
            {
                ClearTempLetters();
            }

            if (!horizontalInput)
            {
                ClearTempLetters();
            }
            
            selectedTile.x -= 1;

            UpdateSelectedTile();
            CreateColumnRow();
        }
    }
    
    public Tile GetLetterTile(){

        int r = Random.Range(0, letterTile.Length);
        return letterTile[r];
    }
    
    // Update is called once per frame
    void Update()
    {
        CheckTime();

        if (health <= 0)
        {
            ShowGameOver();
            return;
        }

        HandleMovementInput();
        
        HandleWordVerification();
        
        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.anyKeyDown)
        {
            Debug.Log(Input.inputString);
            
            string s = Input.inputString;

            if (!string.IsNullOrEmpty(s) && s.Length == 1)
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
                    
                
                char c = char.ToUpperInvariant(s[0]);
                s = c.ToString();

                // if (wordTilemap.GetTile(selectedTile) != lockedLetterTile || lockedStrDictionary[selectedTile] == s.ToLower() || lockedStrDictionary[selectedTile] == s.ToUpper()) - OLD STATEMENT (LOCKEDLETTERTILE SWITCH)
                
                
                if (!lockedStrDictionary.ContainsKey(selectedTile) || lockedStrDictionary[selectedTile] == s.ToLower() || lockedStrDictionary[selectedTile] == s.ToUpper()) //THIS IS THE PROBLEM WE ARENT PASSING HERE
                {
                    string curs = c.ToString();
                    bool inBank = true;
                
                    if (useLetterBank)
                    {
                        inBank = false;
                        bool replacingUnlockedLetter = currentLetterIndex >= 0 &&
                                                       currentLetterIndex < temporaryLetterTiles.Count &&
                                                       !lockedStrDictionary.ContainsKey(temporaryLetterTiles[currentLetterIndex]);
                    
                        for (int i = 0; i < letterBank.Count; i++)
                        {
                            if (letterBank[i] == curs)
                            {
                                inBank = true;
                                break;
                            }
                        }

                        if (!inBank && replacingUnlockedLetter && currentTempChars[currentLetterIndex].ToUpper() == curs)
                        {
                            inBank = true;
                        }
                    }

                    Debug.Log(selectedTile);

                    Debug.Log("getting here");
                    if (lockedStrDictionary.ContainsKey(selectedTile)) Debug.Log("Dictionary Value for SelectedTile: " + lockedStrDictionary[selectedTile]);

                    Debug.Log(c.ToString());

                    if (IsLetter(c) && (inBank || lockedStrDictionary.ContainsKey(selectedTile) &&
                        (lockedStrDictionary[selectedTile] == c.ToString().ToLower() ||
                         lockedStrDictionary[selectedTile] == c.ToString().ToUpper())))
                    {

                            string letterToRefund = null;
                            bool skipLetterBankConsume = false;

                            selectedTilemap.SetTile(selectedTile, transparentTile);
                            if (currentLetterIndex < currentTempChars.Count)
                            {
                                //inserting in word OR before word

                                //inserting before word
                                if (currentLetterIndex < 0)
                                {

                                    currentTempChars.Insert(0, s);
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
                                    string previousLetter = currentTempChars[currentLetterIndex];
                                    Vector3Int previousTile = temporaryLetterTiles[currentLetterIndex];

                                    currentTempChars.RemoveAt(currentLetterIndex);
                                    currentTempChars.Insert(currentLetterIndex, s);

                                    temporaryLetterTiles.RemoveAt(currentLetterIndex);
                                    temporaryLetterTiles.Insert(currentLetterIndex, selectedTile);

                                    GameObject g1 = Instantiate(stringToTileScript.StringTile(s),
                                        Vector3Int.RoundToInt(selectedTilemap.CellToWorld(selectedTile)),
                                        Quaternion.identity);

                                    GameObject g2 = temporaryTextTiles[currentLetterIndex];

                                    temporaryTextTiles.RemoveAt(currentLetterIndex);
                                    Destroy(g2);

                                    temporaryTextTiles.Insert(currentLetterIndex, g1);
                                    if (useLetterBank && !lockedStrDictionary.ContainsKey(previousTile))
                                    {
                                        if (previousLetter.ToUpper() == curs)
                                        {
                                            skipLetterBankConsume = true;
                                        }
                                        else
                                        {
                                            letterToRefund = previousLetter.ToUpper();
                                        }
                                    }
                                }

                                //inserting mid word
                            }
                            else
                            {
                                //typing at end of word
                                currentTempChars.Add(s);
                                temporaryLetterTiles.Add(selectedTile);

                                GameObject g = Instantiate(stringToTileScript.StringTile(s),
                                    Vector3Int.RoundToInt(selectedTilemap.CellToWorld(selectedTile)),
                                    Quaternion.identity);

                                temporaryTextTiles.Add(g);
                            }

                            if (useLetterBank && !lockedStrDictionary.ContainsKey(selectedTile) && !skipLetterBankConsume)
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

                            if (useLetterBank && letterToRefund != null)
                            {
                                letterBank.Add(letterToRefund);
                                InitializeLetterBank();
                            }
                            
                            // OLD LINE (PRE LOCKEDLETTERTILE) if (wordTilemap.GetTile(selectedTile) != lockedLetterTile)
                            
                            if (!lockedStrDictionary.ContainsKey(selectedTile))
                            {
                                float angle = GetOrCreateTileRotation(selectedTile);
                                ApplyTilemapRotation(selectedTile, angle);

                                wordTilemap.SetTile(selectedTile, GetLetterTile());
                            }


                            if (!horizontalInput)
                            {
                                selectedTile.y += 1;
                            }
                            else
                            {
                                selectedTile.x -= 1;
                            }

                            impulseSource.GenerateImpulse();

                            currentLetterIndex++;
							typeAudio.PlayOneShot(typeAudio.clip);
                            UpdateSelectedTile();
                        }

                    }
                
            }
            

        }
        
        if (Input.GetMouseButtonDown(0))
        {
            
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
        }

  
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
    
	
}
