namespace ChessEngine.src
{

    // Using PeSTO eval function
    internal class Evaluate
    {
        public static readonly int[] MG_PAWN_TABLE =
        {
            0,   0,   0,   0,   0,   0,  0,   0,
            98, 134,  61,  95,  68, 126, 34, -11,
            -6,   7,  26,  31,  65,  56, 25, -20,
            -14,  13,   6,  21,  23,  12, 17, -23,
            -27,  -2,  -5,  12,  17,   6, 10, -25,
            -26,  -4,  -4, -10,   3,   3, 33, -12,
            -35,  -1, -20, -23, -18,  24, 38, -22,
            0,   0,   0,   0,   0,   0,  0,   0
        };

        public static readonly int[] MG_KNIGHT_TABLE =
        {
            -167, -89, -34, -49,  61, -97, -15, -107,
     -73, -41,  72,  36,  23,  62,   7,  -17,
     -47,  60,  37,  65,  84, 129,  73,   44,
      -9,  17,  19,  53,  37,  69,  18,   22,
     -13,   4,  16,  13,  28,  19,  21,   -8,
     -23,  -9,  12,  10,  19,  16,  25,  -16,
     -29, -53, -12,  -3,  -1,  18, -14,  -19,
    -105, -19, -58, -33, -17, -28, -17,  -23,
        };

        public static readonly int[] MG_BISHOP_TABLE =
        {
            -29,   4, -82, -37, -25, -42,   7,  -8,
    -26,  16, -18, -13,  30,  59,  18, -47,
    -16,  37,  43,  40,  35,  50,  37,  -2,
     -4,   5,  19,  50,  37,  37,   7,  -2,
     -6,  13,  13,  26,  34,  12,  10,   4,
      0,  15,  15,  15,  14,  27,  18,  10,
      4,  15,  16,   0,   7,  21,  33,   1,
    -33,  -3, -14, -21, -13, -12, -39, -21,
        };

        public static readonly int[] MG_ROOK_TABLE =
        {
        32,  42,  32,  51, 63,  9,  31,  43,
     27,  32,  58,  62, 80, 67,  26,  44,
     -5,  19,  26,  36, 17, 45,  61,  16,
    -24, -11,   7,  26, 24, 35,  -8, -20,
    -36, -26, -12,  -1,  9, -7,   6, -23,
    -45, -25, -16, -17,  3,  0,  -5, -33,
    -44, -16, -20,  -9, -1, 11,  -6, -71,
    -19, -13,   1,  17, 16,  7, -37, -26,
        };

        public static readonly int[] MG_QUEEN_TABLE =
{
            -28,   0,  29,  12,  59,  44,  43,  45,
    -24, -39,  -5,   1, -16,  57,  28,  54,
    -13, -17,   7,   8,  29,  56,  47,  57,
    -27, -27, -16, -16,  -1,  17,  -2,   1,
     -9, -26,  -9, -10,  -2,  -4,   3,  -3,
    -14,   2, -11,  -2,  -5,   2,  14,   5,
    -35,  -8,  11,   2,   8,  15,  -3,   1,
     -1, -18,  -9,  10, -15, -25, -31, -50,
};

        public static readonly int[] MG_KING_TABLE = {
    -65,  23,  16, -15, -56, -34,   2,  13,
     29,  -1, -20,  -7,  -8,  -4, -38, -29,
     -9,  24,   2, -16, -20,   6,  22, -22,
    -17, -20, -12, -27, -30, -25, -14, -36,
    -49,  -1, -27, -39, -46, -44, -33, -51,
    -14, -14, -22, -46, -44, -30, -15, -27,
      1,   7,  -8, -64, -43, -16,   9,   8,
    -15,  36,  12, -54,   8, -28,  24,  14,
};

        public static readonly int[] EG_PAWN_TABLE =
        {
            0,   0,   0,   0,   0,   0,   0,   0,
    178, 173, 158, 134, 147, 132, 165, 187,
     94, 100,  85,  67,  56,  53,  82,  84,
     32,  24,  13,   5,  -2,   4,  17,  17,
     13,   9,  -3,  -7,  -7,  -8,   3,  -1,
      4,   7,  -6,   1,   0,  -5,  -1,  -8,
     13,   8,   8,  10,  13,   0,   2,  -7,
      0,   0,   0,   0,   0,   0,   0,   0,
        };

        public static readonly int[] EG_KNIGHT_TABLE =
        {
            -58, -38, -13, -28, -31, -27, -63, -99,
    -25,  -8, -25,  -2,  -9, -25, -24, -52,
    -24, -20,  10,   9,  -1,  -9, -19, -41,
    -17,   3,  22,  22,  22,  11,   8, -18,
    -18,  -6,  16,  25,  16,  17,   4, -18,
    -23,  -3,  -1,  15,  10,  -3, -20, -22,
    -42, -20, -10,  -5,  -2, -20, -23, -44,
    -29, -51, -23, -15, -22, -18, -50, -64,
        };

