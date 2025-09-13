//+------------------------------------------------------------------+
//|                                       LotMarginExposureTable.mq5 |
//|                               Copyright (c) 2021-2022, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2021-2022, Marketeer"
#property link      "https://www.mql5.com/en/users/marketeer"
#property version   "1.0"
#property description "Display a table with lots, risks, margins and exposure estimation for selected symbols.\n"

#include <MQL5Book/LotMarginExposure.mqh>
#include <MQL5Book/Tableau.mqh>
#define TBL_COLUMNS 8

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input ENUM_ORDER_TYPE Action = ORDER_TYPE_BUY;
input string WorkList = "";                   // Symbols (comma,separated,list)
input double Money = 0;                       // Money (0 = free margin)
input double Lot = 0;                         // Lot (0 = min lot)
input double Exposure = 5.0;                  // Exposure (%)
input double RiskLevel = 5.0;                 // RiskLevel (%)
input int RiskPoints = 0;                     // RiskPoints/SL (0 = auto-range of RiskPeriod)
input ENUM_TIMEFRAMES RiskPeriod = PERIOD_W1;
input int UpdateFrequency = 0;                // UpdateFrequency (sec, 0 - once per bar)
input bool PrintToLog = false;
input ENUM_BASE_CORNER Corner = CORNER_RIGHT_LOWER;
input int Gap = 16;
input int FontSize = 8;
input string DefaultFontName = "Consolas";
input string TitleFontName = "Arial Black";
input string MotoTypeFontsHint = "Consolas/Courier/Courier New/Lucida Console/Lucida Sans Typewriter";
input color BackgroundColor = 0x808080;
input uchar BackgroundTransparency = 0xC0;    // BackgroundTransparency (255 - opaque, 0 - glassy)

//+------------------------------------------------------------------+
//| Globals                                                          |
//+------------------------------------------------------------------+
string symbols[];
datetime lastTime;
Tableau *t;
int sortByColumn;

//+------------------------------------------------------------------+
//| Columns of the table                                             |
//+------------------------------------------------------------------+
enum LME_FIELDS // 10 data fields, 3 additional properties of symbol
{
   eLot,
   eAtrPointsNormalized,
   eAtrValue,
   eLotFromExposureRaw,
   eLotFromExposure,
   eLotFromRiskOfStopLossRaw,
   eLotFromRiskOfStopLoss,
   eExposureFromLot,
   eMarginLevelFromLot,
   eLotDig,
   eMinLot,
   eContract,
   eSymbol
};

//+------------------------------------------------------------------+
//| Hints (shown in tooltips on mouse hover) for 8 columns' headers  |
//+------------------------------------------------------------------+
string hints[] =
{
   "Balance",
   "Lot (Exposure,%)",
   "Lot (Risk,%)",
   "Exposure,% (Lot)\nLot=0 means minimal lot",
   "Margin Level,% (Lot)\nLot=0 means minimal lot",
   "Minimal lot",
   "Contract size",
   "P/L for 1 lot (range)\nfor RiskPoints or RiskPeriod"
};

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
void OnInit()
{
   Comment("Starting...");
   lastTime = 0;
   t = NULL;

   if(GlobalVariableCheck("LEMT_SORT")) // restore sorting by column
   {
      sortByColumn = (int)GlobalVariableGet("LEMT_SORT");
      if(sortByColumn == 0 || sortByColumn > TBL_COLUMNS || sortByColumn < -TBL_COLUMNS)
      {
         sortByColumn = INT_MAX;
      }
   }
   else
   {
      sortByColumn = INT_MAX;
   }
   EventSetTimer(1);
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   if(lastTime == 0)           // first time calculation
   {
      OnTick();
      Comment("Started");
   }
   else if(lastTime != -1)
   {
      if(UpdateFrequency <= 0) // if no frequency given, wait for new bar in OnTick
      {
         EventKillTimer();     // drop the timer, no need it anymore
      }
      else if(TimeCurrent() - lastTime >= UpdateFrequency)
      {
         lastTime = LONG_MAX;  // prevent reentrancy to this if-branch
         OnTick();
         if(lastTime != -1)    // OnTick processed without error
         {
            lastTime = TimeCurrent(); // update the time mark
         }
      }
      Comment("");
   }
}

//+------------------------------------------------------------------+
//| Find a single character mark for account currency                |
//+------------------------------------------------------------------+
string getCurrencyMark()
{
   string acc = AccountInfoString(ACCOUNT_CURRENCY);
   StringToUpper(acc);
   if(acc == "USD") return "$";
   if(acc == "EUR") return "€";
   if(acc == "GBP") return "£";
   if(acc == "JPY") return "¥";
   return StringSubstr(acc, 0, 1);
}

//+------------------------------------------------------------------+
//| Short presentation of Kilo-numbers                               |
//+------------------------------------------------------------------+
template<typename T>
string compact(T v, string fmt)
{
   if(v > 1000)
   {
      if(StringReplace(fmt, "%%", "K%%") <= 0)
      {
         fmt += "K";
      }
      return StringFormat(fmt, (T)(v / 1000));
   }
   return StringFormat(fmt, v);
}

