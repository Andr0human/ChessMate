using UnityEngine;

public class MoveGenerator : MonoBehaviour {

    public PrelookupTable pt = new PrelookupTable();
    private int kpos, ekpos, eps, color;
    private readonly int ofs = 7;
    ulong mkbd = 0, Apieces, Free_sq, Attacked_Squares;

    #region Utility

    public void Set_piece(ref ChessBoard cb) {
        color = cb.pColor;
        kpos = cb.idxs[cb.Pieces[ofs + 6 * color] % 67];
        ekpos = cb.idxs[cb.Pieces[ofs - 6 * color] % 67];
        eps = cb.csep & 127;
        Apieces = cb.Pieces[ofs + 7] ^ cb.Pieces[ofs - 7];
        Free_sq = ~Apieces;
    }

    ulong LSb(ulong N) {
        return N ^ (N & (N - 1));
    }

    ulong MSb(ulong N) {
        ulong res = 0;
        while (N != 0) {
            res = N;
            N &= N - 1;
        }
        return res;
    }

    public int PopCount(ulong N) {
        int res = 0;
        while (N != 0) {
            N &= N - 1;
            res++;
        }
        return res;
    }

    private bool En_passant_recheck(int ip, ref ChessBoard cb) {
        ulong erq = cb.Pieces[ofs - 5 * color] ^ cb.Pieces[ofs - 4 * color];
        ulong Ap = Apieces ^ (1UL << ip) ^ (1UL << (eps - 8 * color));
        ulong res = MSb(pt.lBoard[kpos] & Ap) | LSb(pt.rBoard[kpos] & Ap);
        if ((res & erq) != 0) return false;
        return true;
    }

