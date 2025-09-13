//+------------------------------------------------------------------+
//|                                                    Simple Levels |
//|                                Copyright 2015, Vladimir V. Tkach |
//+------------------------------------------------------------------+
#property version "1.0"
#property copyright "Copyright © 2015, Vladimir V. Tkach"
#property description "Expert opens trades from the trend lines and"
#property description "revolve it if reversal signal is appeared."
#property strict
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum on_off_mode
  {
   on=0,
   off=1,
  };
  
extern int
sl=300,     //Stop Loss (in pips)
tp=900,     //Take Profit (in pips)
slip=30,    //Slippage (in pips)
magic=120;  //Magic number

extern double
lot=0.01;   //Lot size

input on_off_mode revolve_deal=0; //Revolve deal

double
   price,
   LinePrice[20][2];

int
   LineTime[20][2];
//+------------------------------------------------------------------+
//|Initialisation function                                           |
//+------------------------------------------------------------------+ 
void init()
  {
   return;
  }
//+------------------------------------------------------------------+
//|Deinitialisation function                                         |
//+------------------------------------------------------------------+
void deinit()
  {
   return;
  }
//+------------------------------------------------------------------+
//|Trading function                                                  |
//+------------------------------------------------------------------+
void start()
  {
   ProcessingLines();

//Check for the reversal signal
   if(OrderExist()!=-1 && EnumToString(revolve_deal)=="on")
      {
         for(int i=0; i<ObjectsTotal(); i++)
           {
            if(!ObjectGet("Trendline"+DoubleToStr(i,0),OBJPROP_COLOR)) continue;
            if(TradeDirection(i)!=-1 && TradeDirection(i)!=OrderExist())
               {
                  price=Bid;
                  if(OrderType()==OP_SELL) price=Ask;
                  
                  if(OrderClose(OrderTicket(),OrderLots(),price,slip)) break;
                  else return;
               }
           }
      }
     
//Trading conditions
   if(MarketInfo(Symbol(),MODE_MARGINREQUIRED)*lot>AccountEquity() || OrderExist()!=-1) return;
   
//Check for signal (crossing any level)
   for(int i=0; i<ObjectsTotal(); i++)
     {
      if(!ObjectGet("Trendline"+DoubleToStr(i,0),OBJPROP_COLOR)) continue;

      if(TradeDirection(i)!=-1)
        {
         int ticket;
         ticket=OrderSend(Symbol(),TradeDirection(i),lot,price,slip,0,0,"",magic,0);

         if(GetLastError()!=false) {Print("Error during order opening."); break;}
         if(OrderSelect(ticket,SELECT_BY_TICKET)==false) break;

         double sl_,tp_;

         if(OrderType()==OP_BUY)
           {
            if(sl!=0) sl_=OrderOpenPrice()-MathAbs(sl)*Point;
            if(tp!=0) tp_=OrderOpenPrice()+MathAbs(tp)*Point;
           }
         else
           {
            if(sl!=0) sl_=OrderOpenPrice()+MathAbs(sl)*Point;
            if(tp!=0) tp_=OrderOpenPrice()-MathAbs(tp)*Point;
           }

         if(OrderModify(OrderTicket(),OrderOpenPrice(),sl_,tp_,0)==false) Print("Error during setting stop loss/take profit.");

         break;
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//|Checking for the new lines                                        |
//+------------------------------------------------------------------+
void ProcessingLines()
  {
   for(int h=0; h<ObjectsTotal(); h++)
     {
      //If a new line was placed on a chart start to work with it
      if(StringSubstr(ObjectName(h),0,10)=="Trendline ")
        {
         double
         price1=ObjectGet(ObjectName(h),OBJPROP_PRICE1),
         price2=ObjectGet(ObjectName(h),OBJPROP_PRICE2);

         int
         time1=(int)ObjectGet(ObjectName(h),OBJPROP_TIME1),
         time2=(int)ObjectGet(ObjectName(h),OBJPROP_TIME2);

         int a=0;
         while(ObjectGet("Trendline"+DoubleToStr(a,0),OBJPROP_COLOR)) a++;

         ObjectDelete(ObjectName(h));
         if(GetLastError())
           {
            ObjectCreate("Trendline"+DoubleToStr(a,0),OBJ_TREND,0,time1,price1,time2,price2);

            LineTime[a][0]=(int)ObjectGet("Trendline"+DoubleToStr(a,0),OBJPROP_TIME1);
            LinePrice[a][0]=ObjectGet("Trendline"+DoubleToStr(a,0),OBJPROP_PRICE1);

            LineTime[a][1]=(int)ObjectGet("Trendline"+DoubleToStr(a,0),OBJPROP_TIME2);
            LinePrice[a][1]=ObjectGet("Trendline"+DoubleToStr(a,0),OBJPROP_PRICE2);

            PlaceArrows(a);
            ObjectSet("Trendline"+DoubleToStr(a,0),OBJPROP_WIDTH,0);
           }
        }
     }

//If line was deleted, delete its arrows
   for(int l=0; l<ObjectsTotal(); l++)
     {
      if(!ObjectGet("Trendline"+DoubleToStr(l,0),OBJPROP_COLOR))
        {
         if(ObjectGet("Trendline"+DoubleToStr(l,0)+"_1",OBJPROP_COLOR)) ObjectDelete("Trendline"+DoubleToStr(l,0)+"_1");
         if(ObjectGet("Trendline"+DoubleToStr(l,0)+"_2",OBJPROP_COLOR)) ObjectDelete("Trendline"+DoubleToStr(l,0)+"_2");
         if(ObjectGet("Arrow_up"+DoubleToStr(l,0),OBJPROP_COLOR)) ObjectDelete("Arrow_up"+DoubleToStr(l,0));
         if(ObjectGet("Arrow_down"+DoubleToStr(l,0),OBJPROP_COLOR)) ObjectDelete("Arrow_down"+DoubleToStr(l,0));
         if(ObjectGet("Arrow_flat"+DoubleToStr(l,0),OBJPROP_COLOR)) ObjectDelete("Arrow_flat"+DoubleToStr(l,0));
         continue;
        }

      //Update line's coordinates     
      LineTime[l][0]=(int)ObjectGet("Trendline"+DoubleToStr(l,0),OBJPROP_TIME1);
      LinePrice[l][0]=ObjectGet("Trendline"+DoubleToStr(l,0),OBJPROP_PRICE1);

      LineTime[l][1]=(int)ObjectGet("Trendline"+DoubleToStr(l,0),OBJPROP_TIME2);
      LinePrice[l][1]=ObjectGet("Trendline"+DoubleToStr(l,0),OBJPROP_PRICE2);

      //Check its status - "off" or "on"
      LevelStatus(l);
     }

   return;
  }
//+------------------------------------------------------------------+
//|Allowed only one opened order per each instrument                 |
//+------------------------------------------------------------------+
int OrderExist()
  {
   if(OrdersTotal()==0) return(-1);

   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS) && OrderSymbol()==Symbol() && OrderMagicNumber()==magic) return(OrderType());
     }
   return(-1);
  }
