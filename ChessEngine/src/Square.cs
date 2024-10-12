namespace ChessEngine.src
{
    /// <summary/>
    /// For helping to convert between square representation like "e4" and "a7" to their respective integer indicies
    /// </summary>
    public static class Square
    {
        public static readonly string[] files = { "a", "b", "c", "d", "e", "f", "g", "h" };


        public static string IntToString(int square)
        {
            int rankInt = 8 - (square / 8);
            string rank = rankInt.ToString();
            string file = files[square % 8];

            return file + rank;
        }

        public static int StringToInt(string square)
        {
            int file = Array.IndexOf(files, square[0].ToString());
            // File not in "files"
            if (file == -1)
            {
                throw new Exception($"Invalid file in {square}");
            }

            if (int.TryParse(square[1].ToString(), out int rank))
            {
                rank = 8 - rank;
                return rank * 8 + file;
            }
            else
            {
                throw new Exception($"Invalid rank in {square}");
            }
        }

        public static string MoveToUCI(Move move)
        {
            string output = IntToString(move.StartSquare) + IntToString(move.TargetSquare);
            if (move.IsPromotion)
            {
                string promotionPieceType = Piece.GetSymbol(move.PromotionPieceType).ToString().ToLower();
                output += promotionPieceType;
            }
            return output;
        }

        public static int SquareToRank(int square) => square / 8;
        public static int SquareToFile(int square) => square % 8;
        public static int FlipSquare(int square) => (7 - square / 8) * 8 + square % 8;
    }
}