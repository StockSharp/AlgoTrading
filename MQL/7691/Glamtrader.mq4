//+------------------------------------------------------------------+
//|                                 Copyright 2005, Gideon Smolders. |
//|                                              complied by Oba Ire |
//|                http://finance.groups.yahoo.com/group/autotraders |
//|   To be used with the Laguerre indicator test for best time frame|
//+------------------------------------------------------------------+
/*
This EA is intended for simulation trading only you will need to test. 
It currently is looking at m15 for ma, m5 Laguerre, 
and H1 for awesome oscillator. We will experiment with these settings.
*/
#property copyright "Copyright 2005, Gideon Smolders."
#property link      "http://groups.yahoo.com/group/MetaTrader_Experts_and_Indicators"
//----
#define MAGIC 771986
//----
extern double lStopLoss=20;
extern double sStopLoss=20;
extern double lTakeProfit=50;
extern double sTakeProfit=50;
extern double lTrailingStop=15;
extern double sTrailingStop=15;
extern color clOpenBuy=Blue;
extern color clCloseBuy=Aqua;
extern color clOpenSell=Red;
extern color clCloseSell=Violet;
extern color clModiBuy=Blue;
extern color clModiSell=Red;
extern string Name_Expert="GLAM Trader";
extern int Slippage=0;
extern bool UseSound=True;
extern string NameFileSound="alert.wav";
extern double Lots=0.10;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void deinit() 
  {
   Comment("");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start()
  {
     if(Bars<100)
     {
      Print("bars less than 100");
      return(0);
     }
     if(lStopLoss<10)
     {
      Print("StopLoss less than 10");
      return(0);
     }
     if(lTakeProfit<10)
     {
      Print("TakeProfit less than 10");
      return(0);
     }
     if(sStopLoss<10)
     {
      Print("StopLoss less than 10");
      return(0);
     }
     if(sTakeProfit<10)
     {
      Print("TakeProfit less than 10");
      return(0);
     }
//----
   double diMA0=iMA(NULL,15,3,0,MODE_EMA,PRICE_CLOSE,0);
   double diClose1=iClose(NULL,15,0);
   double diCustom2=iCustom(NULL, 5, "Laguerre", 0.7, 0, 0);
   double diClose3=iClose(NULL,5,0);
   double diAO4=iAO(NULL,60,0);
   double diClose5=iClose(NULL,60,0);
   double diMA6=iMA(NULL,15,3,0,MODE_EMA,PRICE_CLOSE,0);
   double diClose7=iClose(NULL,15,0);
   double diCustom8=iCustom(NULL, 5, "Laguerre", 0.7, 0, 0);
   double diClose9=iClose(NULL,5,0);
   double diAO10=iAO(NULL,60,0);
   double diClose11=iClose(NULL,60,0);
//----
     if(AccountFreeMargin()<(1000*Lots))
     {
      Print("We have no money. Free Margin = ", AccountFreeMargin());
      return(0);
     }
//-- I think that these should be changed to the meta trader of of the book statements for buy/sell.
     if (!ExistPositions())
     {
        if ((diMA0>diClose1 && diCustom2>diClose3 && diAO4>diClose5))
        {
         OpenBuy();
         return(0);
        }
        if ((diMA6<diClose7 && diCustom8<diClose9 && diAO10<diClose11))
        {
         OpenSell();
         return(0);
        }
     }
   TrailingPositionsBuy(lTrailingStop);
   TrailingPositionsSell(sTrailingStop);
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  bool ExistPositions() 
  {
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
           {
            return(True);
           }
        }
     }
   //----  
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void TrailingPositionsBuy(int trailingStop) 
  {
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
           {
              if (OrderType()==OP_BUY) 
              {
                 if (Bid-OrderOpenPrice()>trailingStop*Point) 
                 {
                  if (OrderStopLoss()<Bid-trailingStop*Point)
                     ModifyStopLoss(Bid-trailingStop*Point);
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void TrailingPositionsSell(int trailingStop) 
  {
     for(int i=0; i<OrdersTotal(); i++) 
     {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
        {
           if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
           {
              if (OrderType()==OP_SELL) 
              {
                 if (OrderOpenPrice()-Ask>trailingStop*Point) 
                 {
                  if (OrderStopLoss()>Ask+trailingStop*Point || OrderStopLoss()==0)
                     ModifyStopLoss(Ask+trailingStop*Point);
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void ModifyStopLoss(double ldStopLoss) 
  {
   bool fm;
   fm=OrderModify(OrderTicket(),OrderOpenPrice(),ldStopLoss,OrderTakeProfit(),0,CLR_NONE);
   if (fm && UseSound) PlaySound(NameFileSound);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
   void OpenBuy() 
   {
   double ldLot, ldStop, ldTake;
   string lsComm;
   ldLot=GetSizeLot();
   ldStop=GetStopLossBuy();
   ldTake=GetTakeProfitBuy();
   lsComm=GetCommentForOrder();
   OrderSend(Symbol(),OP_BUY,ldLot,Ask,Slippage,ldStop,ldTake,lsComm,MAGIC,0,clOpenBuy);
   if (UseSound) PlaySound(NameFileSound);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void OpenSell() 
  {
   double ldLot, ldStop, ldTake;
   string lsComm;
//----
   ldLot=GetSizeLot();
   ldStop=GetStopLossSell();
   ldTake=GetTakeProfitSell();
   lsComm=GetCommentForOrder();
   OrderSend(Symbol(),OP_SELL,ldLot,Bid,Slippage,ldStop,ldTake,lsComm,MAGIC,0,clOpenSell);
   if (UseSound) PlaySound(NameFileSound);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string GetCommentForOrder() {    return(Name_Expert); }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetSizeLot() {    return(Lots); }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetStopLossBuy() {    return(Bid-lStopLoss*Point);}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetStopLossSell() {    return(Ask+sStopLoss*Point); }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTakeProfitBuy() {    return(Ask+lTakeProfit*Point); }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTakeProfitSell() {    return(Bid-sTakeProfit*Point); }
//----
return(0);
//+------------------------------------------------------------------+