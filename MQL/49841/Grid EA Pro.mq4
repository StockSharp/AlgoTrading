//+------------------------------------------------------------------+
//|                                                  Grid EA Pro.mq4 |
//|                                COPYRIGHT 2020, NGUYEN NGHIEM DUY |
//|                     https://www.mql5.com/en/users/closetoyou2005 |
//+------------------------------------------------------------------+
#property copyright "COPYRIGHT 2020, NGUYEN NGHIEM DUY"
#property link      "https://www.mql5.com/en/users/closetoyou2005"
#property version   "2.0"
#property strict

//#define EXPIRATION_TIME   D'2126.01.01 23:59'
//#define  ACC_NUMBER   24204994

enum ENUM_STRATEGY
  {
   RSI     = 1,
   Fixed_Points    = 2,
   MANUAL  = 3
  };
enum enum_mode
  {
   BUY       = 0,
   SELL      = 1,
   BOTH   = 2
  };

input enum_mode          MODE                     = 2;
extern   ENUM_STRATEGY   STRATEGY                 = 1;
//---
extern int               RSI_PERIOD               = 10;
extern int               UP_LEVEL                 = 70;
extern int               DN_LEVEL                 = 30;
input ENUM_TIMEFRAMES    RSI_TIMEFRAME            = PERIOD_H4;
//---
//---
input int                DISTANCE                 = 50;
input int                TIMER_SEC                = 10;
//---

input double             VOLUME                   = 0.01;
input int                FROM_BALANCE             = 1000;
input double             RISK_PER_TRADE           = 0.0;

input double             LOT_MULTIPLIER           = 1.1;
input double             MAX_LOT                  = 999.9;
//---
input int                STEP_ORDERS              = 100;
input double             STEP_MULTIPLIER          = 1.1;
input int                MAX_STEP                 = 1000;
//---
input int                OVERLAP_ORDERS           = 5;
input int                OVERLAP_PIPS             = 10;
//---
input int                STOPLOSS                 = -1;
input int                TAKEPROFIT               = 500;
//---
input int                BREAKEVEN_STOP            = -1;
input int                BREAKEVEN_STEP            = 10;
//---
input int                TRAILING_STOP            = 50;
input int                TRAILING_STEP            = 50;
//---
input int                MAGIC_NUMBER             = -1;
input string             ORDERS_COMMENT           = "Grid EA Pro";
//---
input string             START_TIME               = "00:00";
input string             END_TIME                 = "00:00";
//---
bool CLOSE_BUY=false,CLOSE_SELL=false,CLOSE_ALL=false;
double ZEROLEVEL_ALL=0,ZEROLEVEL_BUY=0,ZEROLEVEL_SELL=0;

double TRAILINGSTOP_BUY=0,TRAILINGSTEP_BUY=0;
double TRAILINGSTOP_SELL=0,TRAILINGSTEP_SELL=0;

double BREAKEVENSTOP_BUY=0,BREAKEVENSTEP_BUY=0;
double BREAKEVENSTOP_SELL=0,BREAKEVENSTEP_SELL=0;

double TAKEPROFIT_BUY=0,STOPLOSS_BUY=0;
double TAKEPROFIT_SELL=0,STOPLOSS_SELL=0;

double BUY_PROFIT=0,SELL_PROFIT=0,BUY_LOTS=0,SELL_LOTS=0;

double TICKVALUE=0;
int SPREAD=0;

int PREV_BUYS=0,PREV_SELLS=0;
bool DEINIT = false;
bool FIRST_RUN=true;

int BUYS=0, SELLS=0, BUYLIMITS=0, SELLLIMITS=0, BUYSTOPS=0, SELLSTOPS=0;

