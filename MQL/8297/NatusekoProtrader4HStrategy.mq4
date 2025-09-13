//+------------------------------------------------------------------+
//|                                  NatusekoProtrader4HStrategy.mq4 |
//|                                                     FORTRADER.RU |
//|                                              http://FORTRADER.RU |
//+------------------------------------------------------------------+
#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"
extern string x="Настройки ценовых EMA:";
extern  int perema1=13;
extern  int perema2=21;
extern  int persma3=55;

extern string x1="Настройки MACD:";
extern int lowema4=5;
extern int fastema4=200;

extern string x2="Настройки MACD Bolinger:";
extern int bbper=20;
extern int bbotcl=1;

extern string x3="Настройки MACD SMA:";
extern int smaper=3;

extern string x4="Настройки RSI:";
extern int rsiper=21;
extern int rsiur=50;

extern string x5="Настройки силы движения:";
extern int sila=100;

extern string x6="Настройки ParabolicSar:";
extern double step=0.02;
extern double maximum=0.2;

extern string x7="Настройки выбора StopLoss:";
extern bool StopLossParabolic=false;
extern bool StopLossEMA=true;
extern int otstup=0;

extern string x8="Настройки выбора TakeProfit:";
extern bool TakeProfitParabolic=true;
extern bool TakeProfitRSI=true;
extern int rsitpurbuy=65;
extern int rsitpursell=35;


int maxdrow=24250;
double macd[1000];
int start()
  {
  
        NatusPro4HStrClassicPattern();
        NatusPro4HStrPosManager();

   return(0);
  }
int buy,sell,i,flagtest,wait,waits;int nummodb,nummods;
int NatusPro4HStrClassicPattern()
{  double sl;
     buy=0;sell=0;
     for(int  i=0;i<OrdersTotal();i++)
         {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
           if(OrderType()==OP_BUY ){buy=1;}
           if(OrderType()==OP_SELL ){sell=1;}
         }  
         
 double ema13 =iMA(NULL,0,perema1,0,MODE_EMA,PRICE_CLOSE,1);
 double ema21 =iMA(NULL,0,perema2,0,MODE_EMA,PRICE_CLOSE,1);
 double sma55 =iMA(NULL,0,persma3,0,MODE_EMA,PRICE_CLOSE,1);
 
 //double looker=iCustom(NULL, 0, "NatusekoProtrader4HStrategy",1,1);
 
        ArraySetAsSeries(macd,true);
        for( i=10; i>=0; i--)          
        {
        macd[i]=iMACD(NULL,0,lowema4,fastema4,1,PRICE_CLOSE,MODE_MAIN,i);
        }
        
        if(i<=1)
        {     
             double bbmain=iBandsOnArray(macd,0,bbper,bbotcl,0,MODE_MAIN,1);
             double bbup=iBandsOnArray(macd,0,bbper,bbotcl,0,MODE_UPPER,1);
             double bblow=iBandsOnArray(macd,0,bbper,bbotcl,0,MODE_LOWER,1);
             double macdsma=iMAOnArray(macd,0,smaper,0,MODE_SMA,1);
         }
         
         double rsi=iRSI(NULL,0,rsiper,PRICE_CLOSE,1);
         double pb=iSAR(NULL,0,step,maximum,1);
         
         double high=MathAbs(High[1]-Close[1]);  
         double telo=MathAbs(Close[1]-Open[1]); 
         double low=MathAbs(Open[1]-Low[1]); 
         
         
  if(ema13>ema21 && ema13>sma55 && rsi>rsiur && rsi<rsitpurbuy &&macd[1]>macdsma && macd[1]>bbmain && macdsma>bbmain && telo>0 && telo>low && telo>high && pb<Close[1]&& buy==0)
  {
   if((Close[1]-sma55)<sila*Point)
   {if(StopLossParabolic==true){sl=pb-otstup*Point;}if(StopLossEMA==true){sl=sma55-otstup*Point;}
   OrderSend(Symbol(),OP_BUY,0.1,Ask,3,NormalizeDouble(sl,4),0,"FORTRADER.RU",0,0,Red);
   nummodb=0;
   }
   else{wait=1;}
  }
  
  if(Low[1]<=ema13 && rsi<rsitpurbuy && pb<Close[1] && wait==1&& buy==0)
      {if(StopLossParabolic==true){sl=pb-otstup*Point;}if(StopLossEMA==true){sl=sma55-otstup*Point;}
         OrderSend(Symbol(),OP_BUY,0.1,Ask,3,NormalizeDouble(sl,4),0,"FORTRADER.RU",0,0,Red);
         nummodb=0;
         wait=0;
      }
  
  
  /******************************************************************************************************************************************/
  if(ema13<ema21 && ema13<sma55 && rsi<rsiur && rsi>rsitpursell &&macd[1]<macdsma && macd[1]<bbmain && macdsma<bbmain && telo>0 && telo>low && telo>high && pb>Close[1]&& sell==0)
  {
   if((sma55-Close[1])<sila*Point)
   {if(StopLossParabolic==true){sl=pb+otstup*Point;}if(StopLossEMA==true){sl=sma55+otstup*Point;}
   OrderSend(Symbol(),OP_SELL,0.1,Bid,3,NormalizeDouble(sl,4),0,"FORTRADER.RU",0,0,Red);
   nummods=0;
   }
   else{waits=1;}
  }
  
  if(High[1]>=ema13 && rsi>rsitpursell && pb>Close[1] && waits==1&& sell==0)
      {if(StopLossParabolic==true){sl=pb+otstup*Point;}if(StopLossEMA==true){sl=sma55+otstup*Point;}
         OrderSend(Symbol(),OP_SELL,0.1,Bid,3,NormalizeDouble(sl,4),0,"FORTRADER.RU",0,0,Red);
         nummods=0;
         waits=0;
      }
  
 /******************************************************************************************************************************************/ 
}

