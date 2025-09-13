//+------------------------------------------------------------------+
//|                                                  TesterTable.mqh |
//|                                 Copyright 2015, Vasiliy Sokolov. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, Vasiliy Sokolov."
#property link      "http://www.mql5.com"
#include <Prototypes.mqh>
//+------------------------------------------------------------------+
//| TesterTable.mqh                                                  |
//| Copyright 2015, Vasiliy Sokolov.                                 |
//| http://www.mql5.com                                              |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, Vasiliy Sokolov."
#property link "http://www.mql5.com"
#include <Prototypes.mqh>
//+------------------------------------------------------------------+
//| Class displays a simple table status active positions in         |
//| graph. Can be used for visualization in the tester.              |
//+------------------------------------------------------------------+
class CTesterTable
{
private:
ulong m_magic; // Magic number. If equal to zero, to display information about all positions.
string OrderType(void);
string Id(void);
string Volume(void);
string SL(void);
string TP(void);
string CurrentProfit(void);
string State();

public:
void SetExpertMagicNumber(ulong mg){m_magic = mg;}
void PrintTable();
};
//+------------------------------------------------------------------+
//| Returns the order type as a string. Returns an empty string      |
//| if the order type could not be obtained.                         |
//+------------------------------------------------------------------+
string CTesterTable::OrderType(void)
{
ENUM_ORDER_TYPE type = (ENUM_ORDER_TYPE)HedgePositionGetInteger(HEDGE_POSITION_TYPE);
string values[];
StringSplit(EnumToString(type), '_', values);
string result = "";
for(int i = 2; i < ArraySize(values); i++)
result += values[i] + " ";
return result;
}
//+------------------------------------------------------------------+
//| Returns the ID of the selected position.                         |
//+------------------------------------------------------------------+
string CTesterTable::Id(void)
{
return (string)HedgePositionGetInteger(HEDGE_POSITION_ENTRY_ORDER_ID);
}
//+------------------------------------------------------------------+
//| Returns the volume of the selected position.                     |
//+------------------------------------------------------------------+
string CTesterTable::Volume(void)
{
double vol = HedgePositionGetDouble(HEDGE_POSITION_VOLUME);
return DoubleToString(vol, 2);
}
//+------------------------------------------------------------------+
//| Returns the stop loss level selected position.                   |
//+------------------------------------------------------------------+
string CTesterTable::SL(void)
{
double sl = HedgePositionGetDouble(HEDGE_POSITION_SL);
string symbol = HedgePositionGetString(HEDGE_POSITION_SYMBOL);
int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
return DoubleToString(sl, digits);
}
//+------------------------------------------------------------------+
//| Returns the level of the take profit selected position. |
//+------------------------------------------------------------------+
string CTesterTable::TP(void)
{
double tp = HedgePositionGetDouble(HEDGE_POSITION_TP);
string symbol = HedgePositionGetString(HEDGE_POSITION_SYMBOL);
int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
return DoubleToString(tp, digits);
}


//+------------------------------------------------------------------+
//| Returns the level of the take profit selected position.          |
//+------------------------------------------------------------------+
string CTesterTable::CurrentProfit(void)
{
   double cp = HedgePositionGetDouble(HEDGE_POSITION_PROFIT_CURRENCY);
   string  symbol = HedgePositionGetString(HEDGE_POSITION_SYMBOL);
   int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
   return DoubleToString(cp, digits);
}
//+------------------------------------------------------------------+
//| Returns the state of the current position.                       |
//+------------------------------------------------------------------+
string CTesterTable::State()
{
   ENUM_HEDGE_POSITION_STATE state = (ENUM_HEDGE_POSITION_STATE)HedgePositionGetInteger(HEDGE_POSITION_STATE);
   return EnumToString(state);
}
//+------------------------------------------------------------------+
//| Prints a table of the current position by using Comment().       |
//+------------------------------------------------------------------+
void CTesterTable::PrintTable(void)
{
   string t = "  ";
   string line = "# Type  Vol.   SL         TP          Profit\n";
   for(int i = 0; i < TransactionsTotal(); i++)
   {
      if(!TransactionSelect(i))continue;
      if(TransactionType() != TRANS_HEDGE_POSITION)continue;
      if(m_magic != 0)
      {
         if(HedgePositionGetInteger(HEDGE_POSITION_MAGIC) != m_magic)
            continue;
      }
      line += (string)(i+1) + t + OrderType() + t + Volume() + t + SL() + t + TP() + t + CurrentProfit() + "\n";
   }   
   Comment(line);
}