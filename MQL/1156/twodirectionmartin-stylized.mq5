//+------------------------------------------------------------------+
//|                                                       My-I28.mq5 |
//+------------------------------------------------------------------+
#property copyright ""
#property link      ""
#property version   "1.28"
#property description "... just for fun"

//--- input parameters
input double KTP=0.35; // Koef. of TakeProfit, as % of Price
input double VolumeToOrder=0.10; // Minimal volume to send(request)
input double VolumeLimitOrder=0.75; // Maximal volume of one order
input double PercentSame=75.0; // Level of Same side (Martingale) = (0,0...100,0%)

                               // Program Global Varible
MqlTick         T; // Tick
MqlTradeRequest R; // Request
MqlTradeResult  D; // Deal (result)
double TP;
double WinPerTrade;
double StopLevel;
double Spread;
int    I;
ulong TicketBuy;
ulong TicketSell;
bool ExistBUY;
bool ExistSELL;
double OldBuyTP;
double OldSellTP;
double OldBuyVol;
double OldSellVol;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ExistOrder() // return some MAIN program varible
  {
   ulong Ticket;
   ExistBUY=false; ExistSELL=false;
   OldSellVol=0.0; OldBuyVol=0.0;
   for(I=0;I<(int)OrdersTotal();I++)
     {
      Ticket=OrderGetTicket(I); // Order selected here
      if(OrderGetString(ORDER_SYMBOL)==_Symbol)
         switch(OrderGetInteger(ORDER_TYPE))
           {
            case ORDER_TYPE_BUY_LIMIT :
              {
               if(OldSellVol<OrderGetDouble(ORDER_VOLUME_CURRENT))
                 {
                  OldSellVol=OrderGetDouble(ORDER_VOLUME_CURRENT);
                  OldSellTP=OrderGetDouble(ORDER_PRICE_OPEN);
                  TicketSell=Ticket;
                  ExistSELL=true; 
                 }
               break; 
              }
            case ORDER_TYPE_SELL_LIMIT :
              {
               if(OldBuyVol<OrderGetDouble(ORDER_VOLUME_CURRENT))
                 {
                  OldBuyVol=OrderGetDouble(ORDER_VOLUME_CURRENT);
                  OldBuyTP=OrderGetDouble(ORDER_PRICE_OPEN);
                  TicketBuy=Ticket;
                  ExistBUY=true; 
                 }
               break; 
              }
            default : {}
           } // end : switch
     } // end : for
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DataForSymbol() // Return Tick valu for Symbol, as program varible
  {
   SymbolInfoTick(_Symbol,T);
   StopLevel=SymbolInfoInteger(_Symbol,SYMBOL_TRADE_STOPS_LEVEL)*_Point;
   Spread=(T.ask-T.bid);
   TP=MathMax(KTP*T.ask/100.0,StopLevel+Spread);
   WinPerTrade=Spread;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DeleteOld() // Remove old pending order ( in wrong direction )
  {
   R.action=TRADE_ACTION_REMOVE;
   R.type_filling=ORDER_FILLING_FOK;
   if(OldBuyVol>0.005)
     {
      R.order=TicketBuy;
      bool Done=OrderSend(R,D); int J=0;
      while(!Done && J<19) { Done=OrderSend(R,D); J++; Sleep(12345); } // REQUOTE
      Print("==> DeleteBuy : Ret.Code=",D.retcode);
     }
   if(OldSellVol>0.005)
     {
      R.order=TicketSell;
      bool Done=OrderSend(R,D); int J=0;
      while(!Done && J<19) { Done=OrderSend(R,D); J++; Sleep(12345); } // REQUOTE
      Print("==> DeleteSell : Ret.Code=",D.retcode);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void BuyLimitOrderSend(double BLVolume,double BLPrice) // Sene BUY_LIMIT
  {
   R.volume=NormalizeDouble(BLVolume,2);
   R.price=NormalizeDouble(BLPrice,_Digits);
   R.action=TRADE_ACTION_PENDING;
   R.type=ORDER_TYPE_BUY_LIMIT;
   R.type_filling=ORDER_FILLING_RETURN;
   bool Done=OrderSend(R,D); int J=0;
   while(!Done && J<19) { Done=OrderSend(R,D); J++; Sleep(12345); } // REQUOTE
   Print("==> BuyLimit : Ret.Code=",D.retcode);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SellLimitOrderSend(double SLVolume,double SLPrice) // Send SELL_LIMIT
  {
   R.volume=NormalizeDouble(SLVolume,2);
   R.price=NormalizeDouble(SLPrice,_Digits);
   R.action=TRADE_ACTION_PENDING;
   R.type=ORDER_TYPE_SELL_LIMIT;
   R.type_filling=ORDER_FILLING_RETURN;
   bool Done=OrderSend(R,D); int J=0;
   while(!Done && J<19) { Done=OrderSend(R,D); J++; Sleep(12345); } // REQUOTE
   Print("==> SellLimit : Ret.Code=",D.retcode);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AddBuySend(double AddVolume) // Send BUY order
  {
   R.volume=NormalizeDouble(AddVolume,2);
   R.action=TRADE_ACTION_DEAL;
   R.type_filling=ORDER_FILLING_FOK;
   R.type=ORDER_TYPE_BUY;
   bool Done=false; int J=0;
   while(!Done && J<19) { DataForSymbol(); R.price=T.ask; Done=OrderSend(R,D); J++; Sleep(12345); } // REQUOTE
   Print("==> AddBuy : Ret.Code=",D.retcode);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AddSellSend(double AddVolume) // Send SELL order
  {
   R.volume=NormalizeDouble(AddVolume,2);
   R.action=TRADE_ACTION_DEAL;
   R.type_filling=ORDER_FILLING_FOK;
   R.type=ORDER_TYPE_SELL;
   bool Done=false; int J=0;
   while(!Done && J<19) { DataForSymbol(); R.price=T.bid; Done=OrderSend(R,D); J++; Sleep(12345); } // REQUOTE
   Print("==> AddSell : Ret.Code=",D.retcode);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ShrinkRange() // Move orders close to current price 
  {
   DataForSymbol();
   double OldLOSS=((OldBuyTP-T.bid)*OldBuyVol+(T.ask-OldSellTP)*OldSellVol);
   double NewTotalVol=MathMax(2.0*VolumeToOrder,MathCeil(((OldLOSS+WinPerTrade)/TP)*100.0)/100.0);
   double NewBuyVol,NewSellVol,AddVolume;
   if(OldBuyVol>=OldSellVol)
     {
      NewBuyVol=MathMax(VolumeToOrder,MathCeil(NewTotalVol*PercentSame)/100.0);
      NewSellVol=MathMax(VolumeToOrder,MathRound((NewTotalVol-NewBuyVol)*100.0)/100.0); 
     }
   else
     {
      NewSellVol=MathMax(VolumeToOrder,MathCeil(NewTotalVol*PercentSame)/100.0);
      NewBuyVol=MathMax(VolumeToOrder,MathRound((NewTotalVol-NewSellVol)*100.0)/100.0); 
     }
   AddVolume=(NewBuyVol-NewSellVol)-(OldBuyVol-OldSellVol);

   DeleteOld();
   BuyLimitOrder(NewSellVol,T.bid-TP);
   SellLimitOrder(NewBuyVol,T.ask+TP);
   if( AddVolume>+0.005 ) AddBUY(AddVolume);
   if( AddVolume<-0.005 ) AddSELL(-AddVolume);

   Print("===> OldLOSS=",DoubleToString(OldLOSS/VolumeToOrder,_Digits),"  TP=",DoubleToString(TP,_Digits),"  NewTotalVol=",DoubleToString(NewTotalVol,2));
   Print("===> OldBuyVol=",OldBuyVol,"  OldSellVol=",OldSellVol,"  ==> NewBuyVol=",NewBuyVol,"  NewSellVol=",NewSellVol);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AddBUY(double Volume) // Here order BUY send VOLUME is LIMITED !
  {
   while(Volume>VolumeLimitOrder)
     { AddBuySend(VolumeLimitOrder); Volume=Volume-VolumeLimitOrder; }
   AddBuySend(Volume);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AddSELL(double Volume) // Here order SELL send VOLUME is LIMITED !
  {
   while(Volume>VolumeLimitOrder)
     { AddSellSend(VolumeLimitOrder); Volume=Volume-VolumeLimitOrder; }
   AddSellSend(Volume);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void BuyLimitOrder(double Volume,double Price) // Here order BUY_LIMIT send VOLUME is LIMITED !
  {
   while(Volume>VolumeLimitOrder)
     { BuyLimitOrderSend(VolumeLimitOrder,Price); Volume=Volume-VolumeLimitOrder; Price=Price-Spread; }
   BuyLimitOrderSend(Volume,Price);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SellLimitOrder(double Volume,double Price) // Here order SELL_LIMIT send VOLUME is LIMITED !
  {
   while(Volume>VolumeLimitOrder)
     { SellLimitOrderSend(VolumeLimitOrder,Price); Volume=Volume-VolumeLimitOrder; Price=Price+Spread; }
   SellLimitOrderSend(Volume,Price);
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  { // OrderSend constant
   R.sl=0;
   R.tp=0;
   R.magic=2012;
   R.deviation=99;
   R.comment="AutoSend";
   R.symbol=_Symbol;
   return(0);
  }

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
// Set program varible
   ExistOrder();
   DataForSymbol();

// INIT or RESTART
   if(!ExistBUY && !ExistSELL)
     {
      AddSELL(VolumeToOrder); BuyLimitOrder(VolumeToOrder,T.bid-TP);
      AddBUY(VolumeToOrder);  SellLimitOrder(VolumeToOrder,T.ask+TP);
      return;
     }

// MAIN LOGIC
   if(!ExistBUY || !ExistSELL) { ShrinkRange(); return; }

   if(( OldBuyTP>T.ask+2.0*TP && OldSellTP<T.bid-TP && OldBuyVol<=OldSellVol) || 
      (OldSellTP<T.bid-2.0*TP && OldBuyTP>T.ask+TP && OldBuyVol>=OldSellVol)) ShrinkRange();
  }
//+------------------------------------------------------------------+
