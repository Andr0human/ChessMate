using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class ChessBoard
{
    // 1 = wP , 2 = wB, 3 = wN, 4 = wR, 5 = wQ, 6 = wK, 0 = free square 
    public int[] board = new int[64];
    public int pColor, csep = 0, ofs = 7;
    public int halfmove = 0, fullmove = 0;

    public int[] idxs = new int[67];

    public ulong[] Pieces = new ulong[15];

    public ulong
    Lsb(ulong __x)
    { return __x ^ (__x & (__x - 1)); }

    public ulong
    Msb(ulong __x)
    {
        ulong tmp = __x;
        while (__x != 0) {
            tmp = __x;
            __x &= __x - 1;
        }
        return tmp;
    }

    public int
    PpCnt(ulong __x)
    {
        int ones = 0;
        while (__x != 0) {
            __x &= __x - 1;
            ones++;
        }
        return ones;
    }

    public void
    BoardReset()
    {
        for (int i = 0; i < 64; i++) board[i] = 0;
        for (int i = 0; i < 15; i++) Pieces[i] = 0;
        pColor = csep = 0;
        halfmove = fullmove = 0;

        for (int i = 0; i < 64; i++)
            idxs[(1UL << i) % 67] = i;
    }

    public
    ChessBoard()
    { BoardReset(); }

    public
    ChessBoard(string fen)
    {
        BoardReset();
        LoadFromFEN(fen);
    }

    public void
    LoadFromFEN(string fen)
    {
        System.Func<char, int> CharToPieceType = (char ch) => {
            char piece = (char)((ch < 'a') ? (ch) : (ch - 32));
            int res;
            if (piece == 'P') res = 1;
            else if (piece == 'B') res = 2;
            else if (piece == 'N') res = 3;
            else if (piece == 'R') res = 4;
            else if (piece == 'Q') res = 5;
            else res = 6;
            
            return res * ((ch < 'a') ? 1 : -1);
        };

        BoardReset();
        string[] val = fen.Split();

        int square = 56;
        foreach (char ch in val[0])
        {
            if (char.IsDigit(ch))
                square += (char)(ch - '0');
            else if (ch == '/')
                square -= (square & 7) != 0 ? ((square & 7) + 8) : (16);
            else
            {
                int __x = CharToPieceType(ch);
                board[square] = __x;
                Pieces[7 + __x] |= 1UL << square;
                square++;
            }
        }

        for (int i = 1; i <= 6; i++)
        {
            Pieces[14] |= Pieces[i + 7];
            Pieces[0] |= Pieces[7 - i];
        }

        if (val[1] == "w") pColor = 1;
        else pColor = -1;
        
        foreach (char k in val[2])
        {
            if (k == 'K') csep |= 1024;
            else if (k == 'Q') csep |= 512;
            else if (k == 'k') csep |= 256;
            else if (k == 'q') csep |= 128;
        }
        
        if (val[3][0] == '-') csep |= 64;
        else csep |= 28 + (pColor * 12) + (val[3][0] - 'a');
        
        halfmove = int.Parse(val[4]);
        fullmove = int.Parse(val[5]);
    }

    public void
    MakeMove(int move)
    {
        int ip = move & 63, fp = (move >> 6) & 63, ep = csep & 127;
        int pt = ((move >> 12) & 7) * pColor, cpt = ((move >> 15) & 7) * (-pColor);
        ulong iPos = 1UL << ip, fPos = 1UL << fp;
        board[ip] = 0;
        board[fp] = pt;
        csep = (csep & 1920) ^ 64;

        if (pt == pColor) {
            if (fp == ep) {     // Check if captured square in en passant square
                Pieces[ofs - pt] ^= 1UL << (ep - 8 * pt);
                Pieces[ofs - 7 * pt] ^= 1UL << (ep - 8 * pt);
                board[ep - 8 * pt] = 0;
            }
            else if (fp - ip == 16 * pt) {  // Check if pawns move 2 two steps up or down
                csep = (csep & 1920) | (fp - 8 * pt);
            }
            else if ((fPos & 0xFF000000000000FF) != 0) {   // Check for Pawn Promotion
                pt = (((move >> 18) & 3) + 2) * pColor;
                board[fp] = pt;
                Pieces[ofs + pColor] ^= iPos;
                Pieces[ofs + pt] ^= iPos;
            }
        }
        else if (pt == 6 * pColor) {  /// Check for a king Move
            if (pColor == 1) {
                csep &= 511;
                if (Mathf.Abs(fp - ip) == 2) {
                    if (fp == 6) {
                        Pieces[11] ^= 160;
                        Pieces[14] ^= 160;
                        board[7] = 0;
                        board[5] = 4;
                    }
                    else {
                        Pieces[11] ^= 9;
                        Pieces[14] ^= 9;
                        board[0] = 0;
                        board[3] = 4;
                    }
                }
            }
            else {
                csep &= 1663;
                if (Mathf.Abs(fp - ip) == 2) {
                    if (fp == 62) {
                        Pieces[3] ^= 0xA000000000000000;
                        Pieces[0] ^= 0xA000000000000000;
                        board[63] = 0;
                        board[61] = -4;
                    }
                    else {
                        Pieces[3] ^= 0x900000000000000;
                        Pieces[0] ^= 0x900000000000000;
                        board[56] = 0;
                        board[59] = -4;
                    }
                }
            }
        }

        ////// Check if a rook got captured
        if ((fPos & 0x8100000000000081) != 0 && (move & 229376) == 131072) {
            if (fp == 0) csep &= 1535;
            else if (fp == 7) csep &= 1023;
            else if (fp == 56) csep &= 1919;
            else if (fp == 63) csep &= 1791;
        }

        ///// CHECK if a Rook Moved
        if ((iPos & 0x8100000000000081) != 0 && (move & 28672) == 16384) {
            if (ip == 0) csep &= 1535;
            else if (ip == 7) csep &= 1023;
            else if (ip == 56) csep &= 1919;
            else if (ip == 63) csep &= 1791;
        }

        if (cpt != 0) {
            Pieces[ofs + cpt] ^= fPos;
            Pieces[ofs - 7 * pColor] ^= fPos;
        }

        Pieces[7 + pt] ^= iPos ^ fPos;
        Pieces[7 + 7 * pColor] ^= iPos ^ fPos;
        pColor *= -1;
    }

    public void
    UnMakeMove(int move, int t_csep)
    {
        pColor *= -1;
        int ip = move & 63, fp = (move >> 6) & 63, ep = t_csep & 127;
        int pt = ((move >> 12) & 7) * pColor, cpt = ((move >> 15) & 7) * (-pColor);
        ulong iPos = 1UL << ip, fPos = 1UL << fp;

        board[ip] = pt;
        board[fp] = cpt;
        if (cpt != 0) {
            Pieces[ofs + cpt] ^= fPos;
            Pieces[ofs - 7 * pColor] ^= fPos;
        }
        Pieces[pt + ofs] ^= iPos ^ fPos;
        Pieces[ofs + 7 * pColor] ^= iPos ^ fPos;

        if (pt == pColor) {
            if (fp == ep) {
                Pieces[ofs - pt] ^= 1UL << (ep - 8 * pt);
                Pieces[ofs - 7 * pt] ^= 1UL << (ep - 8 * pt);
                board[ep - 8 * pt] = -pt;
            }
            else if ((fPos & 0xFF000000000000FF) != 0) {
                pt = (((move >> 18) & 3) + 2) * pColor;
                Pieces[ofs + pColor] ^= fPos;
                Pieces[ofs + pt] ^= fPos;
            }
        }
        else if (pt == 6 * pColor) {  /// Check for a king Move
            if (pColor == 1) {
                if (Mathf.Abs(fp - ip) == 2) {
                    if (fp == 6) {
                        Pieces[11] ^= 160;
                        Pieces[14] ^= 160;
                        board[7] = 4;
                        board[5] = 0;
                    }
                    else {
                        Pieces[11] ^= 9;
                        Pieces[14] ^= 9;
                        board[0] = 4;
                        board[3] = 0;
                    }
                }
            }
            else {
                if (Mathf.Abs(fp - ip) == 2) {
                    if (fp == 62) {
                        Pieces[3] ^= 0xA000000000000000;
                        Pieces[0] ^= 0xA000000000000000;
                        board[63] = -4;
                        board[61] = 0;
                    }
                    else {
                        Pieces[3] ^= 0x900000000000000;
                        Pieces[0] ^= 0x900000000000000;
                        board[56] = -4;
                        board[59] = 0;
                    }
                }
            }
        }

        csep = t_csep;
        return;
    }

    public string
    Fen()
    {
        System.Func<int, string> PieceToChar = (int __p) => {
            int piece = __p >= 0 ? __p : -__p;

            char res = 'K';
            if (piece == 1) res = 'P';
            else if (piece == 2) res = 'B';
            else if (piece == 3) res = 'N';
            else if (piece == 4) res = 'R';
            else if (piece == 5) res = 'Q';

            res += (char)((__p < 0) ? 32 : 0);
            return res.ToString();
        };

        string ans = "";
        int zero = 0;

        for (int row = 7; row >= 0; row--)
        {
            for (int col = 0; col < 8; col++)
            {
                if (board[8 * row + col] == 0) zero++;
                else
                {
                    if (zero != 0)
                    {
                        ans += zero.ToString();
                        zero = 0;
                    }
                    ans += PieceToChar(board[8 * row + col]);
                }
            }
            if (zero != 0)
            {
                ans += zero.ToString();
                zero = 0;
            }
            if (row != 0) ans += '/';
        }
        ans += ' ';
        if (pColor == 1) ans += "w ";
        else ans += "b ";

        if ((csep & 1024) != 0) ans += 'K';
        if ((csep & 512) != 0) ans += 'Q';
        if ((csep & 256) != 0) ans += 'k';
        if ((csep & 128) != 0) ans += 'q';
        if ((csep & 1920) == 0) ans += '-';
        ans += ' ';

        if ((csep & 64) != 0) ans += "-";
        else
        {
            ans += (char)((csep & 7) + 'a');
            if (pColor == 1) ans += '6';
            else ans += '3';
        }

        ans += " " + halfmove.ToString();
        ans += " " + fullmove.ToString();
        return ans;
    }

    public ulong
    GenerateHashKey()
    {
        ulong key = 0;
        if (pColor == -1) key ^= TT.HashIndex[0];
        key ^= TT.HashIndex[(csep & 127) + 1];
        key ^= TT.HashIndex[(csep >> 7) + 66];

        for (int i = 0; i <= 6; i++)
        {
            ulong tmp = Pieces[7 + i], val;
            while (tmp != 0) {
                val = tmp ^ (tmp & (tmp - 1));
                tmp &= tmp - 1;
                key ^= TT.HashIndex[470 + i * (idxs[val % 67] + 1)];
            }
            tmp = Pieces[7 - i];
            while (tmp != 0) {
                val = tmp ^ (tmp & (tmp - 1));
                tmp &= tmp - 1;
                key ^= TT.HashIndex[470 - i * (idxs[val % 67] + 1)];
            }
        }

        return key;
    }

    public int
    PositionWeight()
    {
        int weight = 0;
        weight += 100 * PpCnt(Pieces[6] | Pieces[8]);
        weight += 320 * PpCnt(Pieces[5] | Pieces[9]);
        weight += 300 * PpCnt(Pieces[4] | Pieces[10]);
        weight += 500 * PpCnt(Pieces[3] | Pieces[11]);
        weight += 900 * PpCnt(Pieces[2] | Pieces[12]);
        return weight;
    }
}