namespace ChessEngine.src
{
    class MoveGenerator
    {
        private List<Move> generatedMoves;
        private List<Move> pseudoMoves;
        public static readonly ulong RankMask = ulong.MaxValue << 56;
        public static readonly ulong WhitePromotionMask = RankMask >> (8 * 6);
        public static readonly ulong BlackPromotionMask = RankMask >> (8 * 1);

        public static readonly int[] DirectionOffSets = [-8, 8, -1, 1, -9, 9, -7, 7];

        public ulong attackedSquares;
        ulong checkBitBoard;
        ulong pinnedSquares;
        int[] pinnedSquareDirectionIndexes;
        int numChecks;


        public MoveGenerator()
        {
            pinnedSquareDirectionIndexes = new int[64];
            for (int i = 0; i < 64; i++)
            {
                pinnedSquareDirectionIndexes[i] = -101;
            }
            generatedMoves = new List<Move>();
            pseudoMoves = new List<Move>();
            numChecks = int.MinValue;
        }

        public List<Move> GenerateLegalMoves(Board board)
        {
            Init();


            GenerateAttackBitboard(board);
            int[] Squares = board.Squares;
            bool WhiteToMove = board.WhiteToMove;
            bool WhiteKingSide = board.WhiteCanCastleKingSide;
            bool WhiteQueenSide = board.WhiteCanCastleQueenSide;
            bool BlackKingSide = board.BlackCanCastleKingSide;
            bool BlackQueenSide = board.BlackCanCastleQueenSide;
            int enPassantSquare = board.enPassantSquareStack[^1];
            bool inCheck = numChecks > 0;
            int colour = WhiteToMove ? 0 : 8;
            int oppositeColour = WhiteToMove ? 8 : 0;

            ulong AllPieces = board.AllPieceBitBoard;
            ulong FriendlyPieces;
            ulong Enemypieces;
            if (WhiteToMove)
            {
                FriendlyPieces = board.WhitePieceBitBoard;
                Enemypieces = board.BlackPieceBitBoard;
            }
            else
            {
                FriendlyPieces = board.BlackPieceBitBoard;
                Enemypieces = board.WhitePieceBitBoard;
            }
            //Console.WriteLine(numChecks);
            //Board.PrintBitBoard(checkBitBoard);
            //Board.PrintBitBoard(pinnedSquares);
            //Board.PrintBitBoard(attackedSquares);
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {

                int piece = Squares[squareIndex];
                if (((FriendlyPieces >> squareIndex) & 1) == 1)
                {
                    if (numChecks < 2)
                    {
                        if (Piece.IsSlidingPiece(piece))
                        {
                            GenerateSlidingMoves(squareIndex, piece, Squares, inCheck, FriendlyPieces, Enemypieces);
                        }
                        else
                        {
                            switch (Piece.PieceType(piece))
                            {
                                case Piece.King:
                                    GenerateKingMoves(squareIndex, Squares, WhiteToMove, WhiteKingSide, WhiteQueenSide, BlackKingSide, BlackQueenSide, inCheck, colour);
                                    break;
                                case Piece.Knight:
                                    GenerateKnightMoves(squareIndex, Squares, inCheck, FriendlyPieces);
                                    break;
                                case Piece.Pawn:
                                    GeneratePawnMoves(board, squareIndex, Squares, WhiteToMove, enPassantSquare, inCheck, colour, AllPieces, Enemypieces, FriendlyPieces);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (Piece.PieceType(piece) == Piece.King)
                        {
                            GenerateKingMoves(squareIndex, Squares, WhiteToMove, WhiteKingSide, WhiteQueenSide, BlackKingSide, BlackQueenSide, inCheck, colour);
                        }
                    }
                }
            }


            return generatedMoves;
            //return FilterMoves(board);

        }

        public List<Move> GenerateLegalCaptures(Board board)
        {
            Init();


            GenerateAttackBitboard(board);
            int[] Squares = board.Squares;
            bool WhiteToMove = board.WhiteToMove;
            bool WhiteKingSide = board.WhiteCanCastleKingSide;
            bool WhiteQueenSide = board.WhiteCanCastleQueenSide;
            bool BlackKingSide = board.BlackCanCastleKingSide;
            bool BlackQueenSide = board.BlackCanCastleQueenSide;
            int enPassantSquare = board.enPassantSquareStack[^1];
            bool inCheck = numChecks > 0;
            int colour = WhiteToMove ? 0 : 8;
            int oppositeColour = WhiteToMove ? 8 : 0;
            //Console.WriteLine(numChecks);
            //board.PrintBitBoard(checkBitBoard);
            //board.PrintBitBoard(pinnedSquares);
            //board.PrintBitBoard(attackedSquares);
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {

                int piece = Squares[squareIndex];
                if (Piece.IsColour(piece, colour))
                {
                    if (numChecks < 2)
                    {
                        if (Piece.IsSlidingPiece(piece))
                        {
                            GenerateSlidingCaptures(squareIndex, piece, Squares, inCheck, colour, oppositeColour);
                        }
                        else
                        {
                            switch (Piece.PieceType(piece))
                            {
                                case Piece.King:
                                    GenerateKingCaptures(squareIndex, Squares, colour);
                                    break;
                                case Piece.Knight:
                                    GenerateKnightCaptures(squareIndex, Squares, inCheck, colour);
                                    break;
                                case Piece.Pawn:
                                    GeneratePawnCaptures(board, squareIndex, Squares, WhiteToMove, enPassantSquare, inCheck, colour);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (Piece.PieceType(piece) == Piece.King)
                        {
                            GenerateKingCaptures(squareIndex, Squares, colour);
                        }
                    }
                }
            }


            return generatedMoves;
            //return FilterMoves(board);

        }

        private bool FilterMove(Board board, Move move)
        {
            board.MakeMove(move);
            GeneratePseudoLegalMoves(board);
            board.UndoMove();
            if (pseudoMoves.Any(response => response.TargetSquare == board.KingSquares[board.WhiteToMove ? 0 : 1]))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public Move[] GeneratePseudoLegalMoves(Board boardState)
        {

            InitPseudo();
            int[] Squares = boardState.Squares;
            bool WhiteToMove = boardState.WhiteToMove;
            bool WhiteKingSide = boardState.WhiteCanCastleKingSide;
            bool WhiteQueenSide = boardState.WhiteCanCastleQueenSide;
            bool BlackKingSide = boardState.BlackCanCastleKingSide;
            bool BlackQueenSide = boardState.BlackCanCastleQueenSide;
            int enPassantSquare = boardState.enPassantSquareStack[^1];

            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                int piece = Squares[squareIndex];
                if (Piece.IsColour(piece, WhiteToMove ? 0 : 8))
                {

                    if (Piece.IsSlidingPiece(piece))
                    {
                        GeneratePseudoSlidingMoves(squareIndex, piece, Squares, WhiteToMove);
                    }
                    else
                    {
                        switch (Piece.PieceType(piece))
                        {
                            case Piece.King:
                                GeneratePseudoKingMoves(squareIndex, Squares, WhiteToMove, WhiteKingSide, WhiteQueenSide, BlackKingSide, BlackQueenSide);
                                break;
                            case Piece.Knight:
                                GeneratePseudoKnightMoves(squareIndex, Squares, WhiteToMove);
                                break;
                            case Piece.Pawn:
                                GeneratePseudoPawnMoves(squareIndex, Squares, WhiteToMove, enPassantSquare);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            return pseudoMoves.ToArray();
        }

        public void GenerateAttackBitboard(Board boardState)
        {
            bool NotWhiteToMove = !boardState.WhiteToMove;
            int oppositeColour = NotWhiteToMove ? 0 : 8;
            int colour = boardState.WhiteToMove ? 0 : 8;
            int KingSquare = boardState.KingSquares[boardState.WhiteToMove ? 0 : 1];
            ulong KingBoard = ~boardState.BitBoards[boardState.WhiteToMove ? Piece.WhiteKing : Piece.BlackKing] & boardState.AllPieceBitBoard;

            ulong EnemyPieces = boardState.WhiteToMove ? boardState.BlackPieceBitBoard : boardState.WhitePieceBitBoard;
            GeneratePinnedSquares(KingSquare, boardState.Squares, colour, oppositeColour, boardState.WhiteToMove, boardState.BitBoards);
            CountChecks(boardState.Squares, KingSquare, boardState.WhiteToMove, boardState);
            int[] Squares = boardState.Squares;
            


            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {

                int piece = Squares[squareIndex];
                if (((EnemyPieces >> squareIndex) & 1) == 1)
                {

                    if (Piece.IsSlidingPiece(piece))
                    {
                        GenerateSlidingAttacks(squareIndex, piece, Squares, KingBoard);
                    }
                    else
                    {
                        switch (Piece.PieceType(piece))
                        {
                            case Piece.King:
                                GenerateKingAttacks(squareIndex);
                                break;
                            case Piece.Knight:
                                GenerateKnightAttacks(squareIndex);
                                break;
                            case Piece.Pawn:
                                GeneratePawnAttacks(squareIndex, NotWhiteToMove);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        void GeneratePinnedSquares(int KingSquare, int[] squares, int colour, int oppositeColour, bool WhiteToMove, ulong[] BitBoards)
        {
            ulong DiagonalAttackers;
            ulong OrthogonalAttackers;
            if (WhiteToMove)
            {
                DiagonalAttackers = BitBoards[Piece.BlackBishop] | BitBoards[Piece.BlackQueen];
                OrthogonalAttackers = BitBoards[Piece.BlackRook] | BitBoards[Piece.BlackQueen];
            }
            else
            {
                DiagonalAttackers = BitBoards[Piece.WhiteBishop] | BitBoards[Piece.WhiteQueen];
                OrthogonalAttackers = BitBoards[Piece.WhiteRook] | BitBoards[Piece.WhiteQueen];
            }

            bool onePiece = false;
            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                ulong pinnedMask = 0UL;
                if ((directionIndex < 4 && (PrecomputedMoveData.RayMasks[KingSquare, directionIndex] & OrthogonalAttackers) != 0) || (directionIndex > 3 && (PrecomputedMoveData.RayMasks[KingSquare, directionIndex] & DiagonalAttackers) != 0))
                {
                    for (int n = 0; n < PrecomputedMoveData.numSquaresToEdge[KingSquare][directionIndex]; n++)
                    {

                        int targetSquare = KingSquare + DirectionOffSets[directionIndex] * (n + 1);
                        int pieceOnTargetSquare = squares[targetSquare];
                        pinnedSquareDirectionIndexes[targetSquare] = directionIndex;

                        // Blocked by opponent piece
                        if (Piece.IsColour(pieceOnTargetSquare, oppositeColour))
                        {
                            if (directionIndex > 3 && Piece.IsDiagonalSlider(pieceOnTargetSquare))
                            {
                                pinnedMask |= 1UL << targetSquare;
                                if (!onePiece)
                                {
                                    checkBitBoard |= pinnedMask;
                                }
                                pinnedSquares |= pinnedMask;
                                pinnedSquareDirectionIndexes[targetSquare] = directionIndex;
                                break;
                            }
                            else if (directionIndex < 4 && Piece.IsOrthogonalSlider(pieceOnTargetSquare))
                            {
                                pinnedMask |= 1UL << targetSquare;
                                if (!onePiece)
                                {
                                    checkBitBoard |= pinnedMask;
                                }
                                pinnedSquares |= pinnedMask;
                                pinnedSquareDirectionIndexes[targetSquare] = directionIndex;
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (Piece.IsColour(pieceOnTargetSquare, colour) && pieceOnTargetSquare != 0)
                        {

                            if (onePiece)
                            {
                                break;
                            }
                            onePiece = true;
                        }
                        pinnedMask |= 1UL << targetSquare;
                    }
                    onePiece = false;
                }
            }
        }

        void Init()
        {
            generatedMoves = [];
            checkBitBoard = 0UL;
            pinnedSquares = 0UL;
            attackedSquares = 0UL;
            pinnedSquareDirectionIndexes = new int[64];
            for (int i = 0; i < 64; i++)
            {
                pinnedSquareDirectionIndexes[i] = -101;
            }
            numChecks = 0;
        }

        void InitPseudo()
        {
            pseudoMoves = new List<Move>();
        }

        void CountChecks(int[] squares, int KingSquare, bool WhiteToMove, Board board)
        {
            int targetSquare;
            ulong FriendlyPieces;
            ulong EnemyPawns;
            ulong EnemyKnights;
            ulong EnemySlidingPieces;
            ulong Queens = board.BitBoards[Piece.WhiteQueen] | board.BitBoards[Piece.BlackQueen];

            if (WhiteToMove)
            {
                FriendlyPieces = board.WhitePieceBitBoard;
                EnemyPawns = board.BitBoards[Piece.BlackPawn];
                EnemyKnights = board.BitBoards[Piece.BlackKnight];
                EnemySlidingPieces = board.BitBoards[Piece.BlackBishop] | board.BitBoards[Piece.BlackRook] | board.BitBoards[Piece.BlackQueen];
            }
            else
            {
                FriendlyPieces = board.BlackPieceBitBoard;
                EnemyPawns = board.BitBoards[Piece.WhitePawn];
                EnemyKnights = board.BitBoards[Piece.WhiteKnight];
                EnemySlidingPieces = board.BitBoards[Piece.WhiteBishop] | board.BitBoards[Piece.WhiteRook] | board.BitBoards[Piece.WhiteQueen];
            }

            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                for (int n = 0; n < PrecomputedMoveData.numSquaresToEdge[KingSquare][directionIndex]; n++)
                {
                    targetSquare = KingSquare + DirectionOffSets[directionIndex] * (n + 1);
                    int pieceOnTargetSquare = squares[targetSquare];
                    bool isTargetPieceFriendly = ((FriendlyPieces >> targetSquare) & 1) == 1;
                    bool IsEnemySliderPiece = ((EnemySlidingPieces >> targetSquare) & 1) == 1;


                    // Blocked by friendly piece
                    if ((isTargetPieceFriendly || !IsEnemySliderPiece) && pieceOnTargetSquare != 0)
                    {
                        break;
                    }
                    if (((Queens >> targetSquare) & 1) == 1)
                    {
                        numChecks++;
                        break;
                    }
                    if (IsEnemySliderPiece)
                    {
                        if (Piece.IsOrthogonalSlider(pieceOnTargetSquare) && directionIndex < 4)
                        {
                            numChecks++;
                        }
                        if (Piece.PieceType(pieceOnTargetSquare) == Piece.Bishop && directionIndex > 3)
                        {
                            numChecks++;
                        }
                        break;
                    }
                    
                }

                targetSquare = PrecomputedMoveData.knightMoveTable[KingSquare][directionIndex].Item2;
                if (PrecomputedMoveData.knightMoveTable[KingSquare][directionIndex].Item1 && ((EnemyKnights >> targetSquare) & 1) == 1)
                {
                    checkBitBoard |= 1UL << targetSquare;
                    numChecks++;
                }
            }

            if (WhiteToMove)
            {
                targetSquare = KingSquare - 7;
                if (targetSquare > 7 && PrecomputedMoveData.pawnCaptureTable[KingSquare][1] && ((EnemyPawns >> targetSquare) & 1) == 1)
                {

                    checkBitBoard |= 1UL << targetSquare;
                    numChecks++;
                }
                targetSquare = KingSquare - 9;
                if (targetSquare > 8 && PrecomputedMoveData.pawnCaptureTable[KingSquare][0] && ((EnemyPawns >> targetSquare) & 1) == 1)
                {
                    checkBitBoard |= 1UL << targetSquare;
                    numChecks++;
                }
            }
            else
            {
                targetSquare = KingSquare + 7;
                if (targetSquare < 56 && PrecomputedMoveData.pawnCaptureTable[KingSquare][0] && ((EnemyPawns >> targetSquare) & 1) == 1)
                {
                    checkBitBoard |= 1UL << targetSquare;
                    numChecks++;
                }
                targetSquare = KingSquare + 9;
                if (targetSquare < 57 && PrecomputedMoveData.pawnCaptureTable[KingSquare][1] && ((EnemyPawns >> targetSquare) & 1) == 1)
                {
                    checkBitBoard |= 1UL << targetSquare;
                    numChecks++;
                }
            }
        }

        void GenerateSlidingMoves(int square, int piece, int[] Squares, bool inCheck, ulong FriendlyPieces, ulong EnemyPieces)
        {
            int startDirIndex;
            int endDirIndex;
            bool isPinned = (pinnedSquares & (1UL << square)) != 0;
            int pinnedDirectionIndex = pinnedSquareDirectionIndexes[square];


            // Get the correct startDirIndex and endDirIndex
            if (Piece.PieceType(piece) == Piece.Rook)
            {

                if (isPinned && pinnedDirectionIndex > 3)
                {
                    return;
                }
                startDirIndex = 0;
            }
            else
            {
                startDirIndex = 4;
            }
            if (Piece.PieceType(piece) == Piece.Bishop)
            {
                if (isPinned && pinnedDirectionIndex < 4)
                {
                    return;
                }
                endDirIndex = 8;
            }
            else
            {
                endDirIndex = 4;
            }
            if (Piece.PieceType(piece) == Piece.Queen)
            {
                startDirIndex = 0;
                endDirIndex = 8;
            }


            if (!isPinned)
            {
                for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
                {

                    for (int n = 0; n < PrecomputedMoveData.numSquaresToEdge[square][directionIndex]; n++)
                    {

                        int targetSquare = square + DirectionOffSets[directionIndex] * (n + 1);
                        int pieceOnTargetSquare = Squares[targetSquare];

                        // Blocked by a friendly piece
                        
                        if (((FriendlyPieces >> targetSquare) & 1) == 1)
                        {
                            break;
                        }

                        // Assuming not pinned
                        if (inCheck)
                        {
                            if ((checkBitBoard & (1Ul << targetSquare)) != 0)
                            {
                                generatedMoves.Add(new Move(square, targetSquare));
                            }
                        }
                        else
                        {
                            generatedMoves.Add(new Move(square, targetSquare));
                        }

                        // Blocked by opponent piece
                        if (((EnemyPieces >> targetSquare) & 1) == 1)
                        {
                            break;
                        }
                    }
                }
            }
            else if (!inCheck)
            {

                // move "along" pinned direction
                int targetSquare = square + DirectionOffSets[pinnedDirectionIndex];
                
                while (((pinnedSquares >> targetSquare) & 1) == 1 && 63 >= targetSquare && targetSquare >= 0 && pinnedDirectionIndex == pinnedSquareDirectionIndexes[targetSquare])
                {

                    generatedMoves.Add(new Move(square, targetSquare));
                    targetSquare += DirectionOffSets[pinnedDirectionIndex];
                }

                targetSquare = square - DirectionOffSets[pinnedDirectionIndex];

                while (((pinnedSquares >> targetSquare) & 1) == 1 && 63 >= targetSquare && targetSquare >= 0 && pinnedDirectionIndex == pinnedSquareDirectionIndexes[targetSquare])
                {
                    generatedMoves.Add(new Move(square, targetSquare));
                    targetSquare -= DirectionOffSets[pinnedDirectionIndex];
                }
            }
        }

        void GenerateKingMoves(int square, int[] Squares, bool WhiteToMove, bool WhiteKingSide, bool WhiteQueenSide, bool BlackKingSide, bool BlackQueenSide, bool inCheck, int colour)
        {

            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                if (PrecomputedMoveData.numSquaresToEdge[square][directionIndex] != 0)
                {
                    int targetSquare = square + DirectionOffSets[directionIndex];
                    int pieceOnTargetSquare = Squares[targetSquare];

                    // Blocked by friendly piece or illegal move
                    if (Piece.IsColour(pieceOnTargetSquare, colour) || (attackedSquares & (1UL << targetSquare)) != 0)
                    {
                        continue;
                    }
                    generatedMoves.Add(new Move(square, targetSquare));

                }
            }
            if (!inCheck)
            {
                if (WhiteToMove)
                {
                    if (Squares[61] == 0 && Squares[62] == 0 && WhiteKingSide && (attackedSquares & 6917529027641081856UL) == 0)
                    {
                        generatedMoves.Add(new Move(square, 62, 0b0010));
                    }
                    if (Squares[57] == 0 && Squares[58] == 0 && Squares[59] == 0 && WhiteQueenSide && (attackedSquares & 864691128455135232Ul) == 0)
                    {
                        generatedMoves.Add(new Move(square, 58, 0b0010));
                    }
                }
                else
                {
                    if (Squares[5] == 0 && Squares[6] == 0 && BlackKingSide && (attackedSquares & 96UL) == 0)
                    {
                        generatedMoves.Add(new Move(square, 6, 0b0010));
                    }
                    if (Squares[3] == 0 && Squares[2] == 0 && Squares[1] == 0 && BlackQueenSide && (attackedSquares & 12UL) == 0)
                    {
                        generatedMoves.Add(new Move(square, 2, 0b0010));
                    }
                }
            }
        }

        void GenerateKnightMoves(int square, int[] Squares, bool inCheck, ulong FriendlyPieces)
        {
            int targetSquare;
            if (((pinnedSquares >> square) & 1) == 0)
            {

                for (int directionIndex = 0; directionIndex < 8; directionIndex++)
                {
                    targetSquare = PrecomputedMoveData.knightMoveTable[square][directionIndex].Item2;
                    if (PrecomputedMoveData.knightMoveTable[square][directionIndex].Item1)
                    {
                        if (((FriendlyPieces >> targetSquare) & 1) == 0)
                        {
                            if (!inCheck)
                            {
                                generatedMoves.Add(new Move(square, targetSquare));
                            }
                            else if (((checkBitBoard >> targetSquare) & 1) == 1)
                            {
                                generatedMoves.Add(new Move(square, targetSquare));
                            }
                        }
                    }
                }
            }
        }

        void GeneratePawnMoves(Board board, int square, int[] Squares, bool WhiteToMove, int enPassantSquare, bool inCheck, int colour, ulong AllPieces, ulong EnemyPieces, ulong FriendlyPieces)
        {
            int friendlyRank;
            int promotionRank;
            int directionOffset;
            if (WhiteToMove)
            {
                friendlyRank = 6; // Starting rank
                directionOffset = -8; // Offset for moving
                promotionRank = 1; // Rank for promotion
            }
            else
            {
                friendlyRank = 1; // Starting rank
                directionOffset = 8; // Offset for moving
                promotionRank = 6; // Rank for promotion
            }



            int targetSquare = square + directionOffset; // Target square
            int captureTargetSquareLeft = targetSquare - 1;
            int captureTargetSquareRight = targetSquare + 1;
            int currentRank = square / 8;
            bool isPinned = (pinnedSquares & (1UL << square)) != 0;
            int pinnedDirectionIndex = pinnedSquareDirectionIndexes[square];
            bool targetSquareIsChecking = (checkBitBoard & (1UL << targetSquare)) != 0;
            bool rightSquareIsChecking = (checkBitBoard & (1UL << captureTargetSquareRight)) != 0;
            bool leftSquareIsChecking = (checkBitBoard & (1UL << captureTargetSquareLeft)) != 0;


            if (!isPinned)
            {

                if (inCheck)
                {

                    // If the following square is empty then the pawn can move
                    if (Squares[targetSquare] == Piece.None && targetSquareIsChecking)
                    {
                        // If the pawn is on he promotion rank
                        if (currentRank == promotionRank)
                        {
                            // One move for each promotion
                            generatedMoves.Add(new Move(square, targetSquare, 0b0100));
                            generatedMoves.Add(new Move(square, targetSquare, 0b0101));
                            generatedMoves.Add(new Move(square, targetSquare, 0b0110));
                            generatedMoves.Add(new Move(square, targetSquare, 0b0111));
                        }
                        else
                        {
                            generatedMoves.Add(new Move(square, targetSquare));
                        }
                    }
                    
                    // Pawn can move two squares to block check
                    if (currentRank == friendlyRank && ((AllPieces >> (targetSquare + directionOffset)) & 1) == 0 && ((checkBitBoard >> (targetSquare + directionOffset)) & 1) == 1 && ((AllPieces >> targetSquare) & 1) == 0)
                    {
                        generatedMoves.Add(new Move(square, targetSquare + directionOffset, 0b0011));
                    }
                    
                    // Pawn capture left
                    if (PrecomputedMoveData.pawnCaptureTable[square][0] && ((EnemyPieces >> captureTargetSquareLeft) & 1) == 1 && leftSquareIsChecking)
                    {
                        if (currentRank == promotionRank)
                        {
                            // One move for each promotion
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0100));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0101));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0110));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0111));
                        }
                        else
                        {
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft));
                        }
                    }
                    // Pawn capture right
                    if (PrecomputedMoveData.pawnCaptureTable[square][1] && ((EnemyPieces >> captureTargetSquareRight) & 1) == 1 && rightSquareIsChecking)
                    {
                        if (currentRank == promotionRank)
                        {
                            // One move for each promotion
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0100));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0101));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0110));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0111));
                        }
                        else
                        {
                            generatedMoves.Add(new Move(square, captureTargetSquareRight));
                        }
                    }

                    if (enPassantSquare == captureTargetSquareLeft && PrecomputedMoveData.pawnCaptureTable[square][0] && FilterMove(board, new Move(square, captureTargetSquareLeft, 0b0001)))
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0001));
                    }
                    if (enPassantSquare == captureTargetSquareRight && PrecomputedMoveData.pawnCaptureTable[square][1] && FilterMove(board, new Move(square, captureTargetSquareRight, 0b0001)))
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0001));
                    }
                }
                else
                {

                    // If the following square is empty then the pawn can move
                    if (Squares[targetSquare] == Piece.None)
                    {
                        // If the pawn is on he promotion rank
                        if (currentRank == promotionRank)
                        {
                            // One move for each promotion
                            generatedMoves.Add(new Move(square, targetSquare, 0b0100));
                            generatedMoves.Add(new Move(square, targetSquare, 0b0101));
                            generatedMoves.Add(new Move(square, targetSquare, 0b0110));
                            generatedMoves.Add(new Move(square, targetSquare, 0b0111));
                        }
                        else
                        {
                            generatedMoves.Add(new Move(square, targetSquare));
                            // Pawn can move two squares
                            if (currentRank == friendlyRank && Squares[targetSquare + directionOffset] == 0)
                            {
                                generatedMoves.Add(new Move(square, targetSquare + directionOffset, 0b0011));
                            }
                        }
                    }

                    // Pawn capture left
                    if (PrecomputedMoveData.pawnCaptureTable[square][0] && ((EnemyPieces >> captureTargetSquareLeft) & 1) == 1)
                    {
                        if (currentRank == promotionRank)
                        {
                            // One move for each promotion
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0100));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0101));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0110));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0111));
                        }
                        else
                        {
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft));
                        }
                    }
                    // Pawn capture right
                    if (PrecomputedMoveData.pawnCaptureTable[square][1] && ((EnemyPieces >> captureTargetSquareRight) & 1) == 1)
                    {
                        if (currentRank == promotionRank)
                        {
                            // One move for each promotion
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0100));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0101));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0110));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0111));
                        }
                        else
                        {
                            generatedMoves.Add(new Move(square, captureTargetSquareRight));
                        }
                    }

                    // En passant
                    if (enPassantSquare == captureTargetSquareLeft && PrecomputedMoveData.pawnCaptureTable[square][0] && FilterMove(board, new Move(square, captureTargetSquareLeft, 0b0001)))
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0001));
                    }
                    if (enPassantSquare == captureTargetSquareRight && PrecomputedMoveData.pawnCaptureTable[square][1] && FilterMove(board, new Move(square, captureTargetSquareRight, 0b0001)))
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0001));
                    }
                }
            }
            else if (!inCheck)
            {
                // If the following square is empty then the pawn can move
                if (Squares[targetSquare] == Piece.None && pinnedDirectionIndex is 0 or 1)
                {

                    // If the pawn is on he promotion rank
                    if (currentRank == promotionRank)
                    {
                        // One move for each promotion
                        generatedMoves.Add(new Move(square, targetSquare, 0b0100));
                        generatedMoves.Add(new Move(square, targetSquare, 0b0101));
                        generatedMoves.Add(new Move(square, targetSquare, 0b0110));
                        generatedMoves.Add(new Move(square, targetSquare, 0b0111));
                    }
                    else
                    {

                        generatedMoves.Add(new Move(square, targetSquare));
                        // Pawn can move two squares
                        if (currentRank == friendlyRank && Squares[targetSquare + directionOffset] == 0)
                        {
                            generatedMoves.Add(new Move(square, targetSquare + directionOffset, 0b0011));
                        }
                    }
                }
                // Pawn capture left
                if (PrecomputedMoveData.pawnCaptureTable[square][0] && !Piece.IsColour(Squares[captureTargetSquareLeft], colour) && Squares[captureTargetSquareLeft] != 0 && ((pinnedDirectionIndex == 7 && !WhiteToMove) || (pinnedDirectionIndex == 4 && WhiteToMove)))
                {
                    if (currentRank == promotionRank)
                    {
                        // One move for each promotion
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0100));
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0101));
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0110));
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0111));
                    }
                    else
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft));
                    }
                }

                // Pawn capture right
                if (PrecomputedMoveData.pawnCaptureTable[square][1] && !Piece.IsColour(Squares[captureTargetSquareRight], colour) && Squares[captureTargetSquareRight] != 0 && ((pinnedDirectionIndex == 5 && !WhiteToMove) || (pinnedDirectionIndex == 6 && WhiteToMove)))
                {
                    if (currentRank == promotionRank)
                    {
                        // One move for each promotion
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0100));
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0101));
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0110));
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0111));
                    }
                    else
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareRight));
                    }
                }

                if (enPassantSquare == captureTargetSquareLeft && PrecomputedMoveData.pawnCaptureTable[square][0] && FilterMove(board, new Move(square, captureTargetSquareLeft, 0b0001)))
                {
                    generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0001));
                }
                if (enPassantSquare == captureTargetSquareRight && PrecomputedMoveData.pawnCaptureTable[square][1] && FilterMove(board, new Move(square, captureTargetSquareRight, 0b0001)))
                {
                    generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0001));
                }

            }
        }

        void GeneratePseudoSlidingMoves(int square, int piece, int[] Squares, bool WhiteToMove)
        {

            int startDirIndex = Piece.IsOrthogonalSlider(piece) ? 0 : 4;
            int endDirIndex = Piece.IsDiagonalSlider(piece) ? 8 : 4;


            for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
            {
                for (int n = 0; n < PrecomputedMoveData.numSquaresToEdge[square][directionIndex]; n++)
                {

                    int targetSquare = square + DirectionOffSets[directionIndex] * (n + 1);
                    int pieceOnTargetSquare = Squares[targetSquare];

                    // Blocked by friendly piece
                    if (Piece.IsColour(pieceOnTargetSquare, WhiteToMove ? 0 : 8))
                    {
                        break;
                    }

                    pseudoMoves.Add(new Move(square, targetSquare));

                    if (Piece.IsColour(pieceOnTargetSquare, WhiteToMove ? 8 : 0))
                    {
                        break;
                    }
                }
            }
        }

        void GeneratePseudoKingMoves(int square, int[] Squares, bool WhiteToMove, bool WhiteKingSide, bool WhiteQueenSide, bool BlackKingSide, bool BlackQueenSide)
        {

            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                if (PrecomputedMoveData.numSquaresToEdge[square][directionIndex] != 0)
                {
                    int targetSquare = square + DirectionOffSets[directionIndex];
                    int pieceOnTargetSquare = Squares[targetSquare];

                    // Blocked by friendly piece
                    if (Piece.IsColour(pieceOnTargetSquare, WhiteToMove ? 0 : 8))
                    {
                        break;
                    }

                    pseudoMoves.Add(new Move(square, targetSquare));

                }
            }
            if (WhiteToMove)
            {
                if (Squares[61] == 0 && Squares[62] == 0 && WhiteKingSide)
                {
                    pseudoMoves.Add(new Move(square, 62, Move.CastleFlag));
                }
                if (Squares[57] == 0 && Squares[58] == 0 && Squares[59] == 0 && WhiteQueenSide)
                {
                    pseudoMoves.Add(new Move(square, 58, Move.CastleFlag));
                }
            }
            else
            {
                if (Squares[5] == 0 && Squares[6] == 0 && BlackKingSide)
                {
                    pseudoMoves.Add(new Move(square, 6, Move.CastleFlag));
                }
                if (Squares[3] == 0 && Squares[2] == 0 && Squares[1] == 0 && BlackQueenSide)
                {
                    pseudoMoves.Add(new Move(square, 2, Move.CastleFlag));
                }
            }
        }

        void GeneratePseudoKnightMoves(int square, int[] Squares, bool WhiteToMove)
        {
            int targetSquare;
            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                targetSquare = PrecomputedMoveData.knightMoveTable[square][directionIndex].Item2;
                if (PrecomputedMoveData.knightMoveTable[square][directionIndex].Item1)
                {
                    if ((Piece.IsWhite(Squares[targetSquare]) != WhiteToMove) || Squares[targetSquare] == Piece.None)
                    {
                        pseudoMoves.Add(new Move(square, PrecomputedMoveData.knightMoveTable[square][directionIndex].Item2));
                    }

                }
            }
        }

        void GeneratePseudoPawnMoves(int square, int[] Squares, bool WhiteToMove, int enPassantSquare)
        {

            int friendlyRank = WhiteToMove ? 6 : 1; // Starting rank
            int directionOffset = WhiteToMove ? -8 : 8; // Offset for moving
            int targetSquare = square + directionOffset; // Target square
            int captureTargetSquareLeft = targetSquare - 1;
            int captureTargetSquareRight = targetSquare + 1;
            int promotionRank = WhiteToMove ? 1 : 6; // Rank for promotion
            int currentRank = square / 8;


            // If the following square is empty then the pawn can move
            if (Squares[targetSquare] == Piece.None)
            {
                // If the pawn is on he promotion rank
                if (currentRank == promotionRank)
                {
                    // One move for each promotion
                    pseudoMoves.Add(new Move(square, targetSquare, 0b0100));
                    pseudoMoves.Add(new Move(square, targetSquare, 0b0101));
                    pseudoMoves.Add(new Move(square, targetSquare, 0b0110));
                    pseudoMoves.Add(new Move(square, targetSquare, 0b0111));
                }
                else
                {
                    pseudoMoves.Add(new Move(square, targetSquare));
                    // Pawn can move two squares
                    if (currentRank == friendlyRank && Squares[targetSquare + directionOffset] == 0)
                    {
                        pseudoMoves.Add(new Move(square, targetSquare + directionOffset, 0b0011));
                    }
                }
            }

            // Pawn capture left
            if (PrecomputedMoveData.pawnCaptureTable[square][0] && !Piece.IsColour(Squares[captureTargetSquareLeft], WhiteToMove ? 0 : 8) && Squares[captureTargetSquareLeft] != 0)
            {
                if (currentRank == promotionRank)
                {
                    // One move for each promotion
                    pseudoMoves.Add(new Move(square, captureTargetSquareLeft, 0b0100));
                    pseudoMoves.Add(new Move(square, captureTargetSquareLeft, 0b0101));
                    pseudoMoves.Add(new Move(square, captureTargetSquareLeft, 0b0110));
                    pseudoMoves.Add(new Move(square, captureTargetSquareLeft, 0b0111));
                }
                else
                {
                    pseudoMoves.Add(new Move(square, captureTargetSquareLeft));
                }
            }
            // Pawn capture right
            if (PrecomputedMoveData.pawnCaptureTable[square][1] && !Piece.IsColour(Squares[captureTargetSquareRight], WhiteToMove ? 0 : 8) && Squares[captureTargetSquareRight] != 0)
            {
                if (currentRank == promotionRank)
                {
                    // One move for each promotion
                    pseudoMoves.Add(new Move(square, captureTargetSquareRight, 0b0100));
                    pseudoMoves.Add(new Move(square, captureTargetSquareRight, 0b0101));
                    pseudoMoves.Add(new Move(square, captureTargetSquareRight, 0b0110));
                    pseudoMoves.Add(new Move(square, captureTargetSquareRight, 0b0111));
                }
                else
                {
                    pseudoMoves.Add(new Move(square, captureTargetSquareRight));
                }
            }

            // En passant
            if (enPassantSquare == captureTargetSquareLeft && PrecomputedMoveData.pawnCaptureTable[square][0])
            {
                pseudoMoves.Add(new Move(square, captureTargetSquareLeft, 0b0001));


            }
            if (enPassantSquare == captureTargetSquareRight && PrecomputedMoveData.pawnCaptureTable[square][1])
            {
                pseudoMoves.Add(new Move(square, captureTargetSquareRight, 0b0001));
            }
        }

        void GenerateSlidingAttacks(int square, int piece, int[] Squares, ulong BitBoard)
        {

            int startDirIndex = Piece.IsOrthogonalSlider(piece) ? 0 : 4;
            int endDirIndex = Piece.IsDiagonalSlider(piece) ? 8 : 4;
            for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
            {
                for (int n = 0; n < PrecomputedMoveData.numSquaresToEdge[square][directionIndex]; n++)
                {

                    int targetSquare = square + DirectionOffSets[directionIndex] * (n + 1);
                    attackedSquares |= 1UL << targetSquare;

                    if (((BitBoard >> targetSquare) & 1) == 1)
                    {
                        break;
                    }
                }
            }
        }

        void GenerateKingAttacks(int square)
        {

            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                for (int n = 0; n < Math.Min(PrecomputedMoveData.numSquaresToEdge[square][directionIndex], 1); n++)
                {
                    int targetSquare = square + DirectionOffSets[directionIndex] * (n + 1);

                    attackedSquares |= 1UL << targetSquare;
                }
            }
        }

        void GenerateKnightAttacks(int square)
        {
            int targetSquare;
            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                targetSquare = PrecomputedMoveData.knightMoveTable[square][directionIndex].Item2;
                if (PrecomputedMoveData.knightMoveTable[square][directionIndex].Item1)
                {
                    attackedSquares |= 1UL << targetSquare;
                }
            }
        }

        void GeneratePawnAttacks(int square, bool WhiteToMove)
        {
            int directionOffset = WhiteToMove ? -8 : 8; // Offset for moving
            int targetSquare = square + directionOffset; // Target square
            int captureTargetSquareLeft = targetSquare - 1;
            int captureTargetSquareRight = targetSquare + 1;


            // Pawn attack left
            if (PrecomputedMoveData.pawnCaptureTable[square][0])
            {
                attackedSquares |= 1UL << captureTargetSquareLeft;
            }
            // Pawn attack right
            if (PrecomputedMoveData.pawnCaptureTable[square][1])
            {
                attackedSquares |= 1UL << captureTargetSquareRight;
            }
        }

        void GenerateSlidingCaptures(int square, int piece, int[] Squares, bool inCheck, int colour, int oppositeColour)
        {
            int startDirIndex;
            int endDirIndex;
            bool isPinned = (pinnedSquares & (1UL << square)) != 0;
            int pinnedDirectionIndex = pinnedSquareDirectionIndexes[square];


            // Get the correct startDirIndex and endDirIndex
            if (Piece.PieceType(piece) == Piece.Rook)
            {

                if (isPinned && pinnedDirectionIndex > 3)
                {
                    return;
                }
                startDirIndex = 0;
            }
            else
            {
                startDirIndex = 4;
            }
            if (Piece.PieceType(piece) == Piece.Bishop)
            {
                if (isPinned && pinnedDirectionIndex < 4)
                {
                    return;
                }
                endDirIndex = 8;
            }
            else
            {
                endDirIndex = 4;
            }
            if (Piece.PieceType(piece) == Piece.Queen)
            {
                startDirIndex = 0;
                endDirIndex = 8;
            }


            if (!isPinned)
            {
                for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
                {

                    for (int n = 0; n < PrecomputedMoveData.numSquaresToEdge[square][directionIndex]; n++)
                    {

                        int targetSquare = square + DirectionOffSets[directionIndex] * (n + 1);
                        int pieceOnTargetSquare = Squares[targetSquare];

                        // Blocked by a friendly piece

                        if (Piece.IsColour(pieceOnTargetSquare, colour))
                        {
                            break;
                        }

                        // Captures only
                        if (pieceOnTargetSquare != Piece.None)
                        {
                            // Assuming not pinned
                            if (inCheck)
                            {
                                if ((checkBitBoard & (1Ul << targetSquare)) != 0)
                                {
                                    generatedMoves.Add(new Move(square, targetSquare));
                                }
                            }
                            else
                            {
                                generatedMoves.Add(new Move(square, targetSquare));
                            }

                        }

                        // Blocked by opponent piece
                        if (Piece.IsColour(pieceOnTargetSquare, oppositeColour))
                        {
                            break;
                        }
                    }
                }
            }
            else if (!inCheck)
            {

                // move "along" pinned direction
                int targetSquare = square + DirectionOffSets[pinnedDirectionIndex];
                while ((pinnedSquares & (1UL << targetSquare)) != 0 && 63 >= targetSquare && targetSquare >= 0 && pinnedDirectionIndex == pinnedSquareDirectionIndexes[targetSquare])
                {

                    if (Squares[targetSquare] != Piece.None)
                    {
                        generatedMoves.Add(new Move(square, targetSquare));
                    }

                    targetSquare += DirectionOffSets[pinnedDirectionIndex];
                }
                targetSquare = square - DirectionOffSets[pinnedDirectionIndex];
                while ((pinnedSquares & (1UL << targetSquare)) != 0 && 63 >= targetSquare && targetSquare >= 0 && pinnedDirectionIndex == pinnedSquareDirectionIndexes[targetSquare])
                {
                    if (Squares[targetSquare] != Piece.None)
                    {
                        generatedMoves.Add(new Move(square, targetSquare));
                    }
                    targetSquare -= DirectionOffSets[pinnedDirectionIndex];
                }
            }
        }

        void GenerateKingCaptures(int square, int[] Squares, int colour)
        {

            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                if (PrecomputedMoveData.numSquaresToEdge[square][directionIndex] != 0)
                {
                    int targetSquare = square + DirectionOffSets[directionIndex];
                    int pieceOnTargetSquare = Squares[targetSquare];

                    // Blocked by friendly piece or illegal move
                    if (pieceOnTargetSquare == Piece.None || Piece.IsColour(pieceOnTargetSquare, colour) || (attackedSquares & (1UL << targetSquare)) != 0)
                    {
                        continue;
                    }
                    generatedMoves.Add(new Move(square, targetSquare));
                }
            }
        }

        void GenerateKnightCaptures(int square, int[] Squares, bool inCheck, int colour)
        {
            int targetSquare;
            if ((pinnedSquares & (1Ul << square)) == 0)
            {
                for (int directionIndex = 0; directionIndex < 8; directionIndex++)
                {
                    targetSquare = PrecomputedMoveData.knightMoveTable[square][directionIndex].Item2;
                    if (PrecomputedMoveData.knightMoveTable[square][directionIndex].Item1)
                    {
                        if (!Piece.IsColour(Squares[targetSquare], colour) && Squares[targetSquare] != Piece.None)
                        {
                            if (!inCheck)
                            {
                                generatedMoves.Add(new Move(square, targetSquare));
                            }
                            else if ((checkBitBoard & (1UL << targetSquare)) != 0)
                            {
                                generatedMoves.Add(new Move(square, targetSquare));
                            }
                        }
                    }
                }
            }
        }

        void GeneratePawnCaptures(Board board, int square, int[] Squares, bool WhiteToMove, int enPassantSquare, bool inCheck, int colour)
        {
            int directionOffset = WhiteToMove ? -8 : 8; // Offset for moving
            int targetSquare = square + directionOffset; // Target square
            int captureTargetSquareLeft = targetSquare - 1;
            int captureTargetSquareRight = targetSquare + 1;
            int promotionRank = WhiteToMove ? 1 : 6; // Rank for promotion
            int currentRank = square / 8;
            bool isPinned = (pinnedSquares & (1UL << square)) != 0;
            int pinnedDirectionIndex = pinnedSquareDirectionIndexes[square];
            bool targetSquareIsChecking = (checkBitBoard & (1UL << targetSquare)) != 0;
            bool rightSquareIsChecking = (checkBitBoard & (1UL << captureTargetSquareRight)) != 0;
            bool leftSquareIsChecking = (checkBitBoard & (1UL << captureTargetSquareLeft)) != 0;


            if (!isPinned)
            {

                if (inCheck)
                {
                    // Pawn capture left
                    if (PrecomputedMoveData.pawnCaptureTable[square][0] && !Piece.IsColour(Squares[captureTargetSquareLeft], colour) && Squares[captureTargetSquareLeft] != 0 && leftSquareIsChecking)
                    {
                        if (currentRank == promotionRank)
                        {
                            // One move for each promotion
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0100));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0101));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0110));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0111));
                        }
                        else
                        {
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft));
                        }
                    }
                    // Pawn capture right
                    if (PrecomputedMoveData.pawnCaptureTable[square][1] && !Piece.IsColour(Squares[captureTargetSquareRight], colour) && Squares[captureTargetSquareRight] != 0 && rightSquareIsChecking)
                    {
                        if (currentRank == promotionRank)
                        {
                            // One move for each promotion
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0100));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0101));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0110));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0111));
                        }
                        else
                        {
                            generatedMoves.Add(new Move(square, captureTargetSquareRight));
                        }
                    }

                    if (enPassantSquare == captureTargetSquareLeft && PrecomputedMoveData.pawnCaptureTable[square][0] && FilterMove(board, new Move(square, captureTargetSquareLeft, 0b0001)))
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0001));
                    }
                    if (enPassantSquare == captureTargetSquareRight && PrecomputedMoveData.pawnCaptureTable[square][1] && FilterMove(board, new Move(square, captureTargetSquareRight, 0b0001)))
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0001));
                    }
                }
                else
                {
                    // Pawn capture left
                    if (PrecomputedMoveData.pawnCaptureTable[square][0] && !Piece.IsColour(Squares[captureTargetSquareLeft], colour) && Squares[captureTargetSquareLeft] != 0)
                    {
                        if (currentRank == promotionRank)
                        {
                            // One move for each promotion
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0100));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0101));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0110));
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0111));
                        }
                        else
                        {
                            generatedMoves.Add(new Move(square, captureTargetSquareLeft));
                        }
                    }
                    // Pawn capture right
                    if (PrecomputedMoveData.pawnCaptureTable[square][1] && !Piece.IsColour(Squares[captureTargetSquareRight], colour) && Squares[captureTargetSquareRight] != 0)
                    {
                        if (currentRank == promotionRank)
                        {
                            // One move for each promotion
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0100));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0101));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0110));
                            generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0111));
                        }
                        else
                        {
                            generatedMoves.Add(new Move(square, captureTargetSquareRight));
                        }
                    }

                    // En passant
                    if (enPassantSquare == captureTargetSquareLeft && PrecomputedMoveData.pawnCaptureTable[square][0] && FilterMove(board, new Move(square, captureTargetSquareLeft, 0b0001)))
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0001));
                    }
                    if (enPassantSquare == captureTargetSquareRight && PrecomputedMoveData.pawnCaptureTable[square][1] && FilterMove(board, new Move(square, captureTargetSquareRight, 0b0001)))
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0001));
                    }
                }
            }
            else if (!inCheck)
            {
                // Pawn capture left
                if (PrecomputedMoveData.pawnCaptureTable[square][0] && !Piece.IsColour(Squares[captureTargetSquareLeft], colour) && Squares[captureTargetSquareLeft] != 0 && ((pinnedDirectionIndex == 7 && !WhiteToMove) || (pinnedDirectionIndex == 4 && WhiteToMove)))
                {
                    if (currentRank == promotionRank)
                    {
                        // One move for each promotion
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0100));
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0101));
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0110));
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0111));
                    }
                    else
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareLeft));
                    }
                }

                // Pawn capture right
                if (PrecomputedMoveData.pawnCaptureTable[square][1] && !Piece.IsColour(Squares[captureTargetSquareRight], colour) && Squares[captureTargetSquareRight] != 0 && ((pinnedDirectionIndex == 5 && !WhiteToMove) || (pinnedDirectionIndex == 6 && WhiteToMove)))
                {
                    if (currentRank == promotionRank)
                    {
                        // One move for each promotion
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0100));
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0101));
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0110));
                        generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0111));
                    }
                    else
                    {
                        generatedMoves.Add(new Move(square, captureTargetSquareRight));
                    }
                }

                if (enPassantSquare == captureTargetSquareLeft && PrecomputedMoveData.pawnCaptureTable[square][0] && FilterMove(board, new Move(square, captureTargetSquareLeft, 0b0001)))
                {
                    generatedMoves.Add(new Move(square, captureTargetSquareLeft, 0b0001));
                }
                if (enPassantSquare == captureTargetSquareRight && PrecomputedMoveData.pawnCaptureTable[square][1] && FilterMove(board, new Move(square, captureTargetSquareRight, 0b0001)))
                {
                    generatedMoves.Add(new Move(square, captureTargetSquareRight, 0b0001));
                }

            }
        }

        public bool IsInCheck => numChecks > 0;
        public static bool CanPromotePawn(ulong PawnBitBoard, bool WhiteToMove) => (PawnBitBoard & (WhiteToMove ? WhitePromotionMask : BlackPromotionMask)) != 0;
    }
}