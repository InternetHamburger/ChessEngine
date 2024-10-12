namespace ChessEngine.src
{

    /// <summary>
    /// Contains functions for parsing moves, representing board state etc
    /// </summary>
    public class Board
    {
        public static readonly string startingPos = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        // The board
        public int[] Squares;

        // Bitboards, Use the piecetype as an index (whitepawn = 1, blackpawn = 9, etc)
        public ulong[] BitBoards;

        // Player to move
        public bool WhiteToMove;

        // Castling rights
        public bool WhiteCanCastleKingSide;
        public bool WhiteCanCastleQueenSide;
        public bool BlackCanCastleKingSide;
        public bool BlackCanCastleQueenSide;

        readonly Zobrist zobrist;

        // white king [0] black king [1]
        public int[] KingSquares;

        // Keep track of repetitions for detecting threefold repetition
        public List<ulong> Repetitions;
        public ulong ZobristHash;

        public int PlyCount;

        // Movestack
        // (piece moved, piece captured (0 if not a capture), move value)
        public List<(int, int, ushort)> MoveStack;

        // En passant square stack
        public List<int> enPassantSquareStack;

        // White kingside, white queenside, ... black queenside
        private List<(bool, bool, bool, bool)> castlingMoveStack;

        /// <summary>
        ///  Board constructor
        ///  Optional argument for position
        /// </summary>
        public Board(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
        {
            Repetitions = new List<ulong>();
            BitBoards = new ulong[15];
            castlingMoveStack = new List<(bool, bool, bool, bool)>();
            KingSquares = new int[2];


            zobrist = new Zobrist();


            string[] tokens = fen.Split(' ');
            WhiteToMove = tokens[1][0] == 'w';


            // Set the squares
            Squares = new int[64];
            for (int i = 0; i < Squares.Length; i++)
            {
                Squares[i] = 0;
            }
            // Initialize move stack
            MoveStack = [];
            enPassantSquareStack = [];


            SetBoard(tokens);
        }

        /// <summary>
        /// Sets the board to the fen
        /// </summary>
        public void SetBoard(string[] tokens)
        {
            PlyCount = 0;
            Repetitions = new List<ulong>();
            BitBoards = new ulong[15];
            ZobristHash = 0UL;
            // Reset the board
            // Set the squares
            Squares = new int[64];
            for (int i = 0; i < Squares.Length; i++)
            {
                Squares[i] = 0;
            }
            // Initialize move stack
            MoveStack = [];
            enPassantSquareStack = [];
            castlingMoveStack = [];
            WhiteToMove = tokens[1][0] == 'w';

            WhiteCanCastleKingSide = false;
            WhiteCanCastleQueenSide = false;
            BlackCanCastleKingSide = false;
            BlackCanCastleQueenSide = false;


            string purefen = tokens[0].Replace("/", "");

            int absoluteIndex = 0;
            int fenIndex = 0;

            while (fenIndex < purefen.Length)
            {
                // Is the current char in "fen" a number
                if (int.TryParse(purefen[fenIndex].ToString(), out int num))
                {
                    fenIndex++;
                    absoluteIndex += num;
                }
                // It is not a num (it is a char/piece)
                else
                {
                    // Current Piece
                    int currentPiece = Piece.GetPieceTypeFromSymbol(purefen[fenIndex]) | (char.IsLower(purefen[fenIndex]) ? 8 : 0);


                    if (currentPiece == Piece.WhiteKing)
                    {
                        KingSquares[0] = absoluteIndex;
                    }

                    else if (currentPiece == Piece.BlackKing)
                    {
                        KingSquares[1] = absoluteIndex;
                    }

                    BitBoards[currentPiece] |= 1UL << absoluteIndex;
                    Squares[absoluteIndex] = currentPiece;

                    // Use the piece as an index
                    ZobristHash ^= zobrist.SquareZobristValues[currentPiece, absoluteIndex];

                    absoluteIndex++;
                    fenIndex++;
                }
            }
            for (int i = 0; i < tokens[2].Length; i++)
            {
                switch (tokens[2][i])
                {
                    case 'K':
                        WhiteCanCastleKingSide = true;
                        ZobristHash ^= zobrist.WhiteCastleKingSide;
                        break;
                    case 'Q':
                        WhiteCanCastleQueenSide = true;
                        ZobristHash ^= zobrist.WhiteCastleQueenSide;
                        break;
                    case 'k':
                        BlackCanCastleKingSide = true;
                        ZobristHash ^= zobrist.BlackCastleKingSide;
                        break;
                    case 'q':
                        BlackCanCastleQueenSide = true;
                        ZobristHash ^= zobrist.BlackCastleQueenSide;
                        break;
                    case '-':
                        break;
                    default:
                        throw new Exception("Error: Invalid castling rights in fen");
                }
            }

            castlingMoveStack.Add((WhiteCanCastleKingSide, WhiteCanCastleQueenSide, BlackCanCastleKingSide, BlackCanCastleQueenSide));
            // En passant square
            if (tokens[3] == "-")
            {
                enPassantSquareStack.Add(-100);
            }
            else
            {
                try
                {
                    int enPassantSquare = Square.StringToInt(tokens[3]);
                    enPassantSquareStack.Add(enPassantSquare);
                    ZobristHash ^= zobrist.EnPassantFiles[enPassantSquare % 8];
                }
                catch
                {
                    throw new Exception("Error: Invalid en passant square in fen");
                }
            }

            if (!WhiteToMove)
            {
                ZobristHash ^= zobrist.SideToMove;
            }
            Repetitions.Add(ZobristHash);
        }


        string GetFen()
        {
            string CastlingRights = "";
            string Position = "";
            string EnPassantSquare;
            string SideToMove;


            if (WhiteCanCastleKingSide)
            {
                CastlingRights += "K";
            }
            if (WhiteCanCastleQueenSide)
            {
                CastlingRights += "Q";
            }
            if (BlackCanCastleKingSide)
            {
                CastlingRights += "k";
            }
            if (BlackCanCastleQueenSide)
            {
                CastlingRights += "q";
            }
            if (CastlingRights == "")
            {
                CastlingRights = "-";
            }

            SideToMove = WhiteToMove ? "w" : "b";

            if (enPassantSquareStack[^1] > 0)
            {
                EnPassantSquare = Square.IntToString(enPassantSquareStack[^1]);
            }
            else
            {
                EnPassantSquare = "-";
            }

            int absIndex = 0;
            int addIndex = 0;
            bool hasSeenPiece = false;
            for (int rank = 0; rank < 8; rank++)
            {
                addIndex = 0;
                for (int file = 0; file < 8; file++)
                {
                    if (Squares[absIndex] != 0)
                    {

                        if (addIndex != 0)
                        {
                            Position += addIndex.ToString();
                        }

                        hasSeenPiece = true;
                        Position += Piece.GetSymbol(Squares[absIndex]);
                        addIndex = 0;
                    }
                    else
                    {
                        addIndex++;
                    }
                    absIndex++;
                    if (file == 7)
                    {
                        if (!hasSeenPiece)
                        {
                            Position += "8";
                        }
                        else if (addIndex != 0)
                        {
                            Position += addIndex.ToString();
                        }
                    }

                }
                hasSeenPiece = false;
                if (rank != 7)
                {
                    Position += "/";
                }
            }
            return Position + " " + SideToMove + " " + CastlingRights + " " + EnPassantSquare + " 0 1";
        }


        /// <summary>
        /// Prints a basic ascii representation of the board
        /// </summary>
        public void PrintBoard()
        {
            char[] b = new char[64];

            for (int i = 0; i < 64; i++)
            {
                b[i] = Piece.GetSymbol(Squares[i]);
            }

            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[0]} | {b[1]} | {b[2]} | {b[3]} | {b[4]} | {b[5]} | {b[6]} | {b[7]} | 8");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[8]} | {b[9]} | {b[10]} | {b[11]} | {b[12]} | {b[13]} | {b[14]} | {b[15]} | 7");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[16]} | {b[17]} | {b[18]} | {b[19]} | {b[20]} | {b[21]} | {b[22]} | {b[23]} | 6");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[24]} | {b[25]} | {b[26]} | {b[27]} | {b[28]} | {b[29]} | {b[30]} | {b[31]} | 5");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[32]} | {b[33]} | {b[34]} | {b[35]} | {b[36]} | {b[37]} | {b[38]} | {b[39]} | 4");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[40]} | {b[41]} | {b[42]} | {b[43]} | {b[44]} | {b[45]} | {b[46]} | {b[47]} | 3");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[48]} | {b[49]} | {b[50]} | {b[51]} | {b[52]} | {b[53]} | {b[54]} | {b[55]} | 2");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[56]} | {b[57]} | {b[58]} | {b[59]} | {b[60]} | {b[61]} | {b[62]} | {b[63]} | 1");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"  a   b   c   d   e   f   g   h\n");

        }


        public void D()
        {
            Console.WriteLine();
            PrintBoard();

            Console.WriteLine("Fen: " + GetFen());
            Console.WriteLine("Key: " + ZobristHash);
        }


        public static void PrintBitBoard(ulong bitboard)
        {
            char[] b = new char[64];
            for (int i = 0; i < 64; i++)
            {
                b[i] = (bitboard >> i & 1) == 0 ? ' ' : '1';
            }

            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[0]} | {b[1]} | {b[2]} | {b[3]} | {b[4]} | {b[5]} | {b[6]} | {b[7]} | 8");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[8]} | {b[9]} | {b[10]} | {b[11]} | {b[12]} | {b[13]} | {b[14]} | {b[15]} | 7");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[16]} | {b[17]} | {b[18]} | {b[19]} | {b[20]} | {b[21]} | {b[22]} | {b[23]} | 6");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[24]} | {b[25]} | {b[26]} | {b[27]} | {b[28]} | {b[29]} | {b[30]} | {b[31]} | 5");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[32]} | {b[33]} | {b[34]} | {b[35]} | {b[36]} | {b[37]} | {b[38]} | {b[39]} | 4");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[40]} | {b[41]} | {b[42]} | {b[43]} | {b[44]} | {b[45]} | {b[46]} | {b[47]} | 3");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[48]} | {b[49]} | {b[50]} | {b[51]} | {b[52]} | {b[53]} | {b[54]} | {b[55]} | 2");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"| {b[56]} | {b[57]} | {b[58]} | {b[59]} | {b[60]} | {b[61]} | {b[62]} | {b[63]} | 1");
            Console.WriteLine($"+---+---+---+---+---+---+---+---+");
            Console.WriteLine($"  a   b   c   d   e   f   g   h\n\n");
        }

        /// <summary>
        /// Makes a move on the board and puts it on the move stack
        /// </summary>
        /// <param name="move"></param>
        public void MakeMove(Move move)
        {
            PlyCount++;
            int currentPiece = Squares[move.StartSquare];
            int targetPiece = Squares[move.TargetSquare];

            BitBoards[currentPiece] ^= 1UL << move.StartSquare | 1UL << move.TargetSquare;


            ZobristHash ^= zobrist.SideToMove;
            ZobristHash ^= zobrist.SquareZobristValues[currentPiece, move.StartSquare];
            ZobristHash ^= zobrist.SquareZobristValues[currentPiece, move.TargetSquare];

            // Move is a capture
            if (targetPiece != 0)
            {
                BitBoards[targetPiece] ^= 1UL << move.TargetSquare;
                ZobristHash ^= zobrist.SquareZobristValues[targetPiece, move.TargetSquare];
            }

            if (enPassantSquareStack[^1] > 0)
            {
                ZobristHash ^= zobrist.EnPassantFiles[enPassantSquareStack[^1] % 8];
            }

            castlingMoveStack.Add((WhiteCanCastleKingSide, WhiteCanCastleQueenSide, BlackCanCastleKingSide, BlackCanCastleQueenSide));

            if (Piece.PieceType(currentPiece) == Piece.King)
            {
                if (WhiteToMove)
                {
                    WhiteCanCastleKingSide = false;
                    WhiteCanCastleQueenSide = false;
                }
                else
                {
                    BlackCanCastleKingSide = false;
                    BlackCanCastleQueenSide = false;
                }
                KingSquares[WhiteToMove ? 0 : 1] = move.TargetSquare;
            }

            MoveStack.Add((currentPiece, targetPiece, move.Value));

            // Set the en passant square to the correct value
            if (move.MoveFlag == Move.PawnTwoUpFlag)
            {
                ZobristHash ^= zobrist.EnPassantFiles[move.TargetSquare % 8];
                enPassantSquareStack.Add(move.TargetSquare + (WhiteToMove ? 8 : -8));
            }
            else
            {
                enPassantSquareStack.Add(-100);
            }


            UpdateCastlingRights(move.TargetSquare);
            UpdateCastlingRights(move.StartSquare);


            // Promotion is handled later
            Squares[move.TargetSquare] = currentPiece;
            Squares[move.StartSquare] = Piece.None;


            // Promotion
            if (move.IsPromotion)
            {
                int promotionPiece = Piece.MakePiece(move.PromotionPieceType, WhiteToMove);

                ZobristHash ^= zobrist.SquareZobristValues[currentPiece, move.TargetSquare];
                ZobristHash ^= zobrist.SquareZobristValues[promotionPiece, move.TargetSquare];

                BitBoards[currentPiece] ^= 1UL << move.TargetSquare;
                BitBoards[promotionPiece] ^= 1UL << move.TargetSquare;

                Squares[move.TargetSquare] = promotionPiece;
            }
            // En passant
            if (move.MoveFlag == Move.EnPassantCaptureFlag)
            {
                int capturedPawn = WhiteToMove ? Piece.BlackPawn : Piece.WhitePawn;
                int enPassantSquare = move.TargetSquare + (WhiteToMove ? 8 : -8);
                ZobristHash ^= zobrist.SquareZobristValues[capturedPawn, enPassantSquare];
                BitBoards[capturedPawn] ^= 1UL << enPassantSquare;
                Squares[enPassantSquare] = Piece.None;
            }
            // Castling
            if (move.MoveFlag == Move.CastleFlag)
            {
                if (move.TargetSquare == 62)
                {

                    ZobristHash ^= zobrist.SquareZobristValues[Piece.WhiteRook, 63];
                    ZobristHash ^= zobrist.SquareZobristValues[Piece.WhiteRook, 61];

                    BitBoards[Piece.WhiteRook] ^= 1UL << 63;
                    BitBoards[Piece.WhiteRook] ^= 1UL << 61;

                    Squares[61] = Squares[63];
                    Squares[63] = Piece.None;
                    WhiteCanCastleKingSide = false;
                }
                else if (move.TargetSquare == 58)
                {
                    ZobristHash ^= zobrist.SquareZobristValues[Piece.WhiteRook, 56];
                    ZobristHash ^= zobrist.SquareZobristValues[Piece.WhiteRook, 59];

                    BitBoards[Piece.WhiteRook] ^= 1UL << 56;
                    BitBoards[Piece.WhiteRook] ^= 1UL << 59;

                    Squares[59] = Squares[56];
                    Squares[56] = Piece.None;
                    WhiteCanCastleQueenSide = false;
                }

                else if (move.TargetSquare == 6)
                {
                    ZobristHash ^= zobrist.SquareZobristValues[Piece.BlackRook, 7];
                    ZobristHash ^= zobrist.SquareZobristValues[Piece.BlackRook, 5];

                    BitBoards[Piece.BlackRook] ^= 1UL << 7;
                    BitBoards[Piece.BlackRook] ^= 1UL << 5;

                    Squares[5] = Squares[7];
                    Squares[7] = Piece.None;
                    BlackCanCastleKingSide = false;
                }
                else if (move.TargetSquare == 2)
                {
                    ZobristHash ^= zobrist.SquareZobristValues[Piece.BlackRook, 0];
                    ZobristHash ^= zobrist.SquareZobristValues[Piece.BlackRook, 3];

                    BitBoards[Piece.BlackRook] ^= 1UL << 0;
                    BitBoards[Piece.BlackRook] ^= 1UL << 3;

                    Squares[3] = Squares[0];
                    Squares[0] = Piece.None;
                    BlackCanCastleQueenSide = false;
                }
            }
            var LastCastlingRights = castlingMoveStack[^1];

            if (WhiteCanCastleKingSide != LastCastlingRights.Item1) ZobristHash ^= zobrist.WhiteCastleKingSide;
            if (WhiteCanCastleQueenSide != LastCastlingRights.Item2) ZobristHash ^= zobrist.WhiteCastleQueenSide;
            if (BlackCanCastleKingSide != LastCastlingRights.Item3) ZobristHash ^= zobrist.BlackCastleKingSide;
            if (BlackCanCastleQueenSide != LastCastlingRights.Item4) ZobristHash ^= zobrist.BlackCastleQueenSide;

            WhiteToMove = !WhiteToMove;

            Repetitions.Add(ZobristHash);
        }

        public void UndoMove()
        {
            PlyCount--;


            var lastCastlingRights = castlingMoveStack[^1];
            var lastMoveInfo = MoveStack[^1];
            Move lastMove = new(lastMoveInfo.Item3);
            if (WhiteCanCastleKingSide != lastCastlingRights.Item1) ZobristHash ^= zobrist.WhiteCastleKingSide;
            if (WhiteCanCastleQueenSide != lastCastlingRights.Item2) ZobristHash ^= zobrist.WhiteCastleQueenSide;
            if (BlackCanCastleKingSide != lastCastlingRights.Item3) ZobristHash ^= zobrist.BlackCastleKingSide;
            if (BlackCanCastleQueenSide != lastCastlingRights.Item4) ZobristHash ^= zobrist.BlackCastleQueenSide;

            // Directly set castling rights after XOR operations
            (WhiteCanCastleKingSide, WhiteCanCastleQueenSide, BlackCanCastleKingSide, BlackCanCastleQueenSide) = lastCastlingRights;


            WhiteToMove = !WhiteToMove;
            castlingMoveStack.RemoveAt(castlingMoveStack.Count - 1);
            enPassantSquareStack.RemoveAt(enPassantSquareStack.Count - 1);

            if (lastMove.IsPromotion)
            {
                BitBoards[WhiteToMove ? Piece.WhitePawn : Piece.BlackPawn] ^= 1UL << lastMove.TargetSquare;
                BitBoards[Piece.MakePiece(lastMove.PromotionPieceType, WhiteToMove)] ^= 1UL << lastMove.TargetSquare;
                ZobristHash ^= zobrist.SquareZobristValues[WhiteToMove ? Piece.WhitePawn : Piece.BlackPawn, lastMove.TargetSquare];
                ZobristHash ^= zobrist.SquareZobristValues[Piece.MakePiece(lastMove.PromotionPieceType, WhiteToMove), lastMove.TargetSquare];
            }


            int lastEnPassantSquare = enPassantSquareStack[^1];

            if (lastMove.MoveFlag == Move.PawnTwoUpFlag)
            {
                ZobristHash ^= zobrist.EnPassantFiles[lastMove.TargetSquare % 8];
            }

            if (lastEnPassantSquare > 0)
            {
                ZobristHash ^= zobrist.EnPassantFiles[lastEnPassantSquare % 8];
            }

            BitBoards[lastMoveInfo.Item1] ^= 1UL << lastMove.StartSquare;
            BitBoards[lastMoveInfo.Item1] ^= 1UL << lastMove.TargetSquare;
            Squares[lastMove.StartSquare] = lastMoveInfo.Item1;
            Squares[lastMove.TargetSquare] = lastMoveInfo.Item2;

            ZobristHash ^= zobrist.SideToMove;
            ZobristHash ^= zobrist.SquareZobristValues[lastMoveInfo.Item1, lastMove.StartSquare];
            ZobristHash ^= zobrist.SquareZobristValues[lastMoveInfo.Item1, lastMove.TargetSquare];


            // Move is a capture
            if (lastMoveInfo.Item2 != 0)
            {
                BitBoards[lastMoveInfo.Item2] ^= 1UL << lastMove.TargetSquare;
                ZobristHash ^= zobrist.SquareZobristValues[lastMoveInfo.Item2, lastMove.TargetSquare];
            }
            if (Piece.PieceType(Squares[lastMove.StartSquare]) == Piece.King)
            {
                KingSquares[WhiteToMove ? 0 : 1] = lastMove.StartSquare;
            }


            // Last move was en passant
            if (lastMove.MoveFlag == Move.EnPassantCaptureFlag)
            {
                int CapturedPawn = WhiteToMove ? Piece.BlackPawn : Piece.WhitePawn;
                int enPassantSquare = lastEnPassantSquare + (WhiteToMove ? 8 : -8);
                BitBoards[CapturedPawn] ^= 1Ul << enPassantSquare;
                ZobristHash ^= zobrist.SquareZobristValues[CapturedPawn, enPassantSquare];
                Squares[enPassantSquare] = CapturedPawn;
            }
            if (lastMove.MoveFlag == Move.CastleFlag)
            {
                switch (lastMove.TargetSquare)
                {
                    case 62:
                        BitBoards[Piece.WhiteRook] ^= 1UL << 63;
                        BitBoards[Piece.WhiteRook] ^= 1UL << 61;
                        HandleCastle(61, 63, Piece.WhiteRook);
                        break;
                    case 58:
                        BitBoards[Piece.WhiteRook] ^= 1UL << 56;
                        BitBoards[Piece.WhiteRook] ^= 1UL << 59;
                        HandleCastle(59, 56, Piece.WhiteRook);
                        break;
                    case 6:
                        BitBoards[Piece.BlackRook] ^= 1UL << 7;
                        BitBoards[Piece.BlackRook] ^= 1UL << 5;
                        HandleCastle(5, 7, Piece.BlackRook);
                        break;
                    case 2:
                        BitBoards[Piece.BlackRook] ^= 1UL << 0;
                        BitBoards[Piece.BlackRook] ^= 1UL << 3;
                        HandleCastle(3, 0, Piece.BlackRook);
                        break;
                }
            }

            Repetitions.RemoveAt(Repetitions.Count - 1);
            MoveStack.RemoveAt(MoveStack.Count - 1);
        }



        void UpdateCastlingRights(int square)
        {
            switch (square)
            {
                case 63: WhiteCanCastleKingSide = false; break;
                case 56: WhiteCanCastleQueenSide = false; break;
                case 7: BlackCanCastleKingSide = false; break;
                case 0: BlackCanCastleQueenSide = false; break;
                default: break;
            }
        }

        void HandleCastle(int rookStart, int rookEnd, int piece)
        {
            ZobristHash ^= zobrist.SquareZobristValues[piece, rookStart];
            ZobristHash ^= zobrist.SquareZobristValues[piece, rookEnd];
            Squares[rookEnd] = piece;
            Squares[rookStart] = Piece.None;
        }

        public bool IsTwofoldRepetition()
        {
            // Two-fold repetition
            if (Repetitions.Count(pos => pos == ZobristHash) >= 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string Fen => GetFen();

        public ulong AllPieceBitBoard => BitBoards[Piece.WhitePawn] | BitBoards[Piece.BlackPawn] | BitBoards[Piece.WhiteKnight] | BitBoards[Piece.BlackKnight] | BitBoards[Piece.WhiteBishop] | BitBoards[Piece.BlackBishop] | BitBoards[Piece.WhiteRook] | BitBoards[Piece.BlackRook] | BitBoards[Piece.WhiteQueen] | BitBoards[Piece.BlackQueen] | BitBoards[Piece.WhiteKing] | BitBoards[Piece.BlackKing];
        public ulong WhitePieceBitBoard => BitBoards[Piece.WhitePawn] | BitBoards[Piece.WhiteKnight] | BitBoards[Piece.WhiteBishop] | BitBoards[Piece.WhiteRook] | BitBoards[Piece.WhiteQueen] | BitBoards[Piece.WhiteKing];
        public ulong BlackPieceBitBoard => BitBoards[Piece.BlackPawn] |  BitBoards[Piece.BlackKnight] |  BitBoards[Piece.BlackBishop] |  BitBoards[Piece.BlackRook] |  BitBoards[Piece.BlackQueen] |  BitBoards[Piece.BlackKing];
    }
}