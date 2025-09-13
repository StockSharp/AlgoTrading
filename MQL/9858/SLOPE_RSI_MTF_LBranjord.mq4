//Slope + RSI - EUR USD 5m


extern double   ATR_Percent = 0.15,  //This value sets the ATR Used, The ATR is 15%
                ATR_Period = 21;
extern int  shift        = 1;
extern int MaxOrders = 5;
extern bool compounding   = true;
double myATR, myATR2;
double Lots=0.1;
double Slippage = 3;

extern int    SDL1_trigger   = 60;
extern int    SDL1_period    = 200;
extern int    SDL1_method    = 3;
extern int    SDL1_price     = 0;

int start()
  {
  if (Fun_New_Bar())Check();
   return(0);
  }
 
  void Check(){
  if (compounding)Lots=AccountBalance()/100000;

double SDL1 = iCustom(NULL,0,"Slope_Direction_Line_Alert",SDL1_trigger,SDL1_method,SDL1_price,0,shift);
double SDL11 = iCustom(NULL,0,"Slope_Direction_Line_Alert",SDL1_trigger,SDL1_method,SDL1_price,0,shift+1);
   double SDL2 = iCustom(PERIOD_H1,0,"Slope_Direction_Line_Alert",SDL1_period,SDL1_method,SDL1_price,0,shift);
   double SDL22 = iCustom(PERIOD_H1,0,"Slope_Direction_Line_Alert",SDL1_period,SDL1_method,SDL1_price,0,shift+1);
double SDL3 = iCustom(PERIOD_H4,0,"Slope_Direction_Line_Alert",SDL1_period,SDL1_method,SDL1_price,0,shift);
double SDL33 = iCustom(PERIOD_H4,0,"Slope_Direction_Line_Alert",SDL1_period,SDL1_method,SDL1_price,0,shift+1);
   double SDL4 = iCustom(PERIOD_D1,0,"Slope_Direction_Line_Alert",SDL1_period,SDL1_method,SDL1_price,0,shift);
   double SDL44 = iCustom(PERIOD_D1,0,"Slope_Direction_Line_Alert",SDL1_period,SDL1_method,SDL1_price,0,shift+1);
   
   
   double RSI4=iRSI(PERIOD_D1,0,14,PRICE_CLOSE,0);
   double RSI3=iRSI(PERIOD_H4,0,14,PRICE_CLOSE,0);
   double RSI2=iRSI(PERIOD_H1,0,14,PRICE_CLOSE,0);
   double RSI1=iRSI(NULL,0,14,PRICE_CLOSE,0);
   
   bool buy = false, sell = false;
   
   if(SDL1>SDL11 && SDL2>SDL22 && SDL3>SDL33 && SDL4>SDL44 && RSI1>50 && RSI1<90 && RSI2>50 && RSI2<90 && RSI3>50 && RSI3<90 && RSI4>50 && RSI4<90)buy = true; 
   if(SDL1<SDL11 && SDL2<SDL22 && SDL3<SDL33 && SDL4<SDL44 && RSI1<50 && RSI1>10 && RSI2<50 && RSI2>10 && RSI3<50 && RSI3>10 && RSI4<50 && RSI4>10)sell = true;
  
   if(OrdersTotal()<MaxOrders){
   myATR = iATR(NULL,PERIOD_H1,ATR_Period,1);
   myATR2 = iATR(NULL,PERIOD_D1,ATR_Period,1);
   if(buy)OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,Bid-myATR,Ask+myATR2);
   if(sell)OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,Ask+myATR,Bid-myATR2);
   }
  } 
  
//+------------------------------------------------------------------+
//  NEW BAR FUNCTION
//+------------------------------------------------------------------+
   // Identify new bars
   bool Fun_New_Bar()
      {
      static datetime New_Time = 0;
      bool New_Bar = false;
      if (New_Time!= Time[0])
         {
         New_Time = Time[0];
         New_Bar = true;
         }
      return(New_Bar);
      }


