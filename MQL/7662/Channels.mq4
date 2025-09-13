//+----------------------------------------------------------+
//| Copyright 2005, Gordago Software Corp.                   |
//| http://www.gordago.com/                                  |
//| version 2.0                                              |
//+----------------------------------------------------------+
#property copyright "Copyright 2005, Gordago Software Corp."
#property link "http://www.gordago.com"
//----
#define MIN_STOPLOSS_POINT 10
#define MIN_TAKEPROFIT_POINT 10
#define MAGIC 0
//----
extern string sNameExpert="Generate from Gordago";
extern int nAccount =0;
extern double dBuyStopLossPoint=0;
extern double dSellStopLossPoint=0;
extern double dBuyTakeProfitPoint=0;
extern double dSellTakeProfitPoint=0;
extern double dBuyTrailingStopPoint=30;
extern double dSellTrailingStopPoint=30;
extern double dLots=0.10;
extern int nSlippage=1;
extern bool lFlagUseHourTrade=False;
extern int nFromHourTrade=0;
extern int nToHourTrade=23;
extern bool lFlagUseSound=False;
extern string sSoundFileName="alert.wav";
extern color colorOpenBuy=Blue;
extern color colorCloseBuy=Aqua;
extern color colorOpenSell=Red;
extern color colorCloseSell=Aqua;
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
     if (lFlagUseHourTrade)
     {
        if (!(Hour()>=nFromHourTrade && Hour()<=nToHourTrade)) 
        {
         Comment("Time for trade has not come else!");
         return(0);
        }
     }
     if(Bars < 100)
     {
      Print("bars less than 100");
      return(0);
     }
     if (nAccount > 0 && nAccount!=AccountNumber())
     {
      Comment("Trade on account :"+AccountNumber()+" FORBIDDEN!");
      return(0);
     }
   if((dBuyStopLossPoint > 0 && dBuyStopLossPoint <
   MIN_STOPLOSS_POINT) ||
   (dSellStopLossPoint > 0 && dSellStopLossPoint <
       MIN_STOPLOSS_POINT))
     {
      Print("StopLoss less than " + MIN_STOPLOSS_POINT);
      return(0);
     }
   if((dBuyTakeProfitPoint > 0 && dBuyTakeProfitPoint <
   MIN_TAKEPROFIT_POINT) ||
   (dSellTakeProfitPoint > 0 && dSellTakeProfitPoint <
       MIN_TAKEPROFIT_POINT))
     {
      Print("TakeProfit less than " + MIN_TAKEPROFIT_POINT);
      return(0);
     }
   double diMA0=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,0);
   double diEnvelopes1=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,1,MODE_LOWER,0);
   double diMA2=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,0);
   double diEnvelopes3=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.7,MODE_LOWER,0);
   double diMA4=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,0);
   double diEnvelopes5=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.3,MODE_LOWER,0);
   double diMA6=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,0);
   double diMA7=iMA(NULL,60,220,0,MODE_EMA,PRICE_CLOSE,0);
   double diMA8=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,0);
   double diEnvelopes9=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.3,MODE_UPPER,0);
   double diMA10=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,0);
   double diEnvelopes11=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.7,MODE_UPPER,0);
   double diMA12=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,0);
   double diEnvelopes13=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,1,MODE_UPPER,0);
   double diMA14=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,0);
   double diEnvelopes15=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.7,MODE_UPPER,0);
   double diMA16=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,0);
   double diEnvelopes17=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.3,MODE_UPPER,0);
   double diMA18=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,0);
   double diMA19=iMA(NULL,60,220,0,MODE_EMA,PRICE_CLOSE,0);
   double diMA20=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,0);
   double diEnvelopes21=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.3,MODE_LOWER,0);
   double diMA22=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,0);
   double diEnvelopes23=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.7,MODE_LOWER,0);
   double diMA24=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,1);
   double diEnvelopes25=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,1,MODE_LOWER,0);
   double diMA26=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,1);
   double diEnvelopes27=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.7,MODE_LOWER,0);
   double diMA28=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,1);
   double diEnvelopes29=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.3,MODE_LOWER,0);
   double diMA30=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,1);
   double diMA31=iMA(NULL,60,220,0,MODE_EMA,PRICE_CLOSE,0);
   double diMA32=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,1);
   double diEnvelopes33=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.3,MODE_UPPER,0);
   double diMA34=iMA(NULL,60,2,0,MODE_EMA,PRICE_CLOSE,1);
   double diEnvelopes35=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.7,MODE_UPPER,0);
   double diMA36=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,1);
   double diEnvelopes37=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,1,MODE_UPPER,0);
   double diMA38=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,1);
   double diEnvelopes39=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.7,MODE_UPPER,0);
   double diMA40=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,1);
   double diEnvelopes41=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.3,MODE_UPPER,0);
   double diMA42=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,1);
   double diMA43=iMA(NULL,60,220,0,MODE_EMA,PRICE_CLOSE,0);
   double diMA44=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,1);
   double diEnvelopes45=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.3,MODE_LOWER,0);
   double diMA46=iMA(NULL,60,2,0,MODE_EMA,PRICE_OPEN,1);
   double diEnvelopes47=iEnvelopes
   (NULL,60,220,MODE_EMA,0,PRICE_CLOSE,0.7,MODE_LOWER,0);
     if(AccountFreeMargin() < (1000*dLots))
     {
      Print("We have no money. Free Margin = " + AccountFreeMargin
      ());
      return(0);
     }
   bool lFlagBuyOpen=false, lFlagSellOpen=false, lFlagBuyClose=
   false, lFlagSellClose=false;
