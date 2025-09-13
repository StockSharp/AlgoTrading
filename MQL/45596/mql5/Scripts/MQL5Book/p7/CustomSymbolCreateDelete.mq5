//+------------------------------------------------------------------+
//|                                     CustomSymbolCreateDelete.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Create or delete specified custom symbol."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

input string CustomSymbol = "Dummy";         // Custom Symbol Name
input string CustomPath = "MQL5Book\\Part7"; // Custom Symbol Folder
input string Origin;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   bool custom = false;
   if(!PRTF(SymbolExist(CustomSymbol, custom)))
   {
      if(IDYES == MessageBox("Create new custom symbol?", "Please, confirm", MB_YESNO))
      {
         PRTF(CustomSymbolCreate(CustomSymbol, CustomPath, Origin));
      }
   }
   else
   {
      if(custom)
      {
         if(IDYES == MessageBox("Delete existing custom symbol?", "Please, confirm", MB_YESNO))
         {
            PRTF(CustomSymbolDelete(CustomSymbol));
         }
      }
      else
      {
         Print("Can't delete non-custom symbol");
      }
   }
}
//+------------------------------------------------------------------+