        public static readonly int[] EG_BISHOP_TABLE =
        {
            -14, -21, -11,  -8, -7,  -9, -17, -24,
     -8,  -4,   7, -12, -3, -13,  -4, -14,
      2,  -8,   0,  -1, -2,   6,   0,   4,
     -3,   9,  12,   9, 14,  10,   3,   2,
     -6,   3,  13,  19,  7,  10,  -3,  -9,
    -12,  -3,   8,  10, 13,   3,  -7, -15,
    -14, -18,  -7,  -1,  4,  -9, -15, -27,
    -23,  -9, -23,  -5, -9, -16,  -5, -17,
        };

        public static readonly int[] EG_ROOK_TABLE =
        {
        13, 10, 18, 15, 12,  12,   8,   5,
    11, 13, 13, 11, -3,   3,   8,   3,
     7,  7,  7,  5,  4,  -3,  -5,  -3,
     4,  3, 13,  1,  2,   1,  -1,   2,
     3,  5,  8,  4, -5,  -6,  -8, -11,
    -4,  0, -5, -1, -7, -12,  -8, -16,
    -6, -6,  0,  2, -9,  -9, -11,  -3,
    -9,  2,  3, -1, -5, -13,   4, -20,
        };

        public static readonly int[] EG_QUEEN_TABLE =
{
            -9,  22,  22,  27,  27,  19,  10,  20,
    -17,  20,  32,  41,  58,  25,  30,   0,
    -20,   6,   9,  49,  47,  35,  19,   9,
      3,  22,  24,  45,  57,  40,  57,  36,
    -18,  28,  19,  47,  31,  34,  39,  23,
    -16, -27,  15,   6,   9,  17,  10,   5,
    -22, -23, -30, -16, -16, -23, -36, -32,
    -33, -28, -22, -43,  -5, -32, -20, -41,
};

        public static readonly int[] EG_KING_TABLE = {
    -74, -35, -18, -18, -11,  15,   4, -17,
    -12,  17,  14,  17,  17,  38,  23,  11,
     10,  17,  23,  15,  20,  45,  44,  13,
     -8,  22,  24,  27,  26,  33,  26,   3,
    -18,  -4,  21,  24,  27,  23,   9, -11,
    -19,  -3,  11,  21,  23,  16,   7,  -9,
    -27, -11,   4,  13,  14,   4,  -5, -17,
    -53, -34, -21, -11, -28, -14, -24, -43
};

        public int[,] PieceValues;
        public const int MG_PAWN_VALUE = 82;
        public const int MG_KNIGHT_VALUE = 337;
        public const int MG_BISHOP_VALUE = 365;
        public const int MG_ROOK_VALUE = 477;
        public const int MG_QUEEN_VALUE = 1025;
        public const int MG_KING_VALUE = 0;

        public const int EG_PAWN_VALUE = 94;
        public const int EG_KNIGHT_VALUE = 281;
        public const int EG_BISHOP_VALUE = 297;
        public const int EG_ROOK_VALUE = 512;
        public const int EG_QUEEN_VALUE = 936;
        public const int EG_KING_VALUE = 0;


        int WhiteKingSquare;
        int BlackKingSquare;

        int friendlyKingSquare;
        int opponentKingSquare;

        ulong FriendlyPieces;
        ulong EnemyPieces;

        public static readonly int[] DirectionOffSets = [-8, 8, -1, 1, -9, 9, -7, 7];
        public static readonly int[] PassedPawnBonuses = [0, 90, 60, 40, 25, 15, 15];

        public int EvaluateBoard(Board board, int GamePhase)
        {

            int color;
            ulong AllPieces = board.AllPieceBitBoard;
            if (board.WhiteToMove)
            {
                color = 1;
                opponentKingSquare = WhiteKingSquare;
                friendlyKingSquare = BlackKingSquare;
                FriendlyPieces = board.WhitePieceBitBoard;
                EnemyPieces = board.BlackPieceBitBoard;
            }
            else
            {
                color = -1;
                opponentKingSquare = BlackKingSquare;
                friendlyKingSquare = WhiteKingSquare;
                FriendlyPieces = board.BlackPieceBitBoard;
                EnemyPieces = board.WhitePieceBitBoard;
            }
            int MGeval = 0;
            int EGeval = 0;
            for (int SquareIndex = 0; SquareIndex < 64; SquareIndex++)
            {
                if (((AllPieces >> SquareIndex) & 1) == 1)
                {
                    int piece = board.Squares[SquareIndex];
                    MGeval += PieceValues[piece, SquareIndex];
                    EGeval += EGpieceValue(board, piece, SquareIndex);
                }
            }
            EGeval += MopUpEval();

            MGeval -= GenerateQueenKingHeuristic(WhiteKingSquare) * 2;
            MGeval += GenerateQueenKingHeuristic(BlackKingSquare) * 2;

            int eval = (MGeval * (256 - GamePhase) + EGeval * GamePhase) / 256;



            return eval * color;
        }

