//+------------------------------------------------------------------+
//|                                    VBS - Very Blondie System.mq4 |
//|                                                            David |
//|                                               Broker77@gmail.com |
//+------------------------------------------------------------------+
#property copyright "David"
#property link      "broker77@gmail.com"
#define MAGICMA  20081109

extern int PeriodX = 60;
extern int Limit = 1000;

extern int Grid = 1500;
extern int Amount = 40;

extern int LockDown = 0;
int Slippage = 50;

//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
  {
//---- check for history and trading
   if(Bars<100 || IsTradeAllowed()==false) return;
//---- calculate open orders by current symbol
   if(OrdersTotal()==0) CheckForOpen();
   else CheckForClose();
//----
  }
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
int CheckForOpen()
{
  double L = Low[iLowest(NULL,0,MODE_LOW,PeriodX,0)];
  double H = High[iHighest(NULL,0,MODE_HIGH,PeriodX,0)];
  double Lots = MathRound(AccountBalance()/100)/1000;
  
  if((H-Bid>Limit*Point))
    {OrderSend(Symbol(),OP_BUY,Lots,Ask,1,0,0,"",MAGICMA,0,CLR_NONE);
     for(int i=1; i<5; i++){OrderSend(Symbol(),OP_BUYLIMIT,MathPow(2,i)*Lots,Ask-i*Grid*Point,1,0,0,"",MAGICMA,0,CLR_NONE);}
    }
    
  if((Bid-L>Limit*Point))
    {OrderSend(Symbol(),OP_SELL,Lots,Bid,1,0,0,"",MAGICMA,0,CLR_NONE);
     for(int j=1; j<5; j++){OrderSend(Symbol(),OP_SELLLIMIT,MathPow(2,j)*Lots,Bid+j*Grid*Point,1,0,0,"",MAGICMA,0,CLR_NONE);}
    }
    
}  
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
int CheckForClose()
{
  if(getProfit()>=Amount){CloseAll();}
    
  if(LockDown>0)
  {
    for(int TradeNumber = OrdersTotal(); TradeNumber >= 0; TradeNumber--)
    {
      if (OrderSelect(TradeNumber, SELECT_BY_POS, MODE_TRADES)&&(LockDown>0))
      { int Pos=OrderType();
        if((Pos==OP_BUY)&&(Bid-OrderOpenPrice()>Point*LockDown)&&(OrderStopLoss() == 0))
        {OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point,OrderTakeProfit(),0,CLR_NONE);}
        if((Pos==OP_SELL)&&(OrderOpenPrice()-Ask>Point*LockDown)&&(OrderStopLoss() == 0))
        {OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point,OrderTakeProfit(),0,CLR_NONE);}
      }
    }
  } 

}  
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
double getProfit()
{
   double Profit = 0;
   for (int TradeNumber = OrdersTotal(); TradeNumber >= 0; TradeNumber--)
   {
     if (OrderSelect(TradeNumber, SELECT_BY_POS, MODE_TRADES))
     Profit = Profit + OrderProfit() + OrderSwap();
   }
   return (Profit);
}
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
void CloseAll()
{
   bool   Result;
   int    i,Pos,Error;
   int    Total=OrdersTotal();
   
   if(Total>0)
   {
     for(i=Total-1; i>=0; i--) 
     {
       if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == TRUE) 
       {
         Pos=OrderType();
         if(Pos==OP_BUY)
         {Result=OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, CLR_NONE);}
         if(Pos==OP_SELL)
         {Result=OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, CLR_NONE);}
         if((Pos==OP_BUYSTOP)||(Pos==OP_SELLSTOP)||(Pos==OP_BUYLIMIT)||(Pos==OP_SELLLIMIT))
         {Result=OrderDelete(OrderTicket(), CLR_NONE);}
//-----------------------
         if(Result!=true) 
          { 
             Error=GetLastError(); 
             Print("LastError = ",Error); 
          }
         else Error=0;
//-----------------------
       }   
     }
   }
   return(0);
}
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         

