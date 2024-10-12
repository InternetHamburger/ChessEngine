using ChessEngine.src;
using System.Diagnostics;

namespace ChessEngine
{
    struct TTEntry
    {
        public int Score;
        public ushort Flag;  // Stores both depth and move flag
        public ushort BestMove; // Store the best move, In case the value can't be used we can search the best move form previous iteration first

        // Constants for bit masking
        public const ushort DepthMask = 0b1111111111110000;  // 12 bits for depth (up to 4096)
        public const ushort FlagMask = 0b0000000000001111;   // 4 bits for flag

        // Flag types
        public const ushort NullFlag = 0b0000;
        public const ushort ExactFlag = 0b0001;
        public const ushort UpperBoundFlag = 0b0010;
        public const ushort LowerBoundFlag = 0b0011;
        public const ushort KillerMoveFlag = 0b0100;

        // Get depth from the Flag byte
        public readonly int Depth => (Flag & DepthMask) >> 4;

        // Check if entry is null
        public readonly bool IsNull => (Flag & FlagMask) == NullFlag;
        public readonly bool IsExact => (Flag & FlagMask) == ExactFlag;
        public readonly bool IsUpperBound => (Flag & FlagMask) == UpperBoundFlag;
        public readonly bool IsLowerBound => (Flag & FlagMask) == LowerBoundFlag;
        public readonly bool ContainsMove => BestMove != 0;

        // Constructor with score, depth, and flag
        public TTEntry(int score, int depth, ushort flag)
        {
            Score = score;
            BestMove = 0;
            // Combine depth (shifted) and flag into the Flag byte
            Flag = (ushort)((depth << 4) | (flag & FlagMask));
        }

        public TTEntry(int score, int depth, ushort flag, ushort move)
        {
            Score = score;
            BestMove = move;
            // Combine depth (shifted) and flag into the Flag byte
            Flag = (ushort)((depth << 4) | (flag & FlagMask));
        }

        // Constructor for initializing a null entry
        public TTEntry(int score, ushort flag, ushort move)
        {
            Score = score;
            BestMove = move;
            Flag = flag;
        }
    }


    class Engine
    {
        public ulong leafNodeCount;
        public ulong totalNodeCount;
        public ulong quiescentNodeCount;
        public int TotalDepth;

        public Evaluate evaluate;
        public readonly MoveGenerator moveGenerator;
        public Board board;

        public TTEntry[] TT;
        public Move[] KillerMoveTable;

        // Size in megabytes for the transposition table
        public const int MBsize = 128;
        public const int SizeEachEntryBits = 64;
        public const int TTMaxNumEntries = MBsize * 1024 * 1024 * 8 / SizeEachEntryBits;

        public const int negativeInf = int.MinValue + 1;
        public const int positiveInf = int.MaxValue - 1;

        public int GamePhase;
        public const int TotalPhase = 24;

        // Search time in ms
        public double searchTime;
        public double startTime;

        // The ply the search starts at
        public int TopPly;
        public bool BestPlay;
        public int Skill;

        public Engine()
        {
            // Store an arbitrary amount of killer moves, if crashes then can simply increase
            KillerMoveTable = new Move[256];
            TT = new TTEntry[TTMaxNumEntries];
            GamePhase = 0;
            evaluate = new Evaluate();
            moveGenerator = new MoveGenerator();
            board = new Board();
            totalNodeCount = 0;
            leafNodeCount = 0;
            quiescentNodeCount = 0;
            BestPlay = true;
        }

        public void Init()
        {
            GamePhase = 0;
            totalNodeCount = 0;
            leafNodeCount = 0;
            quiescentNodeCount = 0;
            TT = new TTEntry[TTMaxNumEntries];
            KillerMoveTable = new Move[256];
            TopPly = board.PlyCount;
            Skill = BestPlay ? 1 : -1;
        }


