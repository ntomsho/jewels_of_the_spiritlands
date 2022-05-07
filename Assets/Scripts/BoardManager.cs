using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BoardManager : MonoBehaviour
{
    [SerializeField] int[] _debugBoard;
    public BattleManager battleManager;

    [Header("Board")]
    GameObject boardBG;
    [SerializeField] GameObject boardBGPrefab;
    [SerializeField] float boardTimeIncrement;
    public int boardSize;
    [SerializeField] float fallSpeed;
    [SerializeField] float hintDelay;

    [Header("Base Tiles")]
    [SerializeField] GameObject tilePrefab;
    [SerializeField] List<Sprite> tileSprites;
    public List<Sprite> glowSprites;
    [SerializeField] List<Color> glowColors;
    
    [Header("Special Tiles")]
    [SerializeField] List<Sprite> columnClearSprites;
    [SerializeField] List<Sprite> rowClearSprites;
    [SerializeField] List<Sprite> colorBombSprites;
    [SerializeField] List<ParticleSystem> tileClearParticles;
    [SerializeField] List<ParticleSystem> lineClearParticles;
    public Sprite bombGlowSprite;
    public ParticleSystem activateParticle;

    [Header("Audio")]
    [SerializeField] AudioClip breakSFX1;
    [SerializeField] AudioClip breakSFX2;
    [SerializeField] AudioClip activateSpecialSound;

    [System.NonSerialized] public bool battleOver;
    [System.NonSerialized] public Tile[,] board;
    float boardHeight;
    bool boardStable = true;
    float boardWidth;
    [System.NonSerialized] public bool canSwap;
    List<Match> currentMatches = new List<Match>();
    bool firstTurnStarted;
    bool gameStarted;
    Match hintMatch;
    bool hintOn;
    float hintTimer = 0;
    [System.NonSerialized] public SpecialSelect specialSelect;
    [System.NonSerialized] public int[] spiritLevels = new int[5];
    TileColor[] tileColors = new TileColor[] { TileColor.Red, TileColor.Yellow, TileColor.Green, TileColor.Blue, TileColor.Purple };

    public void Setup() 
    {
        boardBG = Instantiate(boardBGPrefab, new Vector3(/*6.5f*/17f, 0f, -0.5f), Quaternion.identity);
        boardBG.transform.DOMoveX(6.5f, 1f).SetDelay(2f).OnComplete(() => {
            CreateStartingBoard();
            // _CreateDebugBoard();
        });
        SpriteRenderer bgRenderer = boardBG.GetComponent<SpriteRenderer>();
        boardWidth = bgRenderer.sprite.rect.width;
        boardHeight = bgRenderer.sprite.rect.height;
        board = new Tile[boardSize, boardSize];
    }

    public void Update()
    {
        if (!gameStarted) return;
        if (!boardStable)
        {
            float dropAmount = fallSpeed * Time.deltaTime;
            foreach(Tile tile in board)
            {
                if (tile != null && !tile.stable)
                {
                    tile.Drop(dropAmount);
                }
            }
            CheckIfBoardStable();
        }

        if (canSwap && !hintOn)
        {
            if (hintTimer < hintDelay)
            {
                hintTimer += Time.deltaTime;
            }
            else
            {
                if (hintMatch != null)
                {
                    hintOn = true;
                    hintMatch.ApplyHintToTiles();
                }
            }
        }
    }

    public void CreateStartingBoard()
    {
        for (int ind1 = 0; ind1 < boardSize; ind1++) {
            for (int ind2 = 0; ind2 < boardSize; ind2++) {
                // ** Change this to not allow repeats of colors when deciding what can go there 
                TileColor newTileColor = GetRandomTileColor(tileColors);
                if (ind1 >= 2 || ind2 >= 2)
                {
                    while (CheckForMatchOnStartingBoard(ind2, ind1, newTileColor))
                    {
                        newTileColor = GetRandomTileColor(tileColors);
                    }
                }
                // **
                // CreateTile(newTileColor, ind2, ind1, SpecialTile.None);
                Tile newTile = CreateOverflowTile(newTileColor, ind2, -5 + ind1, SpecialTile.None);
                newTile.stable = false;
                newTile.dropDestination = GetPosFromGrid(ind2, ind1);
                SetTileGridPos(newTile, ind2, ind1, true);
            }
        }
        //fix timing on this so it checks when board is in place and doesn't start the player turn until a valid board is in place
        // CheckForAvailableMatches();
        gameStarted = true;
        boardStable = false;
        // StartPlayerTurn();
    }

    public void _CreateDebugBoard()
    {
        for (int ind = 0; ind < _debugBoard.Length; ind++)
        {
            CreateTile((TileColor) _debugBoard[ind], ind % boardSize, ind / boardSize, SpecialTile.None);
        }
        // CheckForAvailableMatches();
        gameStarted = true;
        boardStable = false;
        // StartPlayerTurn();

        StartCoroutine(_DebugParticleTest(GetTileFromGrid(0,1)));
    }

    IEnumerator _DebugParticleTest(Tile tile)
    {
        yield return new WaitForSeconds(2f);
        tile.ApplyStatus(TileStatus.Frozen);
        // yield return new WaitForSeconds(2f);
        // tile.ApplyStatus(TileStatus.Free);
    }

    public void StartPlayerTurn()
    {
        CheckForAvailableMatches();
        canSwap = true;
        foreach(Tile tile in board)
        {
            if (tile.status != TileStatus.Inactive)
            {
                tile.ToggleGrayOut(false);
            }
        }
    }

    public void EndPlayerTurn()
    {
        foreach(Tile tile in board)
        {
            if (tile.status != TileStatus.Inactive)
            {
                tile.ToggleGrayOut(true);
            }
        }
    }

    List<Sprite> GetTileSpriteSet(SpecialTile specialType)
    {
        switch (specialType)
        {
            case SpecialTile.ClearColumn:
                return columnClearSprites;
            case SpecialTile.ClearRow:
                return rowClearSprites;
            case SpecialTile.ColorBomb:
                return colorBombSprites;
            default:
                return tileSprites;
        }
    }

    Tile CreateTile(TileColor tileColor, int startX, int startY, SpecialTile specialType = SpecialTile.None)
    {
        GameObject newTile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity);
        newTile.transform.parent = boardBG.transform;
        Vector3 startPos = GetPosFromGrid(startX, startY);
        newTile.transform.localPosition = startPos;

        Tile tileScript = newTile.GetComponent<Tile>();
        tileScript.Setup(this, tileColor, GetTileSpriteSet(specialType)[(int)tileColor], specialType);
        
        board[startY, startX] = tileScript;
        tileScript.x = startX;
        tileScript.y = startY;

        if (IsColorCharacterKnockedOut(tileColor))
        {
            tileScript.GetComponent<SpriteRenderer>().color = Color.gray;
        }
        
        return tileScript;
    }

    Tile CreateOverflowTile(TileColor tileColor, int startX, int overflowOffset, SpecialTile specialType = SpecialTile.None)
    {
        GameObject newTile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity);
        newTile.transform.parent = boardBG.transform;
        Vector3 startPos = GetPosFromGrid(startX, overflowOffset);
        newTile.transform.localPosition = startPos;

        Tile tileScript = newTile.GetComponent<Tile>();
        tileScript.Setup(this, tileColor, tileSprites[(int)tileColor], specialType);
        
        tileScript.x = startX;
        tileScript.y = overflowOffset;

        if (IsColorCharacterKnockedOut(tileColor))
        {
            tileScript.GetComponent<SpriteRenderer>().color = Color.gray;
        }
        
        return tileScript;
    }

    public Vector3 GetPosFromGrid(int xPos, int yPos)
    {
        return new Vector3(-0.75f + 1.5f * (float)xPos / (float)(boardSize - 1), 0.75f - 1.5f * (float)yPos / (float)(boardSize - 1), -1f);
    }

    public Tile GetTileFromGrid(int xPos, int yPos)
    {
        if (xPos >= 0 && xPos < boardSize && yPos >= 0 && yPos < boardSize)
        {
            return board[yPos, xPos];
        }
        else
        {
            return null;
        }
    }

    void SetTileGridPos(Tile tile, int newX, int newY, bool clearOldPlace)
    {
        if (clearOldPlace)
        {
            ClearGridSpace(tile.x, tile.y);
        }
        board[newY, newX] = tile;
        tile.x = newX;
        tile.y = newY;
    }

    void ClearGridSpace(int clearX, int clearY)
    {
        if (clearX < 0 || clearX >= boardSize || clearY < 0 || clearY >= boardSize) return;
        board[clearY, clearX] = null;
    }

    TileColor GetRandomTileColor(TileColor[] availableColors)
    {
        return availableColors[Random.Range(0, availableColors.Length)];
    }