//+------------------------------------------------------------------+
//| Helper equivalent of a string like number, used for unified sort |
//+------------------------------------------------------------------+
double pack2double(const string s)
{
   double r = 0;
   for(int i = 0; i < StringLen(s); i++)
   {
      r = (r * 255) + (StringGetCharacter(s, i) % 255);
   }
   return r;
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   if(lastTime == -1) return; // error was already there, just exit
  
   if(UpdateFrequency <= 0)
   {
      if(lastTime == iTime(NULL, 0, 0)) return; // wait for a new bar
   }
   else if(TimeCurrent() - lastTime < UpdateFrequency)
   {
      return;
   }

   const int ns = StringSplit((WorkList == "" ? _Symbol : WorkList), ',', symbols);
   if(ns <= 0)
   {
      Print("Empty symbols");
      lastTime = -1;
      return;
   }

   if(Exposure > 100 || Exposure <= 0)
   {
      Print("Percent of Exposure is incorrect: ", Exposure);
      lastTime = -1;
      return;
   }

   if(RiskLevel > 100 || RiskLevel <= 0)
   {
      Print("Percent of RiskLevel is incorrect: ", RiskLevel);
      lastTime = -1;
      return;
   }

   lastTime = UpdateFrequency > 0 ? TimeCurrent() : iTime(NULL, 0, 0);

   const double stopOutLevel = AccountInfoDouble(ACCOUNT_MARGIN_SO_CALL);
   const int digits = (int)AccountInfoInteger(ACCOUNT_CURRENCY_DIGITS);
   const double money = Money == 0 ? AccountInfoDouble(ACCOUNT_MARGIN_FREE) : Money;

   string used = "";
   if(Money == 0 && AccountInfoDouble(ACCOUNT_MARGIN) > 0)
   {
      hints[0] = "Free\nIn Use: " + StringFormat("%.*f", digits, AccountInfoDouble(ACCOUNT_MARGIN));
   }
   else
   {
      hints[0] = "Balance";
   }
   
   hints[0] += "\nStop Out: " + (string)(float)stopOutLevel;

   LEMLR::SymbolLotExposureRisk r = {};

   // In total 13 fields per symbol are stored in 'LME' array (below)
   // and used to render the table in UI.
   // In the table 8 visible columns comprise:
   // - 5 columns formed from 10 calculated values (marginal characteristics)
   // - 3 columns with additional properties (symbol name, minimal lot, contact size)
   // 
   // * symbol
   // lotFromExposure/lotFromExposureRaw
   // lotFromRiskOfStopLoss/lotFromRiskOfStopLossRaw
   // exposureFromLot
   // marginLevelFromLot
   // * minlot
   // * contract
   // atrValue
  
   double LME[][13];
   ArrayResize(LME, ns);
   ArrayInitialize(LME, 0);
  
   for(int i = 0; i < ns; i++)
   {
      if(!LEMLR::Estimate(Action, symbols[i], Lot, 0,
         Exposure, RiskLevel, RiskPoints, RiskPeriod, Money, r))
      {
        Print("Calc failed (will try on the next bar, or refresh manually)");
        return;
      }
      
      LME[i][eLot] = r.lot;
      LME[i][eAtrPointsNormalized] = r.atrPointsNormalized;
      LME[i][eAtrValue] = r.atrValue;
      LME[i][eLotFromExposureRaw] = r.lotFromExposureRaw;
      LME[i][eLotFromExposure] = r.lotFromExposure;
      LME[i][eLotFromRiskOfStopLossRaw] = r.lotFromRiskOfStopLossRaw;
      LME[i][eLotFromRiskOfStopLoss] = r.lotFromRiskOfStopLoss;
      LME[i][eExposureFromLot] = r.exposureFromLot;
      LME[i][eMarginLevelFromLot] = r.marginLevelFromLot;
      LME[i][eLotDig] = r.lotDigits;
      LME[i][eMinLot] = SymbolInfoDouble(symbols[i], SYMBOL_VOLUME_MIN);
      LME[i][eContract] = SymbolInfoDouble(symbols[i], SYMBOL_TRADE_CONTRACT_SIZE);
      LME[i][eSymbol] = pack2double(symbols[i]);
   }

   // prepare the string array to render the table in UI
   string data[];
   ArrayResize(data, (ns + 1) * TBL_COLUMNS);
  
   const string C = getCurrencyMark();
   const int lotDigits = Lot <= 0.0 ? 0 : (int)MathLog10(1.0 / Lot);
   
   // the first row in the header
   data[0] = StringFormat("%s%.*f%s", C, digits, money, used);
   data[1] = StringFormat("L(E=%.*f%%)", 1, Exposure);
   data[2] = StringFormat("L(R=%.*f%%)", 1, RiskLevel);
   data[3] = StringFormat("E%%(L=%.*f)", lotDigits, Lot);
   data[4] = StringFormat("M%%(L=%.*f)", lotDigits, Lot);
   data[5] = "MinL";
   data[6] = "Contract";
   data[7] = StringFormat("Risk%s",
      (RiskPoints == 0 ?
      "(" + StringSubstr(EnumToString(RiskPeriod), StringLen("PERIOD_")) + ")" :
      "(" + (string)RiskPoints + (RiskPoints < 0 ? "%)" : "pt)")));

   double sorter[][2];

   if(sortByColumn != INT_MAX) // if some column was clicked, reorder by its values
   {
      ArrayResize(sorter, ns);
      const int s = sortByColumn != INT_MAX ? MathAbs(sortByColumn) : 1;
     
      // from column number to field index
      static int converter[] = {eSymbol, eLotFromExposureRaw, eLotFromRiskOfStopLossRaw,
         eExposureFromLot, eMarginLevelFromLot, eMinLot, eContract, eAtrValue};
     
      // copy selected column to 0-th for sorting
      for(int i = 0; i < ns; i++)
      {
         sorter[i][0] = LME[i][converter[s - 1]];
         sorter[i][1] = i;
      }
   
      ArraySort(sorter);
      if(sortByColumn < 1)
      {
         ArrayReverse(sorter);
      }
      data[MathAbs(sortByColumn) - 1] += sortByColumn > 0 ? "˅" : "˄";
   }
  
   int c = TBL_COLUMNS; // place cursor right after header strings

   // fill array with actual values, stringified using appropriate formatting
   for(int j = 0; j < ns; j++)
   {
      int i = (int)(sortByColumn != INT_MAX ? sorter[j][1] : j);
      data[c + 0] = symbols[i];
      data[c + 1] = StringFormat("%s", (LME[i][eLotFromExposure] == 0 ?
         "(" + (string)(float)LME[i][eLotFromExposureRaw] + ")" :
         StringFormat("%.*f", MathMax((int)LME[i][eLotDig], 2), LME[i][eLotFromExposure])));
      data[c + 2] = StringFormat("%.*f %s", MathMax((int)LME[i][eLotDig], 2),
         LME[i][eLotFromRiskOfStopLoss], (LME[i][eLotFromRiskOfStopLoss] == 0 ?
         "(" + (string)(float)LME[i][eLotFromRiskOfStopLossRaw] + ")" : ""));
      data[c + 3] = StringFormat("%.2f%%", LME[i][eExposureFromLot]);
      data[c + 4] = StringFormat("%s%%", compact(LME[i][eMarginLevelFromLot], "%.2f"));
      data[c + 5] = StringFormat("%.*f", MathMax((int)LME[i][eLotDig], 2),
         SymbolInfoDouble(symbols[i], SYMBOL_VOLUME_MIN));
      data[c + 6] = StringFormat("%s", compact((int)SymbolInfoDouble(symbols[i],
         SYMBOL_TRADE_CONTRACT_SIZE), "%d"));
      data[c + 7] = StringFormat("%s%s", (RiskPoints >= 0 ? C + compact(LME[i][eAtrValue], "%.2f") : ""),
         (RiskPoints <= 0 ? "(" + compact((int)LME[i][eAtrPointsNormalized], "%d") + "pt)" : ""));
      c += TBL_COLUMNS;
   }

   // create the table object if not exist already
   if(t == NULL)
   {
      t = new Tableau("LEMT", ns + 1, TBL_COLUMNS, -1, 100,
         Corner, Gap, FontSize, DefaultFontName, TitleFontName,
         TBL_FLAG_COL_0_HEADER | TBL_FLAG_ROW_0_HEADER,
         BackgroundColor, BackgroundTransparency);
   }
   if(PrintToLog)
   {
      ArrayPrint(data);
   }
   t.fill(data, hints); // display the data array into the table
}

//+------------------------------------------------------------------+
//| Chart event handler (used for sorting by columns)                |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   if(id == CHARTEVENT_OBJECT_CLICK)
   {
      if(StringFind(sparam, "LEMT") == 0)
      {
         string parts[];
         if(StringSplit(sparam, '_', parts) == 3)
         {
            const int s = (int)StringToInteger(parts[2]) + 1;
            if(s != MathAbs(sortByColumn))
            {
               sortByColumn = s;
            }
            else
            {
               if(sortByColumn > 0)
               {
                  sortByColumn = -sortByColumn; // change direction
               }
               else
               {
                  sortByColumn = INT_MAX;
               }
            }
        
            if(sortByColumn != INT_MAX)
            {
               GlobalVariableSet("LEMT_SORT", sortByColumn);
            }
            else
            {
               GlobalVariableDel("LEMT_SORT");
            }
        
            lastTime = 0;
            OnTick();
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Finalization function                                            |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   if(CheckPointer(t) == POINTER_DYNAMIC) delete t;
}
//+------------------------------------------------------------------+
