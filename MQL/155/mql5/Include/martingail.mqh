//+------------------------------------------------------------------+
//|                                                   Martingail.mqh |
//|                        Copyright 2010, MetaQuotes Software Corp. |
//|        http://forum.liteforex.org/showthread.php?p=6210#post6210 |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, Alf."
#property link      "http://forum.liteforex.org/showthread.php?p=6210#post6210"
//+------------------------------------------------------------------+
//| Martingail                                                       |
//+------------------------------------------------------------------+
class Martingail
  {
private:
   int               ud;
public:
   double            Shape;
   int               DoublingCount;
   string            GVarName;
   void              GVarGet();
   void              GVarSet();
   double            Lot();
  };
//+------------------------------------------------------------------+
//| GVarGet                                                          |
//+------------------------------------------------------------------+
void Martingail::GVarGet(void)
  {
   if(GlobalVariableCheck(GVarName)) GlobalVariableSet(GVarName,0);
   ud=(int)GlobalVariableGet(GVarName);
  }
//+------------------------------------------------------------------+
//| GVarSet                                                          |
//+------------------------------------------------------------------+
void Martingail::GVarSet(void)
  {
   GlobalVariableSet(GVarName,ud);
  }
//+------------------------------------------------------------------+
//| Lot                                                              |
//+------------------------------------------------------------------+
double Martingail::Lot(void)
  {
   double Lot=MathFloor(AccountInfoDouble(ACCOUNT_BALANCE)/Shape)*SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   if(Lot==0)Lot=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   if(DoublingCount<=0) return Lot;
   double MaxLot=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX);

   if(Lot>MaxLot)Lot=MaxLot;
   double lt1=Lot;
   HistorySelect(0,TimeCurrent());
   if(HistoryOrdersTotal()==0)return(Lot);
   double cl=HistoryOrderGetDouble(HistoryOrderGetTicket(HistoryOrdersTotal()-1),ORDER_PRICE_OPEN);
   double op=HistoryOrderGetDouble(HistoryOrderGetTicket(HistoryOrdersTotal()-2),ORDER_PRICE_OPEN);

   long typeor=HistoryOrderGetInteger(HistoryOrderGetTicket(HistoryOrdersTotal()-2),ORDER_TYPE);
   if(typeor==ORDER_TYPE_BUY)
     {
      if(op>cl)
        {
         if(ud<DoublingCount)
           {
            lt1=HistoryOrderGetDouble(HistoryOrderGetTicket(HistoryOrdersTotal()-2),ORDER_VOLUME_INITIAL)*2;
            ud++;
           }
         else ud=0;
        }
      else ud=0;
     }
   if(typeor==ORDER_TYPE_SELL)
     {
      if(cl>op)
        {
         if(ud<DoublingCount)
           {
            lt1=HistoryOrderGetDouble(HistoryOrderGetTicket(HistoryOrdersTotal()-2),ORDER_VOLUME_INITIAL)*2;
            ud++;
           }
         else ud=0;
        }
      else ud=0;
     }
   if(lt1>MaxLot)lt1=MaxLot;
   return(lt1);
  }
//+------------------------------------------------------------------+
