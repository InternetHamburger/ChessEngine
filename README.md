A basic chess engine that supports the UCI protocol

Go [here]([url](https://www.wbec-ridderkerk.nl/html/UCIProtocol.html)) for more info about UCI

**Features**
* Move ordering
   - MVV-LVA
   - Transposition table
   - Killer moves

**Commands**
* go
   - Depth
   - Movetime (time left and total search time)
   - Perft (not a search per se, but useful in move gen debugging)
* d
   - gives a basic ascii representation of the board with fen and zobrist hash
