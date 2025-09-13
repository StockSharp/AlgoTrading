//+------------------------------------------------------------------+
//|                                                         Lily.mq4 |
//+------------------------------------------------------------------+
#define MAGICMA  3937

extern int Verison = 2;
extern int  Slippage = 100;
extern int Anchor = 250;
extern double xFactor = 1.8;

extern string XX = "------------below for manual option";
extern bool Automated = TRUE;
extern double PriceUp = 1.37001;
extern double PriceDown = 1.36501;
extern double Lots = 0.02;
extern double Amount = 10;
extern int RiskPercent = 100;

int PendingBuy, PendingSell, Buys, Sells, i, Spread;
double BuyLots, SellLots, PendingBuyLots, PendingSellLots;
double Focus, Profit, Risk, Up, Dw;
//+------------------------------------------------------------------+
//| Init function                                                    |
//+------------------------------------------------------------------+
void init()
  {
   Spread=MarketInfo(Symbol(),MODE_SPREAD);
   Risk=(PriceUp-PriceDown)*10000;
   Amount=AccountBalance()/1000;
  }
//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
  {
   Count();
   if(Buys==0 && Sells==0){CheckForOpen();}
   else{CheckForClose();}
  }
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
int CheckForOpen()
{
  if(Automated)
    {
     double SAR=iSAR(NULL,0,0.02,0.2,0);
     Lots = MathRound(AccountBalance()/1000)/100;
     Amount=AccountBalance()/1000;
     if(Ask>=SAR){OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,0,0,"",MAGICMA,0,CLR_NONE); Focus=Ask-Anchor*Point;}
     if(Bid<=SAR){OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,0,0,"",MAGICMA,0,CLR_NONE); Focus=Bid+Anchor*Point;}
    }//if
  else
    {
     if(PendingBuyLots==0)
       {
       if(Ask+Spread*Point<PriceUp){OrderSend(Symbol(),OP_BUYSTOP,Lots,PriceUp,Slippage,0,0,"",MAGICMA,0,CLR_NONE);}
       if(Bid-Spread*Point>PriceUp){OrderSend(Symbol(),OP_BUYLIMIT,Lots,PriceUp,Slippage,0,0,"",MAGICMA,0,CLR_NONE);} 
       }//if
     if(PendingSellLots==0)
       {
       if(Ask+Spread*Point<PriceDown){OrderSend(Symbol(),OP_SELLLIMIT,Lots,PriceDown,Slippage,0,0,"",MAGICMA,0,CLR_NONE);}
       if(Bid-Spread*Point>PriceDown){OrderSend(Symbol(),OP_SELLSTOP,Lots,PriceDown,Slippage,0,0,"",MAGICMA,0,CLR_NONE);} 
       }//if  
    }//if
} 
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
int CheckForClose()
{
  Count();
  
  //CONDIZIONE DI CHIUSURA DI SUCCESSO
  if(Profit>=Amount){CloseAll();}

  //CONDIZIONE DI BLOCCAGGIO DI EMERGENZA
  if(Profit<-AccountBalance()*RiskPercent/100)
    {
     if(SellLots>BuyLots+PendingBuyLots)
       {OrderSend(Symbol(),OP_BUY,(SellLots-(BuyLots+PendingBuyLots)),Ask,Slippage,0,0,"",MAGICMA,0,CLR_NONE);}
     if(BuyLots>SellLots+PendingSellLots)
       {OrderSend(Symbol(),OP_SELL,(BuyLots-(SellLots+PendingSellLots)),Bid,Slippage,0,0,"",MAGICMA,0,CLR_NONE);}
    }

  //SETTAGGIO AUTOMATICO O NO
  if(Automated){Up=Focus+Anchor*Point;Dw=Focus-Anchor*Point;}
  else{Up=NormalizeDouble(PriceUp,5);Dw=NormalizeDouble(PriceUp,5);    }

  //SYSTEM CORE   
  if(SellLots>BuyLots+PendingBuyLots)
    {OrderSend(Symbol(),OP_BUYSTOP,(SellLots*xFactor)-BuyLots,Up,Slippage,0,0,"",MAGICMA,0,CLR_NONE);}
  if(BuyLots>SellLots+PendingSellLots)
    {OrderSend(Symbol(),OP_SELLSTOP,(BuyLots*xFactor)-SellLots,Dw,Slippage,0,0,"",MAGICMA,0,CLR_NONE);}


  Comment("Gain= ",Profit," LookingFor= ",Amount,
          ";\nBuy=",Buys,"; Sell=", Sells,"; BuyLots=",BuyLots,"; SellLots=",SellLots,
          ";\nRisk=",Risk); 
                  
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
   {for(i=Total-1; i>=0; i--) 
     {if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == TRUE) 
       {Pos=OrderType();
        if(Pos==OP_BUY){Result=OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, CLR_NONE);}
        if(Pos==OP_SELL){Result=OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, CLR_NONE);}
        if((Pos==OP_BUYSTOP)||(Pos==OP_SELLSTOP)||(Pos==OP_BUYLIMIT)||(Pos==OP_SELLLIMIT)){Result=OrderDelete(OrderTicket(), CLR_NONE);}
//-----------------------
        if(Result!=true){Error=GetLastError();Print("LastError = ",Error);}
        else Error=0;
//-----------------------
       }//if
     }//for
   }//if
   return(0);
}
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------
void Count()
{  
  Buys=0; Sells=0; PendingBuy=0; PendingSell=0; BuyLots=0; SellLots=0; PendingBuyLots=0; PendingSellLots=0; Profit=0;
  for(i=OrdersTotal(); i>=0; i--)
    {OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(OrderMagicNumber()==MAGICMA && OrderSymbol()==Symbol())
       {
        Profit = Profit + OrderProfit() + OrderSwap();
        if(OrderType()==OP_SELL){SellLots=SellLots+OrderLots();Sells++;}
        if(OrderType()==OP_BUY){BuyLots=BuyLots+OrderLots();Buys++;}
        if(OrderType()==OP_SELLSTOP || OrderType()==OP_SELLLIMIT){PendingSellLots=PendingSellLots+OrderLots();}
        if(OrderType()==OP_BUYSTOP || OrderType()==OP_BUYLIMIT){PendingBuyLots=PendingBuyLots+OrderLots();}
       }//if
    }//for
}