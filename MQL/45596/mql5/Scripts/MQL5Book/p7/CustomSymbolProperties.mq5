//+------------------------------------------------------------------+
//|                                       CustomSymbolProperties.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Create or delete specified custom symbol based on current chart's symbol."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/CustomSymbolMonitor.mqh>

input string CustomSymbol = "Dummy";         // Custom Symbol Name
input string CustomPath = "MQL5Book\\Part7"; // Custom Symbol Folder
input bool AutoUnselectInMarketWatch = true;
input bool ReverseOrder = false;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   bool custom = false;
   if(!PRTF(SymbolExist(CustomSymbol, custom)))
   {
      if(IDYES == MessageBox(StringFormat("Create new custom symbol based on %s?", _Symbol),
         "Please, confirm", MB_YESNO))
      {
         if(PRTF(CustomSymbolCreate(CustomSymbol, CustomPath)))
         {
            // this will virtually move the symbol into _Symbol's path inside Custom folder
            CustomSymbolMonitor cs(CustomSymbol, _Symbol);
            cs.setAll(ReverseOrder);
         }
      }
   }
   else
   {
      if(custom)
      {
         if(IDYES == MessageBox("Delete existing custom symbol?", "Please, confirm", MB_YESNO))
         {
            if(AutoUnselectInMarketWatch) // without this we can't delete implicitly selected symbol
            {
               CustomSymbolMonitor cs(CustomSymbol);
               if(PRTF(cs.get(SYMBOL_SELECT)) && PRTF(!cs.get(SYMBOL_VISIBLE)))
               {
                  Print("Unselecting implicitly selected symbol");
                  // SYMBOL_SELECT is read-only
                  // PRTF(cs.set(SYMBOL_SELECT, false));
                  PRTF(SymbolSelect(CustomSymbol, false));
               }
            }
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
