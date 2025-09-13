//+---------------------------------------------------------------------+
//|                                                        SudokuUI.mq5 |
//|                                       Copyright (c) 2019, Marketeer |
//|                             https://www.mql5.com/en/users/marketeer |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2019, Marketeer"
#property link      "https://www.mql5.com/en/users/marketeer"
#property version   "1.4"
#property description "Classic Sudoku puzzle - allows you to generate, import, solve, and play sudoku."

// COMMENT OUT THE FOLLOWING LINE BEFORE COMPILATION
// IT'S REQUIRED ONLY TO PASS MQL CODEBASE SUBMISSION,
// WHICH STRANGELY DO NOT SUPPORT BMP-FILES,
// WHILE BMP IS THE MAIN TYPE OF GRAPHIC RESOURCES IN MQL!

#define CODEBASE_CHECKUP

// NB! IF NOT COMMENTED, THIS LINE WILL MAKE SOME CONTROL BUTTONS UNAVALIABLE IN GUI
// The bmp-files are attached as ZIP-archive,
// it should be unpacked into MQL5\Include\Sudoku\Layouts\res\


// #define SUDOKU_LOG_USER_MOVES // use this to note user moves and undos in the expert log
#include "SudokuUI.mqh"


input string _Sudoku = ""; // Sudoku File
input int ShufflingRandomSeed = -1;
input int CompositionRandomSeed = -1;
input uint ShufflingCycles = 100;
input uint EliminateLabel = 0;
input bool EnableAutoUpdate = false; // Auto-Assistant


SudokuDialog MainDialog;

int OnInit()
{
  if(!MainDialog.Create(0, "Classic Sudoku Puzzle", 0, 50, 50, 260, 290))
    return (INIT_FAILED);

  MainDialog.randomize(ShufflingRandomSeed, CompositionRandomSeed, EliminateLabel, ShufflingCycles);
  MainDialog.preload(_Sudoku);
  MainDialog.enableAutoUpdate(EnableAutoUpdate);

  if(!MainDialog.Run())
    return (INIT_FAILED);

  return (INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
  MainDialog.Destroy(reason);
  Comment("");
}

void OnTick()
{
}

void OnChartEvent(const int id, const long &lparam, const double &dparam,
                  const string &sparam)
{
  MainDialog.ChartEvent(id, lparam, dparam, sparam);
}
