//+------------------------------------------------------------------+
//|            MoStAsHaR15 FoReX - Pivot Line Strategy               |
//+------------------------------------------------------------------+
#property copyright "MoStAsHaR15 FoReX © CopyRights 2005"
#property link      "mostashar15@yahoo.com"
//----
extern double Lots=1.0;
extern double TrailingStop=10;
extern double StopLoss=20;
extern int TimeZone=2;
//----
double EMA5_0, EMA6_0, EMA5_1, EMA6_1, ADX, ADXpos_0, ADXneg_0, ADXpos_1, ADXneg_1, OsMA_0, OsMA_1;
double R1=0, R2=0, R3=0, M0=0, M1=0, M2=0, M3=0, M4=0, M5=0, S1=0, S2=0, S3=0;
double day_high=0, day_low=0, yesterday_high=0, yesterday_open=0, yesterday_low=0, yesterday_close=0, today_open=0, today_high=0, today_low=0, P=0, Q=0, nQ=0, nD=0, D=0, rates_h1[2][6], rates_d1[2][6],Bound[13];
double Buy_TP, Sell_TP, Sup, Res, ticket, SL;
int cnt,dif1,dif2;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   if(Bars<150)
     {
      Print("bars less than 100");
     }
   if(Buy_TP < 10 || Sell_TP < 10)
     {
      Print("TakeProfit less than 10");
     }
   // Delete Old Pivot Lines to Draw the New Lines
   ObjectDelete("R1 Label");
   ObjectDelete("R1 Line");
   ObjectDelete("R2 Label");
   ObjectDelete("R2 Line");
   ObjectDelete("R3 Label");
   ObjectDelete("R3 Line");
   ObjectDelete("S1 Label");
   ObjectDelete("S1 Line");
   ObjectDelete("S2 Label");
   ObjectDelete("S2 Line");
   ObjectDelete("S3 Label");
   ObjectDelete("S3 Line");
   ObjectDelete("P Label");
   ObjectDelete("P Line");
   ObjectDelete("M5 Label");
   ObjectDelete("M5 Line");
   ObjectDelete("M4 Label");
   ObjectDelete("M4 Line");
   ObjectDelete("M3 Label");
   ObjectDelete("M3 Line");
   ObjectDelete("M2 Label");
   ObjectDelete("M2 Line");
   ObjectDelete("M1 Label");
   ObjectDelete("M1 Line");
   ObjectDelete("M0 Label");
   /// PIVOT POINT CALCULATIONS
   int i=0, j=0;
   if(Period() > 1440)
     {
      Comment("Error - Chart period is greater than 1 day.");
      return(-1);
     }
   ArrayCopyRates(rates_d1, Symbol(), PERIOD_D1);
   yesterday_high=rates_d1[1][3];
   yesterday_low=rates_d1[1][2];
   day_high=rates_d1[0][3];
   day_low=rates_d1[0][2];
//----
   ArrayCopyRates(rates_h1, Symbol(), PERIOD_H1);
   for(i=0;i<=25;i++)
     {
      if (TimeMinute(rates_h1[i][0])==0 && (TimeHour(rates_h1[i][0])-TimeZone)==0)
        {
         yesterday_close=rates_h1[i+1][4];
         yesterday_open=rates_h1[i+24][1];
         today_open=rates_h1[i][1];
         break;
        }
     }
   // Calculate Pivots
   D=(day_high - day_low);
   Q=(yesterday_high - yesterday_low);
   P=(yesterday_high + yesterday_low + yesterday_close)/3;
   R1=(2*P)-yesterday_low;
   S1=(2*P)-yesterday_high;
//----
   R2=P+(yesterday_high - yesterday_low);
   S2=P-(yesterday_high - yesterday_low);
   R3=(2*P)+(yesterday_high-(2*yesterday_low));
   S3=(2*P)-((2* yesterday_high)-yesterday_low);
//----
   M5=(R2+R3)/2;
   M4=(R1+R2)/2;
   M3=(P+R1)/2;
   M2=(P+S1)/2;
   M1=(S1+S2)/2;
   M0=(S2+S3)/2;
