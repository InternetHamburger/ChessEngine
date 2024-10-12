using ChessEngine.src;
using System.Diagnostics;

namespace ChessEngine
{
    internal class UCI
    {
        public Engine engine;
        public string bestMove;
        public int eval;
        public int timeSearched;

        /// <summary>
        /// Handle UCI protocol
        /// </summary>
        public UCI()
        {
            engine = new Engine();
            bestMove = "";
            eval = 0;
            timeSearched = 0;
        }

        public void HandleInput(string? input)
        {
            string[] tokens = input.Split(" ");

            switch (tokens[0])
            {
                case "uci":
                    Console.WriteLine("id name saltettorrfisk c# 3");
                    Console.WriteLine("id author Sindre Wolden\n");
                    Console.WriteLine("option name Skill type check default true");
                    Console.WriteLine("uciok");
                    break;
                case "isready":
                    Console.WriteLine("readyok");
                    break;
                case "ucinewgame":
                    engine = new Engine();
                    break;
                case "position":
                    HandlePositionCommand(tokens);
                    break;
                case "go":
                    HandleGoCommand(tokens);
                    break;
                case "setoption":
                    HandleOptionCommand(tokens);
                    break;
                case "quit":
                    Environment.Exit(0);
                    break;
                case "stop":
                    break;
                case "d":
                    engine.board.D();
                    break;
                default:
                    Console.WriteLine("Either I have not implemented the feature yet, or it's an invalid input");
                    break;
            }
        }

        public void HandlePositionCommand(string[] tokens)
        {
            engine.board = new();
            bestMove = "";
            switch (tokens[1])
            {
                case "startpos":
                    MakeMoves(tokens);
                    break;
                case "fen":
                    engine.board.SetBoard([tokens[2], tokens[3], tokens[4], tokens[5], tokens[6], tokens[7]]);
                    MakeMoves(tokens);
                    break;
            }
            engine.moveGenerator.GenerateLegalMoves(engine.board);
        }

        public void HandleGoCommand(string[] tokens)
        {
            if (tokens.Length == 1 || tokens[1] != "perft")
            {
                // infinite search time default
                int searchTime = int.MaxValue;
                int maxDepth = int.MaxValue;
                if (tokens.Contains("depth"))
                {
                    maxDepth = int.Parse(tokens[Array.IndexOf(tokens, "depth") + 1]);
                }
                else if (tokens.Contains("movetime"))
                {
                    searchTime = int.Parse(tokens[Array.IndexOf(tokens, "movetime") + 1]);
                }
                else
                {
                    int WhiteTimeLeft = 0;
                    int WhiteIncrement = 0;
                    int BlackTimeLeft = 0;
                    int BlackIncrement = 0;
                    int tokenIndex = 1;
                    while (tokenIndex < tokens.Length)
                    {
                        switch (tokens[tokenIndex])
                        {
                            case "wtime":
                                WhiteTimeLeft = int.Parse(tokens[tokenIndex + 1]);
                                break;
                            case "winc":
                                WhiteIncrement = int.Parse(tokens[tokenIndex + 1]);
                                break;
                            case "btime":
                                BlackTimeLeft = int.Parse(tokens[tokenIndex + 1]);
                                break;
                            case "binc":
                                BlackIncrement = int.Parse(tokens[tokenIndex + 1]);
                                break;
                        }
                        tokenIndex++;
                    }

                    int myTimeLeft = engine.board.WhiteToMove ? WhiteTimeLeft : BlackTimeLeft;
                    int myIncrement = engine.board.WhiteToMove ? WhiteIncrement : BlackIncrement;
                    searchTime = Math.Clamp(myTimeLeft / 60 + myIncrement / 3, 100, 5000) + myIncrement / 5;
                }
                var watch = new Stopwatch();

                watch.Start();
                // Search
                var bestMoveMove = engine.Search(searchTime, maxDepth);

                watch.Stop();

                // Convert to UCI notation
                bestMove = Square.MoveToUCI(bestMoveMove.Item1);
                timeSearched = (int)watch.ElapsedMilliseconds;
                eval = bestMoveMove.Item2;

                Console.WriteLine("info score " + GetEval(eval) +
                  " time " + timeSearched +
                  " depth " + engine.TotalDepth +
                  " nodes " + engine.totalNodeCount +
                  " leafNodes " + engine.leafNodeCount +
                  " quiecentNodes " + engine.quiescentNodeCount +
                  " nps " + 1000 * engine.totalNodeCount / (ulong)Math.Max(timeSearched, 1) +
                  " pv " + engine.GetPVLine(bestMoveMove.Item1));
                Console.WriteLine("bestmove " + bestMove);
            }
            else
            {
                if (int.TryParse(tokens[2], out int depth))
                {
                    engine.PerftTest(depth);
                }
                else
                {
                    Console.WriteLine($"Invalid depth in perft command: {depth}");
                }
            }
        }

