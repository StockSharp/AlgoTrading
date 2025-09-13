//+------------------------------------------------------------------+
//| Daily Breakout                                Cidomo_v1.mq4      |
//|                            Copyright © 2015 theclai              |
//|                                           balebajuanak@yahoo.com |
//+------------------------------------------------------------------+
#property link      "theclai"
//---
extern int    Delta                       = 0;
extern double Maxbars                     = 31.8;
extern int    Slippage                    = 3;
extern string Settings                    = "----- Settings -----";
extern string RobotName                   = "Cidomo_v1";
extern bool   UseTimeFilter               = true;
extern string TimeSet                     = "09:00";
extern bool   OpenStop                    = true;
extern color  color_BAR                   = Red;
extern int    Magic                       = 10292015;
extern string MoneyManagement             = "----- MM -----";
extern bool   UseMM                       = true;
extern int    SL                          = 60;
extern int    TP                          = 70;
extern int    Risk                        = 20;
extern int    NoLoss                      = 35;
extern int    Trailing                    = 5;
extern double ManualLotSize               = 0.01;
//---
int vSlipppage;
int lastDay;
double lotsize;
double vRisk;
double stoplevel;
double stopout;
double vManualLotsize;
bool vUseTimeFilter;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   vManualLotsize=ManualLotSize;
   vSlipppage=Slippage;
   vUseTimeFilter=UseTimeFilter;

