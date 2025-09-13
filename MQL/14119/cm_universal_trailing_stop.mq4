//+------------------------------------------------------------------+
//|                              Copyright © 2015, Khlystov Vladimir |
//|                                         http://cmillion.narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, cmillion@narod.ru"
#property link      "http://cmillion.ru"
#property strict
#property description "The Expert Advisor moves the Stop Loss in the direction of the price movement using various methods"
#property description "by candlesticks, by fractals, by ATR MA Parabolic indicators, by profit percent..."
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum t
  {
   b=1,     // by extremums of candlesticks
   c=2,     // by fractals
   d=3,     // by ATR indicator
   e=4,     // by Parabolic indicator
   f=5,     // by MA indicator
   g=6,     // by profit % 
   i=7,     // by points
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum tf
  {
   af=0,     // current
   bf=1,     // 1 minute 
   cf=2,     // 5 minutes
   df=3,     // 15 minutes
   ef=4,     // 30 minutes
   ff=5,     // 1 hour
   gf=6,     // 4 hours
   hf=7,     // 1 day
  };
//+------------------------------------------------------------------+
extern bool    VirtualTrailingStop=false;  // Virtual trailing stop
input t        parameters_trailing=i;      // Trailing method
extern int     delta=50;        // Offset from the stop loss calculation level
input tf       TF_Tralling=af;  // Timeframe of the indicators (0-current)
extern int     StepTrall=1;     // Stop loss movement step
extern int     StartTrall=1;    // Minimal profit of trailing stop in points
extern bool    GeneralNoLoss=true; // Trailing from breakeven point
sinput  int     Magic=-1; // Which magic to trail (-1 all)
color   text_color=Lime;  // Information output color
sinput string Advanced_Options="";
input int     period_ATR=14;// ATR period (method 3)
input double Step=0.02;   // Parabolic Step (method 4)
input double Maximum=0.2; // Parabolic Maximum (method 4)
input int ma_period=34; // MA period (method 5)
input ENUM_MA_METHOD ma_method=MODE_SMA; // Averaging method (method 5)
input ENUM_APPLIED_PRICE applied_price=PRICE_CLOSE; // Price type (method 5)
input double PercetnProfit=50; // Percent of profit (method 6)
//---
int TF[10]={0,1,5,15,30,60,240,1440,10080,43200};
int STOPLEVEL;
string val;
double SLB=0,SLS=0;
int slippage=100;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   val=AccountCurrency();
   DrawLABEL(2,"cm Copyright","Copyright © 2015, http://cmillion.ru",5,10,clrGray);
   string txt;
   switch(parameters_trailing)
     {
      case 1: // by extremums of candlesticks
         txt=StringConcatenate("by candlesticks ",StrPer(TF[TF_Tralling])," +- ",delta);
         break;
      case 2: // by fractals
         txt=StringConcatenate("by fractals ",StrPer(TF[TF_Tralling])," ",StrPer(TF[TF_Tralling])," +- ",delta);
         break;
      case 3: // by ATR indicator
         txt=StringConcatenate("by ATR (",delta,") ",StrPer(TF[TF_Tralling]),"+- ",delta);
         break;
      case 4: // by Parabolic indicator
         txt=StringConcatenate("by Parabolic (",DoubleToStr(Step,2)," ",DoubleToStr(Maximum,2),") ",StrPer(TF[TF_Tralling])," +- ",delta);
         break;
      case 5: // by MA indicator
         txt=StringConcatenate("by MA (",ma_period," ",ma_method," ",applied_price,") ",StrPer(TF[TF_Tralling])," +- ",delta);
         break;
      case 6: // by profit %
         txt=StringConcatenate(" ",DoubleToStr(PercetnProfit,2),"% of profit)");
         break;
      default: // by points
         txt=StringConcatenate("by points ",delta," p");
         break;
     }
   if(VirtualTrailingStop)
     {
      if(GeneralNoLoss) DrawLABEL(3,"cm 3",StringConcatenate("Virtual trailing from beakeven ",txt),5,10,text_color);
      else DrawLABEL(3,"cm 3",StringConcatenate("Virtual trailing ",txt),5,10,text_color);
     }
   else
     {
      if(GeneralNoLoss) DrawLABEL(3,"cm 3",StringConcatenate("Trailing from breakeven ",txt),5,10,text_color);
      else DrawLABEL(3,"cm 3",StringConcatenate("Trailing ",txt),5,10,text_color);
     }

   if(Magic==-1) DrawLABEL(3,"cm 2","All Magic",5,25,text_color);
   else DrawLABEL(3,"cm 2",StringConcatenate("Magic ",Magic,"\n"),5,15,text_color);

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(!IsTradeAllowed())
     {
      Comment("Trading forbidden ",TimeToStr(TimeCurrent(),TIME_MINUTES));
      return;
     }
   int b=0,s=0;
   double ProfitB=0,ProfitS=0,OOP,price_b=0,price_s=0,lot=0,NLb=0,NLs=0,LS=0,LB=0;
   for(int j=0; j<OrdersTotal(); j++)
     {
      if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES)==true)
        {
         if((Magic==OrderMagicNumber() || Magic==-1) && OrderSymbol()==Symbol())
           {
            OOP = OrderOpenPrice();
            lot = OrderLots();
            if(OrderType()==OP_BUY ) {ProfitB+=OrderProfit()+OrderSwap()+OrderCommission();price_b += OOP*lot; LB+=lot; b++;}
            if(OrderType()==OP_SELL) {ProfitS+=OrderProfit()+OrderSwap()+OrderCommission();price_s += OOP*lot; LS+=lot; s++;}
           }
        }
     }