//----
   if (Q > 5)
     {
      nQ=Q;
     }
   else
     {
      nQ=Q*10000;
     }
   if (D > 5)
     {
      nD=D;
     }
   else
     {
      nD=D*10000;
     }
   ObjectDelete("M0 Line");
   // Pivot Lines Labeling
   if(ObjectFind("R1 label")!=0)
     {
      ObjectCreate("R1 label", OBJ_TEXT, 0, Time[20], R1);
      ObjectSetText("R1 label", " R1", 8, "Arial", Yellow);
     }
   else
     {
      ObjectMove("R1 label", 0, Time[20], R1);
     }
   if(ObjectFind("R2 label")!=0)
     {
      ObjectCreate("R2 label", OBJ_TEXT, 0, Time[20], R2);
      ObjectSetText("R2 label", " R2", 8, "Arial", Orange);
     }
   else
     {
      ObjectMove("R2 label", 0, Time[20], R2);
     }
   if(ObjectFind("R3 label")!=0)
     {
      ObjectCreate("R3 label", OBJ_TEXT, 0, Time[20], R3);
      ObjectSetText("R3 label", " R3", 8, "Arial", Red);
     }
   else
     {
      ObjectMove("R3 label", 0, Time[20], R3);
     }
   if(ObjectFind("P label")!=0)
     {
      ObjectCreate("P label", OBJ_TEXT, 0, Time[20], P);
      ObjectSetText("P label", "Pivot", 8, "Arial", DeepPink);
     }
   else
     {
      ObjectMove("P label", 0, Time[20], P);
     }
   if(ObjectFind("S1 label")!=0)
     {
      ObjectCreate("S1 label", OBJ_TEXT, 0, Time[20], S1);
      ObjectSetText("S1 label", "S1", 8, "Arial", Yellow);
     }
   else
     {
      ObjectMove("S1 label", 0, Time[20], S1);
     }
   if(ObjectFind("S2 label")!=0)
     {
      ObjectCreate("S2 label", OBJ_TEXT, 0, Time[20], S2);
      ObjectSetText("S2 label", "S2", 8, "Arial", Orange);
     }
   else
     {
      ObjectMove("S2 label", 0, Time[20], S2);
     }
   if(ObjectFind("S3 label")!=0)
     {
      ObjectCreate("S3 label", OBJ_TEXT, 0, Time[20], S3);
      ObjectSetText("S3 label", "S3", 8, "Arial", Red);
     }
   else
     {
      ObjectMove("S3 label", 0, Time[20], S3);
     }
   // Drawing Pivot lines
   if(ObjectFind("S1 line")!=0)
     {
      ObjectCreate("S1 line", OBJ_HLINE, 0, Time[40], S1);
      ObjectSet("S1 line", OBJPROP_STYLE, STYLE_SOLID);
      ObjectSet("S1 line", OBJPROP_COLOR, Yellow);
     }
   else
     {
      ObjectMove("S1 line", 0, Time[40], S1);
     }
   if(ObjectFind("S2 line")!=0)
     {
      ObjectCreate("S2 line", OBJ_HLINE, 0, Time[40], S2);
      ObjectSet("S2 line", OBJPROP_STYLE, STYLE_SOLID);
      ObjectSet("S2 line", OBJPROP_COLOR, Orange);
     }
   else
     {
      ObjectMove("S2 line", 0, Time[40], S2);
     }
   if(ObjectFind("S3 line")!=0)
     {
      ObjectCreate("S3 line", OBJ_HLINE, 0, Time[40], S3);
      ObjectSet("S3 line", OBJPROP_STYLE, STYLE_SOLID);
      ObjectSet("S3 line", OBJPROP_COLOR, Red);
     }
   else
     {
      ObjectMove("S3 line", 0, Time[40], S3);
     }
   if(ObjectFind("P line")!=0)
     {
      ObjectCreate("P line", OBJ_HLINE, 0, Time[40], P);
      ObjectSet("P line", OBJPROP_STYLE, STYLE_SOLID);
      ObjectSet("P line", OBJPROP_COLOR, DeepPink);
     }
   else
     {
      ObjectMove("P line", 0, Time[40], P);
     }
   if(ObjectFind("R1 line")!=0)
     {
      ObjectCreate("R1 line", OBJ_HLINE, 0, Time[40], R1);
      ObjectSet("R1 line", OBJPROP_STYLE, STYLE_SOLID);
      ObjectSet("R1 line", OBJPROP_COLOR, Yellow);
     }
   else
     {
      ObjectMove("R1 line", 0, Time[40], R1);
     }
   if(ObjectFind("R2 line")!=0)
     {
      ObjectCreate("R2 line", OBJ_HLINE, 0, Time[40], R2);
      ObjectSet("R2 line", OBJPROP_STYLE, STYLE_SOLID);
      ObjectSet("R2 line", OBJPROP_COLOR, Orange);
     }
   else
     {
      ObjectMove("R2 line", 0, Time[40], R2);
     }
   if(ObjectFind("R3 line")!=0)
     {
      ObjectCreate("R3 line", OBJ_HLINE, 0, Time[40], R3);
      ObjectSet("R3 line", OBJPROP_STYLE, STYLE_SOLID);
      ObjectSet("R3 line", OBJPROP_COLOR, Red);
     }
   else
     {
      ObjectMove("R3 line", 0, Time[40], R3);
     }
   // Midpoints Labeling
   if(ObjectFind("M5 line")!=0)
     {
      ObjectCreate("M5 line", OBJ_HLINE, 0, Time[40], M5);
      ObjectSet("M5 line", OBJPROP_STYLE, STYLE_DOT);
      ObjectSet("M5 line", OBJPROP_COLOR, White);
     }
   else
     {
      ObjectMove("M5 line", 0, Time[40], M5);
     }
   if(ObjectFind("M4 line")!=0)
     {
      ObjectCreate("M4 line", OBJ_HLINE, 0, Time[40], M4);
      ObjectSet("M4 line", OBJPROP_STYLE, STYLE_DOT);
      ObjectSet("M4 line", OBJPROP_COLOR, White);
     }
   else
     {
      ObjectMove("M4 line", 0, Time[40], M4);
     }
   if(ObjectFind("M3 line")!=0)
     {
      ObjectCreate("M3 line", OBJ_HLINE, 0, Time[40], M3);
      ObjectSet("M3 line", OBJPROP_STYLE, STYLE_DOT);
      ObjectSet("M3 line", OBJPROP_COLOR, White);
     }
   else
     {
      ObjectMove("M3 line", 0, Time[40], M3);
     }
   if(ObjectFind("M2 line")!=0)
     {
      ObjectCreate("M2 line", OBJ_HLINE, 0, Time[40], M2);
      ObjectSet("M2 line", OBJPROP_STYLE, STYLE_DOT);
      ObjectSet("M2 line", OBJPROP_COLOR, White);
     }
   else
     {
      ObjectMove("M2 line", 0, Time[40], M2);
     }
   if(ObjectFind("M1 line")!=0)
     {
      ObjectCreate("M1 line", OBJ_HLINE, 0, Time[40], M1);
      ObjectSet("M1 line", OBJPROP_STYLE, STYLE_DOT);
      ObjectSet("M1 line", OBJPROP_COLOR, White);
     }
   else
     {
      ObjectMove("M1 line", 0, Time[40], M1);
     }
   if(ObjectFind("M0 line")!=0)
     {
      ObjectCreate("M0 line", OBJ_HLINE, 0, Time[40], M0);
      ObjectSet("M0 line", OBJPROP_STYLE, STYLE_DOT);
      ObjectSet("M0 line", OBJPROP_COLOR, White);
     }
   else
     {
      ObjectMove("M0 line", 0, Time[40], M0);
     }