//----
   lFlagBuyOpen=((diMA0>diEnvelopes1 && diMA24<diEnvelopes25) ||
   (diMA2>diEnvelopes3 && diMA26<diEnvelopes27) || (diMA4<diEnvelopes5
   && diMA28<diEnvelopes29) || (diMA6>diMA7 && diMA30<diMA31) ||
   (diMA8>diEnvelopes9 && diMA32<diEnvelopes33) ||
   (diMA10>diEnvelopes11 && diMA34<diEnvelopes35));
   lFlagSellOpen=((diMA12<diEnvelopes13 && diMA36>diEnvelopes37)
   || (diMA14<diEnvelopes15 && diMA38>diEnvelopes39) ||
   (diMA16<diEnvelopes17 && diMA40>diEnvelopes41) || (diMA18<diMA19 &&
   diMA42>diMA43) || (diMA20<diEnvelopes21 && diMA44>diEnvelopes45) ||
   (diMA22<diEnvelopes23 && diMA46>diEnvelopes47));
   lFlagBuyClose=False;
   lFlagSellClose=False;
//----
     if (!ExistPositions())
     {
        if (lFlagBuyOpen)
        {
         OpenBuy();
         return(0);
        }
        if (lFlagSellOpen)
        {
         OpenSell();
         return(0);
        }
     }
     if (ExistPositions())
     {
        if(OrderType()==OP_BUY)
        {
           if (lFlagBuyClose)
           {
            bool flagCloseBuy=OrderClose(OrderTicket(), OrderLots
            (), Bid, nSlippage, colorCloseBuy);
            if (flagCloseBuy && lFlagUseSound)
               PlaySound(sSoundFileName);
            return(0);
           }
        }
        if(OrderType()==OP_SELL)
        {
           if (lFlagSellClose)
           {
            bool flagCloseSell=OrderClose(OrderTicket(), OrderLots
            (), Ask, nSlippage, colorCloseSell);
            if (flagCloseSell && lFlagUseSound)
               PlaySound(sSoundFileName);
            return(0);
           }
        }
     }
     if (dBuyTrailingStopPoint > 0 || dSellTrailingStopPoint > 0)
     {
        for(int i=0; i<OrdersTotal(); i++) 
        {
           if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
           {
            bool lMagic=true;
            if (MAGIC > 0 && OrderMagicNumber()!=MAGIC)
               lMagic=false;
              if (OrderSymbol()==Symbol() && lMagic) 
              {
               if (OrderType()==OP_BUY && dBuyTrailingStopPoint > 0)
                 {
                  if (Bid-OrderOpenPrice() >
                      dBuyTrailingStopPoint*Point) 
                      {
                     if (OrderStopLoss()<Bid-
                     dBuyTrailingStopPoint*Point)
                        ModifyStopLoss(Bid-
                        dBuyTrailingStopPoint*Point);
                    }
                 }
                 if (OrderType()==OP_SELL) 
                 {
                  if (OrderOpenPrice()-
                      Ask>dSellTrailingStopPoint*Point) 
                      {
                     if (OrderStopLoss()
                     >Ask+dSellTrailingStopPoint*Point || OrderStopLoss()==0)
                        ModifyStopLoss
                        (Ask+dSellTrailingStopPoint*Point);
                    }
                 }
              }
           }
        }
     }
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
         bool lMagic=true;
//----
         if (MAGIC > 0 && OrderMagicNumber()!=MAGIC)
            lMagic=false;
           if (OrderSymbol()==Symbol() && lMagic) 
           {
            return(True);
           }
        }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void ModifyStopLoss(double ldStopLoss) 
  {
   bool lFlagModify=OrderModify(OrderTicket(), OrderOpenPrice(),
   ldStopLoss, OrderTakeProfit(), 0, CLR_NONE);
   if (lFlagModify && lFlagUseSound)
      PlaySound(sSoundFileName);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void OpenBuy() 
  {
   double dStopLoss=0, dTakeProfit=0;
   if (dBuyStopLossPoint > 0)
      dStopLoss=Bid-dBuyStopLossPoint*Point;
   if (dBuyTakeProfitPoint > 0)
      dTakeProfit=Ask + dBuyTakeProfitPoint * Point;
   int numorder=OrderSend(Symbol(), OP_BUY, dLots, Ask, nSlippage,
   dStopLoss, dTakeProfit, sNameExpert, MAGIC, 0, colorOpenBuy);
//----
   if (numorder > -1 && lFlagUseSound)
      PlaySound(sSoundFileName);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void OpenSell() 
  {
   double dStopLoss=0, dTakeProfit=0;
//----   
   if (dSellStopLossPoint > 0)
      dStopLoss=Ask+dSellStopLossPoint*Point;
   if (dSellTakeProfitPoint > 0)
      dTakeProfit=Bid-dSellTakeProfitPoint*Point;
//----      
   int numorder=OrderSend(Symbol(),OP_SELL, dLots, Bid, nSlippage,
   dStopLoss, dTakeProfit, sNameExpert, MAGIC, 0, colorOpenSell);
   if (numorder > -1 && lFlagUseSound)
      PlaySound(sSoundFileName);
  }
//+------------------------------------------------------------------+