//----
   if(b!=0)
     {
      NLb=price_b/LB;
      ARROW("cm_NL_Buy",NLb,6,clrAqua);
     }
   if(s!=0)
     {
      NLs=price_s/LS;
      ARROW("cm_NL_Sell",NLs,6,clrRed);
     }
   DrawLABEL(1,"cm Balance",StringConcatenate("Balance ",DoubleToStr(AccountBalance(),2),val),5,20,Lime);
   DrawLABEL(1,"cm Equity",StringConcatenate("Equity ",DoubleToStr(AccountEquity(),2),val),5,35,Lime);
   DrawLABEL(1,"cm OrdersB",StringConcatenate(b," Buy ",DoubleToStr(LB,2)," ",DoubleToStr(ProfitB,2),val),5,50,Color(ProfitB>0,Lime,Red));
   DrawLABEL(1,"cm OrdersS",StringConcatenate(s," Sell ",DoubleToStr(LS,2)," ",DoubleToStr(ProfitS,2),val),5,65,Color(ProfitS>0,Lime,Red));
//----
   if(!VirtualTrailingStop) STOPLEVEL=(int)MarketInfo(Symbol(),MODE_STOPLEVEL);
   int tip,Ticket;
   bool error;
   double SL,OSL;
   int n=0;
   if(b==0) SLB=0;
   if(s==0) SLS=0;
   for(int i=OrdersTotal(); i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS)==true)
        {
         tip=OrderType();
         if(tip<2 && (OrderSymbol()==Symbol()) && (OrderMagicNumber()==Magic || Magic==-1))
           {
            OSL    = OrderStopLoss();
            OOP    = OrderOpenPrice();
            Ticket = OrderTicket();
            n++;
            if(tip==OP_BUY)
              {
               if(GeneralNoLoss)
                 {
                  SL=SlLastBar(OP_BUY,Bid,NLb);
                  if(SL<NLb+StartTrall*Point) continue;
                 }
               else
                 {
                  SL=SlLastBar(OP_BUY,Bid,OOP);
                  if(SL<OOP+StartTrall*Point) continue;
                 }
               //if (OSL  >= OOP && only_NoLoss) continue;
               if(SL>=OSL+StepTrall*Point && (Bid-SL)/Point>STOPLEVEL)
                 {
                  if(VirtualTrailingStop)
                    {
                     if(SLB<SL) SLB=SL;
                     if(SLB!=0 && Bid<=SLB)
                       {
                        if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),slippage,clrNONE)) continue;
                       }
                    }
                  else
                    {
                     error=OrderModify(Ticket,OOP,SL,OrderTakeProfit(),0,White);
                     if(!error) Comment("TrailingStop Error ",GetLastError(),"  order ",Ticket,"   SL ",SL);
                     else Comment("TrailingStop ",Ticket," ",TimeToStr(TimeCurrent(),TIME_MINUTES));
                    }
                 }
              }
            if(tip==OP_SELL)
              {
               if(GeneralNoLoss)
                 {
                  SL=SlLastBar(OP_SELL,Ask,NLs);
                  if(SL>NLs-StartTrall*Point) continue;
                 }
               else
                 {
                  SL=SlLastBar(OP_SELL,Ask,OOP);
                  if(SL>OOP-StartTrall*Point) continue;
                 }
               //if (OSL  <= OOP && only_NoLoss) continue;
               if((SL<=OSL-StepTrall*Point || OSL==0) && (SL-Ask)/Point>STOPLEVEL)
                 {
                  if(VirtualTrailingStop)
                    {
                     if(SLS==0 || SLS>SL) SLS=SL;
                     if(SLS!=0 && Ask>=SLS)
                       {
                        if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),slippage,clrNONE)) continue;
                       }
                    }
                  else
                    {
                     error=OrderModify(Ticket,OOP,SL,OrderTakeProfit(),0,White);
                     if(!error) Comment("TrailingStop Error ",GetLastError(),"  order ",Ticket,"   SL ",SL);
                     else Comment("TrailingStop ",Ticket," ",TimeToStr(TimeCurrent(),TIME_MINUTES));
                    }
                 }
              }
           }
        }
     }