//---- Indicator Calculations
   ADX=iADX(NULL,60,14,PRICE_CLOSE,MODE_MAIN,0);
   ADXpos_0=iADX(NULL,60,14,PRICE_CLOSE,MODE_PLUSDI,0);
   ADXneg_0=iADX(NULL,60,14,PRICE_CLOSE,MODE_MINUSDI,0);
   ADXpos_1=iADX(NULL,60,14,PRICE_CLOSE,MODE_PLUSDI,1);
   ADXneg_1=iADX(NULL,60,14,PRICE_CLOSE,MODE_MINUSDI,1);
//----
   EMA5_0=iMA(NULL,60,5,0,MODE_EMA,PRICE_CLOSE,1);
   EMA6_0=iMA(NULL,60,8,0,MODE_EMA,PRICE_OPEN,1);
   EMA5_1=iMA(NULL,60,5,0,MODE_EMA,PRICE_CLOSE,1);
   EMA6_1=iMA(NULL,60,8,0,MODE_EMA,PRICE_OPEN,1);
//----
   OsMA_0=iOsMA(NULL,60,12,26,9,PRICE_CLOSE,0);
   OsMA_1=iOsMA(NULL,60,12,26,9,PRICE_CLOSE,1);
   // Determining Bounding Pivot Lines & Take Profit Point
   if((Bid-S3) * (Bid-M0) < 0)
     {
      Sup=S3;
      Res=M0;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   if((Bid-M0) * (Bid-S2) < 0)
     {
      Sup=M0;
      Res=S2;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   if((Bid-S2) * (Bid-M1) < 0)
     {
      Sup=S2;
      Res=M1;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   if((Bid-M1) * (Bid-S1) < 0)
     {
      Sup=M1;
      Res=S1;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   if((Bid-S1) * (Bid-M2) < 0)
     {
      Sup=S1;
      Res=M2;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   if((Bid-M2) * (Bid-P) < 0)
     {
      Sup=M2;
      Res=P;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   if((Bid-P) * (Bid-M3) < 0)
     {
      Sup=P;
      Res=M3;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   if((Bid-M3) * (Bid-R1) < 0)
     {
      Sup=M3;
      Res=R1;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   if((Bid-R1) * (Bid-M4) < 0)
     {
      Sup=R1;
      Res=M4;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   if((Bid-M4) * (Bid-R2) < 0)
     {
      Sup=M4;
      Res=R2;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   if((Bid-R2) * (Bid-M5) < 0)
     {
      Sup=R2;
      Res=M5;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   if((Bid-M5) * (Bid-R3) < 0)
     {
      Sup=S3;
      Res=M0;
      Sell_TP=Sup;
      Buy_TP=Res;
     }
   dif1=(Bid-Sell_TP)/Point;
   dif2=(Buy_TP-Ask)/Point;
//----
   Comment("MoStAsHaR15 FoReX - Pivot Strategy","\nSupport = ",Sup," - Difference = ",dif1," Pips", "\nResistance = ",Res," - Difference = ",dif2," Pips");
   // Checking Account Free Margin       
   int total=OrdersTotal();
   if(total<1)
     {
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
        }
      // Check for long positions (if you have money and no more than 1 order open)
      if(dif2 > 14 && ADX > 20 && ADXpos_0 > ADXpos_1 && ADXpos_0 > ADXneg_0 && (EMA5_0 - EMA6_0)>=(5 * Point) && EMA5_1 > EMA6_1 && OsMA_0 > OsMA_1)
        {
         SL=Ask - StopLoss * Point;
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,SL,Buy_TP,"MoStAsHaR15 FoReX",16384,0,White);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Comment("BUY order opened : ",OrderOpenPrice());
           }
         else Comment("Error opening BUY order : ",GetLastError());
         return(0);
        }
      // Check for short positions (if you have money and no more than 2 orders open)
      if(dif1 > 14 && ADX > 20 && ADXneg_0 > ADXneg_1 && ADXpos_0 < ADXneg_0 && (EMA6_0 - EMA5_0)>=5 * Point && EMA6_1 > EMA5_1 && OsMA_0 < OsMA_1)
        {
         SL=Bid + StopLoss * Point;
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,SL,Sell_TP,"MoStAsHaR15 FoReX",16384,0,White);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Comment("SELL order opened : ",OrderOpenPrice());
           }
         else Comment ("Error opening SELL order : ",GetLastError());
         return(0);
        }
      return(0);
     }
   // Open Trades Management
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_BUY)   // long position is opened
           {
            // Trailing Stop Managment
            if(TrailingStop>0)
              {
               if(Bid-OrderOpenPrice()>Point*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-Point*TrailingStop)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
                     return(0);
                    }
                 }
              }
           }
         else // go to short position
            if(TrailingStop>0)
              {
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
                 {
                  if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
                     return(0);
                    }
                 }
              }
        }
      return(0);
     }
  }
//+------------------------------------------------------------------+