#nullable enable
    Match? GetMatchFromTile(Tile startTile, Tile? tileOverride = null)
    {
        List<Tile> matchingTiles = new List<Tile>();
        Tile baseTile = tileOverride == null ? startTile : tileOverride;
        TileColor matchColor = tileOverride == null ? startTile.tileColor : tileOverride.tileColor;

        List<Tile> horiz = CheckForMatchInDirection(startTile, GetTileFromGrid(startTile.x + 1, startTile.y), new List<Tile>() { baseTile }, 0, matchColor);
        horiz = CheckForMatchInDirection(startTile, GetTileFromGrid(startTile.x - 1, startTile.y), horiz, 0, matchColor);

        List<Tile> vert = CheckForMatchInDirection(startTile, GetTileFromGrid(startTile.x, startTile.y + 1), new List<Tile>() { baseTile }, 0, matchColor);
        vert = CheckForMatchInDirection(startTile, GetTileFromGrid(startTile.x, startTile.y - 1), vert, 0, matchColor);

        if (horiz.Count >= 3 && vert.Count >= 3)
        {
            matchingTiles.AddRange(horiz);
            foreach (Tile tile in vert)
            {
                if (tile != startTile)
                {
                    matchingTiles.Add(tile);
                }
            }
        }
        else if (horiz.Count >= 3)
        {
            matchingTiles.AddRange(horiz);
        }
        else if (vert.Count >= 3)
        {
            matchingTiles.AddRange(vert);
        }
        
        if (matchingTiles.Count > 0)
        {
            return new Match(matchingTiles.ToArray());
        }
        else
        {
            return null;
        }
    }

    bool DoTilesMatch(Tile tile1, Tile tile2)
    {
        return (tile1 != null && tile2 != null && tile1.IsMatchable() && tile2.IsMatchable() && tile1.tileColor == tile2.tileColor);
    }

    List<Tile> CheckForMatchInDirection(Tile startTile, Tile nextTile, List<Tile> tileList, int iterations, TileColor matchColor)
    {
        if (iterations < 4 && nextTile && !tileList.Contains(nextTile) && nextTile.IsMatchable() && nextTile.tileColor == matchColor)
        {
            tileList.Add(nextTile);
            Tile newNextTile = GetTileFromGrid(nextTile.x + (nextTile.x - startTile.x), nextTile.y + (nextTile.y - startTile.y));
            if (newNextTile)
            {
                return CheckForMatchInDirection(nextTile, newNextTile, tileList, iterations + 1, matchColor);
            }
        }
        return tileList;
    }

    bool CheckForMatchOnStartingBoard(int x, int y, TileColor matchColor)
    {
        return
        (x >= 2 && GetTileFromGrid(x - 1, y).tileColor == matchColor && GetTileFromGrid(x - 2, y).tileColor == matchColor)
        ||
        (y >= 2 && GetTileFromGrid(x, y - 1).tileColor == matchColor && GetTileFromGrid(x, y - 2).tileColor == matchColor);
    }

    public void CheckForBoardMatches(Tile tile)
    {
        Match? match = GetMatchFromTile(tile);
        if (match != null)
        {
            tile.stable = false;
            match.SetTilesToMatched();
            currentMatches.Add(match);
            StartCoroutine(InitiateMatchProcessing());
        }
    }

    public void InitiateSwap(Tile tile1, Tile tile2)
    {
        if (battleOver || tile1 == null || tile2 == null) return;
        
        if (!tile1.IsMovable() || !tile2.IsMovable())
        {
            ShakeImmovableTile(!tile1.IsMovable() ? tile1 : tile2);
            return;
        }
        
        canSwap = false;
        SwapTiles(tile1, tile2);

        List<Match> matches = new List<Match>();
        Match? match = GetMatchFromTile(tile1);
        if (match != null)
        {
            match.SetTilesToMatched();
            matches.Add(match);
        }
        match = GetMatchFromTile(tile2);
        if (match != null)
        {
            match.SetTilesToMatched();
            matches.Add(match);
        }

        if (matches.Count > 0)
        {
            if (hintMatch != null)
            {
                hintMatch.RemoveHintFromTiles();
                hintMatch = null;
                hintOn = false;
                hintTimer = 0;
            }
            currentMatches.AddRange(matches);
            StartCoroutine(InitiateMatchProcessing());
        }
        else
        {
            StartCoroutine(UndoSwap(tile1, tile2));
        }
    }

    void SwapTiles(Tile tile1, Tile tile2)
    {   
        Vector3 tile1Origin = GetPosFromGrid(tile1.x, tile1.y);
        Vector3 tile2Origin = GetPosFromGrid(tile2.x, tile2.y);

        int tempX = tile1.x;
        int tempY = tile1.y;

        SetTileGridPos(tile1, tile2.x, tile2.y, false);
        SetTileGridPos(tile2, tempX, tempY, false);

        tile1.MoveSpriteTo(tile2Origin, boardTimeIncrement, false);
        tile2.MoveSpriteTo(tile1Origin, boardTimeIncrement, false);
    }

    SpecialTile GetSpecial(int matchValue)
    {
        if (matchValue == 4)
        {
            //change matches to remember what direction swipe was involved and assign based on that
            return (Random.Range(0f,1f) > 0.5f ? SpecialTile.ClearColumn : SpecialTile.ClearRow);
        }
        else
        {
            return SpecialTile.ColorBomb;
        }
    }

    void ProcessMatches()
    {
        //need to account for frozen/locked tiles not being destroyed on match
        Tile? specialReplaceTile = null;
        List<Tile> tilesToUnmatch = new List<Tile>();
        while (currentMatches.Count > 0)
        {
            boardStable = false;
            Match match = currentMatches[0];
            battleManager.AddToActiveMatches(match);
            AddToSpiritLevel((int) match.matchColor, match.value);

            if (match.value > 3)
            {
                specialReplaceTile = match.tiles[Random.Range(0,match.tiles.Length)];
            }

            foreach (Tile tile in match.tiles)
            {
                ParticleSystem ps = Instantiate(tileClearParticles[(int)tile.tileColor], boardBG.transform, false);
                ps.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, -5f);
                ps.Play();
                GameManager.Instance.PlaySound(breakSFX1);
                GameManager.Instance.PlaySound(breakSFX2);
                
                if (tile.specialType == SpecialTile.ClearColumn || tile.specialType == SpecialTile.ClearRow)
                {
                    ClearLine(tile, tile.specialType == SpecialTile.ClearRow);
                }
                else if (tile.specialType == SpecialTile.ColorBomb)
                {
                    DetonateColorBomb(tile);
                }

                if (tile.status == TileStatus.Frozen)
                {
                    tile.ApplyStatus(TileStatus.Free);
                    tilesToUnmatch.Add(tile);
                }
                else
                {
                    tile.ClearTile();
                    if (tile == specialReplaceTile)
                    {
                        CreateTile(specialReplaceTile.tileColor, specialReplaceTile.x, specialReplaceTile.y, GetSpecial(match.value));
                    }
                }
            }

            CreateSpiritParticles(match);

            currentMatches.RemoveAt(0);
        }
        foreach(Tile tile in tilesToUnmatch)
        {
            if (tile != null) tile.matched = false;
        }
    }

    void AddToSpiritLevel(int partyInd, int matchValue)
    {
        int amountToAdd = matchValue * GameManager.Instance.spiritMod[partyInd];
        spiritLevels[partyInd] += Mathf.Min(100 - spiritLevels[partyInd], amountToAdd);
    }

    bool IsColorCharacterKnockedOut(TileColor checkColor)
    {
        foreach (PartyMember pm in battleManager.knockedOutPartyMembers)
        {
            if (pm.tileColor == checkColor)
            {
                return true;
            }
        }
        return false;
    }

    public void ApplyTileStatus(List<Tile> tiles, TileStatus tileStatus)
    {
        foreach(Tile tile in tiles)
        {
            tile.ApplyStatus(tileStatus);
        }
    }

    public void ApplyStatusToColor(TileColor colorToApply, TileStatus statusToApply)
    {
        foreach (Tile tile in board)
        {
            if (tile != null && tile.tileColor == colorToApply)
            {
                tile.ApplyStatus(statusToApply);
            }
        }
    }

    IEnumerator InitiateMatchProcessing()
    {
        yield return new WaitForSeconds(boardTimeIncrement);
        ProcessMatches();
        StartCoroutine(CheckBoard());
    }

    IEnumerator CheckBoard()
    {
        yield return new WaitForSeconds(boardTimeIncrement);
        UpdateBoard();
    }

    void CheckIfBoardStable()
    {
        if (IsBoardStable())
        {
            boardStable = true;
            if (firstTurnStarted)
            {
                EndPlayerTurn();
                battleManager.ProcessMatches();
            }
            else
            {
                firstTurnStarted = true;
                GameManager.Instance.overlayManager.ShowTurnCanvas(true, new System.Action(StartPlayerTurn));
            }
        }
    }

    IEnumerator ShakeImmovableTile(Tile tile)
    {
        canSwap = false;
        tile.gameObject.transform.DOShakePosition(boardTimeIncrement, new Vector3(0.1f, 0f, 0f), 20, 0);
        yield return new WaitForSeconds(boardTimeIncrement);
        canSwap = true;
    }

    IEnumerator UndoSwap(Tile tile1, Tile tile2)
    {
        yield return new WaitForSeconds(boardTimeIncrement);
        tile1.gameObject.transform.DOShakePosition(boardTimeIncrement, new Vector3(0.1f, 0f, 0f), 20, 0);
        tile2.gameObject.transform.DOShakePosition(boardTimeIncrement, new Vector3(0.1f, 0f, 0f), 20, 0);
        yield return new WaitForSeconds(boardTimeIncrement);
        SwapTiles(tile1, tile2);
        canSwap = true;
    }

    void FillEmptySpace(int indX, int indY, int overflowOffset)
    {
        Tile? fillTile = null;
        for (int y = indY; y >= 0; y--)
        {
            Tile tile = GetTileFromGrid(indX, y);
            if (tile != null)
            {
                fillTile = tile;
                break;
            }
        }
        if (fillTile == null)
        {
            fillTile = CreateOverflowTile(GetRandomTileColor(tileColors), indX, overflowOffset);
        }
        fillTile.stable = false;
        fillTile.dropDestination = GetPosFromGrid(indX, indY);
        SetTileGridPos(fillTile, indX, indY, true);
    }

    void UpdateBoard()
    {
        int[] overflowOffset = new int[5];
        for (int indY = boardSize - 1; indY >= 0; indY--)
        {
            for (int indX = 0; indX < boardSize; indX++)
            {
                if (GetTileFromGrid(indX, indY) == null)
                {
                    overflowOffset[indX] -= 1;
                    FillEmptySpace(indX, indY, overflowOffset[indX]);
                }
            }
        }
    }

    void ClearLine(Tile startTile, bool isRow)
    {
        ParticleSystem particleEffect = Instantiate(lineClearParticles[(int)startTile.tileColor], boardBG.transform, false);
        particleEffect.transform.position = startTile.transform.position;
        particleEffect.transform.rotation = Quaternion.Euler(0f, 0f, isRow ? 0f : 90f);

        List<Tile> hitTiles = new List<Tile>();
        for (int ind = 1; ind < boardSize; ind++)
        {
            if ((isRow ? startTile.x : startTile.y) + ind < this.boardSize)
            {
                Tile tile = GetTileFromGrid(startTile.x + (isRow ? ind : 0), startTile.y + (isRow ? 0 : ind));
                if (tile != null && tile.IsMatchable())
                {
                    hitTiles.Add(tile);
                }
            }
            if ((isRow ? startTile.x : startTile.y) - ind >= 0)
            {
                Tile tile = GetTileFromGrid(startTile.x - (isRow ? ind : 0), startTile.y - (isRow ? 0 : ind));
                if (tile != null && tile.IsMatchable())
                {
                    hitTiles.Add(tile);
                }
            }
        }
        GenerateMatchesFromSpecial(hitTiles);
    }

    void DetonateColorBomb(Tile startTile)
    {
        List<Tile> hitTiles = new List<Tile>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Tile tile = GetTileFromGrid(startTile.x + x, startTile.y + y);
                if (tile && tile.IsMatchable() && tile != startTile)
                {
                    hitTiles.Add(tile);
                }
            }
        }
        GenerateMatchesFromSpecial(hitTiles);
    }

    void GenerateMatchesFromSpecial(List<Tile> tiles)
    {
        Dictionary<TileColor, List<Tile>> tileDict = new Dictionary<TileColor, List<Tile>>();

        foreach(Tile tile in tiles)
        {
            List<Tile> destList;
            if (!tileDict.TryGetValue(tile.tileColor, out destList))
            {
                destList = new List<Tile>();
                tileDict[tile.tileColor] = destList;
            }
            tileDict[tile.tileColor].Add(tile);
        }

        foreach(var item in tileDict)
        {
            Match newMatch = new Match(item.Value.ToArray());
            newMatch.SetTilesToMatched();
            currentMatches.Add(newMatch);
        }
    }

    public List<Tile> GetTilesOfColor(TileColor colorToGet, int numTiles)
    {
        List<Tile> tiles = new List<Tile>();
        foreach(Tile tile in board)
        {
            if (tile.tileColor == colorToGet)
            {
                tiles.Add(tile);
                if (tiles.Count == numTiles)
                {
                    break;
                }
            }
        }
        return tiles;
    }

    bool IsBoardStable()
    {
        foreach (Tile tile in board)
        {
            if (tile == null || !tile.stable || tile.matched)
            {
                return false;
            }
        }
        return true;
    }

    void CreateSpiritParticles(Match match)
    {
        specialSelect.CreateSpiritParticles(match.tiles[match.tiles.Length / 2].gameObject.transform.position, match.value, match.matchColor, glowColors[(int)match.matchColor]);
    }

    public void ToggleSpecialTargeting(int partyMemberInd)
    {
        // EndPlayerTurn();
        GameManager.Instance.PlaySound(activateSpecialSound);
        battleManager.ToggleSpecialTargeting(partyMemberInd);
    }

    bool CheckForAvailableMatches()
    {
        List<Match> allMatches = GetAllAvailableMatches();
        if (allMatches.Count == 0)
        {
            canSwap = false;
            StartCoroutine(ResetBoard());
            return false;
        }
        else
        {
            hintMatch = allMatches[Random.Range(0, allMatches.Count)];
            return true;
        }
    }

    List<Match> GetAllAvailableMatches()
    {
        List<Match> allMatches = new List<Match>();

        foreach(Tile tile in board)
        {
            if (tile == null || !tile.IsMatchable() || !tile.IsMovable()) continue;
            allMatches.AddRange(GetPotentialMatches(tile));
        }

        return allMatches;
    }

    List<Match> GetPotentialMatches(Tile baseTile)
    {
        List<Match> matches = new List<Match>();
        Tile[] tilesToCheck = new Tile[]
        { 
            GetTileFromGrid(baseTile.x - 1, baseTile.y),
            GetTileFromGrid(baseTile.x, baseTile.y - 1),
            GetTileFromGrid(baseTile.x + 1, baseTile.y),
            GetTileFromGrid(baseTile.x, baseTile.y + 1)
        };
        
        foreach(Tile startTile in tilesToCheck)
        {
            if (startTile != null)
            {
                Match? match = GetMatchFromTile(startTile, baseTile);
                if (match != null)
                {
                    matches.Add(match);
                }
            }
        }
        return matches;
    }

    IEnumerator ResetBoard()
    {
        yield return new WaitForSeconds(1f);
        foreach(Tile tile in board)
        {
            tile.ClearTile();
        }
        yield return new WaitForSeconds(1.5f);
        CreateStartingBoard();
    }
}