//---
   if(IsTesting())
     {
      if(b==0 || s==0)
        {
         if(AccountFreeMarginCheck(Symbol(),OP_BUY,0.1)+AccountFreeMarginCheck(Symbol(),OP_SELL,0.1)>0)
           {
            if(OrderSend(Symbol(),OP_BUY,0.1,NormalizeDouble(Ask,Digits),slippage,0,0,NULL,0,0,clrBlue)==-1)
               Print("Error OrderSend ",GetLastError());
            if(OrderSend(Symbol(),OP_SELL,0.1,NormalizeDouble(Bid,Digits),slippage,0,0,NULL,0,0,clrRed)==-1)
               Print("Error OrderSend ",GetLastError());
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   if(IsTesting()) return;
   string PN="cm";
   for(int i=ObjectsTotal()-1; i>=0; i--)
     {
      string Obj_Name=ObjectName(i);
      if(StringFind(Obj_Name,PN,0)!=-1)
        {
         ObjectDelete(Obj_Name);
        }
     }
   Comment("");
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double SlLastBar(int tip,double price,double OOP)
  {
   double prc=0;
   int i;
   switch(parameters_trailing)
     {
      case 1: // by extremums of candlesticks
         if(tip==OP_BUY)
           {
            for(i=1; i<500; i++)
              {
               prc=NormalizeDouble(iLow(Symbol(),TF[TF_Tralling],i)-delta*Point,Digits);
               if(prc!=0) if(price-STOPLEVEL*Point>prc) break;
               else prc=0;
              }
            ARROW("cm_SL_Buy",prc,4,clrAqua);
            DrawLABEL(1,"cm SL Buy",StringConcatenate("SL Buy candle ",DoubleToStr(prc,Digits)),5,100,text_color);
           }
         if(tip==OP_SELL)
           {
            for(i=1; i<500; i++)
              {
               prc=NormalizeDouble(iHigh(Symbol(),TF[TF_Tralling],i)+delta*Point,Digits);
               if(prc!=0) if(price+STOPLEVEL*Point<prc) break;
               else prc=0;
              }
            ARROW("cm_SL_Sell",prc,4,clrRed);
            DrawLABEL(1,"cm SL Sell",StringConcatenate("SL Sell candle ",DoubleToStr(prc,Digits)),5,120,text_color);
           }
         break;

      case 2: // by fractals
         if(tip==OP_BUY)
           {
            for(i=1; i<100; i++)
              {
               prc=iFractals(Symbol(),TF[TF_Tralling],MODE_LOWER,i);
               if(prc!=0)
                 {
                  prc=NormalizeDouble(prc-delta*Point,Digits);
                  if(price-STOPLEVEL*Point>prc) break;
                 }
               else prc=0;
              }
            ARROW("cm_SL_Buy",prc,218,clrAqua);
            DrawLABEL(1,"cm SL Buy",StringConcatenate("SL Buy Fractals ",DoubleToStr(prc,Digits)),5,100,text_color);
           }
         if(tip==OP_SELL)
           {
            for(i=1; i<100; i++)
              {
               prc=iFractals(Symbol(),TF[TF_Tralling],MODE_UPPER,i);
               if(prc!=0)
                 {
                  prc=NormalizeDouble(prc+delta*Point,Digits);
                  if(price+STOPLEVEL*Point<prc) break;
                 }
               else prc=0;
              }
            ARROW("cm_SL_Sell",prc,217,clrRed);
            DrawLABEL(1,"cm SL Sell",StringConcatenate("SL Sell Fractals ",DoubleToStr(prc,Digits)),5,120,text_color);
           }
         break;
      case 3: // by ATR indicator
         if(tip==OP_BUY)
           {
            prc=NormalizeDouble(Bid-iATR(Symbol(),TF[TF_Tralling],period_ATR,0)-delta*Point,Digits);
            ARROW("cm_SL_Buy",prc,4,clrAqua);
            DrawLABEL(1,"cm SL Buy",StringConcatenate("SL Buy ATR ",DoubleToStr(prc,Digits)),5,100,text_color);
           }
         if(tip==OP_SELL)
           {
            prc=NormalizeDouble(Ask+iATR(Symbol(),TF[TF_Tralling],period_ATR,0)+delta*Point,Digits);
            ARROW("cm_SL_Sell",prc,4,clrRed);
            DrawLABEL(1,"cm SL Sell",StringConcatenate("SL Sell ATR ",DoubleToStr(prc,Digits)),5,120,text_color);
           }
         break;

      case 4: // by Parabolic indicator
         prc=iSAR(Symbol(),TF[TF_Tralling],Step,Maximum,0);
         if(tip==OP_BUY)
           {
            prc=NormalizeDouble(prc-delta*Point,Digits);
            if(price-STOPLEVEL*Point<prc) prc=0;
            ARROW("cm_SL_Buy",prc,4,clrAqua);
            DrawLABEL(1,"cm SL Buy",StringConcatenate("SL Buy Parabolic ",DoubleToStr(prc,Digits)),5,100,text_color);
           }
         if(tip==OP_SELL)
           {
            prc=NormalizeDouble(prc+delta*Point,Digits);
            if(price+STOPLEVEL*Point>prc) prc=0;
            ARROW("cm_SL_Sell",prc,4,clrRed);
            DrawLABEL(1,"cm SL Sell",StringConcatenate("SL Sell Parabolic ",DoubleToStr(prc,Digits)),5,120,text_color);
           }
         break;

      case 5: // by MA indicator
         prc=iMA(NULL,TF[TF_Tralling],ma_period,0,ma_method,applied_price,0);
         if(tip==OP_BUY)
           {
            prc=NormalizeDouble(prc-delta*Point,Digits);
            if(price-STOPLEVEL*Point<prc) prc=0;
            ARROW("cm_SL_Buy",prc,4,clrAqua);
            DrawLABEL(1,"cm SL Buy",StringConcatenate("SL Buy MA ",DoubleToStr(prc,Digits)),5,100,text_color);
           }
         if(tip==OP_SELL)
           {
            prc=NormalizeDouble(prc+delta*Point,Digits);
            if(price+STOPLEVEL*Point>prc) prc=0;
            ARROW("cm_SL_Sell",prc,4,clrRed);
            DrawLABEL(1,"cm SL Sell",StringConcatenate("SL Sell MA ",DoubleToStr(prc,Digits)),5,120,text_color);
           }
         break;
      case 6: // % of profit
         if(tip==OP_BUY)
           {
            prc=NormalizeDouble(OOP+(price-OOP)/100*PercetnProfit,Digits);
            ARROW("cm_SL_Buy",prc,4,clrAqua);
            DrawLABEL(1,"cm SL Buy",StringConcatenate("SL Buy % ",DoubleToStr(prc,Digits)),5,100,text_color);
           }
         if(tip==OP_SELL)
           {
            prc=NormalizeDouble(OOP-(OOP-price)/100*PercetnProfit,Digits);
            ARROW("cm_SL_Sell",prc,4,clrRed);
            DrawLABEL(1,"cm SL Sell",StringConcatenate("SL Sell % ",DoubleToStr(prc,Digits)),5,120,text_color);
           }
         break;
      default: // by points
         if(tip==OP_BUY)
           {
            prc=NormalizeDouble(price-delta*Point,Digits);
            ARROW("cm_SL_Buy",prc,4,clrAqua);
            DrawLABEL(1,"cm SL Buy",StringConcatenate("SL Buy pips ",DoubleToStr(prc,Digits)),5,100,text_color);
           }
         if(tip==OP_SELL)
           {
            prc=NormalizeDouble(price+delta*Point,Digits);
            ARROW("cm_SL_Sell",prc,4,clrRed);
            DrawLABEL(1,"cm SL Sell",StringConcatenate("SL Sell pips ",DoubleToStr(prc,Digits)),5,120,text_color);
           }
         break;
     }
   return(prc);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string StrPer(int per)
  {
   if(per == 0) per=Period();
   if(per == 1) return("M1");
   if(per == 5) return("M5");
   if(per == 15) return("M15");
   if(per == 30) return("M30");
   if(per == 60) return("H1");
   if(per == 240) return("H4");
   if(per == 1440) return("D1");
   if(per == 10080) return("W1");
   if(per == 43200) return("MN1");
   return("timeframe error");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ARROW(string Name,double Price,int ARROWCODE,color c)
  {
   ObjectDelete(Name);
   ObjectCreate(Name,OBJ_ARROW,0,Time[0],Price,0,0,0,0);
   ObjectSetInteger(0,Name,OBJPROP_ARROWCODE,ARROWCODE);
   ObjectSetInteger(0,Name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,Name,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,Name,OBJPROP_COLOR,c);
   ObjectSetInteger(0,Name,OBJPROP_WIDTH,1);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DrawLABEL(int c,string name,string Name,int X,int Y,color clr)
  {
   if(ObjectFind(name)==-1)
     {
      ObjectCreate(name,OBJ_LABEL,0,0,0);
      ObjectSet(name,OBJPROP_CORNER,c);
      ObjectSet(name,OBJPROP_XDISTANCE,X);
      ObjectSet(name,OBJPROP_YDISTANCE,Y);
      ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
      ObjectSetInteger(0,name,OBJPROP_SELECTED,false);
     }
   ObjectSetText(name,Name,10,"Arial",clr);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
color Color(bool P,color a,color b)
  {
   if(P) return(a);
   return(b);
  }
//+------------------------------------------------------------------+