color BUTTONL_CLR=clrNONE;
double LAST_BID=0;
double TEMP_LOT=VOLUME;
double LAST_LOT=0;
//---
int WIN_TYPE=0,WIN_TICKET=0;
double WIN_PROFIT=0,WIN_LOT=0;
//---
int LOSS_TYPE=0,LOSS_TICKET=0;
double LOSS_PROFIT=0,LOSS_LOT=0;
//---
double ZEROLEVEL=0;
double OVERLAP_CLOSE=0;
//---
double LAST_BUY_PRICE=0,LAST_BUY_LOT=0,LAST_SELL_PRICE=0,LAST_SELL_LOT=0;
double GRID_BUY_PRICE=0,GRID_SELL_PRICE=0;
//---
bool SIGNAL=true,SIGNAL_BUY=true,SIGNAL_SELL=true;
double BUY_PRICE=0.0,SELL_PRICE=0.0;
int INP_TIME=TIMER_SEC;
datetime TIME_CURRENT=TimeCurrent();
static datetime TIMER;
//---
//---
//---
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {  
       /*
   if(AccountNumber()!=ACC_NUMBER)
     {
      Alert("Wrong MT4 account number");
      ExpertRemove();
     }
  
   if(TimeCurrent()>EXPIRATION_TIME)
     {
      Alert("Expiration Time");
      ExpertRemove();
     }
          */  
   if(INP_TIME>60)
      INP_TIME=60;

   EventSetTimer(1);
   LAST_BID    = Bid;

   TICKVALUE=SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_VALUE);
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   LAST_LOT=StringToDouble(ObjectGetString(0,"BUTTON_LOT",OBJPROP_TEXT));

   EventKillTimer();
   DEINIT=true;
   DEINIT_HLINE();
   DEINIT_BUTTON();
   DEINIT_LABEL();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTimer()
  {
   OnTick();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(IsVisualMode() || (!IsOptimization() && !IsTesting()))
     {
      INFO();
      DRAW_BUTTON();
      ON_CHART_EVENTS();
      HLINE();

     }

   GET_TICKVALUE();

   BUY_PROFIT=BUY_LOTS=SELL_PROFIT=SELL_LOTS=0;
   TOTAL_VALUE(BUY_PROFIT,BUY_LOTS,SELL_PROFIT,SELL_LOTS,MAGIC_NUMBER);

   BUYS=SELLS=BUYLIMITS=SELLLIMITS=BUYSTOPS=SELLSTOPS=0;
   COUNT_ORDERS(BUYS,SELLS,BUYLIMITS,SELLLIMITS,BUYSTOPS,SELLSTOPS,MAGIC_NUMBER);

   if(OVERLAP_ORDERS>0 && BUYS+SELLS>=OVERLAP_ORDERS)
      OVERLAP(-1);


   if(BUYS>0)
     {
      if(TRAILING_STOP>0)
         VIRTUAL_TRAILINGSTOP(OP_BUY);
      if(BREAKEVEN_STOP>0)
         VIRTUAL_BREAKEVEN(OP_BUY);
      if(STOPLOSS+TAKEPROFIT>0)
         VIRTUAL_STOPLOSS_TAKEPROFIT(OP_BUY);
     }

   if(SELLS>0)
     {
      if(TRAILING_STOP>0)
         VIRTUAL_TRAILINGSTOP(OP_SELL);
      if(BREAKEVEN_STOP>0)
         VIRTUAL_BREAKEVEN(OP_SELL);
      if(STOPLOSS+TAKEPROFIT>0)
         VIRTUAL_STOPLOSS_TAKEPROFIT(OP_SELL);
     }

   LAST_BUY_PRICE=LAST_BUY_LOT=LAST_SELL_PRICE=LAST_SELL_LOT=0;
   OPENED_VALUES(LAST_BUY_PRICE,LAST_BUY_LOT,LAST_SELL_PRICE,LAST_SELL_LOT);

   if(WORKING_HOURS(START_TIME,END_TIME)==true)
     {
      double LOT = LOT_CALCULATE(RISK_PER_TRADE,STOPLOSS,FROM_BALANCE,VOLUME);

      if(STRATEGY==1)
        {
         if(IsVisualMode() || (!IsOptimization() && !IsTesting()))
           {
            if(BUYS+SELLS>0)
              {
               Comment(
                  "WIN ORDER: "+"TYPE "+(string)WIN_TYPE+", TICKET "+(string)WIN_TICKET+", PROFIT "+DoubleToString(WIN_PROFIT,2)+", LOT "+DoubleToString(WIN_LOT,2)+"\n"+
                  "LOSS ORDER: "+"TYPE "+(string)LOSS_TYPE+", TICKET "+(string)LOSS_TICKET+", PROFIT "+DoubleToString(LOSS_PROFIT,2)+", LOT "+DoubleToString(LOSS_LOT,2)+"\n"
               );
              }
           }
         if(CLOSE_SELL==FALSE && SELLS==0 && RSI(RSI_PERIOD,RSI_TIMEFRAME)>=UP_LEVEL)
           {
            if(MODE==2||MODE==1)
               ORDER_SEND(OP_SELL,LOT,ORDERS_COMMENT,MAGIC_NUMBER);
           }

         if(CLOSE_BUY==FALSE && BUYS==0 && RSI(RSI_PERIOD,RSI_TIMEFRAME)<=DN_LEVEL)
           {
            if(MODE==2||MODE==0)
               ORDER_SEND(OP_BUY,LOT,ORDERS_COMMENT,MAGIC_NUMBER);
           }
        }
      //---
      if(STRATEGY==2)
        {
         TIME_CURRENT=TimeCurrent();

         if(INP_TIME==0)
            SIGNAL=true;
         else
           {
            if(TIMER==0)
               TIMER=TIME_CURRENT;
            if(TIME_CURRENT>=TIMER+INP_TIME)
              {
               SIGNAL=true;
               TIMER+=INP_TIME;
              }
           }

         if(SIGNAL)
           {
            if(BUYS==0)
               BUY_PRICE=SymbolInfoDouble(_Symbol,SYMBOL_ASK)+DISTANCE*_Point;
            if(SELLS==0)
               SELL_PRICE=SymbolInfoDouble(_Symbol,SYMBOL_BID)-DISTANCE*_Point;
            SIGNAL=false;
            SIGNAL_BUY=true;
            SIGNAL_SELL=true;
           }

         if(SIGNAL_BUY)
           {
            if(BUY_PRICE>0 && !(BUY_PRICE-SymbolInfoDouble(_Symbol,SYMBOL_ASK)>_Point))
              {
               if(BUYS==0 && CLOSE_BUY==false)
                 {

                  if(MODE==2||MODE==0)ORDER_SEND(OP_BUY,LOT,ORDERS_COMMENT,MAGIC_NUMBER);

                  SIGNAL_BUY=false;
                  BUY_PRICE=0;
                  //---
                  DEINIT_HLINE();

                 }
              }
           }

         if(SIGNAL_SELL)
           {
            if(SELL_PRICE>0 && !(SymbolInfoDouble(_Symbol,SYMBOL_BID)-SELL_PRICE>_Point))
              {
               if(SELLS==0 && CLOSE_SELL==false)
                 {
                  if(MODE==2||MODE==1)ORDER_SEND(OP_SELL,LOT,ORDERS_COMMENT,MAGIC_NUMBER);

                  SIGNAL_SELL=false;
                  SELL_PRICE=0;
                  //---
                  DEINIT_HLINE();

                 }
              }
           }
         //---
         //---
         //---
         if(IsVisualMode() || (!IsOptimization() && !IsTesting()))
           {
            if(BUY_PRICE>0)
               OBJECT_HLINE("HLINE_BUY",BUY_PRICE,"OPEN BUY",clrRed,0);
            if(SELL_PRICE>0)
               OBJECT_HLINE("HLINE_SELL",SELL_PRICE,"OPEN SELL",clrRed,0);


            //if(BUYS+SELLS>0)
            // {
            int PRINT_PRICE=0,PRINT_TIME=0;
            PRINT_TIME = INP_TIME-(int)StringSubstr((TimeToString(TIME_CURRENT-TIMER,TIME_SECONDS)),6,2);
            PRINT_PRICE = (int)MathMin((BUY_PRICE-SymbolInfoDouble(_Symbol,SYMBOL_ASK))/_Point,
                                       (SymbolInfoDouble(_Symbol,SYMBOL_BID)-SELL_PRICE)/_Point);


            Comment(
               "WIN ORDER: "+"TYPE "+(string)WIN_TYPE+", TICKET "+(string)WIN_TICKET+", PROFIT "+DoubleToString(WIN_PROFIT,2)+", LOT "+DoubleToString(WIN_LOT,2)+"\n"+
               "LOSS ORDER: "+"TYPE "+(string)LOSS_TYPE+", TICKET "+(string)LOSS_TICKET+", PROFIT "+DoubleToString(LOSS_PROFIT,2)+", LOT "+DoubleToString(LOSS_LOT,2)+"\n"
               "DISTANCE: "+(string)PRINT_PRICE+" PIPS"+"\n"+
               "TIMER: "+(string)PRINT_TIME+" SEC"
            );
            // }
           }
        }

      if(STEP_ORDERS>0)
        {
         if(BUYS>0)
           {
            int STEP = MathMin(STEP_ORDERS,MAX_STEP);
            if(STEP_MULTIPLIER>1 && BUYS>1)
               STEP = (int)(MathMin((BUYS-1)*STEP_MULTIPLIER*STEP_ORDERS,MAX_STEP));

            LOT = LOT_CALCULATE(RISK_PER_TRADE,STOPLOSS,FROM_BALANCE,VOLUME);
            if(LOT_MULTIPLIER>1 && BUYS>0)
               LOT = MathMin(LAST_BUY_LOT*LOT_MULTIPLIER,MAX_LOT);

            GRID_BUY_PRICE = LAST_BUY_PRICE-STEP*_Point;

            if(SymbolInfoDouble(_Symbol,SYMBOL_ASK)<=GRID_BUY_PRICE && CLOSE_ALL==false && CLOSE_BUY==false)
               ORDER_SEND(OP_BUY,LOT,ORDERS_COMMENT,MAGIC_NUMBER);
           }
         //---
         if(SELLS>0)
           {
            int STEP = MathMin(STEP_ORDERS,MAX_STEP);
            if(STEP_MULTIPLIER>1 && SELLS>1)
               STEP = (int)(MathMin((SELLS-1)*STEP_MULTIPLIER*STEP_ORDERS,MAX_STEP));

            LOT = LOT_CALCULATE(RISK_PER_TRADE,STOPLOSS,FROM_BALANCE,VOLUME);
            if(LOT_MULTIPLIER>1 && SELLS>0)
               LOT = MathMin(LAST_SELL_LOT*LOT_MULTIPLIER,MAX_LOT);

            GRID_SELL_PRICE = LAST_SELL_PRICE+STEP*_Point;

            if(SymbolInfoDouble(_Symbol,SYMBOL_BID)>=GRID_SELL_PRICE && CLOSE_ALL==false && CLOSE_SELL==false)
               ORDER_SEND(OP_SELL,LOT,ORDERS_COMMENT,MAGIC_NUMBER);
           }
        }
     }

   if(CLOSE_BUY==true)
     {
      if(BUYS>0)
         ORDERS_CLOSE(OP_BUY);
      if(BUYS==0)
         CLOSE_BUY=false;
     }

   if(CLOSE_SELL==true)
     {
      if(SELLS>0)
         ORDERS_CLOSE(OP_SELL);
      if(SELLS==0)
         CLOSE_SELL=false;
     }

   if(CLOSE_ALL==true)
     {
      if(BUYS+SELLS>0)
         ORDERS_CLOSE(-1);
      if(BUYS+SELLS==0)
         CLOSE_ALL=false;
     }

   if(PREV_BUYS+PREV_SELLS!=BUYS+SELLS||FIRST_RUN==true)
      RECALCULATION_PARAMETERS();

   FIRST_RUN=false;

   DEINIT=false;

   PREV_BUYS=BUYS;
   PREV_SELLS=SELLS;
   LAST_BID    = Bid;

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OVERLAP(int ORDERTYPE = -1)
  {
   WIN_TYPE=WIN_TICKET=0;
   WIN_PROFIT=WIN_LOT=0;
   WIN_POSITION(WIN_TYPE,WIN_TICKET,WIN_PROFIT,WIN_LOT,_Symbol,ORDERTYPE,MAGIC_NUMBER);

   LOSS_TYPE=LOSS_TICKET=0;
   LOSS_PROFIT=LOSS_LOT=0;
   LOSS_POSITION(LOSS_TYPE,LOSS_TICKET,LOSS_PROFIT,LOSS_LOT,_Symbol,ORDERTYPE,MAGIC_NUMBER);

   if(-LOSS_PROFIT==0 || WIN_PROFIT==0)
      return;

   if(WIN_TYPE==LOSS_TYPE)
     {
      if(TICKVALUE*(WIN_LOT+LOSS_LOT)==0)
        {
         ZEROLEVEL=0;
         OVERLAP_CLOSE=0;
         return;
        }

      if(WIN_TYPE==0 && LOSS_TYPE==0)
        {
         ZEROLEVEL=NormalizeDouble(SymbolInfoDouble(NULL,SYMBOL_BID)-((WIN_PROFIT+LOSS_PROFIT)/(TICKVALUE*(WIN_LOT+LOSS_LOT))*_Point),_Digits);
         OVERLAP_CLOSE=NormalizeDouble(ZEROLEVEL+OVERLAP_PIPS*_Point,_Digits);

         if(SymbolInfoDouble(NULL,SYMBOL_BID)>=OVERLAP_CLOSE)
           {
            ORDERS_CLOSE(WIN_TYPE,WIN_TICKET,WIN_LOT);
            ORDERS_CLOSE(LOSS_TYPE,LOSS_TICKET,LOSS_LOT);
            ZEROLEVEL=0;
            OVERLAP_CLOSE=0;
           }
        }

      if(WIN_TYPE==1 && LOSS_TYPE==1)
        {
         ZEROLEVEL=NormalizeDouble(SymbolInfoDouble(NULL,SYMBOL_ASK)+((WIN_PROFIT+LOSS_PROFIT)/(TICKVALUE*(WIN_LOT+LOSS_LOT))*_Point),_Digits);
         OVERLAP_CLOSE=NormalizeDouble(ZEROLEVEL-OVERLAP_PIPS*_Point,_Digits);

         if(SymbolInfoDouble(NULL,SYMBOL_ASK)<=OVERLAP_CLOSE)
           {
            ORDERS_CLOSE(WIN_TYPE,WIN_TICKET,WIN_LOT);
            ORDERS_CLOSE(LOSS_TYPE,LOSS_TICKET,LOSS_LOT);
            ZEROLEVEL=0;
            OVERLAP_CLOSE=0;
           }
        }
     }
//---
   if(WIN_TYPE!=LOSS_TYPE)
     {
      if(TICKVALUE*MathAbs(WIN_LOT-LOSS_LOT)==0)
        {
         ZEROLEVEL=0;
         OVERLAP_CLOSE=0;
         return;
        }

      if(WIN_LOT>LOSS_LOT)
        {
         if(WIN_TYPE==0)
           {
            ZEROLEVEL=NormalizeDouble(SymbolInfoDouble(NULL,SYMBOL_BID)-((WIN_PROFIT+LOSS_PROFIT)/(TICKVALUE*MathAbs(WIN_LOT-LOSS_LOT))*_Point),_Digits);
            OVERLAP_CLOSE=NormalizeDouble(ZEROLEVEL+OVERLAP_PIPS*_Point,_Digits);

            if(SymbolInfoDouble(NULL,SYMBOL_BID)>=OVERLAP_CLOSE)
              {
               ORDERS_CLOSE(WIN_TYPE,WIN_TICKET,WIN_LOT);
               ORDERS_CLOSE(LOSS_TYPE,LOSS_TICKET,LOSS_LOT);
               ZEROLEVEL=0;
               OVERLAP_CLOSE=0;
              }
           }

         if(WIN_TYPE==1)
           {
            ZEROLEVEL=NormalizeDouble(SymbolInfoDouble(NULL,SYMBOL_ASK)+((WIN_PROFIT+LOSS_PROFIT)/(TICKVALUE*MathAbs(WIN_LOT-LOSS_LOT))*_Point),_Digits);
            OVERLAP_CLOSE=NormalizeDouble(ZEROLEVEL-OVERLAP_PIPS*_Point,_Digits);

            if(SymbolInfoDouble(NULL,SYMBOL_ASK)<=OVERLAP_CLOSE)
              {
               ORDERS_CLOSE(WIN_TYPE,WIN_TICKET,WIN_LOT);
               ORDERS_CLOSE(LOSS_TYPE,LOSS_TICKET,LOSS_LOT);
               ZEROLEVEL=0;
               OVERLAP_CLOSE=0;
              }
           }
        }
      //---
      if(WIN_LOT<LOSS_LOT)
        {
         if(LOSS_TYPE==0)
           {
            ZEROLEVEL=NormalizeDouble(SymbolInfoDouble(NULL,SYMBOL_BID)-((WIN_PROFIT+LOSS_PROFIT)/(TICKVALUE*MathAbs(WIN_LOT-LOSS_LOT))*_Point),_Digits);
            OVERLAP_CLOSE=NormalizeDouble(ZEROLEVEL+OVERLAP_PIPS*_Point,_Digits);

            if(SymbolInfoDouble(NULL,SYMBOL_BID)>=OVERLAP_CLOSE)
              {
               ORDERS_CLOSE(WIN_TYPE,WIN_TICKET,WIN_LOT);
               ORDERS_CLOSE(LOSS_TYPE,LOSS_TICKET,LOSS_LOT);
               ZEROLEVEL=0;
               OVERLAP_CLOSE=0;
              }
           }

         if(LOSS_TYPE==1)
           {
            ZEROLEVEL=NormalizeDouble(SymbolInfoDouble(NULL,SYMBOL_ASK)+((WIN_PROFIT+LOSS_PROFIT)/(TICKVALUE*MathAbs(WIN_LOT-LOSS_LOT))*_Point),_Digits);
            OVERLAP_CLOSE=NormalizeDouble(ZEROLEVEL-OVERLAP_PIPS*_Point,_Digits);

            if(SymbolInfoDouble(NULL,SYMBOL_ASK)<=OVERLAP_CLOSE)
              {
               ORDERS_CLOSE(WIN_TYPE,WIN_TICKET,WIN_LOT);
               ORDERS_CLOSE(LOSS_TYPE,LOSS_TICKET,LOSS_LOT);
               ZEROLEVEL=0;
               OVERLAP_CLOSE=0;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void GET_TICKVALUE()
  {
   SPREAD=(int)MathMax((SymbolInfoDouble(NULL,SYMBOL_ASK)-SymbolInfoDouble(NULL,SYMBOL_BID)/_Point),SymbolInfoInteger(NULL,SYMBOL_SPREAD));

   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==_Symbol)
           {
            if(OrderProfit()!=0)
               TICKVALUE=MathAbs(OrderProfit()/((OrderClosePrice()-OrderOpenPrice())/_Point)/OrderLots());
           }
        }
     }

   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TOTAL_VALUE(double &PROFIT_BUY,double &LOTS_BUY,double &PROFIT_SELL,double &LOTS_SELL, int MAGIC=-1)
  {
   PROFIT_BUY=LOTS_BUY=PROFIT_SELL=LOTS_SELL=0;

   for(int I=0; I<OrdersTotal(); I++)
     {
      if(OrderSelect(I,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==_Symbol && (OrderMagicNumber()==MAGIC || MAGIC==-1))
           {
            if(OrderType()==OP_BUY)
              {
               PROFIT_BUY+=NormalizeDouble(OrderProfit()+OrderSwap()+OrderCommission(),2);
               LOTS_BUY+=OrderLots();
              }
            if(OrderType()==OP_SELL)
              {
               PROFIT_SELL+=NormalizeDouble(OrderProfit()+OrderSwap()+OrderCommission(),2);
               LOTS_SELL+=OrderLots();
              }
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void RECALCULATION_PARAMETERS()
  {
   Print("RECALCULATION");
//---
   ZEROLEVEL_BUY       = 0;
   ZEROLEVEL_SELL      = 0;
   ZEROLEVEL_ALL       = 0;

   ZEROLEVEL           = 0;
   OVERLAP_CLOSE       = 0;

   GRID_BUY_PRICE      = 0;
   GRID_SELL_PRICE     = 0;

   TRAILINGSTOP_BUY    = 0;
   TRAILINGSTEP_BUY    = 0;
   TRAILINGSTOP_SELL   = 0;
   TRAILINGSTEP_SELL   = 0;

   BREAKEVENSTOP_BUY    = 0;
   BREAKEVENSTEP_BUY    = 0;
   BREAKEVENSTOP_SELL   = 0;
   BREAKEVENSTEP_SELL   = 0;

   STOPLOSS_SELL       = 0;
   STOPLOSS_BUY        = 0;

   TAKEPROFIT_SELL     = 0;
   TAKEPROFIT_BUY      = 0;

   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ORDERS_CLOSE(int CMD = -1,int TICKET = -1,double ORDER_LOTS=-1)
  {
   for(int i=OrdersTotal(); i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==_Symbol && (OrderType()==CMD || CMD==-1) && (OrderTicket()==TICKET || TICKET==-1)&& (OrderMagicNumber()==MAGIC_NUMBER || MAGIC_NUMBER==-1))
           {
            if(!OrderClose(OrderTicket(),(ORDER_LOTS==-1?OrderLots():ORDER_LOTS),OrderClosePrice(),0,clrNONE))
              {
               Print(__FUNCTION__," ",__LINE__," ERROR: ",GetLastError());
               Sleep(1000);
               return;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void COUNT_ORDERS(int &BUY,int &SELL,int &BUYLIMIT,int &SELLLIMIT,int &BUYSTOP,int &SELLSTOP, int MAGIC=-1)
  {
   BUY=SELL=BUYLIMIT=SELLLIMIT=BUYSTOP=SELLSTOP=0;

   for(int I=0; I<OrdersTotal(); I++)
     {
      if(OrderSelect(I,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==_Symbol && (OrderMagicNumber()==MAGIC || MAGIC==-1))
           {
            switch(OrderType())
              {
               case OP_BUY:
                  BUY++;
                  break;
               case OP_SELL:
                  SELL++;
                  break;
               case OP_BUYLIMIT:
                  BUYLIMIT++;
                  break;
               case OP_SELLLIMIT:
                  SELLLIMIT++;
                  break;
               case OP_BUYSTOP:
                  BUYSTOP++;
                  break;
               case OP_SELLSTOP:
                  SELLSTOP++;
                  break;
              }
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DEINIT_HLINE()
  {
   for(int I=ObjectsTotal(0,-1,-1)-1; I>=0; I--)
     {
      if(StringFind(ObjectName(0,I,-1,-1),"HLINE",0)>=0)
         ObjectDelete(0,ObjectName(0,I,-1,-1));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OBJECT_HLINE(string NAME,double _PRICE,string TEXT,color CLR,int STYLE=3)
  {
   if(ObjectFind(0,NAME)<0)
     {
      ObjectCreate(0,NAME,OBJ_HLINE,0,0,0);
      ObjectSetInteger(0,NAME,OBJPROP_COLOR,CLR);
      ObjectSetInteger(0,NAME,OBJPROP_STYLE,STYLE);
      ObjectSetInteger(0,NAME,OBJPROP_WIDTH,1);
      ObjectSetInteger(0,NAME,OBJPROP_BACK,true);
      ObjectSetInteger(0,NAME,OBJPROP_SELECTABLE,false);
      ObjectSetInteger(0,NAME,OBJPROP_SELECTED,false);
      ObjectSetInteger(0,NAME,OBJPROP_HIDDEN,true);
      ObjectSetInteger(0,NAME,OBJPROP_ZORDER,0);
      ObjectSet(NAME,OBJPROP_READONLY,false);
      ObjectSetText(NAME,TEXT);
     }
   else
     {
      ObjectSetDouble(0,NAME,OBJPROP_PRICE,0,_PRICE);
      ObjectSetString(0,NAME,OBJPROP_TOOLTIP,TEXT);
      ObjectSetInteger(0,NAME,OBJPROP_COLOR,CLR);
     }

   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DEINIT_BUTTON()
  {
   for(int i=ObjectsTotal()-1; i>=0; i--)
     {
      if(StringFind(ObjectName(i),"DRAW_BUTTON")>=0)
         ObjectDelete(ObjectName(i));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool OBJECT_BUTTON(const long              CHART_ID=0,
                   const string            NAME="",
                   const int               SUB_WINDOW=0,
                   const int               X=0,
                   const int               Y=0,
                   const int               WIDTH=50,
                   const int               HEIGHT=18,
                   const ENUM_BASE_CORNER  CORNER=CORNER_LEFT_UPPER,
                   const string            TEXT="",
                   const string            FONT="",
                   const int               FONT_SIZE=10,
                   const color             CLR=clrBlack,
                   const color             BACK_CLR=clrNONE,
                   const color             BORDER_CLR=clrNONE,
                   const bool              state=FALSE,
                   const bool              BACK=FALSE,
                   const bool              SELECTION=FALSE,
                   const bool              HIDDEN=TRUE,
                   const long              ZORDER=0)
  {
   ResetLastError();
   if(ObjectFind(NAME)<0)
     {
      ObjectCreate(CHART_ID,NAME,OBJ_BUTTON,SUB_WINDOW,0,0);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_XDISTANCE,X);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_YDISTANCE,Y);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_XSIZE,WIDTH);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_YSIZE,HEIGHT);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_CORNER,CORNER);
      ObjectSetString(CHART_ID,NAME,OBJPROP_TEXT,TEXT);
      ObjectSetString(CHART_ID,NAME,OBJPROP_FONT,FONT);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_FONTSIZE,FONT_SIZE);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_COLOR,CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BGCOLOR,BACK_CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BORDER_COLOR,BORDER_CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BACK,BACK);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_STATE,state);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_SELECTABLE,SELECTION);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_SELECTED,SELECTION);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_HIDDEN,HIDDEN);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_ZORDER,ZORDER);
     }
   else
     {
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_COLOR,CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BGCOLOR,BACK_CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BORDER_COLOR,BORDER_CLR);
     }
   return(true);
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DRAW_BUTTON()
  {
   int X=2;
   int Y=15;

   if(Bid!=LAST_BID && Bid>LAST_BID)
      BUTTONL_CLR = color("16,16,190");
   if(Bid!=LAST_BID && Bid<LAST_BID)
      BUTTONL_CLR = color("190,16,15");

//OBJECT_RECTANGLE(0,"PANEL_body",0,X+1,Y+15,255,30,clrBlack,BORDER_FLAT,CORNER_LEFT_UPPER,clrBlack,STYLE_SOLID,1,FALSE,FALSE,TRUE,0);
   OBJECT_BUTTON(0,"BUTTON_SELL",0,X+1,Y+15,55,25,CORNER_LEFT_LOWER,"SELL","Arial",10,clrWhite,BUTTONL_CLR,clrBlack,FALSE,FALSE,FALSE,TRUE,0);
   OBJECT_BUTTON(0,"BUTTON_-LOT",0,X+56,Y+15,20,25,CORNER_LEFT_LOWER,"-","Arial",10,clrBlack,color("235,235,235"),clrBlack,FALSE,FALSE,FALSE,TRUE,0);
   OBJECT_EDIT(0,"BUTTON_LOT",0,X+76,Y+15,45,25,DoubleToString(TEMP_LOT,2),"Arial",10,ALIGN_RIGHT,FALSE,CORNER_LEFT_LOWER,clrBlack,clrWhite,clrBlack,FALSE,FALSE,FALSE,0);
   OBJECT_BUTTON(0,"BUTTON_+LOT",0,X+121,Y+15,20,25,CORNER_LEFT_LOWER,"+","Arial",10,clrBlack,color("235,235,235"),clrBlack,FALSE,FALSE,FALSE,TRUE,0);
   OBJECT_BUTTON(0,"BUTTON_BUY",0,X+141,Y+15,55,25,CORNER_LEFT_LOWER,"BUY","Arial",10,clrWhite,BUTTONL_CLR,clrBlack,FALSE,FALSE,FALSE,TRUE,0);

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool OBJECT_EDIT(const long             CHART_ID=0,
                 const string           NAME        = "",
                 const int              SUB_WINDOW  = 0,
                 const int              X           = 0,
                 const int              Y           = 0,
                 const int              WIDTH       = 0,
                 const int              HEIGHT      = 0,
                 const string           TEXT        = "",
                 const string           FONT        = "",
                 const int              FONT_SIZE   = 0,
                 const ENUM_ALIGN_MODE  ALIGN       = ALIGN_CENTER,
                 const bool             READ_ONLY   = FALSE,
                 const ENUM_BASE_CORNER CORNER      = CORNER_LEFT_UPPER,
                 const color            CLR         = clrBlack,
                 const color            BACK_CLR    = clrWhite,
                 const color            BORDER_CLR  = clrNONE,
                 const bool             BACK        = FALSE,
                 const bool             SELECTION   = FALSE,
                 const bool             HIDDEN      = TRUE,
                 const long             ZORDER=0)
  {
   ResetLastError();

   if(ObjectFind(NAME)<0)
     {
      ObjectCreate(CHART_ID,NAME,OBJ_EDIT,SUB_WINDOW,0,0);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_XDISTANCE,X);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_YDISTANCE,Y);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_XSIZE,WIDTH);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_YSIZE,HEIGHT);
      ObjectSetString(CHART_ID,NAME,OBJPROP_TEXT,TEXT);
      ObjectSetString(CHART_ID,NAME,OBJPROP_FONT,FONT);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_FONTSIZE,FONT_SIZE);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_ALIGN,ALIGN);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_READONLY,READ_ONLY);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_CORNER,CORNER);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_COLOR,CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BGCOLOR,BACK_CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BORDER_COLOR,BORDER_CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BACK,BACK);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_SELECTABLE,SELECTION);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_SELECTED,SELECTION);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_HIDDEN,HIDDEN);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_ZORDER,ZORDER);
     }
   else
     {
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_COLOR,CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BGCOLOR,BACK_CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BORDER_COLOR,BORDER_CLR);
     }
   return(true);
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ON_CHART_EVENTS()
  {
   TEMP_LOT=StringToDouble(ObjectGetString(0,"BUTTON_LOT",OBJPROP_TEXT));
   if(StringToDouble(ObjectGetString(0,"BUTTON_LOT",OBJPROP_TEXT))<MarketInfo(NULL,MODE_MINLOT))
      ObjectSetString(0,"BUTTON_LOT",OBJPROP_TEXT,(string)MathMax(LAST_LOT,MarketInfo(NULL,MODE_MINLOT)));

   if(ObjectGetInteger(0,"BUTTON_+LOT",OBJPROP_STATE)==TRUE)
     {
      double LOTS=StringToDouble(ObjectGetString(0,"BUTTON_LOT",OBJPROP_TEXT));
      LOTS=LOTS+MarketInfo(NULL,MODE_MINLOT);
      if(LOTS>=MarketInfo(NULL,MODE_MAXLOT))
         LOTS=MarketInfo(NULL,MODE_MAXLOT);
      ObjectSetString(0,"BUTTON_LOT",OBJPROP_TEXT,DoubleToStr(LOTS,2));
      TEMP_LOT=StringToDouble(ObjectGetString(0,"BUTTON_LOT",OBJPROP_TEXT));
      ObjectSetInteger(0,"BUTTON_+LOT",OBJPROP_STATE,FALSE);
     }

   if(ObjectGetInteger(0,"BUTTON_-LOT",OBJPROP_STATE)==TRUE)
     {
      double LOTS=StringToDouble(ObjectGetString(0,"BUTTON_LOT",OBJPROP_TEXT));
      LOTS=LOTS-MarketInfo(NULL,MODE_MINLOT);
      if(LOTS<=MarketInfo(NULL,MODE_MINLOT))
         LOTS=MarketInfo(NULL,MODE_MINLOT);
      ObjectSetString(0,"BUTTON_LOT",OBJPROP_TEXT,DoubleToStr(LOTS,2));
      TEMP_LOT=StringToDouble(ObjectGetString(0,"BUTTON_LOT",OBJPROP_TEXT));
      ObjectSetInteger(0,"BUTTON_-LOT",OBJPROP_STATE,FALSE);
     }

   if(ObjectGetInteger(0,"BUTTON_BUY",OBJPROP_STATE)==TRUE)
     {
      ORDER_SEND(OP_BUY,TEMP_LOT,ORDERS_COMMENT,MAGIC_NUMBER);
      ObjectSetInteger(0,"BUTTON_BUY",OBJPROP_STATE,FALSE);
      BUYS++;
     }

   if(ObjectGetInteger(0,"BUTTON_SELL",OBJPROP_STATE)==TRUE)
     {
      ORDER_SEND(OP_SELL,TEMP_LOT,ORDERS_COMMENT,MAGIC_NUMBER);
      ObjectSetInteger(0,"BUTTON_SELL",OBJPROP_STATE,FALSE);
      SELLS++;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ORDER_SEND(ENUM_ORDER_TYPE CMD,double LOTS,string COMMENT, int MAGIC)
  {
   if(OrdersTotal()<AccountInfoInteger(ACCOUNT_LIMIT_ORDERS) || AccountInfoInteger(ACCOUNT_LIMIT_ORDERS)==0)
     {
      if(LOTS<SymbolInfoDouble(NULL,SYMBOL_VOLUME_MIN))
         LOTS=SymbolInfoDouble(NULL,SYMBOL_VOLUME_MIN);
      if(LOTS>SymbolInfoDouble(NULL,SYMBOL_VOLUME_MAX))
         LOTS=SymbolInfoDouble(NULL,SYMBOL_VOLUME_MAX);
      LOTS=MathMin(MathMax(MathRound(LOTS/SymbolInfoDouble(NULL,SYMBOL_VOLUME_STEP))*SymbolInfoDouble(NULL,SYMBOL_VOLUME_STEP),SymbolInfoDouble(NULL,SYMBOL_VOLUME_MIN)),SymbolInfoDouble(NULL,SYMBOL_VOLUME_MAX));

      if((AccountFreeMargin()<=MarketInfo(NULL,MODE_MARGINREQUIRED)*LOTS+SymbolInfoDouble(NULL,SYMBOL_VOLUME_MIN)) ||
         (AccountFreeMarginCheck(NULL,CMD,LOTS+SymbolInfoDouble(NULL,SYMBOL_VOLUME_MIN))<=MarketInfo(NULL,MODE_MARGINREQUIRED)*MarketInfo(NULL,MODE_MINLOT)))
        {
         Print(__FUNCTION__," ",__LINE__," ERROR: ",GetLastError());
         Sleep(1000);
         RefreshRates();
         return;
        }

      if((ENUM_SYMBOL_TRADE_MODE)SymbolInfoInteger(NULL,SYMBOL_TRADE_MODE)==SYMBOL_TRADE_MODE_FULL)
        {
         if(!OrderSend(NULL,CMD,LOTS,SymbolInfoDouble(NULL,CMD==OP_SELL?SYMBOL_BID:SYMBOL_ASK),0,0,0,COMMENT,MAGIC,0,clrNONE))
           {
            Print(__FUNCTION__," ",__LINE__," ERROR: ",GetLastError());
            Sleep(1000);
            RefreshRates();
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void HLINE()
  {
   if(ZEROLEVEL>0)
      OBJECT_HLINE("HLINE_ZEROLEVEL",ZEROLEVEL,"ZEROLEVEL",clrGold,0);
   if(ZEROLEVEL==0 && ObjectFind("HLINE_ZEROLEVEL")>=0)
      ObjectDelete("HLINE_ZEROLEVEL");

   if(OVERLAP_CLOSE>0)
      OBJECT_HLINE("HLINE_OVERLAP_CLOSE",OVERLAP_CLOSE,"OVERLAP CLOSE",clrRed);
   if(OVERLAP_CLOSE==0 && ObjectFind("HLINE_OVERLAP_CLOSE")>=0)
      ObjectDelete("HLINE_OVERLAP_CLOSE");

   if(GRID_BUY_PRICE>0)
      OBJECT_HLINE("HLINE_GRID_BUY_PRICE",GRID_BUY_PRICE,"GRID BUY",clrGreen);
   if(GRID_BUY_PRICE==0 && ObjectFind("HLINE_GRID_BUY_PRICE")>=0)
      ObjectDelete("HLINE_GRID_BUY_PRICE");

   if(GRID_SELL_PRICE>0)
      OBJECT_HLINE("HLINE_GRID_SELL_PRICE",GRID_SELL_PRICE,"GRID SELL",clrGreen);
   if(GRID_SELL_PRICE==0 && ObjectFind("HLINE_GRID_SELL_PRICE")>=0)
      ObjectDelete("HLINE_GRID_SELL_PRICE");

   if(TRAILINGSTOP_BUY>0)
      OBJECT_HLINE("HLINE_TRAILINGSTOP_BUY",TRAILINGSTOP_BUY,"TRAILINGSTOP_BUY",clrYellow);
   if(TRAILINGSTOP_BUY==0 && ObjectFind(0,"HLINE_TRAILINGSTOP_BUY")>=0)
      ObjectDelete(0,"HLINE_TRAILINGSTOP_BUY");

   if(TRAILINGSTEP_BUY>0)
      OBJECT_HLINE("HLINE_TRAILINGSTEP_BUY",TRAILINGSTEP_BUY,"TRAILINGSTEP_BUY",clrRed);
   if(TRAILINGSTEP_BUY==0 && ObjectFind(0,"HLINE_TRAILINGSTEP_BUY")>=0)
      ObjectDelete(0,"HLINE_TRAILINGSTEP_BUY");

   if(TRAILINGSTOP_SELL>0)
      OBJECT_HLINE("HLINE_TRAILINGSTOP_SELL",TRAILINGSTOP_SELL,"TRAILINGSTOP_SELL",clrYellow);
   if(TRAILINGSTOP_SELL==0 && ObjectFind(0,"HLINE_TRAILINGSTOP_SELL")>=0)
      ObjectDelete(0,"HLINE_TRAILINGSTOP_SELL");

   if(TRAILINGSTEP_SELL>0)
      OBJECT_HLINE("HLINE_TRAILINGSTEP_SELL",TRAILINGSTEP_SELL,"TRAILINGSTEP_SELL",clrRed);
   if(TRAILINGSTEP_SELL==0 && ObjectFind(0,"HLINE_TRAILINGSTEP_SELL")>=0)
      ObjectDelete(0,"HLINE_TRAILINGSTEP_SELL");

   if(BREAKEVENSTOP_BUY>0)
      OBJECT_HLINE("HLINE_BREAKEVENSTOP_BUY",BREAKEVENSTOP_BUY,"BREAKEVENSTOP BUY",clrYellow);
   if(BREAKEVENSTOP_BUY==0 && ObjectFind(0,"HLINE_BREAKEVENSTOP_BUY")>=0)
      ObjectDelete(0,"HLINE_BREAKEVENSTOP_BUY");

   if(BREAKEVENSTEP_BUY>0)
      OBJECT_HLINE("HLINE_BREAKEVENSTEP_BUY",BREAKEVENSTEP_BUY,"BREAKEVENSTEP BUY",clrRed);
   if(BREAKEVENSTEP_BUY==0 && ObjectFind(0,"HLINE_BREAKEVENSTEP_BUY")>=0)
      ObjectDelete(0,"HLINE_BREAKEVENSTEP_BUY");

   if(BREAKEVENSTOP_SELL>0)
      OBJECT_HLINE("HLINE_BREAKEVENSTOP_SELL",BREAKEVENSTOP_SELL,"BREAKEVENSTOP SELL",clrYellow);
   if(BREAKEVENSTOP_SELL==0 && ObjectFind(0,"HLINE_BREAKEVENSTOP_SELL")>=0)
      ObjectDelete(0,"HLINE_BREAKEVENSTOP_SELL");

   if(BREAKEVENSTEP_SELL>0)
      OBJECT_HLINE("HLINE_BREAKEVENSTEP_SELL",BREAKEVENSTEP_SELL,"BREAKEVENSTEP SELL",clrRed);
   if(BREAKEVENSTEP_SELL==0 && ObjectFind(0,"HLINE_BREAKEVENSTEP_SELL")>=0)
      ObjectDelete(0,"HLINE_BREAKEVENSTEP_SELL");

   if(STOPLOSS_BUY>0)
      OBJECT_HLINE("HLINE_STOPLOSS_BUY",STOPLOSS_BUY,"STOPLOSS_BUY",clrRed);
   if(STOPLOSS_BUY==0 && ObjectFind("HLINE_STOPLOSS_BUY")>=0)
      ObjectDelete("HLINE_STOPLOSS_BUY");

   if(STOPLOSS_SELL>0)
      OBJECT_HLINE("HLINE_STOPLOSS_SELL",STOPLOSS_SELL,"STOPLOSS_SELL",clrRed);
   if(STOPLOSS_SELL==0 && ObjectFind("HLINE_STOPLOSS_SELL")>=0)
      ObjectDelete("HLINE_STOPLOSS_SELL");

   if(TAKEPROFIT_BUY>0)
      OBJECT_HLINE("HLINE_TAKEPROFIT_BUY",TAKEPROFIT_BUY,"TAKEPROFIT_BUY",clrRed);
   if(TAKEPROFIT_BUY==0 && ObjectFind("HLINE_TAKEPROFIT_BUY")>=0)
      ObjectDelete("HLINE_TAKEPROFIT_BUY");

   if(TAKEPROFIT_SELL>0)
      OBJECT_HLINE("HLINE_TAKEPROFIT_SELL",TAKEPROFIT_SELL,"TAKEPROFIT_SELL",clrRed);
   if(TAKEPROFIT_SELL==0 && ObjectFind("HLINE_TAKEPROFIT_SELL")>=0)
      ObjectDelete("HLINE_TAKEPROFIT_SELL");
   return;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void WIN_POSITION(int &TYPE,int &TICKET,double &PROFIT,double &LOT,string SYMBOL="", int ORDERTYPE=-1, int MAGIC=-1)
  {
   double DISTANCE_=0.0;

   TYPE=TICKET=0;
   PROFIT=LOT=0;

   if(SYMBOL=="")
      SYMBOL=_Symbol;

   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol()==SYMBOL && (OrderType()==ORDERTYPE || ORDERTYPE==-1))
           {
            if(OrderType()==OP_BUY || OrderType()==OP_SELL)
              {
               if(OrderMagicNumber()==MAGIC || MAGIC==-1)
                 {
                  if(OrderType()==OP_BUY)
                    {
                     if(OrderOpenPrice()<MarketInfo(SYMBOL, MODE_BID) && DISTANCE_<MathAbs(OrderOpenPrice()-MarketInfo(SYMBOL, MODE_BID)))
                       {
                        DISTANCE_=MathAbs(OrderOpenPrice()-MarketInfo(SYMBOL, MODE_BID));

                        TYPE=OrderType();
                        TICKET=OrderTicket();
                        PROFIT=NormalizeDouble(OrderProfit()+OrderSwap()+OrderCommission(),2);
                        LOT=OrderLots();
                       }
                    }

                  if(OrderType()==OP_SELL)
                    {
                     if(OrderOpenPrice()>MarketInfo(SYMBOL, MODE_ASK) && DISTANCE_<MathAbs(OrderOpenPrice()-MarketInfo(SYMBOL, MODE_ASK)))
                       {
                        DISTANCE_=MathAbs(OrderOpenPrice()-MarketInfo(SYMBOL, MODE_ASK));

                        TYPE=OrderType();
                        TICKET=OrderTicket();
                        PROFIT=NormalizeDouble(OrderProfit()+OrderSwap()+OrderCommission(),2);
                        LOT=OrderLots();
                       }

                    }
                 }
              }
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void LOSS_POSITION(int &TYPE,int &TICKET,double &PROFIT,double &LOT,string SYMBOL="", int ORDERTYPE=-1, int MAGIC=-1)
  {
   double DISTANCE_=0.0;

   TYPE=TICKET=0;
   PROFIT=LOT=0;

   if(SYMBOL=="")
      SYMBOL=_Symbol;

   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol()==SYMBOL && (OrderType()==ORDERTYPE || ORDERTYPE==-1))
           {
            if(OrderType()==OP_BUY || OrderType()==OP_SELL)
              {
               if(OrderMagicNumber()==MAGIC || MAGIC==-1)
                 {
                  if(OrderType()==OP_BUY)
                    {
                     if(OrderOpenPrice()>MarketInfo(SYMBOL, MODE_BID) && DISTANCE_<MathAbs(OrderOpenPrice()-MarketInfo(SYMBOL, MODE_BID)))
                       {
                        DISTANCE_=MathAbs(OrderOpenPrice()-MarketInfo(SYMBOL, MODE_BID));

                        TYPE=OrderType();
                        TICKET=OrderTicket();
                        PROFIT=NormalizeDouble(OrderProfit()+OrderSwap()+OrderCommission(),2);
                        LOT=OrderLots();
                       }

                    }

                  if(OrderType()==OP_SELL)
                    {
                     if(OrderOpenPrice()<MarketInfo(SYMBOL, MODE_ASK) && DISTANCE_<MathAbs(OrderOpenPrice()-MarketInfo(SYMBOL, MODE_ASK)))
                       {
                        DISTANCE_=MathAbs(OrderOpenPrice()-MarketInfo(SYMBOL, MODE_ASK));

                        TYPE=OrderType();
                        TICKET=OrderTicket();
                        PROFIT=NormalizeDouble(OrderProfit()+OrderSwap()+OrderCommission(),2);
                        LOT=OrderLots();
                       }
                    }
                 }
              }
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OPENED_VALUES(double &PRICE_BUY,double &LOTS_BUY,double &PRICE_SELL,double &LOTS_SELL)
  {
   PRICE_BUY=LOTS_BUY=PRICE_SELL=LOTS_SELL=0;
   int OLD_TICKET=0,TICKET=0;

   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==_Symbol && (OrderMagicNumber()==MAGIC_NUMBER || MAGIC_NUMBER==-1))
           {
            OLD_TICKET=OrderTicket();
            if(OLD_TICKET>TICKET)
              {
               if(OrderType()==OP_BUY)
                 {
                  PRICE_BUY=OrderOpenPrice();
                  LOTS_BUY=OrderLots();
                 }
               if(OrderType()==OP_SELL)
                 {
                  PRICE_SELL=OrderOpenPrice();
                  LOTS_SELL=OrderLots();
                 }
               TICKET=OLD_TICKET;
              }
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void VIRTUAL_TRAILINGSTOP(int CMD=-1)
  {
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==_Symbol && (OrderMagicNumber()==MAGIC_NUMBER || MAGIC_NUMBER==-1))
           {
            if(CMD==OP_BUY && OrderType()==OP_BUY)
              {
               if(TICKVALUE*BUY_LOTS<=0)
                  return;

               ZEROLEVEL_BUY=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID)-(BUY_PROFIT/(TICKVALUE*BUY_LOTS)*_Point),_Digits);
               TRAILINGSTOP_BUY=NormalizeDouble((TRAILINGSTEP_BUY>0?TRAILINGSTEP_BUY:ZEROLEVEL_BUY)+(TRAILING_STOP+TRAILING_STEP)*_Point,_Digits);

               if(SymbolInfoDouble(_Symbol,SYMBOL_BID)>=NormalizeDouble(ZEROLEVEL_BUY+(TRAILING_STOP+TRAILING_STEP)*_Point,_Digits) &&
                  (NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID)-(TRAILING_STOP+TRAILING_STEP)*_Point,_Digits)>=TRAILINGSTEP_BUY || TRAILINGSTEP_BUY==0))
                  TRAILINGSTEP_BUY=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID)-TRAILING_STEP*_Point,_Digits);

               if(TRAILINGSTEP_BUY>0 && SymbolInfoDouble(_Symbol,SYMBOL_BID)<=TRAILINGSTEP_BUY)
                  CLOSE_BUY=true;
              }

            if(CMD==OP_SELL && OrderType()==OP_SELL)
              {
               if(TICKVALUE*SELL_LOTS<=0)
                  return;

               ZEROLEVEL_SELL=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK)+(SELL_PROFIT/(TICKVALUE*SELL_LOTS)*_Point),_Digits);
               TRAILINGSTOP_SELL=NormalizeDouble((TRAILINGSTEP_SELL>0?TRAILINGSTEP_SELL:ZEROLEVEL_SELL)-(TRAILING_STOP+TRAILING_STEP)*_Point,_Digits);

               if(SymbolInfoDouble(_Symbol,SYMBOL_ASK)<=NormalizeDouble(ZEROLEVEL_SELL-(TRAILING_STOP+TRAILING_STEP)*_Point,_Digits) &&
                  (NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK)+(TRAILING_STOP+TRAILING_STEP)*_Point,_Digits)<=TRAILINGSTEP_SELL || TRAILINGSTEP_SELL==0))
                  TRAILINGSTEP_SELL=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK)+TRAILING_STEP*_Point,_Digits);

               if(TRAILINGSTEP_SELL>0 && SymbolInfoDouble(_Symbol,SYMBOL_ASK)>=TRAILINGSTEP_SELL)
                  CLOSE_SELL=true;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void VIRTUAL_BREAKEVEN(int CMD=-1)
  {
   for(int COUNT=0; COUNT<=OrdersTotal(); COUNT++)
     {
      if(OrderSelect(COUNT,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==_Symbol && (OrderMagicNumber()==MAGIC_NUMBER || MAGIC_NUMBER==-1))
           {
            if(CMD==OP_BUY && OrderType()==OP_BUY)
              {
               if(TICKVALUE*BUY_LOTS<=0)
                  return;

               ZEROLEVEL_BUY=NormalizeDouble(Bid-(BUY_PROFIT/(TICKVALUE*BUY_LOTS)*_Point),_Digits);

               BREAKEVENSTOP_BUY = NormalizeDouble(ZEROLEVEL_BUY+(BREAKEVEN_STOP+BREAKEVEN_STEP)*_Point,_Digits);

               if(Bid>=BREAKEVENSTOP_BUY)
                  BREAKEVENSTEP_BUY = NormalizeDouble(ZEROLEVEL_BUY+BREAKEVEN_STEP*_Point,_Digits);

               if(BREAKEVENSTEP_BUY>0 && SymbolInfoDouble(_Symbol,SYMBOL_BID)<=BREAKEVENSTEP_BUY)
                  CLOSE_BUY=true;
              }

            if(CMD==OP_SELL && OrderType()==OP_SELL)
              {
               if(TICKVALUE*SELL_LOTS<=0)
                  return;

               ZEROLEVEL_SELL=NormalizeDouble(Ask+(SELL_PROFIT/(TICKVALUE*SELL_LOTS)*_Point),_Digits);

               BREAKEVENSTOP_SELL = NormalizeDouble(ZEROLEVEL_SELL-(BREAKEVEN_STOP+BREAKEVEN_STEP)*_Point,_Digits);

               if(Ask<=BREAKEVENSTOP_SELL)
                  BREAKEVENSTEP_SELL = NormalizeDouble(ZEROLEVEL_SELL-BREAKEVEN_STEP*_Point,_Digits);

               if(BREAKEVENSTEP_SELL>0 && SymbolInfoDouble(_Symbol,SYMBOL_ASK)>=BREAKEVENSTEP_SELL)
                  CLOSE_SELL=true;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void VIRTUAL_STOPLOSS_TAKEPROFIT(int CMD=-1)
  {
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==_Symbol && (OrderMagicNumber()==MAGIC_NUMBER || MAGIC_NUMBER==-1))
           {
            if(CMD==OP_BUY && OrderType()==OP_BUY)
              {
               if(TICKVALUE*BUY_LOTS<=0)
                  return;
               ZEROLEVEL_BUY=NormalizeDouble(Bid-(BUY_PROFIT/(TICKVALUE*BUY_LOTS)*_Point),_Digits);

               if(STOPLOSS>0)
                  STOPLOSS_BUY=NormalizeDouble(ZEROLEVEL_BUY-STOPLOSS*_Point,_Digits);
               if(TAKEPROFIT>0)
                  TAKEPROFIT_BUY=NormalizeDouble(ZEROLEVEL_BUY+TAKEPROFIT*_Point,_Digits);

               if(TAKEPROFIT>0 && TAKEPROFIT_BUY!=0 && Bid>=TAKEPROFIT_BUY)
                  CLOSE_BUY=true;

               if(STOPLOSS>0 && STOPLOSS_BUY!=0 && Bid<=STOPLOSS_BUY)
                  CLOSE_BUY=true;
              }

            if(CMD==OP_SELL && OrderType()==OP_SELL)
              {
               if(TICKVALUE*SELL_LOTS<=0)
                  return;
               ZEROLEVEL_SELL=NormalizeDouble(Ask+(SELL_PROFIT/(TICKVALUE*SELL_LOTS)*_Point),_Digits);

               if(STOPLOSS>0)
                  STOPLOSS_SELL=NormalizeDouble(ZEROLEVEL_SELL+STOPLOSS*_Point,_Digits);
               if(TAKEPROFIT>0)
                  TAKEPROFIT_SELL=NormalizeDouble(ZEROLEVEL_SELL-TAKEPROFIT*_Point,_Digits);

               if(TAKEPROFIT>0 && TAKEPROFIT_SELL!=0 && Ask<=TAKEPROFIT_SELL)
                  CLOSE_SELL=true;

               if(STOPLOSS>0 && STOPLOSS_SELL!=0 && Ask>=STOPLOSS_SELL)
                  CLOSE_SELL=true;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int HOUR_MQL4()
  {
   MqlDateTime TIME_MQL4;
   TimeCurrent(TIME_MQL4);
   return(TIME_MQL4.hour);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int MINUTE_MQL4()
  {
   MqlDateTime TIME_MQL4;
   TimeCurrent(TIME_MQL4);
   return(TIME_MQL4.min);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool WORKING_HOURS(string STARTTIME="00:00",string ENDTIME="00:00")
  {
   if(STARTTIME=="00:00"&&ENDTIME=="00:00")
      return(true);
   double DATETIME=HOUR_MQL4()+MINUTE_MQL4()/60.0;
   double TEMP_START_TIME=(int)StringSubstr(STARTTIME,0,2)+(int)StringSubstr(STARTTIME,3,2)/60.0;
   double TEMP_END_TIME=(int)StringSubstr(ENDTIME,0,2)+(int)StringSubstr(ENDTIME,3,2)/60.0;
   double START_HOURS=MathMin(TEMP_END_TIME,TEMP_START_TIME);
   double END_HOURS=MathMax(TEMP_END_TIME,TEMP_START_TIME);
   bool ALLOWED=(DATETIME>=START_HOURS && DATETIME<END_HOURS);
   if(TEMP_START_TIME > TEMP_END_TIME)
      return (!ALLOWED);
   else
      return (ALLOWED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void INFO()
  {
   int GROWTH_PIPS=0;
   double TODAY=0,WEEK=0,MONTH=0,GROWTH=0;
   double VOLUME_TODAY=0,VOLUME_WEEK=0,VOLUME_MONTH=0,GROWTH_VOLUME=0;
   PROFIT_OTHER_SYMBOLS(TODAY,WEEK,MONTH,GROWTH,VOLUME_TODAY,VOLUME_WEEK,VOLUME_MONTH,GROWTH_VOLUME,GROWTH_PIPS);

   int SYMB_TODAY_PIPS=0,SYMB_WEEK_PIPS=0,SYMB_MONTH_PIPS=0,SYMB_GROWTH_PIPS=0;
   double SYMB_TODAY=0,SYMB_WEEK=0,SYMB_MONTH=0,SYMB_GROWTH=0;
   double SYMB_VOLUME_TODAY=0,SYMB_VOLUME_WEEK=0,SYMB_VOLUME_MONTH=0,SYMB_GROWTH_VOLUME=0;
   PROFIT_SYMBOL(SYMB_TODAY,SYMB_WEEK,SYMB_MONTH,SYMB_GROWTH,SYMB_VOLUME_TODAY,SYMB_VOLUME_WEEK,SYMB_VOLUME_MONTH,SYMB_GROWTH_VOLUME,SYMB_TODAY_PIPS,SYMB_WEEK_PIPS,SYMB_MONTH_PIPS,SYMB_GROWTH_PIPS);

   double TODAY_PCT  = NormalizeDouble(SYMB_TODAY/(AccountInfoDouble(ACCOUNT_BALANCE)-TODAY+GET_WITHDRAW(PERIOD_D1,0,-1)-GET_DEPOSIT(PERIOD_D1,0,-1)-SYMB_TODAY)*100,2);
   double WEEK_PCT   = NormalizeDouble(SYMB_WEEK/(AccountInfoDouble(ACCOUNT_BALANCE)-WEEK+GET_WITHDRAW(PERIOD_W1,0,-1)-GET_DEPOSIT(PERIOD_W1,0,-1)-SYMB_WEEK)*100,2);
   double MONTH_PCT  = NormalizeDouble(SYMB_MONTH/(AccountInfoDouble(ACCOUNT_BALANCE)-MONTH+GET_WITHDRAW(PERIOD_MN1,0,-1)-GET_DEPOSIT(PERIOD_MN1,0,-1)-SYMB_MONTH)*100,2);
   double GROWTH_PCT = NormalizeDouble(SYMB_GROWTH/(AccountInfoDouble(ACCOUNT_BALANCE)-GROWTH+GET_WITHDRAW(PERIOD_MN1,iBars(NULL,PERIOD_W1),-1)-GET_DEPOSIT(PERIOD_MN1,iBars(NULL,PERIOD_MN1),-1)-SYMB_GROWTH)*100,2);
   double PROFIT_PCT = NormalizeDouble(GROWTH/(AccountInfoDouble(ACCOUNT_BALANCE)+GET_WITHDRAW(PERIOD_MN1,iBars(NULL,PERIOD_W1),-1)-GET_DEPOSIT(PERIOD_MN1,iBars(NULL,PERIOD_MN1),-1)-GROWTH)*100,2);

   string TEXT_TODAY  = "\nTODAY: "+DoubleToString(TODAY_PCT,2)+" %, "+DoubleToString(SYMB_TODAY,2)+" "+(string)AccountInfoString(ACCOUNT_CURRENCY)+", "+(string)SYMB_TODAY_PIPS+" PIPS";
   string TEXT_WEEK   = "\nWEEK: "+DoubleToString(WEEK_PCT,2)+" %, "+DoubleToString(SYMB_WEEK,2)+" "+(string)AccountInfoString(ACCOUNT_CURRENCY)+", "+(string)SYMB_WEEK_PIPS+" PIPS";
   string TEXT_MONTH  = "\nMONTH: "+DoubleToString(MONTH_PCT,2)+" %, "+DoubleToString(SYMB_MONTH,2)+" "+(string)AccountInfoString(ACCOUNT_CURRENCY)+", "+(string)SYMB_MONTH_PIPS+" PIPS";
   string TEXT_GROWTH = "\nGROWTH: "+DoubleToString(GROWTH_PCT,2)+" %, "+DoubleToString(SYMB_GROWTH,2)+" "+(string)AccountInfoString(ACCOUNT_CURRENCY)+", "+(string)SYMB_GROWTH_PIPS+" PIPS";

   OBJECT_LABEL(0,"LABEL_TODAY",0,10,20,CORNER_RIGHT_UPPER,TEXT_TODAY,"Arial",10,clrGold,0,ANCHOR_RIGHT_UPPER,false,false,true,0);
   OBJECT_LABEL(0,"LABEL_WEEK",0,10,40,CORNER_RIGHT_UPPER,TEXT_WEEK,"Arial",10,clrGold,0,ANCHOR_RIGHT_UPPER,false,false,true,0);
   OBJECT_LABEL(0,"LABEL_MONTH",0,10,60,CORNER_RIGHT_UPPER,TEXT_MONTH,"Arial",10,clrGold,0,ANCHOR_RIGHT_UPPER,false,false,true,0);
   OBJECT_LABEL(0,"LABEL_GROWTH",0,10,80,CORNER_RIGHT_UPPER,TEXT_GROWTH,"Arial",10,clrGold,0,ANCHOR_RIGHT_UPPER,false,false,true,0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DEINIT_LABEL()
  {
   for(int i=ObjectsTotal(0,-1,-1)-1; i>=0; i--)
     {
      if(StringFind(ObjectName(0,i,-1,-1),"LABEL",0)>=0)
         ObjectDelete(0,ObjectName(0,i,-1,-1));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool OBJECT_LABEL(const long              CHART_ID=0,
                  const string            NAME        = "",
                  const int               SUB_WINDOW  = 0,
                  const int               X           = 0,
                  const int               Y           = 0,
                  const ENUM_BASE_CORNER  CORNER      = CORNER_LEFT_UPPER,
                  const string            TEXT        = "",
                  const string            FONT        = "",
                  const int               FONT_SIZE   = 10,
                  const color             CLR         = clrRed,
                  const double            ANGLE       = 0.0,
                  const ENUM_ANCHOR_POINT ANCHOR      = ANCHOR_LEFT_UPPER,
                  const bool              BACK        = false,
                  const bool              SELECTION   = false,
                  const bool              HIDDEN      = true,
                  const long              ZORDER      = 0,
                  string                  TOOLTIP     = "\n")
  {
   ResetLastError();
   if(ObjectFind(0,NAME)<0)
     {
      ObjectCreate(CHART_ID,NAME,OBJ_LABEL,SUB_WINDOW,0,0);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_XDISTANCE,X);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_YDISTANCE,Y);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_CORNER,CORNER);
      ObjectSetString(CHART_ID,NAME,OBJPROP_TEXT,TEXT);
      ObjectSetString(CHART_ID,NAME,OBJPROP_FONT,FONT);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_FONTSIZE,FONT_SIZE);
      ObjectSetDouble(CHART_ID,NAME,OBJPROP_ANGLE,ANGLE);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_ANCHOR,ANCHOR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_COLOR,CLR);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_BACK,BACK);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_SELECTABLE,SELECTION);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_SELECTED,SELECTION);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_HIDDEN,HIDDEN);
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_ZORDER,ZORDER);
      ObjectSetString(CHART_ID,NAME,OBJPROP_TOOLTIP,TOOLTIP);
     }
   else
     {
      ObjectSetInteger(CHART_ID,NAME,OBJPROP_COLOR,CLR);
      ObjectSetString(CHART_ID,NAME,OBJPROP_TEXT,TEXT);
     }
   return(true);
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GET_WITHDRAW(ENUM_TIMEFRAMES _TIMEFRAME=PERIOD_CURRENT,int _STARTBAR=0,int _ENDBAR=0)
  {
   double _WITHDRAW=0;
   for(int I=OrdersHistoryTotal()-1; I>=0; I--)
     {
      if(OrderSelect(I,SELECT_BY_POS,MODE_HISTORY))
        {
         if(OrderType()>5 && OrderProfit()+OrderCommission()+OrderSwap()<0)
           {
            if(OrderCloseTime()>=iTime(NULL,_TIMEFRAME,_STARTBAR) &&
               (OrderCloseTime()<iTime(NULL,_TIMEFRAME,_ENDBAR) || _ENDBAR==-1))
               _WITHDRAW-=OrderProfit()+OrderCommission()+OrderSwap();
           }
        }
     }
   return(_WITHDRAW);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GET_DEPOSIT(ENUM_TIMEFRAMES _TIMEFRAME=PERIOD_CURRENT,int _STARTBAR=0,int _ENDBAR=0)
  {
   double _DEPOSIT=0;
   for(int I=OrdersHistoryTotal()-1; I>=0; I--)
     {
      if(OrderSelect(I,SELECT_BY_POS,MODE_HISTORY))
        {
         if(I==0 && OrderType()>5)
            break;
         if(OrderType()>5 && OrderProfit()+OrderCommission()+OrderSwap()>=0)
           {
            if(OrderCloseTime()>=iTime(NULL,_TIMEFRAME,_STARTBAR) &&
               (OrderCloseTime()<iTime(NULL,_TIMEFRAME,_ENDBAR) || _ENDBAR==-1))
               _DEPOSIT+=OrderProfit()+OrderCommission()+OrderSwap();
           }
        }
     }
   return(_DEPOSIT);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PROFIT_OTHER_SYMBOLS(double &_TODAY,
                          double &_WEEK,
                          double &_MONTH,
                          double &_GROWTH,
                          double &_VOLUME_TODAY,
                          double &_VOLUME_WEEK,
                          double &_VOLUME_MONTH,
                          double &_TOTAL_VOLUME,
                          int &_GROWTH_PIPS)
  {
   _GROWTH_PIPS=0;
   _TODAY=_WEEK=_MONTH=_GROWTH=0;
   _VOLUME_TODAY=_VOLUME_WEEK=_VOLUME_MONTH=_TOTAL_VOLUME=0;

   for(int I=0; I<OrdersHistoryTotal(); I++)
     {
      if(OrderSelect(I,SELECT_BY_POS,MODE_HISTORY))
        {
         if((OrderMagicNumber()==MAGIC_NUMBER || MAGIC_NUMBER==-1))
           {
            if(OrderType()<=1)
              {
               if(OrderCloseTime()>=iTime(NULL,PERIOD_D1,0))
                 {
                  _TODAY+=OrderProfit()+OrderSwap()+OrderCommission();
                  _VOLUME_TODAY+=OrderLots();

                 }
               if(OrderCloseTime()>=iTime(NULL,PERIOD_W1,0))
                 {
                  _WEEK+=OrderProfit()+OrderSwap()+OrderCommission();
                  _VOLUME_WEEK+=OrderLots();

                 }
               if(OrderCloseTime()>=iTime(NULL,PERIOD_MN1,0))
                 {
                  _MONTH+=OrderProfit()+OrderSwap()+OrderCommission();
                  _VOLUME_MONTH+=OrderLots();

                 }
               if(OrderCloseTime()>=iTime(NULL,PERIOD_MN1,iBars(NULL,PERIOD_MN1)))
                 {
                  _GROWTH+=OrderProfit()+OrderSwap()+OrderCommission();
                  _TOTAL_VOLUME+=OrderLots();
                  //---
                  if(OrderType() == OP_SELL)
                     _GROWTH_PIPS += (int)MathRound((OrderOpenPrice()-OrderClosePrice())/MarketInfo(OrderSymbol(),MODE_POINT));
                  if(OrderType() == OP_BUY)
                     _GROWTH_PIPS += (int)MathRound((OrderClosePrice()-OrderOpenPrice())/MarketInfo(OrderSymbol(),MODE_POINT));
                 }
              }
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PROFIT_SYMBOL(double &_TODAY,
                   double &_WEEK,
                   double &_MONTH,
                   double &_GROWTH,
                   double &_VOLUME_TODAY,
                   double &_VOLUME_WEEK,
                   double &_VOLUME_MONTH,
                   double &_TOTAL_VOLUME,
                   int &_TODAY_PIPS,
                   int &_WEEK_PIPS,
                   int &_MONTH_PIPS,
                   int &_GROWTH_PIPS)
  {
   _TODAY_PIPS=_WEEK_PIPS=_MONTH_PIPS=_GROWTH_PIPS=0;
   _TODAY=_WEEK=_MONTH=_GROWTH=0;
   _VOLUME_TODAY=_VOLUME_WEEK=_VOLUME_MONTH=_TOTAL_VOLUME=0;

   for(int I=0; I<OrdersHistoryTotal(); I++)
     {
      if(OrderSelect(I,SELECT_BY_POS,MODE_HISTORY))
        {
         if(OrderSymbol()==_Symbol && (OrderMagicNumber()==MAGIC_NUMBER || MAGIC_NUMBER==-1))
           {
            if(OrderType()<=1)
              {
               if(OrderCloseTime()>=iTime(NULL,PERIOD_D1,0))
                 {
                  _TODAY+=OrderProfit()+OrderSwap()+OrderCommission();
                  _VOLUME_TODAY+=OrderLots();
                  //---
                  if(OrderType() == OP_SELL)
                     _TODAY_PIPS += (int)MathRound((OrderOpenPrice()-OrderClosePrice())/_Point);
                  if(OrderType() == OP_BUY)
                     _TODAY_PIPS += (int)MathRound((OrderClosePrice()-OrderOpenPrice())/_Point);
                 }
               if(OrderCloseTime()>=iTime(NULL,PERIOD_W1,0))
                 {
                  _WEEK+=OrderProfit()+OrderSwap()+OrderCommission();
                  _VOLUME_WEEK+=OrderLots();
                  //---
                  if(OrderType() == OP_SELL)
                     _WEEK_PIPS += (int)MathRound((OrderOpenPrice()-OrderClosePrice())/_Point);
                  if(OrderType() == OP_BUY)
                     _WEEK_PIPS += (int)MathRound((OrderClosePrice()-OrderOpenPrice())/_Point);
                 }
               if(OrderCloseTime()>=iTime(NULL,PERIOD_MN1,0))
                 {
                  _MONTH+=OrderProfit()+OrderSwap()+OrderCommission();
                  _VOLUME_MONTH+=OrderLots();
                  //---
                  if(OrderType() == OP_SELL)
                     _MONTH_PIPS += (int)MathRound((OrderOpenPrice()-OrderClosePrice())/_Point);
                  if(OrderType() == OP_BUY)
                     _MONTH_PIPS += (int)MathRound((OrderClosePrice()-OrderOpenPrice())/_Point);
                 }
               if(OrderCloseTime()>=iTime(NULL,PERIOD_MN1,iBars(NULL,PERIOD_MN1)))
                 {
                  _GROWTH+=OrderProfit()+OrderSwap()+OrderCommission();
                  _TOTAL_VOLUME+=OrderLots();
                  //---
                  if(OrderType() == OP_SELL)
                     _GROWTH_PIPS += (int)MathRound((OrderOpenPrice()-OrderClosePrice())/_Point);
                  if(OrderType() == OP_BUY)
                     _GROWTH_PIPS += (int)MathRound((OrderClosePrice()-OrderOpenPrice())/_Point);
                 }
              }
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double LOT_CALCULATE(double RISK,double SL,double BALANCE,double BALANCE_LOT)
  {
   double LOT=0;
   if(RISK_PER_TRADE>0)
     {
      LOT = NormalizeDouble(((AccountInfoDouble(ACCOUNT_BALANCE)*RISK/100.0)/SL)/(TICKVALUE!=0?TICKVALUE:1),2);
      LOT = NormalizeDouble(LOT/MarketInfo(_Symbol,MODE_LOTSTEP),2) * MarketInfo(_Symbol,MODE_LOTSTEP);
     }
   else
      if(FROM_BALANCE>0)
         LOT=AccountBalance()/BALANCE*BALANCE_LOT;
      else
         if(RISK_PER_TRADE==0 && FROM_BALANCE==0)
            LOT=VOLUME;

   return(LOT);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int RSI(int BARS,int PERIOD=PERIOD_CURRENT)
  {
   if(BARS<=0)
      return(0);
   double VALUE=0;
   double U=0,D=0,RS=0;
   for(int i=0; i<BARS; i++)
     {
      if(iClose(NULL,PERIOD,i)>iClose(NULL,PERIOD,i+1))
         U+=iClose(NULL,PERIOD,i)-iClose(NULL,PERIOD,i+1);
      if(iClose(NULL,PERIOD,i)<iClose(NULL,PERIOD,i+1))
         D+=iClose(NULL,PERIOD,i+1)-iClose(NULL,PERIOD,i);
     }
   if(U==0||D==0)
      return(0);
   RS=U/D;
   VALUE=100-(100/(1+RS));
//VALUE=100*(U/(U+D));
   return((int)VALUE);
  }
//+------------------------------------------------------------------+