    public string Print_move(int move, ref ChessBoard __b) {
        string res = "";
        int ip = move & 63, fp = (move >> 6) & 63, ip_x = ip & 7, fp_x = fp & 7;
        int ip_y = (ip - ip_x) >> 3, fp_y = (fp - fp_x) >> 3;
        int _pt = (move >> 12) & 7, _cpt = (move >> 15) & 7, csep = __b.csep;
        color = __b.board[ip] > 0 ? 1 : -1;
        bool checks = false;
        __b.MakeMove(move);
        if (Incheck(ref __b)) checks = true;
        __b.UnMakeMove(move, csep);

        Apieces = __b.Pieces[ofs + 7] ^ __b.Pieces[ofs - 7];
        if (_pt == 1) {
            int enp = 0;
            if (fp_x - ip_x == 1 || ip_x - fp_x == 1 && _cpt == 0) enp = 1;
            if (_cpt != 0 || enp != 0) {
                res += __b.myMap[ip_x];
                res += 'x';
            }
            res += __b.myMap[fp_x];
            res += fp_y + 1;
            int ppt = -1;
            if ((color == 1 && fp_y == 7) || (color == -1 && fp_y == 0))
                ppt = (move >> 18) & 3;
            if (ppt == 0) res += "=B";
            else if (ppt == 1) res += "=N";
            else if (ppt == 2) res += "=R";
            else if (ppt == 3) res += "=Q";
        }
        else if (_pt == 2) {
            int x, y, idx = fp;
            bool row = true, col = true, found = false;
            ulong tmp = pt.urBoard[idx] ^ pt.ulBoard[idx] ^ pt.drBoard[idx] ^ pt.dlBoard[idx], tmp2;

            tmp2 = pt.rBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.rBoard[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = pt.lBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.lBoard[__b.idxs[MSb(tmp2) % 67]];
            tmp2 = pt.uBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.uBoard[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = pt.dBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.dBoard[__b.idxs[MSb(tmp2) % 67]];

            res = "B";
            tmp &= __b.Pieces[ofs + _pt * color];
            tmp ^= 1UL << ip;
            while (tmp != 0) {
                found = true;
                ulong val = tmp ^ (tmp & (tmp - 1));
                tmp &= tmp - 1;
                idx = __b.idxs[val % 67];
                x = idx & 7; y = (idx - x) >> 3;
                if (x == ip_x) col = false;
                if (y == ip_y) row = false;
            }
            if (found) {
                if (col) res += __b.myMap[ip_x];
                else if (row) res += ip_y + 1;
                else {
                    res += __b.myMap[ip_x];
                    res += ip_y + 1;
                }
            }
            if (_cpt != 0) res += 'x';
            res += __b.myMap[fp_x];
            res += fp_y + 1;
        }
        else if (_pt == 3) {
            int idx = fp, x, y;
            bool row = true, col = true, found = false;
            ulong tmp = pt.NtBoard[idx];
            res = "N";
            tmp &= __b.Pieces[ofs + _pt * color];
            tmp ^= 1UL << ip;
            while (tmp != 0) {
                found = true;
                ulong val = tmp ^ (tmp & (tmp - 1));
                tmp &= tmp - 1;
                idx = __b.idxs[val % 67];
                x = idx & 7; y = (idx - x) >> 3;
                if (x == ip_x) col = false;
                if (y == ip_y) row = false;
            }
            if (found) {
                if (col) res += __b.myMap[ip_x];
                else if (row) res += ip_y + 1;
                else {
                    res += __b.myMap[ip_x];
                    res += ip_y + 1;
                }
            }
            if (_cpt != 0) res += 'x';
            res += __b.myMap[fp_x];
            res += fp_y + 1;
        }
        else if (_pt == 4) {
            int x, y, idx = fp;
            bool __row = true, __col = true, found = false;
            ulong tmp2, tmp = pt.rBoard[idx] ^ pt.lBoard[idx] ^ pt.uBoard[idx] ^ pt.dBoard[idx];

            tmp2 = pt.rBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.rBoard[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = pt.lBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.lBoard[__b.idxs[MSb(tmp2) % 67]];
            tmp2 = pt.uBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.uBoard[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = pt.dBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.dBoard[__b.idxs[MSb(tmp2) % 67]];

            res = "R";
            tmp &= __b.Pieces[ofs + _pt * color];
            tmp ^= 1UL << ip;
            while (tmp != 0) {
                found = true;
                ulong val = tmp ^ (tmp & (tmp - 1));
                tmp &= tmp - 1;
                idx = __b.idxs[val % 67];
                x = idx & 7; y = (idx - x) >> 3;
                if (x == ip_x) __col = false;
                if (y == ip_y) __row = false;
            }
            if (found) {
                if (__col) res += __b.myMap[ip_x];
                else if (__row) res += ip_y + 1;
                else {
                    res += __b.myMap[ip_x];
                    res += ip_y + 1;
                }
            }
            if (_cpt != 0) res += 'x';
            res += __b.myMap[fp_x];
            res += fp_y + 1;
        }
        else if (_pt == 5) {
            int x, y, idx = fp;
            bool __row = true, __col = true, found = false;

            ulong tmp2, tmp = pt.rBoard[idx] ^ pt.lBoard[idx] ^ pt.uBoard[idx] ^ pt.dBoard[idx]
                ^ pt.urBoard[idx] ^ pt.ulBoard[idx] ^ pt.drBoard[idx] ^ pt.dlBoard[idx];

            tmp2 = pt.rBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.rBoard[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = pt.lBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.lBoard[__b.idxs[MSb(tmp2) % 67]];
            tmp2 = pt.uBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.uBoard[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = pt.dBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.dBoard[__b.idxs[MSb(tmp2) % 67]];
            tmp2 = pt.rBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.rBoard[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = pt.lBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.lBoard[__b.idxs[MSb(tmp2) % 67]];
            tmp2 = pt.uBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.uBoard[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = pt.dBoard[idx] & Apieces;
            if (tmp2 != 0) tmp ^= pt.dBoard[__b.idxs[MSb(tmp2) % 67]];

            res = "Q";
            tmp &= __b.Pieces[ofs + _pt * color];
            tmp ^= 1UL << ip;
            while (tmp != 0) {
                found = true;
                ulong val = tmp ^ (tmp & (tmp - 1));
                tmp &= tmp - 1;
                idx = __b.idxs[val % 67];
                x = idx & 7; y = (idx - x) >> 3;
                if (x == ip_x) __col = false;
                if (y == ip_y) __row = false;
            }
            if (found) {
                if (__col) res += __b.myMap[ip_x];
                else if (__row) res += ip_y + 1;
                else {
                    res += __b.myMap[ip_x];
                    res += ip_y + 1;
                }
            }
            if (_cpt != 0) res += 'x';
            res += __b.myMap[fp_x];
            res += fp_y + 1;
        }
        else if (_pt == 6) {
            if (Mathf.Abs(fp_x - ip_x) == 2) {
                if (fp == 2 || fp == 58) res = "O-O-O";
                else if (fp == 6 || fp == 62) res = "O-O";
            }
            else {
                res += "K";
                if (_cpt != 0) res += 'x';
                res += __b.myMap[fp_x];
                res += fp_y + 1;
            }
        }
        if (checks) res += "+";

        return res;
    }

    #endregion

    #region Attacking Squares

    private ulong PawnAttkinSq(ref ChessBoard cb) {
        ulong Pwns = cb.Pieces[7 + color];
        if (color == 1) return ((Pwns & 0x7F7F7F7F7F7F00) << 9) | ((Pwns & 0xFEFEFEFEFEFE00) << 7);
        return ((Pwns & 0x7F7F7F7F7F7F00) >> 7) | ((Pwns & 0xFEFEFEFEFEFE00) >> 9);
    }

    private ulong BishopAttkinSq(int idx, ref ChessBoard cb) {
        ulong res, ans = pt.urBoard[idx] ^ pt.ulBoard[idx] ^ pt.drBoard[idx] ^ pt.dlBoard[idx];
        res = pt.urBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.urBoard[cb.idxs[LSb(res) % 67]];
        res = pt.ulBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.ulBoard[cb.idxs[LSb(res) % 67]];
        res = pt.drBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.drBoard[cb.idxs[MSb(res) % 67]];
        res = pt.dlBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.dlBoard[cb.idxs[MSb(res) % 67]];
        return ans;
    }

    private ulong KnightAttkinSq(int idx) {
        return pt.NtBoard[idx];
    }

    private ulong RookAttkinSq(int idx, ref ChessBoard cb) {
        ulong res, ans = pt.rBoard[idx] ^ pt.lBoard[idx] ^ pt.uBoard[idx] ^ pt.dBoard[idx];
        res = pt.rBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.rBoard[cb.idxs[LSb(res) % 67]];
        res = pt.lBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.lBoard[cb.idxs[MSb(res) % 67]];
        res = pt.uBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.uBoard[cb.idxs[LSb(res) % 67]];
        res = pt.dBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.dBoard[cb.idxs[MSb(res) % 67]];
        return ans;
    }

    #endregion

    #region (KA = 0) Piece Movement

    private ulong PawnMovement(int idx, ref ChessBoard cb) {
        ulong ans = 0;
        if (color == 1) {
            ans |= pt.wPboard[idx] & Free_sq;
            if (idx > 7 && idx < 16 && ((pt.wPboard[idx] ^ pt.wPboard[idx + 8]) & Apieces) == 0)
                ans |= pt.wPboard[idx + 8];
            ans |= pt.wpCboard[idx] & cb.Pieces[ofs - 7 * color];
            if (eps != 64 && (pt.wpCboard[idx] & (1UL << eps)) != 0) {
                if (En_passant_recheck(idx, ref cb)) ans |= pt.wpCboard[idx] & (1UL << eps);
            }
            return ans;
        }
        ans |= pt.bPboard[idx] & Free_sq;
        if (idx > 47 && ((pt.bPboard[idx] ^ pt.bPboard[idx - 8]) & Apieces) == 0)
            ans |= pt.bPboard[idx - 8];
        ans |= pt.bpCboard[idx] & cb.Pieces[ofs - 7 * color];
        if (eps != 64 && (pt.bpCboard[idx] & (1UL << eps)) != 0) {
            if (En_passant_recheck(idx, ref cb)) ans |= pt.bpCboard[idx] & (1UL << eps);
        }
        return ans;
    }

    private ulong BishopMovement(int idx, ref ChessBoard cb) {
        ulong res, ans = pt.urBoard[idx] ^ pt.ulBoard[idx] ^ pt.drBoard[idx] ^ pt.dlBoard[idx];

        res = pt.urBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.urBoard[cb.idxs[LSb(res) % 67]];

        res = pt.ulBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.ulBoard[cb.idxs[LSb(res) % 67]];

        res = pt.drBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.drBoard[cb.idxs[MSb(res) % 67]];

        res = pt.dlBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.dlBoard[cb.idxs[MSb(res) % 67]];

        return ans ^ (ans & cb.Pieces[7 + 7 * color]);
    }

    private ulong RookMovement(int idx, ref ChessBoard cb) {
        ulong res, ans = pt.rBoard[idx] ^ pt.lBoard[idx] ^ pt.uBoard[idx] ^ pt.dBoard[idx];

        res = pt.rBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.rBoard[cb.idxs[LSb(res) % 67]];

        res = pt.lBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.lBoard[cb.idxs[MSb(res) % 67]];

        res = pt.uBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.uBoard[cb.idxs[LSb(res) % 67]];

        res = pt.dBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.dBoard[cb.idxs[MSb(res) % 67]];

        return ans ^ (ans & cb.Pieces[7 + 7 * color]);
    }

    private ulong KnightMovement(int idx, ref ChessBoard cb) {
        ulong ans = pt.NtBoard[idx];
        return ans ^ (ans & cb.Pieces[7 + 7 * color]);
    }

    private void pinnedPieceList(ref ChessBoard cb, ref MoveList myMoves) {
        ulong tmp, val1, val2, ans, rem_k = ~(1UL << (kpos));
        ulong rq = cb.Pieces[ofs + 5 * color] ^ cb.Pieces[ofs + 4 * color];
        ulong bq = cb.Pieces[ofs + 5 * color] ^ cb.Pieces[ofs + 2 * color];
        ulong erq = cb.Pieces[ofs - 5 * color] ^ cb.Pieces[ofs - 4 * color];
        ulong ebq = cb.Pieces[ofs - 5 * color] ^ cb.Pieces[ofs - 2 * color];
        if (erq == 0 && ebq == 0) return;

        tmp = pt.rBoard[kpos] & Apieces;
        val1 = LSb(tmp);
        val2 = LSb(tmp ^ val1);
        if ((val2 & erq) != 0) {
            if ((val1 & rq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (pt.rBoard[kpos] ^ pt.rBoard[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            mkbd ^= val1;
        }

        tmp = pt.lBoard[kpos] & Apieces;
        val1 = MSb(tmp);
        val2 = MSb(tmp ^ val1);
        if ((val2 & erq) != 0) {
            if ((val1 & rq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (pt.lBoard[kpos] ^ pt.lBoard[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            mkbd ^= val1;
        }

        tmp = pt.uBoard[kpos] & Apieces;
        val1 = LSb(tmp);
        val2 = LSb(tmp ^ val1);
        if ((val2 & erq) != 0) {
            if ((val1 & rq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (pt.uBoard[kpos] ^ pt.uBoard[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == 1 && (val1 & cb.Pieces[8]) != 0) {
                int idx = cb.idxs[val1 % 67];
                ans = (pt.wPboard[idx] & Apieces) ^ pt.wPboard[idx];
                if (idx < 16 && ((pt.wPboard[idx] ^ pt.wPboard[idx + 8]) & Apieces) == 0)
                    ans |= pt.wPboard[idx + 8];
                myMoves.Add(idx, ans);
            }
            else if (color == -1 && (val1 & cb.Pieces[6]) != 0) {
                int idx = cb.idxs[val1 % 67];
                ans = pt.bPboard[idx] & Free_sq;
                if (idx > 47 && ((pt.bPboard[idx] ^ pt.bPboard[idx - 8]) & Apieces) == 0)
                    ans |= pt.bPboard[idx - 8];
                myMoves.Add(idx, ans);
            }
            mkbd ^= val1;
        }

        tmp = pt.dBoard[kpos] & Apieces;
        val1 = MSb(tmp);
        val2 = MSb(tmp ^ val1);
        if ((val2 & erq) != 0) {
            if ((val1 & rq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (pt.dBoard[kpos] ^ pt.dBoard[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == -1 && (val1 & cb.Pieces[6]) != 0) {
                int idx = cb.idxs[val1 % 67];
                ans = pt.bPboard[idx] & Free_sq;
                if (idx > 47 && ((pt.bPboard[idx] ^ pt.bPboard[idx - 8]) & Apieces) == 0)
                    ans |= pt.bPboard[idx - 8];
                myMoves.Add(idx, ans);
            }
            else if (color == 1 && (val1 & cb.Pieces[8]) != 0) {
                int idx = cb.idxs[val1 % 67];
                ans = pt.wPboard[idx] & Free_sq;
                if (idx < 16 && ((pt.wPboard[idx] ^ pt.wPboard[idx + 8]) & Apieces) == 0)
                    ans |= pt.wPboard[idx + 8];
                myMoves.Add(idx, ans);
            }
            mkbd ^= val1;
        }

        tmp = pt.urBoard[kpos] & Apieces;
        val1 = LSb(tmp);
        val2 = LSb(tmp ^ val1);
        if ((val2 & ebq) != 0) {
            if ((val1 & bq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (pt.urBoard[kpos] ^ pt.urBoard[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == 1 && (val1 & cb.Pieces[8]) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, pt.wpCboard[idx] & val2);
                if (eps != 64 && (((pt.urBoard[kpos] ^ pt.urBoard[cb.idxs[val2 % 67]]) & (1UL << eps)) != 0))
                    myMoves.Add(idx, pt.wpCboard[idx] & (1UL << eps));
            }
            mkbd ^= val1;
        }
        tmp = pt.ulBoard[kpos] & Apieces;
        val1 = LSb(tmp);
        val2 = LSb(tmp ^ val1);
        if ((val2 & ebq) != 0) {
            if ((val1 & bq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (pt.ulBoard[kpos] ^ pt.ulBoard[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == 1 && (val1 & cb.Pieces[8]) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, pt.wpCboard[idx] & val2);
                if (eps != 64 && (((pt.ulBoard[kpos] ^ pt.ulBoard[cb.idxs[val2 % 67]]) & (1UL << eps)) != 0))
                    myMoves.Add(idx, pt.wpCboard[idx] & (1UL << eps));
            }
            mkbd ^= val1;
        }

        tmp = pt.drBoard[kpos] & Apieces;
        val1 = MSb(tmp);
        val2 = MSb(tmp ^ val1);
        if ((val2 & ebq) != 0) {
            if ((val1 & bq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (pt.drBoard[kpos] ^ pt.drBoard[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == -1 && (val1 & cb.Pieces[6]) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, pt.bpCboard[idx] & val2);
                if (eps != 64 && (((pt.drBoard[kpos] ^ pt.drBoard[cb.idxs[val2 % 67]]) & (1UL << eps)) != 0))
                    myMoves.Add(idx, pt.bpCboard[idx] & (1UL << eps));
            }
            mkbd ^= val1;
        }

        tmp = pt.dlBoard[kpos] & Apieces;
        val1 = MSb(tmp);
        val2 = MSb(tmp ^ val1);
        if ((val2 & ebq) != 0) {
            if ((val1 & bq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (pt.dlBoard[kpos] ^ pt.dlBoard[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == -1 && (val1 & cb.Pieces[6]) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, pt.bpCboard[idx] & val2);
                if (eps != 64 && (((pt.dlBoard[kpos] ^ pt.dlBoard[cb.idxs[val2 % 67]]) & (1UL << eps)) != 0))
                    myMoves.Add(idx, pt.bpCboard[idx] & (1UL << eps));
            }
            mkbd ^= val1;
        }

        return;
    }

    public void Ka_zero_pieceMovement(ref ChessBoard cb, ref MoveList myMoves) {
        mkbd = 0;
        pinnedPieceList(ref cb, ref myMoves);
        ulong tmp, val, marked_piece = mkbd & cb.Pieces[ofs + 7 * color];
        int idx;

        tmp = marked_piece;
        while (tmp != 0) {
            val = tmp ^ (tmp & (tmp - 1));
            tmp &= tmp - 1;
            cb.Pieces[ofs + cb.board[cb.idxs[val % 67]]] ^= val;
        }

        tmp = cb.Pieces[ofs + color];               ////// Pawns
        while (tmp != 0) {
            val = tmp ^ (tmp & (tmp - 1));
            tmp &= tmp - 1;
            idx = cb.idxs[val % 67];
            myMoves.Add(idx, PawnMovement(idx, ref cb));
        }

        tmp = cb.Pieces[ofs + 2 * color];           ////// Bishops
        while (tmp != 0) {
            val = tmp ^ (tmp & (tmp - 1));
            tmp &= tmp - 1;
            idx = cb.idxs[val % 67];
            myMoves.Add(idx, BishopMovement(idx, ref cb));
        }

        tmp = cb.Pieces[ofs + 3 * color];           ////// Knights
        while (tmp != 0) {
            val = tmp ^ (tmp & (tmp - 1));
            tmp &= tmp - 1;
            idx = cb.idxs[val % 67];
            myMoves.Add(idx, KnightMovement(idx, ref cb));
        }

        tmp = cb.Pieces[ofs + 4 * color];           ////// Rooks
        while (tmp != 0) {
            val = tmp ^ (tmp & (tmp - 1));
            tmp &= tmp - 1;
            idx = cb.idxs[val % 67];
            myMoves.Add(idx, RookMovement(idx, ref cb));
        }

        tmp = cb.Pieces[ofs + 5 * color];           ////// Queens
        while (tmp != 0) {
            val = tmp ^ (tmp & (tmp - 1));
            tmp &= tmp - 1;
            idx = cb.idxs[val % 67];
            myMoves.Add(idx, BishopMovement(idx, ref cb) ^ RookMovement(idx, ref cb));
        }

        tmp = marked_piece;
        while (tmp != 0) {
            val = tmp ^ (tmp & (tmp - 1));
            tmp &= tmp - 1;
            cb.Pieces[ofs + cb.board[cb.idxs[val % 67]]] ^= val;
        }

        return;
    }

    #endregion

    #region (KA = 1) Piece Movement

    #region TypeC

    private ulong RMovesC(int idx, ulong res) {
        if ((LSb(pt.rBoard[idx] & Apieces) & res) != 0) return res;
        if ((MSb(pt.lBoard[idx] & Apieces) & res) != 0) return res;
        if ((LSb(pt.uBoard[idx] & Apieces) & res) != 0) return res;
        if ((MSb(pt.dBoard[idx] & Apieces) & res) != 0) return res;
        return 0;
    }

    private ulong BMovesC(int idx, ulong res) {
        if ((LSb(pt.urBoard[idx] & Apieces) & res) != 0) return res;
        if ((LSb(pt.ulBoard[idx] & Apieces) & res) != 0) return res;
        if ((MSb(pt.drBoard[idx] & Apieces) & res) != 0) return res;
        if ((MSb(pt.dlBoard[idx] & Apieces) & res) != 0) return res;
        return 0;
    }

    private ulong NMovesC(int idx, ulong res) {
        if ((pt.NtBoard[idx] & res) != 0) return res;
        return 0;
    }

    private ulong PMovesC(int idx, ulong res) {
        if (color == 1) {
            if ((pt.wpCboard[idx] & res) != 0) return res;
            if (eps != 64 && (pt.wpCboard[idx] & (1UL << eps)) != 0) return (1UL << eps);
        }
        if (color == -1) {
            if ((pt.bpCboard[idx] & res) != 0) return res;
            if (eps != 64 && (pt.bpCboard[idx] & (1UL << eps)) != 0) return (1UL << eps);
        }
        return 0;
    }

    private void pieceMovesTypeC(int idx, int type, ref MoveList myMoves, ref KAinfo atk) {
        if (type == 1) myMoves.Add(idx, PMovesC(idx, atk.ppos));
        else if (type == 2) myMoves.Add(idx, BMovesC(idx, atk.ppos));
        else if (type == 3) myMoves.Add(idx, NMovesC(idx, atk.ppos));
        else if (type == 4) myMoves.Add(idx, RMovesC(idx, atk.ppos));
        else if (type == 5) myMoves.Add(idx, RMovesC(idx, atk.ppos) | BMovesC(idx, atk.ppos));
        return;
    }

    #endregion

    private void PieceMovesTypeS(int idx, int type, ref ChessBoard cb, ref MoveList myMoves, ulong area) {
        if (type == 1) myMoves.Add(idx, PawnMovement(idx, ref cb) & area);
        else if (type == 2) myMoves.Add(idx, BishopMovement(idx, ref cb) & area);
        else if (type == 3) myMoves.Add(idx, pt.NtBoard[idx] & area);
        else if (type == 4) myMoves.Add(idx, RookMovement(idx, ref cb) & area);
        else if (type == 5)
            myMoves.Add(idx, (RookMovement(idx, ref cb) & area) | (BishopMovement(idx, ref cb) & area));
        return;
    }

    private void Ka_pinnedPieceList(ref ChessBoard cb) {
        ulong tmp, v1, v2, OwnP = cb.Pieces[7 + 7 * color];
        ulong erq = cb.Pieces[ofs - 5 * color] | cb.Pieces[ofs - 4 * color];
        ulong ebq = cb.Pieces[ofs - 5 * color] | cb.Pieces[ofs - 2 * color];

        tmp = pt.rBoard[kpos] & Apieces;
        v1 = LSb(tmp); v2 = LSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & erq) != 0) mkbd ^= v1;

        tmp = pt.lBoard[kpos] & Apieces;
        v1 = MSb(tmp); v2 = MSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & erq) != 0) mkbd ^= v1;

        tmp = pt.uBoard[kpos] & Apieces;
        v1 = LSb(tmp); v2 = LSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & erq) != 0) mkbd ^= v1;

        tmp = pt.dBoard[kpos] & Apieces;
        v1 = MSb(tmp); v2 = MSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & erq) != 0) mkbd ^= v1;

        tmp = pt.urBoard[kpos] & Apieces;
        v1 = LSb(tmp); v2 = LSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & ebq) != 0) mkbd ^= v1;

        tmp = pt.ulBoard[kpos] & Apieces;
        v1 = LSb(tmp); v2 = LSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & ebq) != 0) mkbd ^= v1;

        tmp = pt.drBoard[kpos] & Apieces;
        v1 = MSb(tmp); v2 = MSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & ebq) != 0) mkbd ^= v1;

        tmp = pt.dlBoard[kpos] & Apieces;
        v1 = MSb(tmp); v2 = MSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & ebq) != 0) mkbd ^= v1;

        return;
    }

    public void Ka_pieceMovement(ref ChessBoard cb, ref MoveList myMoves, ref KAinfo atk) {
        ulong val, tmp;
        mkbd = 0;
        Ka_pinnedPieceList(ref cb);

        for (int i = 1; i < 6; i++) {
            tmp = cb.Pieces[ofs + i * color];
            while (tmp != 0) {
                val = tmp ^ (tmp & (tmp - 1));
                tmp &= tmp - 1;
                int idx = cb.idxs[val % 67];
                if ((mkbd & val) == 0) {
                    if (atk.area == 0) pieceMovesTypeC(idx, i, ref myMoves, ref atk);
                    else PieceMovesTypeS(idx, i, ref cb, ref myMoves, atk.area);
                }
            }
        }
        return;
    }

    #endregion

    #region King Move Generation

    public bool Incheck(ref ChessBoard cb) {

        int cl = cb.pColor, idx = cb.idxs[cb.Pieces[ofs + 6 * cl] % 67];
        ulong Apieces = cb.Pieces[ofs + 7] ^ cb.Pieces[ofs - 7];
        ulong erq = cb.Pieces[ofs - 4 * cl] ^ cb.Pieces[ofs - 5 * cl];
        ulong ebq = cb.Pieces[ofs - 2 * cl] ^ cb.Pieces[ofs - 5 * cl];

        ulong res, ans = pt.rBoard[idx] ^ pt.lBoard[idx] ^ pt.uBoard[idx] ^ pt.dBoard[idx];
        res = pt.rBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.rBoard[cb.idxs[LSb(res) % 67]];
        res = pt.lBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.lBoard[cb.idxs[MSb(res) % 67]];
        res = pt.uBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.uBoard[cb.idxs[LSb(res) % 67]];
        res = pt.dBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.dBoard[cb.idxs[MSb(res) % 67]];
        if ((ans & erq) != 0) return true;

        ans = pt.rBoard[idx] ^ pt.lBoard[idx] ^ pt.uBoard[idx] ^ pt.dBoard[idx];
        res = pt.urBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.urBoard[cb.idxs[LSb(res) % 67]];
        res = pt.ulBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.ulBoard[cb.idxs[LSb(res) % 67]];
        res = pt.drBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.drBoard[cb.idxs[MSb(res) % 67]];
        res = pt.dlBoard[idx] & Apieces;
        if (res != 0) ans ^= pt.dlBoard[cb.idxs[MSb(res) % 67]];
        if ((ans & ebq) != 0) return true;

        ans = pt.NtBoard[idx];
        if ((ans & cb.Pieces[ofs - 3 * cl]) != 0) return true;

        ans = cl == 1 ? pt.wpCboard[idx] : pt.bpCboard[idx];
        if ((ans & cb.Pieces[ofs - cl]) != 0) return true;

        return false;
    }

    public void Generate_AttackedSquares(ref ChessBoard cb) {
        ulong ans = 0, tmp, val;
        color *= -1;
        Apieces ^= 1UL << kpos;
        ans |= PawnAttkinSq(ref cb);
        tmp = cb.Pieces[ofs + 2 * color];
        while (tmp != 0) {
            val = tmp ^ (tmp & (tmp - 1));
            tmp &= tmp - 1;
            ans |= BishopAttkinSq(cb.idxs[val % 67], ref cb);
        }
        tmp = cb.Pieces[ofs + 3 * color];
        while (tmp != 0) {
            val = tmp ^ (tmp & (tmp - 1));
            tmp &= tmp - 1;
            ans |= KnightAttkinSq(cb.idxs[val % 67]);
        }
        tmp = cb.Pieces[ofs + 4 * color];
        while (tmp != 0) {
            val = tmp ^ (tmp & (tmp - 1));
            tmp &= tmp - 1;
            ans |= RookAttkinSq(cb.idxs[val % 67], ref cb);
        }
        tmp = cb.Pieces[ofs + 5 * color];
        while (tmp != 0) {
            val = tmp ^ (tmp & (tmp - 1));
            tmp &= tmp - 1;
            ans |= RookAttkinSq(cb.idxs[val % 67], ref cb) | BishopAttkinSq(cb.idxs[val % 67], ref cb);
        }
        ans |= pt.Kboard[ekpos];
        color *= -1;
        Apieces ^= 1UL << kpos;
        Attacked_Squares = ans;
        return;
    }

    public KAinfo FindkingAttackers(ref ChessBoard cb) {
        KAinfo info = new KAinfo();
        if (((1UL << kpos) & Attacked_Squares) == 0) return info;
        ulong res, rem_k = ~(1UL << (kpos));
        ulong rq = cb.Pieces[ofs - 5 * color] | cb.Pieces[ofs - 4 * color];
        ulong bq = cb.Pieces[ofs - 5 * color] | cb.Pieces[ofs - 2 * color];
        if ((pt.uBoard[kpos] & rq) != 0) {
            res = LSb(pt.uBoard[kpos] & Apieces);
            if ((rq & res) != 0) {
                int pos = cb.idxs[(rq & res) % 67];
                info.Add((pt.uBoard[kpos] ^ pt.uBoard[pos]) & rem_k, 0);
            }
        }

        if ((pt.dBoard[kpos] & rq) != 0) {
            res = MSb(pt.dBoard[kpos] & Apieces);
            if ((rq & res) != 0) {
                int pos = cb.idxs[(rq & res) % 67];
                info.Add((pt.dBoard[kpos] ^ pt.dBoard[pos]) & rem_k, 0);
            }
        }

        if ((pt.lBoard[kpos] & rq) != 0) {
            res = MSb(pt.lBoard[kpos] & Apieces);
            if ((rq & res) != 0) {
                int pos = cb.idxs[(rq & res) % 67];
                info.Add((pt.lBoard[kpos] ^ pt.lBoard[pos]) & rem_k, 0);
            }
        }

        if ((pt.rBoard[kpos] & rq) != 0) {
            res = LSb(pt.rBoard[kpos] & Apieces);
            if ((rq & res) != 0) {
                int pos = cb.idxs[(rq & res) % 67];
                info.Add((pt.rBoard[kpos] ^ pt.rBoard[pos]) & rem_k, 0);
            }
        }

        if ((pt.urBoard[kpos] & bq) != 0) {
            res = LSb(pt.urBoard[kpos] & Apieces);
            if ((bq & res) != 0) {
                int pos = cb.idxs[(bq & res) % 67];
                info.Add((pt.urBoard[kpos] ^ pt.urBoard[pos]) & rem_k, 0);
            }
        }

        if ((pt.dlBoard[kpos] & bq) != 0) {
            res = MSb(pt.dlBoard[kpos] & Apieces);
            if ((bq & res) != 0) {
                int pos = cb.idxs[(bq & res) % 67];
                info.Add((pt.dlBoard[kpos] ^ pt.dlBoard[pos]) & rem_k, 0);
            }
        }

        if ((pt.ulBoard[kpos] & bq) != 0) {
            res = LSb(pt.ulBoard[kpos] & Apieces);
            if ((bq & res) != 0) {
                int pos = cb.idxs[(bq & res) % 67];
                info.Add(pt.ulBoard[kpos] ^ pt.ulBoard[pos], 0);
            }
        }

        if ((pt.drBoard[kpos] & bq) != 0) {
            res = MSb(pt.drBoard[kpos] & Apieces);
            if ((bq & res) != 0) {
                int pos = cb.idxs[(bq & res) % 67];
                info.Add(pt.drBoard[kpos] ^ pt.drBoard[pos], 0);
            }
        }

        res = pt.NtBoard[kpos] & cb.Pieces[ofs - 3 * color];
        if (res != 0) info.Add(0, res);

        if (color == 1) res = pt.wpCboard[kpos] & cb.Pieces[6];
        else if (color == -1) res = pt.bpCboard[kpos] & cb.Pieces[8];
        if (res != 0) info.Add(0, res);

        return info;
    }

    public void KingMoves(ref ChessBoard cb, ref MoveList myMoves) {
        ulong ans, Ksquares = pt.Kboard[kpos];
        ulong opd = Apieces | Attacked_Squares;
        ans = Ksquares ^ (Ksquares & (cb.Pieces[7 + 7 * color] | Attacked_Squares));
        if (color == 1 && ((1UL << kpos) & Attacked_Squares) == 0) {
            if ((cb.csep & 1024) != 0)
                if ((96 & opd) == 0) ans ^= 64;
            if ((cb.csep & 512) != 0)
                if ((Apieces & 2) == 0 && (12 & opd) == 0) ans ^= 4;
        }
        if (color == -1 && ((1UL << kpos) & Attacked_Squares) == 0) {
            if ((cb.csep & 256) != 0)
                if ((opd & 0x6000000000000000) == 0) ans ^= 0x4000000000000000;
            if ((cb.csep & 128) != 0)
                if ((Apieces & 0x200000000000000) == 0 && (opd & 0xC00000000000000) == 0) ans ^= 0x400000000000000;
        }
        myMoves.Add(kpos, ans);
        return;
    }

    #endregion

}