        int EGpieceValue(Board board, int piece, int square)
        {
            switch (piece)
            {
                case Piece.WhitePawn:
                    if ((PrecomputedMoveData.PassedPawnMasks[square].Item1 & board.BitBoards[Piece.BlackPawn]) == 0)
                    {
                        int rank = square / 8;
                        return EG_PAWN_VALUE + EG_PAWN_TABLE[square] + PassedPawnBonuses[rank];
                    }
                    return EG_PAWN_VALUE + EG_PAWN_TABLE[square];
                case Piece.WhiteKnight:
                    return EG_KNIGHT_VALUE + EG_KNIGHT_TABLE[square];
                case Piece.WhiteBishop:
                    return EG_BISHOP_VALUE + EG_BISHOP_TABLE[square];
                case Piece.WhiteRook:
                    return EG_ROOK_VALUE + EG_ROOK_TABLE[square];
                case Piece.WhiteQueen:
                    return EG_QUEEN_VALUE + EG_QUEEN_TABLE[square];
                case Piece.WhiteKing:
                    WhiteKingSquare = square;
                    return EG_KING_TABLE[square];
                case Piece.BlackPawn:
                    if ((PrecomputedMoveData.PassedPawnMasks[square].Item2 & board.BitBoards[Piece.WhitePawn]) == 0)
                    {
                        int rank = square / 8;
                        return -(EG_PAWN_VALUE + EG_PAWN_TABLE[63 - square] + PassedPawnBonuses[7 - rank]);
                    }
                    return -(EG_PAWN_VALUE + EG_PAWN_TABLE[63 - square]);
                case Piece.BlackKnight:
                    return -(EG_KNIGHT_VALUE + EG_KNIGHT_TABLE[63 - square]);
                case Piece.BlackBishop:
                    return -(EG_BISHOP_VALUE + EG_BISHOP_TABLE[63 - square]);
                case Piece.BlackRook:
                    return -(EG_ROOK_VALUE + EG_ROOK_TABLE[63 - square]);
                case Piece.BlackQueen:
                    return -(EG_QUEEN_VALUE + EG_QUEEN_TABLE[63 - square]);
                case Piece.BlackKing:
                    BlackKingSquare = square;
                    return -EG_KING_TABLE[63 - square];
                default:
                    return 0;

            };
        }

        int MopUpEval()
        {
            int eval = PrecomputedMoveData.dstToCentre[opponentKingSquare] * 2;
            int friendlyRank = friendlyKingSquare / 8;
            int friendlyFile = friendlyKingSquare % 8;
            int opponentRank = opponentKingSquare / 8;
            int opponentFile = opponentKingSquare % 8;

            int dstKingRank = Math.Abs(friendlyRank - opponentRank);
            int dstKingFile = Math.Abs(friendlyFile - opponentFile);

            int dstKings = dstKingRank + dstKingFile;

            eval -= dstKings * 7;
            return eval;
        }

        int GenerateQueenKingHeuristic(int square)
        {
            int countMoves = 0;

            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                for (int n = 0; n < PrecomputedMoveData.numSquaresToEdge[square][directionIndex]; n++)
                {

                    int targetSquare = square + DirectionOffSets[directionIndex] * (n + 1);

                    // Blocked by friendly piece
                    if (((FriendlyPieces >> targetSquare) & 1) == 1)
                    {
                        break;
                    }

                    countMoves++;

                    if (((EnemyPieces >> targetSquare) & 1) == 1)
                    {
                        break;
                    }
                }
            }

            return countMoves;
        }

        public static int PieceValue(int piece)
        {
            return Piece.PieceType(piece) switch
            {
                Piece.Pawn => MG_PAWN_VALUE,
                Piece.Knight => MG_KNIGHT_VALUE,
                Piece.Bishop => MG_BISHOP_VALUE,
                Piece.Rook => MG_ROOK_VALUE,
                Piece.Queen => MG_QUEEN_VALUE,
                _ => 0
            };
        }

        public Evaluate()
        {
            PieceValues = new int[15, 64];
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.None, i] = 0;
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.WhitePawn, i] = MG_PAWN_TABLE[i] + MG_PAWN_VALUE;
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.WhiteKnight, i] = MG_KNIGHT_TABLE[i] + MG_KNIGHT_VALUE;
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.WhiteBishop, i] = MG_BISHOP_TABLE[i] + MG_BISHOP_VALUE;
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.WhiteRook, i] = MG_ROOK_TABLE[i] + MG_ROOK_VALUE;
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.WhiteQueen, i] = MG_QUEEN_TABLE[i] + MG_QUEEN_VALUE;
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.WhiteKing, i] = MG_KING_TABLE[i];
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.BlackPawn, i] = -(MG_PAWN_TABLE[63 - i] + MG_PAWN_VALUE);
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.BlackKnight, i] = -(MG_KNIGHT_TABLE[63 - i] + MG_KNIGHT_VALUE);
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.BlackBishop, i] = -(MG_BISHOP_TABLE[63 - i] + MG_BISHOP_VALUE);
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.BlackRook, i] = -(MG_ROOK_TABLE[63 - i] + MG_ROOK_VALUE);
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.BlackQueen, i] = -(MG_QUEEN_TABLE[63 - i] + MG_QUEEN_VALUE);
            }
            for (int i = 0; i < 64; i++)
            {
                PieceValues[Piece.BlackKing, i] = -(MG_KING_TABLE[63 - i]);
            }
        }
    }
}