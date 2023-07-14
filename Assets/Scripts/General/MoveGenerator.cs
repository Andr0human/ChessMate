using UnityEngine;

public class MoveGenerator : MonoBehaviour {

    public LookupTable lt = new LookupTable();
    private int kpos, ekpos, eps, color;
    private readonly int ofs = 7;
    ulong mkbd = 0, Apieces, Free_sq, Attacked_Squares;

    #region Utility

    public void SetPiece(ref ChessBoard cb) {
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

    private bool
    En_passant_recheck(int ip, ref ChessBoard cb) {
        ulong erq = cb.Pieces[ofs - 5 * color] ^ cb.Pieces[ofs - 4 * color];
        ulong Ap = Apieces ^ (1UL << ip) ^ (1UL << (eps - 8 * color));
        ulong res = MSb(lt.LeftMasks[kpos] & Ap) | LSb(lt.RightMasks[kpos] & Ap);
        if ((res & erq) != 0) return false;
        return true;
    }

    public string
    PrintMove(int move, ref ChessBoard __b)
    {
        string IndexToRow(int __x) => (__x + 49).ToString();
        string IndexToCol(int __y) => (__y + 97).ToString();
        // string IndexToSquare(int __x, int __y) => IndexToCol(__y) + IndexToRow(__x);

        string res = "";
        int ip = move & 63, fp = (move >> 6) & 63, ip_x = ip & 7, fp_x = fp & 7;
        int ip_y = (ip - ip_x) >> 3, fp_y = (fp - fp_x) >> 3;
        int _pt = (move >> 12) & 7, _cpt = (move >> 15) & 7, csep = __b.csep;
        color = __b.board[ip] > 0 ? 1 : -1;
        bool checks = false;
        __b.MakeMove(move);
        if (InCheck(ref __b)) checks = true;
        __b.UnMakeMove(move, csep);

        Apieces = __b.Pieces[ofs + 7] ^ __b.Pieces[ofs - 7];
        if (_pt == 1) {
            int enp = 0;
            if (fp_x - ip_x == 1 || ip_x - fp_x == 1 && _cpt == 0) enp = 1;
            if (_cpt != 0 || enp != 0) {
                res += IndexToCol(ip_x);
                res += 'x';
            }
            res += IndexToCol(fp_x);
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
            ulong tmp = lt.UpRightMasks[idx] ^ lt.UpLeftMasks[idx] ^ lt.DownRightMasks[idx] ^ lt.DownLeftMasks[idx], tmp2;

            tmp2 = lt.RightMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.RightMasks[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = lt.LeftMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.LeftMasks[__b.idxs[MSb(tmp2) % 67]];
            tmp2 = lt.UpMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.UpMasks[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = lt.DownMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.DownMasks[__b.idxs[MSb(tmp2) % 67]];

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
                if (col) res += IndexToCol(ip_x);
                else if (row) res += ip_y + 1;
                else {
                    res += IndexToCol(ip_x);
                    res += ip_y + 1;
                }
            }
            if (_cpt != 0) res += 'x';
            res += IndexToCol(fp_x);
            res += fp_y + 1;
        }
        else if (_pt == 3) {
            int idx = fp, x, y;
            bool row = true, col = true, found = false;
            ulong tmp = lt.KnightMasks[idx];
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
                if (col) res += IndexToCol(ip_x);
                else if (row) res += ip_y + 1;
                else {
                    res += IndexToCol(ip_x);
                    res += ip_y + 1;
                }
            }
            if (_cpt != 0) res += 'x';
            res += IndexToCol(fp_x);
            res += fp_y + 1;
        }
        else if (_pt == 4) {
            int x, y, idx = fp;
            bool __row = true, __col = true, found = false;
            ulong tmp2, tmp = lt.RightMasks[idx] ^ lt.LeftMasks[idx] ^ lt.UpMasks[idx] ^ lt.DownMasks[idx];

            tmp2 = lt.RightMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.RightMasks[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = lt.LeftMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.LeftMasks[__b.idxs[MSb(tmp2) % 67]];
            tmp2 = lt.UpMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.UpMasks[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = lt.DownMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.DownMasks[__b.idxs[MSb(tmp2) % 67]];

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
                if (__col) res += IndexToCol(ip_x);
                else if (__row) res += ip_y + 1;
                else {
                    res += IndexToCol(ip_x);
                    res += ip_y + 1;
                }
            }
            if (_cpt != 0) res += 'x';
            res += IndexToCol(fp_x);
            res += fp_y + 1;
        }
        else if (_pt == 5) {
            int x, y, idx = fp;
            bool __row = true, __col = true, found = false;

            ulong tmp2, tmp = lt.RightMasks[idx] ^ lt.LeftMasks[idx] ^ lt.UpMasks[idx] ^ lt.DownMasks[idx]
                ^ lt.UpRightMasks[idx] ^ lt.UpLeftMasks[idx] ^ lt.DownRightMasks[idx] ^ lt.DownLeftMasks[idx];

            tmp2 = lt.RightMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.RightMasks[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = lt.LeftMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.LeftMasks[__b.idxs[MSb(tmp2) % 67]];
            tmp2 = lt.UpMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.UpMasks[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = lt.DownMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.DownMasks[__b.idxs[MSb(tmp2) % 67]];
            tmp2 = lt.RightMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.RightMasks[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = lt.LeftMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.LeftMasks[__b.idxs[MSb(tmp2) % 67]];
            tmp2 = lt.UpMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.UpMasks[__b.idxs[LSb(tmp2) % 67]];
            tmp2 = lt.DownMasks[idx] & Apieces;
            if (tmp2 != 0) tmp ^= lt.DownMasks[__b.idxs[MSb(tmp2) % 67]];

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
                if (__col) res += IndexToCol(ip_x);
                else if (__row) res += ip_y + 1;
                else {
                    res += IndexToCol(ip_x);
                    res += ip_y + 1;
                }
            }
            if (_cpt != 0) res += 'x';
            res += IndexToCol(fp_x);
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
                res += IndexToCol(fp_x);
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
        ulong res, ans = lt.UpRightMasks[idx] ^ lt.UpLeftMasks[idx] ^ lt.DownRightMasks[idx] ^ lt.DownLeftMasks[idx];
        res = lt.UpRightMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.UpRightMasks[cb.idxs[LSb(res) % 67]];
        res = lt.UpLeftMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.UpLeftMasks[cb.idxs[LSb(res) % 67]];
        res = lt.DownRightMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.DownRightMasks[cb.idxs[MSb(res) % 67]];
        res = lt.DownLeftMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.DownLeftMasks[cb.idxs[MSb(res) % 67]];
        return ans;
    }

    private ulong KnightAttkinSq(int idx) {
        return lt.KnightMasks[idx];
    }

    private ulong RookAttkinSq(int idx, ref ChessBoard cb) {
        ulong res, ans = lt.RightMasks[idx] ^ lt.LeftMasks[idx] ^ lt.UpMasks[idx] ^ lt.DownMasks[idx];
        res = lt.RightMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.RightMasks[cb.idxs[LSb(res) % 67]];
        res = lt.LeftMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.LeftMasks[cb.idxs[MSb(res) % 67]];
        res = lt.UpMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.UpMasks[cb.idxs[LSb(res) % 67]];
        res = lt.DownMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.DownMasks[cb.idxs[MSb(res) % 67]];
        return ans;
    }

    #endregion

    #region (KA = 0) Piece Movement

    private ulong PawnMovement(int idx, ref ChessBoard cb) {
        ulong ans = 0;
        if (color == 1) {
            ans |= lt.wPboard[idx] & Free_sq;
            if (idx > 7 && idx < 16 && ((lt.wPboard[idx] ^ lt.wPboard[idx + 8]) & Apieces) == 0)
                ans |= lt.wPboard[idx + 8];
            ans |= lt.wpCboard[idx] & cb.Pieces[ofs - 7 * color];
            if (eps != 64 && (lt.wpCboard[idx] & (1UL << eps)) != 0) {
                if (En_passant_recheck(idx, ref cb)) ans |= lt.wpCboard[idx] & (1UL << eps);
            }
            return ans;
        }
        ans |= lt.bPboard[idx] & Free_sq;
        if (idx > 47 && ((lt.bPboard[idx] ^ lt.bPboard[idx - 8]) & Apieces) == 0)
            ans |= lt.bPboard[idx - 8];
        ans |= lt.bpCboard[idx] & cb.Pieces[ofs - 7 * color];
        if (eps != 64 && (lt.bpCboard[idx] & (1UL << eps)) != 0) {
            if (En_passant_recheck(idx, ref cb)) ans |= lt.bpCboard[idx] & (1UL << eps);
        }
        return ans;
    }

    private ulong BishopMovement(int idx, ref ChessBoard cb) {
        ulong res, ans = lt.UpRightMasks[idx] ^ lt.UpLeftMasks[idx] ^ lt.DownRightMasks[idx] ^ lt.DownLeftMasks[idx];

        res = lt.UpRightMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.UpRightMasks[cb.idxs[LSb(res) % 67]];

        res = lt.UpLeftMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.UpLeftMasks[cb.idxs[LSb(res) % 67]];

        res = lt.DownRightMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.DownRightMasks[cb.idxs[MSb(res) % 67]];

        res = lt.DownLeftMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.DownLeftMasks[cb.idxs[MSb(res) % 67]];

        return ans ^ (ans & cb.Pieces[7 + 7 * color]);
    }

    private ulong RookMovement(int idx, ref ChessBoard cb) {
        ulong res, ans = lt.RightMasks[idx] ^ lt.LeftMasks[idx] ^ lt.UpMasks[idx] ^ lt.DownMasks[idx];

        res = lt.RightMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.RightMasks[cb.idxs[LSb(res) % 67]];

        res = lt.LeftMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.LeftMasks[cb.idxs[MSb(res) % 67]];

        res = lt.UpMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.UpMasks[cb.idxs[LSb(res) % 67]];

        res = lt.DownMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.DownMasks[cb.idxs[MSb(res) % 67]];

        return ans ^ (ans & cb.Pieces[7 + 7 * color]);
    }

    private ulong KnightMovement(int idx, ref ChessBoard cb) {
        ulong ans = lt.KnightMasks[idx];
        return ans ^ (ans & cb.Pieces[7 + 7 * color]);
    }

    private void PinnedPieces(ref ChessBoard cb, ref MoveList myMoves) {
        ulong tmp, val1, val2, ans, rem_k = ~(1UL << (kpos));
        ulong rq = cb.Pieces[ofs + 5 * color] ^ cb.Pieces[ofs + 4 * color];
        ulong bq = cb.Pieces[ofs + 5 * color] ^ cb.Pieces[ofs + 2 * color];
        ulong erq = cb.Pieces[ofs - 5 * color] ^ cb.Pieces[ofs - 4 * color];
        ulong ebq = cb.Pieces[ofs - 5 * color] ^ cb.Pieces[ofs - 2 * color];
        if (erq == 0 && ebq == 0) return;

        tmp = lt.RightMasks[kpos] & Apieces;
        val1 = LSb(tmp);
        val2 = LSb(tmp ^ val1);
        if ((val2 & erq) != 0) {
            if ((val1 & rq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (lt.RightMasks[kpos] ^ lt.RightMasks[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            mkbd ^= val1;
        }

        tmp = lt.LeftMasks[kpos] & Apieces;
        val1 = MSb(tmp);
        val2 = MSb(tmp ^ val1);
        if ((val2 & erq) != 0) {
            if ((val1 & rq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (lt.LeftMasks[kpos] ^ lt.LeftMasks[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            mkbd ^= val1;
        }

        tmp = lt.UpMasks[kpos] & Apieces;
        val1 = LSb(tmp);
        val2 = LSb(tmp ^ val1);
        if ((val2 & erq) != 0) {
            if ((val1 & rq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (lt.UpMasks[kpos] ^ lt.UpMasks[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == 1 && (val1 & cb.Pieces[8]) != 0) {
                int idx = cb.idxs[val1 % 67];
                ans = (lt.wPboard[idx] & Apieces) ^ lt.wPboard[idx];
                if (idx < 16 && ((lt.wPboard[idx] ^ lt.wPboard[idx + 8]) & Apieces) == 0)
                    ans |= lt.wPboard[idx + 8];
                myMoves.Add(idx, ans);
            }
            else if (color == -1 && (val1 & cb.Pieces[6]) != 0) {
                int idx = cb.idxs[val1 % 67];
                ans = lt.bPboard[idx] & Free_sq;
                if (idx > 47 && ((lt.bPboard[idx] ^ lt.bPboard[idx - 8]) & Apieces) == 0)
                    ans |= lt.bPboard[idx - 8];
                myMoves.Add(idx, ans);
            }
            mkbd ^= val1;
        }

        tmp = lt.DownMasks[kpos] & Apieces;
        val1 = MSb(tmp);
        val2 = MSb(tmp ^ val1);
        if ((val2 & erq) != 0) {
            if ((val1 & rq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (lt.DownMasks[kpos] ^ lt.DownMasks[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == -1 && (val1 & cb.Pieces[6]) != 0) {
                int idx = cb.idxs[val1 % 67];
                ans = lt.bPboard[idx] & Free_sq;
                if (idx > 47 && ((lt.bPboard[idx] ^ lt.bPboard[idx - 8]) & Apieces) == 0)
                    ans |= lt.bPboard[idx - 8];
                myMoves.Add(idx, ans);
            }
            else if (color == 1 && (val1 & cb.Pieces[8]) != 0) {
                int idx = cb.idxs[val1 % 67];
                ans = lt.wPboard[idx] & Free_sq;
                if (idx < 16 && ((lt.wPboard[idx] ^ lt.wPboard[idx + 8]) & Apieces) == 0)
                    ans |= lt.wPboard[idx + 8];
                myMoves.Add(idx, ans);
            }
            mkbd ^= val1;
        }

        tmp = lt.UpRightMasks[kpos] & Apieces;
        val1 = LSb(tmp);
        val2 = LSb(tmp ^ val1);
        if ((val2 & ebq) != 0) {
            if ((val1 & bq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (lt.UpRightMasks[kpos] ^ lt.UpRightMasks[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == 1 && (val1 & cb.Pieces[8]) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, lt.wpCboard[idx] & val2);
                if (eps != 64 && (((lt.UpRightMasks[kpos] ^ lt.UpRightMasks[cb.idxs[val2 % 67]]) & (1UL << eps)) != 0))
                    myMoves.Add(idx, lt.wpCboard[idx] & (1UL << eps));
            }
            mkbd ^= val1;
        }
        tmp = lt.UpLeftMasks[kpos] & Apieces;
        val1 = LSb(tmp);
        val2 = LSb(tmp ^ val1);
        if ((val2 & ebq) != 0) {
            if ((val1 & bq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (lt.UpLeftMasks[kpos] ^ lt.UpLeftMasks[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == 1 && (val1 & cb.Pieces[8]) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, lt.wpCboard[idx] & val2);
                if (eps != 64 && (((lt.UpLeftMasks[kpos] ^ lt.UpLeftMasks[cb.idxs[val2 % 67]]) & (1UL << eps)) != 0))
                    myMoves.Add(idx, lt.wpCboard[idx] & (1UL << eps));
            }
            mkbd ^= val1;
        }

        tmp = lt.DownRightMasks[kpos] & Apieces;
        val1 = MSb(tmp);
        val2 = MSb(tmp ^ val1);
        if ((val2 & ebq) != 0) {
            if ((val1 & bq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (lt.DownRightMasks[kpos] ^ lt.DownRightMasks[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == -1 && (val1 & cb.Pieces[6]) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, lt.bpCboard[idx] & val2);
                if (eps != 64 && (((lt.DownRightMasks[kpos] ^ lt.DownRightMasks[cb.idxs[val2 % 67]]) & (1UL << eps)) != 0))
                    myMoves.Add(idx, lt.bpCboard[idx] & (1UL << eps));
            }
            mkbd ^= val1;
        }

        tmp = lt.DownLeftMasks[kpos] & Apieces;
        val1 = MSb(tmp);
        val2 = MSb(tmp ^ val1);
        if ((val2 & ebq) != 0) {
            if ((val1 & bq) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, (lt.DownLeftMasks[kpos] ^ lt.DownLeftMasks[cb.idxs[val2 % 67]] ^ (1UL << idx)) & rem_k);
            }
            else if (color == -1 && (val1 & cb.Pieces[6]) != 0) {
                int idx = cb.idxs[val1 % 67];
                myMoves.Add(idx, lt.bpCboard[idx] & val2);
                if (eps != 64 && (((lt.DownLeftMasks[kpos] ^ lt.DownLeftMasks[cb.idxs[val2 % 67]]) & (1UL << eps)) != 0))
                    myMoves.Add(idx, lt.bpCboard[idx] & (1UL << eps));
            }
            mkbd ^= val1;
        }

        return;
    }

    public void Ka_zero_pieceMovement(ref ChessBoard cb, ref MoveList myMoves) {
        mkbd = 0;
        PinnedPieces(ref cb, ref myMoves);
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
        if ((LSb(lt.RightMasks[idx] & Apieces) & res) != 0) return res;
        if ((MSb(lt.LeftMasks[idx] & Apieces) & res) != 0) return res;
        if ((LSb(lt.UpMasks[idx] & Apieces) & res) != 0) return res;
        if ((MSb(lt.DownMasks[idx] & Apieces) & res) != 0) return res;
        return 0;
    }

    private ulong BMovesC(int idx, ulong res) {
        if ((LSb(lt.UpRightMasks[idx] & Apieces) & res) != 0) return res;
        if ((LSb(lt.UpLeftMasks[idx] & Apieces) & res) != 0) return res;
        if ((MSb(lt.DownRightMasks[idx] & Apieces) & res) != 0) return res;
        if ((MSb(lt.DownLeftMasks[idx] & Apieces) & res) != 0) return res;
        return 0;
    }

    private ulong NMovesC(int idx, ulong res) {
        if ((lt.KnightMasks[idx] & res) != 0) return res;
        return 0;
    }

    private ulong PMovesC(int idx, ulong res) {
        if (color == 1) {
            if ((lt.wpCboard[idx] & res) != 0) return res;
            if (eps != 64 && (lt.wpCboard[idx] & (1UL << eps)) != 0) return (1UL << eps);
        }
        if (color == -1) {
            if ((lt.bpCboard[idx] & res) != 0) return res;
            if (eps != 64 && (lt.bpCboard[idx] & (1UL << eps)) != 0) return (1UL << eps);
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
        else if (type == 3) myMoves.Add(idx, lt.KnightMasks[idx] & area);
        else if (type == 4) myMoves.Add(idx, RookMovement(idx, ref cb) & area);
        else if (type == 5)
            myMoves.Add(idx, (RookMovement(idx, ref cb) & area) | (BishopMovement(idx, ref cb) & area));
        return;
    }

    private void Ka_PinnedPieces(ref ChessBoard cb) {
        ulong tmp, v1, v2, OwnP = cb.Pieces[7 + 7 * color];
        ulong erq = cb.Pieces[ofs - 5 * color] | cb.Pieces[ofs - 4 * color];
        ulong ebq = cb.Pieces[ofs - 5 * color] | cb.Pieces[ofs - 2 * color];

        tmp = lt.RightMasks[kpos] & Apieces;
        v1 = LSb(tmp); v2 = LSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & erq) != 0) mkbd ^= v1;

        tmp = lt.LeftMasks[kpos] & Apieces;
        v1 = MSb(tmp); v2 = MSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & erq) != 0) mkbd ^= v1;

        tmp = lt.UpMasks[kpos] & Apieces;
        v1 = LSb(tmp); v2 = LSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & erq) != 0) mkbd ^= v1;

        tmp = lt.DownMasks[kpos] & Apieces;
        v1 = MSb(tmp); v2 = MSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & erq) != 0) mkbd ^= v1;

        tmp = lt.UpRightMasks[kpos] & Apieces;
        v1 = LSb(tmp); v2 = LSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & ebq) != 0) mkbd ^= v1;

        tmp = lt.UpLeftMasks[kpos] & Apieces;
        v1 = LSb(tmp); v2 = LSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & ebq) != 0) mkbd ^= v1;

        tmp = lt.DownRightMasks[kpos] & Apieces;
        v1 = MSb(tmp); v2 = MSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & ebq) != 0) mkbd ^= v1;

        tmp = lt.DownLeftMasks[kpos] & Apieces;
        v1 = MSb(tmp); v2 = MSb(tmp ^ v1);
        if ((v1 & OwnP) != 0 && (v2 & ebq) != 0) mkbd ^= v1;

        return;
    }

    public void Ka_pieceMovement(ref ChessBoard cb, ref MoveList myMoves, ref KAinfo atk) {
        ulong val, tmp;
        mkbd = 0;
        Ka_PinnedPieces(ref cb);

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

    public bool InCheck(ref ChessBoard cb) {

        int cl = cb.pColor, idx = cb.idxs[cb.Pieces[ofs + 6 * cl] % 67];
        ulong Apieces = cb.Pieces[ofs + 7] ^ cb.Pieces[ofs - 7];
        ulong erq = cb.Pieces[ofs - 4 * cl] ^ cb.Pieces[ofs - 5 * cl];
        ulong ebq = cb.Pieces[ofs - 2 * cl] ^ cb.Pieces[ofs - 5 * cl];

        ulong res, ans = lt.RightMasks[idx] ^ lt.LeftMasks[idx] ^ lt.UpMasks[idx] ^ lt.DownMasks[idx];
        res = lt.RightMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.RightMasks[cb.idxs[LSb(res) % 67]];
        res = lt.LeftMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.LeftMasks[cb.idxs[MSb(res) % 67]];
        res = lt.UpMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.UpMasks[cb.idxs[LSb(res) % 67]];
        res = lt.DownMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.DownMasks[cb.idxs[MSb(res) % 67]];
        if ((ans & erq) != 0) return true;

        ans = lt.RightMasks[idx] ^ lt.LeftMasks[idx] ^ lt.UpMasks[idx] ^ lt.DownMasks[idx];
        res = lt.UpRightMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.UpRightMasks[cb.idxs[LSb(res) % 67]];
        res = lt.UpLeftMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.UpLeftMasks[cb.idxs[LSb(res) % 67]];
        res = lt.DownRightMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.DownRightMasks[cb.idxs[MSb(res) % 67]];
        res = lt.DownLeftMasks[idx] & Apieces;
        if (res != 0) ans ^= lt.DownLeftMasks[cb.idxs[MSb(res) % 67]];
        if ((ans & ebq) != 0) return true;

        ans = lt.KnightMasks[idx];
        if ((ans & cb.Pieces[ofs - 3 * cl]) != 0) return true;

        ans = cl == 1 ? lt.wpCboard[idx] : lt.bpCboard[idx];
        if ((ans & cb.Pieces[ofs - cl]) != 0) return true;

        return false;
    }

    public void GenerateAttackedSquares(ref ChessBoard cb) {
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
        ans |= lt.KingMasks[ekpos];
        color *= -1;
        Apieces ^= 1UL << kpos;
        Attacked_Squares = ans;
        return;
    }

    public KAinfo FindKingAttackers(ref ChessBoard cb) {
        KAinfo info = new KAinfo();
        if (((1UL << kpos) & Attacked_Squares) == 0) return info;
        ulong res, rem_k = ~(1UL << (kpos));
        ulong rq = cb.Pieces[ofs - 5 * color] | cb.Pieces[ofs - 4 * color];
        ulong bq = cb.Pieces[ofs - 5 * color] | cb.Pieces[ofs - 2 * color];
        if ((lt.UpMasks[kpos] & rq) != 0) {
            res = LSb(lt.UpMasks[kpos] & Apieces);
            if ((rq & res) != 0) {
                int pos = cb.idxs[(rq & res) % 67];
                info.Add((lt.UpMasks[kpos] ^ lt.UpMasks[pos]) & rem_k, 0);
            }
        }

        if ((lt.DownMasks[kpos] & rq) != 0) {
            res = MSb(lt.DownMasks[kpos] & Apieces);
            if ((rq & res) != 0) {
                int pos = cb.idxs[(rq & res) % 67];
                info.Add((lt.DownMasks[kpos] ^ lt.DownMasks[pos]) & rem_k, 0);
            }
        }

        if ((lt.LeftMasks[kpos] & rq) != 0) {
            res = MSb(lt.LeftMasks[kpos] & Apieces);
            if ((rq & res) != 0) {
                int pos = cb.idxs[(rq & res) % 67];
                info.Add((lt.LeftMasks[kpos] ^ lt.LeftMasks[pos]) & rem_k, 0);
            }
        }

        if ((lt.RightMasks[kpos] & rq) != 0) {
            res = LSb(lt.RightMasks[kpos] & Apieces);
            if ((rq & res) != 0) {
                int pos = cb.idxs[(rq & res) % 67];
                info.Add((lt.RightMasks[kpos] ^ lt.RightMasks[pos]) & rem_k, 0);
            }
        }

        if ((lt.UpRightMasks[kpos] & bq) != 0) {
            res = LSb(lt.UpRightMasks[kpos] & Apieces);
            if ((bq & res) != 0) {
                int pos = cb.idxs[(bq & res) % 67];
                info.Add((lt.UpRightMasks[kpos] ^ lt.UpRightMasks[pos]) & rem_k, 0);
            }
        }

        if ((lt.DownLeftMasks[kpos] & bq) != 0) {
            res = MSb(lt.DownLeftMasks[kpos] & Apieces);
            if ((bq & res) != 0) {
                int pos = cb.idxs[(bq & res) % 67];
                info.Add((lt.DownLeftMasks[kpos] ^ lt.DownLeftMasks[pos]) & rem_k, 0);
            }
        }

        if ((lt.UpLeftMasks[kpos] & bq) != 0) {
            res = LSb(lt.UpLeftMasks[kpos] & Apieces);
            if ((bq & res) != 0) {
                int pos = cb.idxs[(bq & res) % 67];
                info.Add(lt.UpLeftMasks[kpos] ^ lt.UpLeftMasks[pos], 0);
            }
        }

        if ((lt.DownRightMasks[kpos] & bq) != 0) {
            res = MSb(lt.DownRightMasks[kpos] & Apieces);
            if ((bq & res) != 0) {
                int pos = cb.idxs[(bq & res) % 67];
                info.Add(lt.DownRightMasks[kpos] ^ lt.DownRightMasks[pos], 0);
            }
        }

        res = lt.KnightMasks[kpos] & cb.Pieces[ofs - 3 * color];
        if (res != 0) info.Add(0, res);

        if (color == 1) res = lt.wpCboard[kpos] & cb.Pieces[6];
        else if (color == -1) res = lt.bpCboard[kpos] & cb.Pieces[8];
        if (res != 0) info.Add(0, res);

        return info;
    }

    public void KingMoves(ref ChessBoard cb, ref MoveList myMoves) {
        ulong ans, Ksquares = lt.KingMasks[kpos];
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


    public MoveList GenerateMoves(ref ChessBoard __pos)
    {
        MoveList move_list = new MoveList(__pos.pColor);
        SetPiece(ref __pos);
        GenerateAttackedSquares(ref __pos);
        
        KAinfo ka_info = FindKingAttackers(ref __pos);
        move_list.pColor = __pos.pColor;
        move_list.KingAttackers = ka_info.attackers;
        
        
        if (ka_info.attackers == 0)
            Ka_zero_pieceMovement(ref __pos, ref move_list);
        if (ka_info.attackers == 1)
            Ka_pieceMovement(ref __pos, ref move_list, ref ka_info);

        KingMoves(ref __pos, ref move_list);
        return move_list;
    }


}



/*


uint64_t
bishop_atk_sq(int __pos, uint64_t _Ap)
{
    const auto area = [__pos, _Ap] (const uint64_t *table, const auto& __func)
    { return table[__func(table[__pos] & _Ap)]; };

    return plt::diag_Board[__pos] ^
          (area(plt::ulBoard, lSb_idx) ^ area(plt::urBoard, lSb_idx)
         ^ area(plt::dlBoard, mSb_idx) ^ area(plt::drBoard, mSb_idx));
}

uint64_t
knight_atk_sq(int __pos, uint64_t _Ap)
{ return plt::NtBoard[__pos] + (_Ap - _Ap); }

uint64_t
rook_atk_sq(int __pos, uint64_t _Ap)
{
    const auto area = [__pos, _Ap] (const uint64_t *table, const auto& __func)
    { return table[__func(table[__pos] & _Ap)]; };

    return plt::line_Board[__pos] ^
          (area(plt::uBoard, lSb_idx) ^ area(plt::dBoard, mSb_idx)
         ^ area(plt::rBoard, lSb_idx) ^ area(plt::lBoard, mSb_idx));
}


*/