public class Match
{
    public Tile[] tiles;
    public TileColor matchColor;
    public int value;

    public Match(params Tile[] matchingTiles)
    {
        tiles = matchingTiles;
        matchColor = tiles[0].tileColor;
        SetValue();
    }

    void SetValue()
    {
        int val = 0;
        foreach(Tile tile in tiles)
        {
            if (tile.status == TileStatus.Free)
            {
                val++;
            }
        }
        value = val;
    }

    public List<Tile> GetSpecials()
    {
        List<Tile> specialTiles = new List<Tile>();
        foreach(Tile tile in tiles)
        {
            if (tile.specialType != SpecialTile.None)
            {
                specialTiles.Add(tile);
            }
        }
        return specialTiles;
    }

    public bool Contains(Tile checkTile)
    {
        foreach(Tile tile in tiles)
        {
            if (tile == checkTile)
            {
                return true;
            }
        }
        return false;
    }

    public void SetTilesToMatched()
    {
        foreach(Tile tile in tiles)
        {
            tile.matched = true;
        }
    }

    public void ApplyHintToTiles()
    {
        foreach(Tile tile in tiles)
        {
            tile.ApplyHint();
        }
    }

    public void RemoveHintFromTiles()
    {
        foreach(Tile tile in tiles)
        {
            if (tile != null)
            {
                tile.RemoveHint();
            }
        }
    }
}
