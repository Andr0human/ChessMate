import { FILES, PIECE_SYMBOLS, RANKS } from "./constants";

export const formatTime = (timeMs) => {
  const totalSeconds = Math.floor(timeMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
};

// Convert chess.js position to algebraic notation
export const squareToAlgebraic = (row, col, boardFlipped) => {
  // If board is flipped, we need to invert the coordinates for algebraic notation
  const adjustedCol = boardFlipped ? 7 - col : col;
  const adjustedRow = boardFlipped ? 7 - row : row;

  return FILES[adjustedCol] + RANKS[adjustedRow];
};

export const getPieceSymbol = (piece) => {
  if (!piece) return null;

  return PIECE_SYMBOLS[piece.type] || "";
};

// Generate a random 6-character roomId
export const generateRoomId = () => {
  const chars =
    "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
  let result = "";
  for (let i = 0; i < 6; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
};

export const generateBoardOptions = ({ side, timeControl, increment }) => {
  return {
    side,
    timeControl,
    increment,
    players: {
      white: side === "white" ? "Player1" : "Player2",
      black: side === "black" ? "Player1" : "Player2",
    },
  };
};