        public (Move, int) Search(int timeLimit, int depthLimit = int.MaxValue)
        {
            Init();
            DateTime dateTime = DateTime.Now;
            searchTime = timeLimit;
            startTime = dateTime.TimeOfDay.TotalMilliseconds;
            CalculateGamePhase();

            int bestEval = 0;
            TotalDepth = 1;
            List<Move> moves = moveGenerator.GenerateLegalMoves(board);

            if (moves.Count == 0)
            {
                throw new Exception("Tried search while in termial position");
            }


            moves = OrderMoves(moveGenerator.GenerateLegalMoves(board), Move.NullMove, Move.NullMove);

            Move bestMove = moves[0];
            while (true)
            {
                var outPut = SearchTop(TotalDepth, moves);

                // Only use unfinished search if has actually searched anything
                if (outPut.Item1.Value != 0)
                {
                    bestMove = outPut.Item1;
                    bestEval = outPut.Item2;
                }

                if (IsTimeUp)
                {
                    break;
                }

                moves = MoveElementToFront(moves, bestMove);



                PrintInfo(bestMove, bestEval);

                if (Math.Abs(bestEval) > 100000)
                {
                    bestEval += bestEval < 0 ? TopPly : -TopPly;
                    // Already found a checkmate, don't need to waste more time
                    break;
                }
                else if (TotalDepth >= depthLimit)
                {
                    break;
                }

                TotalDepth++;
            }


            return (bestMove, bestEval);
        }

        public (Move, int) SearchTop(int depth, List<Move> moves)
        {

            totalNodeCount++;
            int alpha = negativeInf;
            int beta = positiveInf;

            Move bestMove = Move.NullMove;

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int score = -NegaMax(depth - 1, -beta, -alpha);
                board.UndoMove();

                if (IsTimeUp)
                {
                    return (bestMove, alpha);
                }

                if (score > alpha)
                {
                    bestMove = move;
                    alpha = score;
                }

                if (score >= beta)
                {
                    return (bestMove, alpha);
                }
                alpha = Math.Max(alpha, score);
            }

            return (bestMove, alpha);
        }

        int NegaMax(int depth, int alpha, int beta, int NumExtensions = 0)
        {
            totalNodeCount++;

            ulong TTIndex = board.ZobristHash % TTMaxNumEntries;
            TTEntry ttEntry = TT[TTIndex];
            int Extension;


            if (ttEntry.IsExact && ttEntry.Depth >= depth)
            {
                return ttEntry.Score;
            }

            List<Move> moves = moveGenerator.GenerateLegalMoves(board);

            if (moves.Count == 0)
            {
                leafNodeCount++;
                if (moveGenerator.IsInCheck)
                {
                    // Checkmate
                    return (negativeInf + board.PlyCount) * Skill;
                }
                // Stalemate
                return 0;
            }
            if (board.IsTwofoldRepetition())
            {
                leafNodeCount++;
                return 0;
            }

            Extension = NumExtensions < 10 && moveGenerator.IsInCheck ? 1 : 0;
            if (depth + Extension <= 0)
            {
                // End of main search
                return Quiecence(alpha, beta);
            }
            if (IsTimeUp)
            {
                return 0;
            }


            Move bestMove;
            if (ttEntry.ContainsMove)
            {
                bestMove = new Move(ttEntry.BestMove);
                moves = OrderMoves(moves, bestMove, KillerMoveTable[board.PlyCount - TopPly]);
            }
            else
            {
                moves = OrderMoves(moves, Move.NullMove, KillerMoveTable[board.PlyCount - TopPly]);
                bestMove = moves[0];
            }


            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];

                int score = 0;
                bool NeedsFullSearch = true;
                board.MakeMove(move);


                // LMR - Late Move Reduction
                if (depth >= 3 && i >= 5 && Extension == 0 && board.Squares[move.TargetSquare] == 0)
                {
                    score = -NegaMax(depth - 2, -alpha - 1, -alpha, NumExtensions);

                    NeedsFullSearch = score > alpha;
                }

