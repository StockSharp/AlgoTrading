//+------------------------------------------------------------------+
//|                                      SymbolFilterDescription.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print out text properties for selected symbols                   |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/SymbolFilter.mqh>

input string SearchPattern = "";

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SymbolFilter f;                      // filter object
   string symbols[];                    // array for resulting names
   string text[][4];                    // array for output data
   
   // specify properties to read from symbols
   ENUM_SYMBOL_INFO_STRING fields[] =
   {
      SYMBOL_DESCRIPTION,
      SYMBOL_SECTOR_NAME,
      SYMBOL_COUNTRY,
      SYMBOL_PATH
   };
   
   if(SearchPattern != "")
   {
      f.let(SYMBOL_DESCRIPTION, SearchPattern);
   }
   
   // apply the filter and build symbol list, sorted by description
   f.select(true, fields, symbols, text, true);
   
   const int n = ArraySize(symbols);
   PrintFormat("===== Text fields for symbols (%d) =====", n);
   for(int i = 0; i < n; ++i)
   {
      Print(symbols[i] + ":");
      ArrayPrint(text, 0, NULL, i, 1, 0);
   }
}
//+------------------------------------------------------------------+
/*

   example output (excerpt):

      ===== Text fields for symbols (16) =====
      AUDUSD:
      "Australian Dollar vs US Dollar" "Currency"  ""  "Forex\AUDUSD"                  
      EURUSD:
      "Euro vs US Dollar" "Currency"  ""  "Forex\EURUSD"     
      UK100:
      "FTSE 100 Index" "Undefined"  ""  "Indexes\UK100" 
      XAUUSD:
      "Gold vs US Dollar" "Commodities"  ""  "Metals\XAUUSD"    
      JAGG:
          "JPMorgan U.S. Aggregate Bond ETF"  "Financial"                           
          "USA"  "ETF\United States\NYSE\JPMorgan\JAGG"
      NZDUSD:
      "New Zealand Dollar vs US Dollar" "Currency"  ""  "Forex\NZDUSD"                   
      GBPUSD:
      "Pound Sterling vs US Dollar" "Currency"  ""  "Forex\GBPUSD"               
      SP500m:
      "Standard & Poor's 500" "Undefined"  ""  "Indexes\SP500m"       
      FIHD:
          "UBS AG FI Enhanced Global High Yield ETN" "Financial"                               
          "USA"  "ETF\United States\NYSE\UBS\FIHD"         
      ...
   
*/
//+------------------------------------------------------------------+