int  NatusPro4HStrPosManager()
{double lt; 
double rsi=iRSI(NULL,0,rsiper,PRICE_CLOSE,1);
double pb=iSAR(NULL,0,step,maximum,1);
 
   for( i=1; i<=OrdersTotal(); i++)          
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
              if(OrderType()==OP_BUY && OrderProfit()>5 && rsi>rsitpurbuy && TakeProfitRSI==true && OrderSymbol()==Symbol() && nummodb==0)
              {  lt=NormalizeDouble(OrderLots()/2,2);if(lt<=0.01){lt=0.01;}
                 OrderClose(OrderTicket(),lt,Bid,3,Violet); 
                 nummodb++;
              }
         }
             
         if (OrderSelect(i-1,SELECT_BY_POS)==true) 
            {  
              if(OrderType()==OP_BUY && OrderProfit()>5 && pb>Close[1] && TakeProfitParabolic==true &&OrderSymbol()==Symbol() && nummodb==0)
              { lt=NormalizeDouble(OrderLots()/2,2);if(lt<=0.01){lt=0.01;}
                 OrderClose(OrderTicket(),lt,Bid,3,Violet); 
                 nummodb++;   
              }
             }
             
            if (OrderSelect(i-1,SELECT_BY_POS)==true) 
            {  
              if(OrderType()==OP_BUY && OrderProfit()>5 && rsi<rsiur  &&OrderSymbol()==Symbol())
              { 
                 OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); 
              }
             }
             
          
             
             /**********************************************************************************************************************************/
             
            if (OrderSelect(i-1,SELECT_BY_POS)==true) 
            { 
              
              if(OrderType()==OP_SELL && OrderProfit()>5 && rsi<rsitpursell && TakeProfitRSI==true  &&OrderSymbol()==Symbol() && nummods==0)
              {lt=NormalizeDouble(OrderLots()/2,2);if(lt<=0.01){lt=0.01;}
               OrderClose(OrderTicket(),lt,Ask,3,Violet); 
               nummods++; 
              }
            }
             
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
        { 
              if(OrderType()==OP_SELL && OrderProfit()>5 && pb<Close[1] && TakeProfitParabolic==true &&  OrderSymbol()==Symbol() && nummods==0)
              {  lt=NormalizeDouble(OrderLots()/2,2);if(lt<=0.01){lt=0.01;}
                 OrderClose(OrderTicket(),lt,Ask,3,Violet); 
                 nummods++;
                  
              }
        }
      
                 if (OrderSelect(i-1,SELECT_BY_POS)==true) 
            {  
              if(OrderType()==OP_SELL && OrderProfit()>5 && rsi>rsiur  &&OrderSymbol()==Symbol())
              { 
                 OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); 
              }
             }
 }
 

 
 
}