        public static string GetEval(int eval)
        {
            if (Math.Abs(eval) > 100000)
            {
                // checkmate
                return "mate " + (Engine.positiveInf - Math.Abs(eval) + 2) / 2 * (eval < 0 ? -1 : 1);
            }
            else
            {
                return "cp " + eval;
            }
        }

        public void MakeMoves(string[] tokens)
        {
            try
            {
                int movesIndex = Array.IndexOf(tokens, "moves") + 1;

                if (movesIndex == 0)
                {
                    throw new Exception("No moves");
                }

                for (int i = movesIndex; i < tokens.Length; i++)
                {
                    engine.board.MakeMove(ReturnMove(tokens[i]));
                }
            }
            catch
            {
                // There werent any moves
                return;
            }
        }

        void HandleOptionCommand(string[] tokens)
        {
            switch (tokens[2])
            {
                case "Skill":
                    if (bool.TryParse(tokens[4], out bool skillLevel))
                    {
                        engine.BestPlay = skillLevel;
                    }
                    else
                    {
                        Console.WriteLine($"Invalid value type: '{tokens[4]}'");
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown option: '{tokens[2]}'");
                    break;
            }
        }

        Move ReturnMove(string move)
        {
            int startSquare = Square.StringToInt(move[0].ToString() + move[1].ToString());
            int targetSquare = Square.StringToInt(move[2].ToString() + move[3].ToString());
            int promotionRank = engine.board.WhiteToMove ? 1 : 6; // Rank for promotion
            int piece = engine.board.Squares[startSquare];
            if (Piece.PieceType(piece) == Piece.Pawn)
            {
                if (Square.SquareToFile(startSquare) != Square.SquareToFile(targetSquare))
                {
                    if (engine.board.Squares[targetSquare] == 0)
                    {
                        return new Move(startSquare, targetSquare, Move.EnPassantCaptureFlag);
                    }
                    else
                    {
                        if (Square.SquareToRank(startSquare) == promotionRank)
                        {
                            int promotionPieceType = Piece.GetPieceTypeFromSymbol(move[4]);

                            return Piece.PieceType(promotionPieceType) switch
                            {
                                Piece.Queen => new Move(startSquare, targetSquare, Move.PromoteToQueenFlag),
                                Piece.Rook => new Move(startSquare, targetSquare, Move.PromoteToRookFlag),
                                Piece.Knight => new Move(startSquare, targetSquare, Move.PromoteToKnightFlag),
                                Piece.Bishop => new Move(startSquare, targetSquare, Move.PromoteToBishopFlag),
                                _ => throw new NotImplementedException()
                            };
                        }
                        else
                        {
                            return new Move(startSquare, targetSquare);
                        }

                    }
                }
                if (Math.Abs(Square.SquareToRank(startSquare) - Square.SquareToRank(targetSquare)) == 2)
                {
                    return new Move(startSquare, targetSquare, Move.PawnTwoUpFlag);
                }
                if (Square.SquareToRank(startSquare) == promotionRank)
                {
                    int promotionPieceType = Piece.GetPieceTypeFromSymbol(move[4]);

                    return Piece.PieceType(promotionPieceType) switch
                    {
                        Piece.Queen => new Move(startSquare, targetSquare, Move.PromoteToQueenFlag),
                        Piece.Rook => new Move(startSquare, targetSquare, Move.PromoteToRookFlag),
                        Piece.Knight => new Move(startSquare, targetSquare, Move.PromoteToKnightFlag),
                        Piece.Bishop => new Move(startSquare, targetSquare, Move.PromoteToBishopFlag),
                        _ => throw new NotImplementedException()
                    };
                }

            }
            else if (Piece.PieceType(piece) == Piece.King)
            {
                if (startSquare == 60 && targetSquare == 62)
                {
                    return new Move(startSquare, targetSquare, Move.CastleFlag);
                }
                else if (startSquare == 60 && targetSquare == 58)
                {
                    return new Move(startSquare, targetSquare, Move.CastleFlag);
                }
                else if (startSquare == 4 && targetSquare == 6)
                {
                    return new Move(startSquare, targetSquare, Move.CastleFlag);
                }
                else if (startSquare == 4 && targetSquare == 2)
                {
                    return new Move(startSquare, targetSquare, Move.CastleFlag);
                }

            }
            if (move.Length > 4)
            {
                throw new NotImplementedException();
            }

            return new Move(startSquare, targetSquare);

        }
    }
}