                if (NeedsFullSearch)
                {
                    score = -NegaMax(depth - 1 + Extension, -beta, -alpha, NumExtensions + Extension);

                }

                board.UndoMove();

                if (IsTimeUp)
                {
                    return 0;
                }

                if (score > alpha)
                {
                    bestMove = move;
                    alpha = score;
                }

                if (alpha >= beta)
                {
                    // Non-capture beta-cutoff move is killer move
                    if (board.Squares[move.TargetSquare] == Piece.None && !move.IsPromotion)
                    {
                        // Store at ply from root node
                        KillerMoveTable[board.PlyCount - TopPly] = move;
                    }
                    TT[TTIndex] = new(alpha, TTEntry.LowerBoundFlag, ttEntry.BestMove);
                    return alpha;
                }
            }

            TT[TTIndex] = new TTEntry(alpha, depth, TTEntry.ExactFlag, bestMove.Value);
            return alpha;
        }

        int Quiecence(int alpha, int beta)
        {

            totalNodeCount++;
            quiescentNodeCount++;

            int standingPat = evaluate.EvaluateBoard(board, GamePhase) * Skill;

            if (standingPat >= beta)
            {
                return beta;
            }
            if (alpha < standingPat)
            {
                alpha = standingPat;
            }

            List<Move> captureMoves = OrderCaptures(moveGenerator.GenerateLegalCaptures(board));

            if (IsTimeUp)
            {
                leafNodeCount++;
                // Evaluate position
                return standingPat;
            }


            int maxEval = negativeInf;

            foreach (Move move in captureMoves)
            {
                board.MakeMove(move);
                int score = -Quiecence(-beta, -alpha);
                board.UndoMove();
                maxEval = Math.Max(maxEval, score);

                if (score >= beta)
                {
                    return beta;
                }
                if (score > alpha)
                {
                    alpha = score;
                }
            }
            if (captureMoves.Count == 0)
            {
                leafNodeCount++;
            }
            return alpha;
        }

        void CalculateGamePhase()
        {
            GamePhase = TotalPhase;
            foreach (int piece in board.Squares)
            {
                GamePhase -= PhaseScore(piece);
            }
            GamePhase = ((GamePhase << 8) + (TotalPhase >> 1)) / TotalPhase;
        }

        static int PhaseScore(int piece)
        {
            return Piece.PieceType(piece) switch
            {
                Piece.Pawn => 0,
                Piece.Knight => 1,
                Piece.Bishop => 1,
                Piece.Rook => 2,
                Piece.Queen => 4,
                _ => 0
            };
        }

        static public List<Move> MoveElementToFront(List<Move> array, Move element)
        {
            // Find the index of the element
            int index = array.IndexOf(element);
            // If element is found and is not already the first element
            if (index > 0)
            {
                // Remove the element
                var temp = array[index];

                // Shift elements to the right
                for (int i = index; i > 0; i--)
                {
                    array[i] = array[i - 1];
                }

                // Place the element at the front
                array[0] = temp;
            }
            return array;
        }

        List<Move> OrderMoves(List<Move> moves, Move bestMove, Move killerMove)
        {
            return moves.OrderByDescending(move => MoveScore(move, bestMove, killerMove)).ToList();
        }

        List<Move> OrderCaptures(List<Move> moves)
        {
            return moves.OrderByDescending(move => CaptureScore(move)).ToList();
        }

        int CaptureScore(Move move)
        {
            return Evaluate.PieceValue(board.Squares[move.TargetSquare]) - Evaluate.PieceValue(board.Squares[move.StartSquare]);
        }

        int MoveScore(Move move, Move bestMove, Move killerMove)
        {
            const int badCaptureBias = -1;
            const int goodCaptureBias = 2;
            if (Move.SameMove(move, bestMove))
            {
                return 100;
            }
            else if (Move.SameMove(move, killerMove))
            {
                // Above all losing captures, below all winning captures
                return 1;
            }
            if (board.Squares[move.TargetSquare] != 0)
            {
                int captureMaterialDelta = Evaluate.PieceValue(board.Squares[move.TargetSquare]) - Evaluate.PieceValue(board.Squares[move.StartSquare]);
                int moveScore = captureMaterialDelta;
                bool opponentCanRecapture = (moveGenerator.attackedSquares & (1UL << move.TargetSquare)) != 0;

                moveScore += (captureMaterialDelta < 0 && opponentCanRecapture) ? badCaptureBias : goodCaptureBias;

                return moveScore;

            }
            else
            {
                return 0;
            }
        }

        void PrintInfo(Move bestMove, int bestEval)
        {
            string currMove = Square.IntToString(bestMove.StartSquare) + Square.IntToString(bestMove.TargetSquare);
            if (bestMove.IsPromotion)
            {
                string promotionPieceType = Piece.GetSymbol(bestMove.PromotionPieceType).ToString().ToLower();
                currMove += promotionPieceType;
            }

            Console.WriteLine("info" +
                                  " currmove " + currMove +
                                  " score " + UCI.GetEval(bestEval) +
                                  " depth " + TotalDepth +
                                  " nodes " + totalNodeCount +
                                  " leafNodes " + leafNodeCount +
                                  " quiecentNodes " + quiescentNodeCount +
                                  " pv " + GetPVLine(bestMove));
        }

        public void PerftTest(int depth)
        {

            var watch = new Stopwatch();
            watch.Start();
            ulong nodes = PerftTestCount(board, depth, depth);
            watch.Stop();
            Console.WriteLine("\nnodes: " + nodes);
            Console.WriteLine(watch.ElapsedMilliseconds + " ms");
            Console.WriteLine((1000 * ((float)nodes / watch.ElapsedMilliseconds)) + " nps\n");
        }

        ulong PerftTestCount(Board perftBoard, int depth, int topDepth)
        {
            if (depth == 0)
            {
                return 1;
            }
            List<Move> moves = moveGenerator.GenerateLegalMoves(board);

            ulong numPositions = 0;
            ulong positions = 0;
            foreach (Move move in moves)
            {
                perftBoard.MakeMove(move);
                positions = PerftTestCount(board, depth - 1, topDepth);
                numPositions += positions;
                if (depth == topDepth)
                {
                    if (move.IsPromotion)
                    {
                        Console.WriteLine($"{Square.IntToString(move.StartSquare)}{Square.IntToString(move.TargetSquare)}{char.ToLower(Piece.GetSymbol(move.PromotionPieceType))}: {positions}");
                    }
                    else
                    {
                        Console.WriteLine($"{Square.IntToString(move.StartSquare)}{Square.IntToString(move.TargetSquare)}: {positions}");
                    }
                }
                perftBoard.UndoMove();

            }
            return numPositions;
        }

        public string GetPVLine(Move bestMove)
        {

            string PvLine = Square.MoveToUCI(bestMove) + " ";
            board.MakeMove(bestMove);
            TTEntry ttEntry = TT[board.ZobristHash % TTMaxNumEntries];
            List<ulong> HashSet = new List<ulong>();
            int i = 0;
            while (ttEntry.BestMove != Move.NullMove.Value && !HashSet.Contains(board.ZobristHash))
            {
                HashSet.Add(board.ZobristHash);
                i++;
                PvLine += Square.MoveToUCI(new Move(ttEntry.BestMove)) + " ";
                bestMove = new Move(ttEntry.BestMove);
                board.MakeMove(bestMove);

                ttEntry = TT[board.ZobristHash % TTMaxNumEntries];
            }
            for (int j = 0; j < i + 1; j++)
            {
                board.UndoMove();
            }

            return PvLine.TrimEnd(' ');
        }

        bool IsTimeUp => DateTime.Now.TimeOfDay.TotalMilliseconds - startTime > searchTime;

        public int StaticEval()
        {
            CalculateGamePhase();
            return evaluate.EvaluateBoard(board, GamePhase);
        }
    }
}