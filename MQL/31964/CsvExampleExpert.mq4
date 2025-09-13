//+------------------------------------------------------------------+
//|                                             CsvExampleExpert.mq4 |
//|                                 Copyright 2020, LucasInvestments |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, LucasInvestments"
#property link      ""
#property version   "1.00"
#property strict
#define cmd0 OP_BUY
#define cmd1 OP_SELL
enum op {Buy,Sell};
extern double TradedLot=0.1;//----Lots
extern double take = 300;//----TP points
extern double stop = 300;//----SL points
input op OpType = Sell;//----Order type
extern bool WriteCloseData = false;//----Write order data to csv
input string FileName = "CSVexpert\\CSVexample.csv";//----Directory and file name
int cmd,tkt,handle;
double checkedLot;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(OpType==Sell)
      cmd=cmd1;
   else
      cmd=cmd0;
   if(WriteCloseData)
     {
      handle=FileOpen(FileName,FILE_CSV|FILE_READ|FILE_WRITE,",");
      FileWrite(handle,"OPType","Gain/Loss","ClosePrice","CloseTime","Symbol","Lots");
      FileClose(handle);
     }
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CheckVolumeValue(double vol)
  {
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(vol<min_volume)
      return(min_volume);
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(vol>max_volume)
      return(max_volume);
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   int ratio=(int)MathRound(vol/volume_step);
   if(MathAbs(ratio*volume_step-vol)>0.0000001)
      return(ratio*volume_step);
   return(vol);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb, double lots,int type)
  {
   double free_margin=AccountFreeMarginCheck(symb,type,lots);
   if(free_margin<0)
     {
      string oper=(type==OP_BUY)? "Buy":"Sell";
      Print("Not enough money for ", oper," ",lots, " ", symb, " Error code=",GetLastError());
      return(false);
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void GetOrders(int op_direction)
  {
   checkedLot=CheckVolumeValue(TradedLot);
   if(CheckMoneyForTrade(Symbol(),checkedLot,op_direction))
      if(op_direction==cmd0)
        {
         if(OrdersTotal()<1)
            tkt=OrderSend(Symbol(),OP_BUY,checkedLot,Ask,5,0,0,NULL,456,0,Lime);
         if(tkt<0)
            return;
        }
      else
        {
         if(OrdersTotal()<1)
            tkt=OrderSend(Symbol(),OP_SELL,checkedLot,Bid,5,0,0,NULL,456,0,Red);
         if(tkt<0)
            return;
        }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseOrders()
  {
   for(int i = OrdersTotal() - 1; i >= 0; i--)
     {
      if(!OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
         break;
      if((OrderType()==OP_BUY)&&(OrderSymbol() == Symbol() && OrderMagicNumber() == 456))
        {
         if(OrderOpenPrice()-Ask>=Point*stop||Ask-OrderOpenPrice()>=Point*take)
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 5, NULL))
               return;
            else
              {
               if(WriteCloseData)
                 {
                  if(OrderSelect(OrderTicket(),SELECT_BY_TICKET,MODE_HISTORY)==TRUE)
                    {
                     handle=FileOpen(FileName,FILE_CSV|FILE_READ|FILE_WRITE,",");
                     FileSeek(handle,0,SEEK_END);
                     FileWrite(handle,"LONG",OrderProfit(),OrderClosePrice(),OrderCloseTime(),OrderSymbol(),OrderLots());
                     FileClose(handle);
                    }
                 }
              }
        }
      if((OrderType() == OP_SELL)&&(OrderSymbol() == Symbol() && OrderMagicNumber() == 456))
        {
         if(OrderOpenPrice()-Bid>=Point*take||Bid-OrderOpenPrice()>=Point*stop)
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 5, NULL))
               return;
            else
              {
               if(WriteCloseData)
                 {
                  if(OrderSelect(OrderTicket(),SELECT_BY_TICKET,MODE_HISTORY)==TRUE)
                    {
                     handle=FileOpen(FileName,FILE_CSV|FILE_READ|FILE_WRITE,",");
                     FileSeek(handle,0,SEEK_END);
                     FileWrite(handle,"SHORT",OrderProfit(),OrderClosePrice(),OrderCloseTime(),OrderSymbol(),OrderLots());
                     FileClose(handle);
                    }
                 }
              }
        }
     }
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   GetOrders(cmd);
   CloseOrders();
  }
//+------------------------------------------------------------------+
