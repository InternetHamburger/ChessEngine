namespace ChessEngine.src
{
    internal class Zobrist
    {

        public readonly ulong[,] SquareZobristValues;

        public readonly ulong[] EnPassantFiles;

        public ulong SideToMove;
        public ulong WhiteCastleKingSide;
        public ulong WhiteCastleQueenSide;
        public ulong BlackCastleKingSide;
        public ulong BlackCastleQueenSide;


        public Zobrist()
        {
            SquareZobristValues = new ulong[15, 64];
            EnPassantFiles = new ulong[8];

            Init();
        }


        public ulong GetPseudoRandomNumber(ulong seed)
        {
            seed ^= ((seed + 92875224317UL) << 48) ^ ((16875224059UL * (seed + 92875223911UL)) >> 32) + 92875224317UL;
            seed ^= ((seed + 52875224453UL) >> 30) ^ ((92875223911UL * (seed + 92875223911UL)) << 48) + 52875224453UL;
            seed ^= (seed >> 30) * 52875224453UL * seed + 92875223911UL;
            return seed * seed;
        }


        public void Init(ulong seed = 16491845763225878665UL)
        {
            ulong newSeed;
            for (int j = 0; j < 15; j++)
            {
                for (int i = 0; i < 64; i++)
                {
                    newSeed = GetPseudoRandomNumber(seed);
                    SquareZobristValues[j, i] = newSeed;
                    seed = newSeed;
                }
            }

            for (int i = 0; i < 64; i++)
            {
                SquareZobristValues[0, i] = 0;
            }

            for (int i = 0; i < 8; i++)
            {
                newSeed = GetPseudoRandomNumber(seed);
                EnPassantFiles[i] = newSeed;
                seed = newSeed;
            }

            newSeed = GetPseudoRandomNumber(seed);
            SideToMove = newSeed;
            seed = newSeed;

            newSeed = GetPseudoRandomNumber(seed);
            WhiteCastleKingSide = newSeed;
            seed = newSeed;
            newSeed = GetPseudoRandomNumber(seed);
            WhiteCastleQueenSide = newSeed;
            seed = newSeed;
            newSeed = GetPseudoRandomNumber(seed);
            BlackCastleKingSide = newSeed;
            seed = newSeed;
            newSeed = GetPseudoRandomNumber(seed);
            BlackCastleQueenSide = newSeed;
            seed = newSeed;

        }
    }
}