//+------------------------------------------------------------------+
//|Which direction deal should be opened                             |
//+------------------------------------------------------------------+
int TradeDirection(int i)
  {
   if((ObjectGet("Arrow_up"+DoubleToStr(i,0),OBJPROP_COLOR) || ObjectGet("Arrow_down"+DoubleToStr(i,0),OBJPROP_COLOR)) && ObjectGet("Arrow_flat"+DoubleToStr(i,0),OBJPROP_COLOR)) return(-1);

   if((Ask<LevelPrice(i,TimeCurrent()) || Bid<LevelPrice(i,TimeCurrent())) && iOpen(Symbol(),Period(),0)>LevelPrice(i,TimeCurrent()))
     {
      if(ObjectGet("Arrow_up"+DoubleToStr(i,0),OBJPROP_COLOR))    {price=Ask; return(0);}
      if(ObjectGet("Arrow_flat"+DoubleToStr(i,0),OBJPROP_COLOR))  {price=Bid; return(1);}
     }

   if((Bid>LevelPrice(i,TimeCurrent()) || Ask>LevelPrice(i,TimeCurrent())) && iOpen(Symbol(),Period(),0)<LevelPrice(i,TimeCurrent()))
     {
      if(ObjectGet("Arrow_down"+DoubleToStr(i,0),OBJPROP_COLOR))  {price=Bid; return(1);}
      if(ObjectGet("Arrow_flat"+DoubleToStr(i,0),OBJPROP_COLOR))  {price=Ask; return(0);}
     }

   return(-1);
  }