// Calculate stoplevel as max of either STOPLEVEL or FREEZELEVEL
   stoplevel=MathMax(MarketInfo(Symbol(),MODE_FREEZELEVEL),MarketInfo(Symbol(),MODE_STOPLEVEL));

   if(SL<stoplevel)
     {
      SL=stoplevel;
     }

   if(TP<stoplevel)
     {
      TP=stoplevel;
     }

   if(NoLoss<stoplevel && NoLoss!=0)
     {
      NoLoss=stoplevel;
     }

   if(Trailing<stoplevel && Trailing!=0)
     {
      Trailing=stoplevel;
     }

   if(UseMM)
     {
      lotsize=Calculate_Lots();
     }
   else
     {
      lotsize=vManualLotsize;
     }

   Comment("Copyright © 2015 theclai \n "+RobotName+"\n"+
           "TimeSet   ",TimeSet,"\n",
           "Delta       ",Delta,"\n",
           "SL           ",SL,"\n",
           "TP          ",TP,"\n",
           "Lot          ",DoubleToStr(lotsize,2),"\n",
           "Risk         ",Risk,"\n",
           "NoLoss    ",NoLoss,"\n",
           "Trailing     ",Trailing);

   if(TimeSet=="00:00")
     {
      lastDay=1;
     }
   return (0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   if(OpenStop)
     {
      Magic=TimeDay(CurTime());
     }

   Trade();
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Trade()
  {
   int ctBuy=0;
   int ctSell= 0;
   int error = 0;
   bool vBuy = false;
   bool vSell= false;
   double maxPrice = 0;
   double minPrice = 0;
   int expiration=CurTime()+(60-TimeMinute(CurTime()))*60*48;
   double trProfit=0;
   double stLoss=0;
   int tip,timeBarbuy,timeBarSell;

//check if there is open order
   bool isOrderAllowed=isTradeAllowed();

   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS)==true)
        {
         if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=Magic)
           {
            continue;
           }

         tip=OrderType();

         if(tip==0) vBuy = true;
         if(tip==1) vSell = true;
         if(tip==4) ctBuy++;
         if(tip==5) ctSell++;
        }
     }

   if((vBuy || vSell) && (ctBuy!=0 || ctSell!=0))
     {
      DelAllStop();   // delete stop orders if order opened
     }

   if(vBuy || vSell)
     {
      if(Trailing!=0)
        {
         TrailingStop(Trailing);
        }
      if(NoLoss!=0)
        {
         No_Loss(NoLoss);
        }
     }

   if(vUseTimeFilter)
     {
      if(TimeStr(CurTime())!=TimeSet)
        {
         return;
        }
     }

   if(UseMM)
     {
      lotsize=Calculate_Lots();
     }
   else
     {
      lotsize=vManualLotsize;
     }

   if(ctBuy<1)
     {
      maxPrice=iHigh(NULL,0,iHighest(NULL,0,MODE_HIGH,Maxbars,0))+NormalizeDouble(Delta*Point,Digits);

      if(Ask+stoplevel*Point>maxPrice)
        {
         maxPrice=NormalizeDouble(Ask+stoplevel*Point,Digits);
        }

      if(TP!=0)
        {
         trProfit=NormalizeDouble(maxPrice+TP*Point,Digits);
        }

      if(SL!=0)
        {
         stLoss=NormalizeDouble(maxPrice-SL*Point,Digits);
        }

      if(isOrderAllowed)
        {
         error=OrderSend(Symbol(),OP_BUYSTOP,lotsize,maxPrice,vSlipppage,stLoss,trProfit,StringConcatenate("Buy Stop ","["+RobotName+"]"),Magic,expiration,Blue);

         if(error==-1)
           {
            printf("Error Buy Stop ",GetLastError()," ",Symbol(),"Lot",lotsize,"Price",maxPrice,"SL",stLoss,"TP",trProfit,"expiration",expiration);
           }
         else
           {
            timeBarbuy=TimeDay(CurTime());
           }
        }
     }

   if(ctSell<1)
     {
      minPrice=iLow(NULL,0,iLowest(NULL,0,MODE_LOW,Maxbars,0))-NormalizeDouble(Delta*Point,Digits);

      if(Bid-stoplevel*Point<minPrice)
         minPrice=NormalizeDouble(Bid-stoplevel*Point,Digits);

      if(TP!=0)
        {
         trProfit=NormalizeDouble(minPrice-TP*Point,Digits);
        }

      if(SL!=0)
        {
         stLoss=NormalizeDouble(minPrice+SL*Point,Digits);
        }

      if(isOrderAllowed)
        {
         error=OrderSend(Symbol(),OP_SELLSTOP,lotsize,minPrice,vSlipppage,stLoss,trProfit,StringConcatenate("Sell Stop ","["+RobotName+"]"),Magic,expiration,Red);

         if(error==-1)
           {
            printf("Error Sell Stop ",GetLastError(),"  ",Symbol(),"Lot",lotsize,"Price",minPrice,"SL",stLoss,"TP",trProfit,"expiration",expiration);
           }
         else
           {
            timeBarSell=TimeDay(CurTime());
           }
        }
     }

   if(ctBuy<1 && ctSell<1)
     {
      ObjectDelete("bar0");
      ObjectCreate("bar0",OBJ_RECTANGLE,0,0,0,0,0);
      ObjectSet("bar0",OBJPROP_STYLE,STYLE_SOLID);
      ObjectSet("bar0",OBJPROP_COLOR,color_BAR);
      ObjectSet("bar0",OBJPROP_BACK,true);
      ObjectSet("bar0",OBJPROP_TIME1,iTime(NULL,1440,0));
      ObjectSet("bar0",OBJPROP_PRICE1,maxPrice);
      ObjectSet("bar0",OBJPROP_TIME2,CurTime());
      ObjectSet("bar0",OBJPROP_PRICE2,minPrice);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DelAllStop()
  {
   int type=0;

   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=Magic)
            continue;

         type=OrderType();

         if(type==4 || type==5)
           {
            if(OrderDelete(OrderTicket()))
              {
               printf("Deleted order ");
              }
           }

        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailingStop(int trailing)
  {
   double StLo;
   int type=0;
   bool error=false;

   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS)==true)
        {
         type=OrderType();

         if(type<2 && OrderSymbol()==Symbol())
           {
            if(OrderMagicNumber()!=Magic)
               continue;

            if(type==0) //Buy               
              {
               StLo=Bid-trailing*Point;

               if(StLo>OrderStopLoss() && StLo>OrderOpenPrice())
                 {
                  error=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(StLo,Digits),OrderTakeProfit(),0,White);
                  Comment("Trailing "+OrderTicket());
                  Sleep(500);
                 }
              }
            if(type==1) //Sell               
              {
               StLo=Ask+trailing*Point;
               if(StLo<OrderStopLoss() && StLo<OrderOpenPrice())
                 {
                  error=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(StLo,Digits),OrderTakeProfit(),0,White);
                  Comment("Trailing "+OrderTicket());
                  Sleep(500);
                 }
              }

            if(error==false && SL!=0)
               printf("Error SELLSTOP ",GetLastError(),"   ",Symbol(),"   SL ",StLo);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Calculate_Lots()
  {
   double minLots=MarketInfo(Symbol(),MODE_MINLOT);
   double currentLots=AccountFreeMargin()*Risk/14/MarketInfo(Symbol(),MODE_MARGINREQUIRED)/10;

   if(currentLots>MarketInfo(Symbol(),MODE_MAXLOT))
      currentLots=MarketInfo(Symbol(),MODE_MAXLOT);

   if(currentLots<minLots)
     {
      currentLots=minLots;
     }

   if(minLots<0.1)
     {
      currentLots=NormalizeDouble(currentLots,2);
     }
   else
     {
      currentLots=NormalizeDouble(currentLots,1);
     }

   return(currentLots);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void No_Loss(int noLoss)
  {
   double StLo;
   int type=0;
   bool error;

   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS)==true)
        {
         type=OrderType();

         if(type<2 && OrderSymbol()==Symbol())
           {
            if(OrderMagicNumber()!=Magic)
               continue;

            if(type==0) //Buy
              {
               if(OrderStopLoss()>=OrderOpenPrice())
                  return;

               StLo=Bid-noLoss*Point;
               if(StLo>OrderStopLoss() && StLo>OrderOpenPrice())
                 {
                  error=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(StLo,Digits),OrderTakeProfit(),0,White);
                  Comment("Trailing "+OrderTicket());
                  Sleep(500);
                 }
              }
            if(type==1) //Sell               
              {
               if(OrderStopLoss()<=OrderOpenPrice())
                  return;

               StLo=Ask+noLoss*Point;
               if(StLo<OrderStopLoss() && StLo<OrderOpenPrice())
                 {
                  error=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(StLo,Digits),OrderTakeProfit(),0,White);
                  Comment("Trailing "+OrderTicket());
                  Sleep(500);
                 }
              }

            if(error==false && SL!=0)
               printf("Error SELLSTOP ",GetLastError(),"   ",Symbol(),"   SL ",StLo);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string TimeStr(int taim)
  {
   string sTaim;
   int HH=TimeHour(taim);     // Hour                  
   int MM=TimeMinute(taim);   // Minute   

   if(HH<10)
      sTaim=StringConcatenate(sTaim,"0",DoubleToStr(HH,0));
   else
      sTaim=StringConcatenate(sTaim,DoubleToStr(HH,0));

   if(MM<10)
      sTaim=StringConcatenate(sTaim,":0",DoubleToStr(MM,0));
   else
      sTaim=StringConcatenate(sTaim,":",DoubleToStr(MM,0));

   return(sTaim);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool isTradeAllowed()
  {
   bool isAllow=true;
   int lastCount=0,count=0,type=0;

   for(int i=OrdersTotal()-1;i>=0;i--)
     {
      if(OrderSelect(i,SELECT_BY_POS)==true)
        {
         type=OrderType();

         if(OrderSymbol()!=Symbol() || type>1 || OrderMagicNumber()!=Magic)
           {
            continue;
           }
         count++;
        }
     }

   if(count>lastCount)
     {
      isAllow=false;
     }

   lastCount=count;
   return isAllow;
  }
//+------------------------------------------------------------------+
