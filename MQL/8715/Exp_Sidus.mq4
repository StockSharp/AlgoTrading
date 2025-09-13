//+------------------------------------------------------------------+
//|                                                    Exp_Sidus.mq4 |
//|                                                     Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

extern double TP = 80;
extern double SL = 20;
extern double Lots = 0.1;
extern int shif =1;

 int period_MA1 =5;
 int period_MA2 =12;
 int ma_method =0;//0-4
 int applied_price = 0;//0-6
 int period_RSI = 21;
 int applied_RSI = 0;//0-6

datetime LastTime=0;

int start()
  {
//----
   int cnt, ticket, total;
   
   total=OrdersTotal();
   if(total<1) 
     {
      if(GetSignal()==1 && Time[shif]!= LastTime)
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-SL*Point,Ask+TP*Point,"",28081975,0,Green);
         if(ticket>0)LastTime = Time[shif];
         return(0); 
        }
      if(GetSignal()==-1 && Time[shif]!= LastTime)
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+SL*Point,Bid-TP*Point,"",28081975,0,Red);
         if(ticket>0)LastTime = Time[shif];
         return(0); 
        }
      return(0);
     }      
//----
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_BUY)
           {
            if(GetSignal()==-1)
                {
                 OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
                 return(0);
                }
           }
         else 
           {
            if(GetSignal()==1)
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); // close position
               return(0);
              }
           }
        }
     }   
//----
   return(0);
  }
//+------------------------------------------------------------------+

double GetSignal()
 { 
  double FastEMA=iMA(NULL,0,period_MA1,0,ma_method,applied_price,shif);
  double SlowEMA=iMA(NULL,0,period_MA2,0,ma_method,applied_price,shif);
  double PrevFastEMA=iMA(NULL,0,period_MA1,0,ma_method,applied_price,shif+1);
  double PrevSlowEMA=iMA(NULL,0,period_MA2,0,ma_method,applied_price,shif+1);  
  double rsi= iRSI(NULL,0,period_RSI,applied_RSI,shif);  
 
  int vSig=0;
  if(PrevFastEMA<=PrevSlowEMA && FastEMA>SlowEMA && rsi>50 )vSig = 1;
  else
  if(PrevFastEMA>=PrevSlowEMA && FastEMA<SlowEMA && rsi<50 )vSig =-1;
  return(vSig); 
 }