//+------------------------------------------------------------------+
//|Current price of the level                                        |
//+------------------------------------------------------------------+
double LevelPrice(int i,datetime t)
  {
   return(NormalizeDouble(ObjectGetValueByShift("Trendline"+DoubleToStr(i,0),iBarShift(Symbol(),0,t)),Digits));
  }
//+------------------------------------------------------------------+
//|Level's status - "on" or "off", "up", "down" or "horizontal"      |
//+------------------------------------------------------------------+
void LevelStatus(int l)
  {
   if(!ObjectGet("Arrow_up"+DoubleToStr(l,0),OBJPROP_COLOR) && !ObjectGet("Arrow_down"+DoubleToStr(l,0),OBJPROP_COLOR) && !ObjectGet("Arrow_flat"+DoubleToStr(l,0),OBJPROP_COLOR))
     {
      //If all arrows were deleted set it again and turn line "off"
      PlaceArrows(l);
      ObjectSet("Trendline"+DoubleToStr(l,0),OBJPROP_WIDTH,0);
     }
   else if((ObjectGet("Arrow_up"+DoubleToStr(l,0),OBJPROP_COLOR) || ObjectGet("Arrow_down"+DoubleToStr(l,0),OBJPROP_COLOR)) && ObjectGet("Arrow_flat"+DoubleToStr(l,0),OBJPROP_COLOR))
     {
      //Turn line "off" if incompatible arrows were set
      ObjectSet("Trendline"+DoubleToStr(l,0),OBJPROP_WIDTH,0);
     }
   else
     {
      //Turn line "on"
      ObjectSet("Trendline"+DoubleToStr(l,0),OBJPROP_WIDTH,2);
     }

//Move arrows if line was moved
   if(ObjectGet("Arrow_up"+DoubleToStr(l,0),OBJPROP_COLOR) && (ObjectGet("Arrow_up"+DoubleToStr(l,0),OBJPROP_TIME1)!=LineTime[l][0]-Period()*300 || ObjectGet("Arrow_up"+DoubleToStr(l,0),OBJPROP_PRICE1)!=LinePrice[l][0]))
     {
      ObjectDelete("Arrow_up"+DoubleToStr(l,0));
      ObjectCreate("Arrow_up"+DoubleToStr(l,0),OBJ_ARROW,0,LineTime[l][0]-Period()*300,LinePrice[l][0]);
      ObjectSet("Arrow_up"+DoubleToStr(l,0),OBJPROP_ARROWCODE,241);
      ObjectSet("Arrow_up"+DoubleToStr(l,0),OBJPROP_COLOR,Blue);
     }

   if(ObjectGet("Arrow_down"+DoubleToStr(l,0),OBJPROP_COLOR) && (ObjectGet("Arrow_down"+DoubleToStr(l,0),OBJPROP_TIME1)!=LineTime[l][0]-Period()*300*2 || ObjectGet("Arrow_down"+DoubleToStr(l,0),OBJPROP_PRICE1)!=LinePrice[l][0]))
     {
      ObjectDelete("Arrow_down"+DoubleToStr(l,0));
      ObjectCreate("Arrow_down"+DoubleToStr(l,0),OBJ_ARROW,0,LineTime[l][0]-Period()*300*2,LinePrice[l][0]);
      ObjectSet("Arrow_down"+DoubleToStr(l,0),OBJPROP_ARROWCODE,242);
      ObjectSet("Arrow_down"+DoubleToStr(l,0),OBJPROP_COLOR,Red);
     }

   if(ObjectGet("Arrow_flat"+DoubleToStr(l,0),OBJPROP_COLOR) && (ObjectGet("Arrow_flat"+DoubleToStr(l,0),OBJPROP_TIME1)!=LineTime[l][0]-Period()*300*3 || ObjectGet("Arrow_flat"+DoubleToStr(l,0),OBJPROP_PRICE1)!=LinePrice[l][0]))
     {
      ObjectDelete("Arrow_flat"+DoubleToStr(l,0));
      ObjectCreate("Arrow_flat"+DoubleToStr(l,0),OBJ_ARROW,0,LineTime[l][0]-Period()*300*3,LinePrice[l][0]);
      ObjectSet("Arrow_flat"+DoubleToStr(l,0),OBJPROP_ARROWCODE,244);
      ObjectSet("Arrow_flat"+DoubleToStr(l,0),OBJPROP_COLOR,Green);
     }

//Set level horizontally if its second time-point at the left
   if(LineTime[l][0]>LineTime[l][1])
     {
      ObjectSet("Trendline"+DoubleToStr(l,0),OBJPROP_PRICE2,LinePrice[l][0]);
      ObjectSet("Trendline"+DoubleToStr(l,0),OBJPROP_TIME2,LineTime[l][0]+(LineTime[l][0]-LineTime[l][1]));

      LineTime[l][0]=(int)ObjectGet("Trendline"+DoubleToStr(l,0),OBJPROP_TIME1);
      LinePrice[l][0]=ObjectGet("Trendline"+DoubleToStr(l,0),OBJPROP_PRICE1);

      LineTime[l][1]=(int)ObjectGet("Trendline"+DoubleToStr(l,0),OBJPROP_TIME2);
      LinePrice[l][1]=ObjectGet("Trendline"+DoubleToStr(l,0),OBJPROP_PRICE2);
     }

//Set level's color
   if(LinePrice[l][0]==LinePrice[l][1]) ObjectSet("Trendline"+DoubleToStr(l,0),OBJPROP_COLOR,(Orange));
   else if(LinePrice[l][0]>LinePrice[l][1]) ObjectSet("Trendline"+DoubleToStr(l,0),OBJPROP_COLOR,(OrangeRed));
   else ObjectSet("Trendline"+DoubleToStr(l,0),OBJPROP_COLOR,(Teal));

   return;
  }
//+------------------------------------------------------------------+
//|Set arrows for the new line                                       |
//+------------------------------------------------------------------+   
void PlaceArrows(int i)
  {
   ObjectDelete("Arrow_up"+DoubleToStr(i,0));
   ObjectDelete("Arrow_down"+DoubleToStr(i,0));
   ObjectDelete("Arrow_flat"+DoubleToStr(i,0));

   ObjectCreate("Arrow_up"+DoubleToStr(i,0),OBJ_ARROW,0,LineTime[i][0]-Period()*300,LinePrice[i][0]);
   ObjectSet("Arrow_up"+DoubleToStr(i,0),OBJPROP_ARROWCODE,241);
   ObjectSet("Arrow_up"+DoubleToStr(i,0),OBJPROP_COLOR,Blue);

   ObjectCreate("Arrow_down"+DoubleToStr(i,0),OBJ_ARROW,0,LineTime[i][0]-Period()*300*2,LinePrice[i][0]);
   ObjectSet("Arrow_down"+DoubleToStr(i,0),OBJPROP_ARROWCODE,242);
   ObjectSet("Arrow_down"+DoubleToStr(i,0),OBJPROP_COLOR,Red);

   ObjectCreate("Arrow_flat"+DoubleToStr(i,0),OBJ_ARROW,0,LineTime[i][0]-Period()*300*3,LinePrice[i][0]);
   ObjectSet("Arrow_flat"+DoubleToStr(i,0),OBJPROP_ARROWCODE,244);
   ObjectSet("Arrow_flat"+DoubleToStr(i,0),OBJPROP_COLOR,Green);

   return;
  }
//+------------------------------------------------------------------+
