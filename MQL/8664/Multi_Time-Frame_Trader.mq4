//+------------------------------------------------------------------+
//|                                      Multi Time-Frame Trader.mq4 |
//|                                       korostelev.andre@gmail.com |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright ""
#property link      "korostelev.andre@gmail.com"
extern bool trade=true;
extern int barstocount=50;
extern double lots=0.01;
extern int slippage=3;
extern int magicnumber=816;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
if(trade==true)
{
   //M1
   double M1_resistance=iCustom(NULL,PERIOD_M1,"!LinRegrBuf","true",barstocount,2,0);
   double M1_resistance_p=iCustom(NULL,PERIOD_M1,"!LinRegrBuf","true",barstocount,2,barstocount-1);
   double M1_line=iCustom(NULL,PERIOD_M1,"!LinRegrBuf","true",barstocount,0,0);
   double M1_support=iCustom(NULL,PERIOD_M1,"!LinRegrBuf","true",barstocount,1,0);
   double slopeM1=((M1_resistance-M1_resistance_p)/barstocount)/Point; 
   
   //M5
   double M5_resistance=iCustom(NULL,PERIOD_M5,"!LinRegrBuf","true",barstocount,2,0);
   double M5_resistance_p=iCustom(NULL,PERIOD_M5,"!LinRegrBuf","true",barstocount,2,barstocount-1);
   double M5_line=iCustom(NULL,PERIOD_M5,"!LinRegrBuf","true",barstocount,0,0);
   double M5_support=iCustom(NULL,PERIOD_M5,"!LinRegrBuf","true",barstocount,1,0);
   double slopeM5=((M5_resistance-M5_resistance_p)/barstocount)/Point;
   
   //M15
   double M15_resistance=iCustom(NULL,PERIOD_M15,"!LinRegrBuf","true",barstocount,2,0);
   double M15_resistance_p=iCustom(NULL,PERIOD_M15,"!LinRegrBuf","true",barstocount,2,barstocount-1);
   double M15_line=iCustom(NULL,PERIOD_M15,"!LinRegrBuf","true",barstocount,0,0);
   double M15_support=iCustom(NULL,PERIOD_M15,"!LinRegrBuf","true",barstocount,1,0);
   double slopeM15=((M15_resistance-M15_resistance_p)/barstocount)/Point;
   
   //M30
   double M30_resistance=iCustom(NULL,PERIOD_M30,"!LinRegrBuf","true",barstocount,2,0);
   double M30_resistance_p=iCustom(NULL,PERIOD_M30,"!LinRegrBuf","true",barstocount,2,barstocount-1);
   double M30_line=iCustom(NULL,PERIOD_M30,"!LinRegrBuf","true",barstocount,0,0);
   double M30_support=iCustom(NULL,PERIOD_M30,"!LinRegrBuf","true",barstocount,1,0);
   double slopeM30=((M30_resistance-M30_resistance_p)/barstocount)/Point;   
   
   //H1
   double H1_resistance=iCustom(NULL,PERIOD_H1,"!LinRegrBuf","true",barstocount,2,0);
   double H1_resistance_p=iCustom(NULL,PERIOD_H1,"!LinRegrBuf","true",barstocount,2,barstocount-1);
   double H1_line=iCustom(NULL,PERIOD_H1,"!LinRegrBuf","true",barstocount,0,0);
   double H1_support=iCustom(NULL,PERIOD_H1,"!LinRegrBuf","true",barstocount,1,0);
   double slopeH1=((H1_resistance-H1_resistance_p)/barstocount)/Point; 
   
   //H4 
   double H4_resistance=iCustom(NULL,PERIOD_H4,"!LinRegrBuf","true",barstocount,2,0);
   double H4_resistance_p=iCustom(NULL,PERIOD_H4,"!LinRegrBuf","true",barstocount,2,barstocount-1);
   double H4_line=iCustom(NULL,PERIOD_H4,"!LinRegrBuf","true",barstocount,0,0);
   double H4_support=iCustom(NULL,PERIOD_H4,"!LinRegrBuf","true",barstocount,1,0);
   double slopeH4=((H4_resistance-H4_resistance_p)/barstocount)/Point; 
   
   //D1
   double D1_resistance=iCustom(NULL,PERIOD_D1,"!LinRegrBuf","true",barstocount,2,0);
   double D1_resistance_p=iCustom(NULL,PERIOD_D1,"!LinRegrBuf","true",barstocount,2,barstocount-1);
   double D1_line=iCustom(NULL,PERIOD_D1,"!LinRegrBuf","true",barstocount,0,0);
   double D1_support=iCustom(NULL,PERIOD_D1,"!LinRegrBuf","true",barstocount,1,0);
   double slopeD1=((D1_resistance-D1_resistance_p)/barstocount)/Point;    
   
   //W1
   double W1_resistance=iCustom(NULL,PERIOD_W1,"!LinRegrBuf","true",barstocount,2,0);
   double W1_resistance_p=iCustom(NULL,PERIOD_W1,"!LinRegrBuf","true",barstocount,2,barstocount-1);
   double W1_line=iCustom(NULL,PERIOD_W1,"!LinRegrBuf","true",barstocount,0,0);
   double W1_support=iCustom(NULL,PERIOD_W1,"!LinRegrBuf","true",barstocount,1,0);
   double slopeW1=((W1_resistance-W1_resistance_p)/barstocount)/Point;     
   
   //MN1
   double MN1_resistance=iCustom(NULL,PERIOD_MN1,"!LinRegrBuf","true",barstocount,2,0);
   double MN1_resistance_p=iCustom(NULL,PERIOD_MN1,"!LinRegrBuf","true",barstocount,2,barstocount-1);
   double MN1_line=iCustom(NULL,PERIOD_MN1,"!LinRegrBuf","true",barstocount,0,0);
   double MN1_support=iCustom(NULL,PERIOD_MN1,"!LinRegrBuf","true",barstocount,1,0);
   double slopeMN1=((MN1_resistance-MN1_resistance_p)/barstocount)/Point;        
   
   //Alert(DoubleToStr(slopeM1,2)+" "+DoubleToStr(slopeM5,2)+" "+DoubleToStr(slopeM15,2)+" "+DoubleToStr(slopeM30,2)+" "+DoubleToStr(slopeH1,2));   
   
   Comment(
   "\n","M1  Slope | ",slopeM1,
   "\n","M5  Slope | ",slopeM5,
   "\n","M15 Slope | ",slopeM15,
   "\n","M30 Slope | ",slopeM30,
   "\n","H1  Slope | ",slopeH1,
   "\n","H4  Slope | ",slopeH4,
   "\n","D1  Slope | ",slopeD1,
   "\n","W1  Slope | ",slopeW1,
   "\n","MN1 Slope | ",slopeMN1);//M1_resistance,"\n",M1_resistance_p
   
  //SHORT ENTRY
  if(slopeH1<0 && IsTradeAllowed()==true)
  {
        bool shortopen=false;
        int ord_cnt1=OrdersTotal();
        for (int start1=0;start1<ord_cnt1;start1++)   
        {
           OrderSelect(start1, SELECT_BY_POS, MODE_TRADES);
           if(OrderMagicNumber()==magicnumber && OrderType()==OP_SELL)
              {shortopen=true;}
        }
  if(shortopen==false)
        {      
         double M5High=iHigh(Symbol(),PERIOD_M5,0);
         if(M5High>=M5_resistance)
              {
               double M1High=iHigh(Symbol(),PERIOD_M1,0);
               if(M1High>=M1_resistance)
                   {
                       double shortSL=(M5_resistance-M5_line)/2;
                          //if(slopeM15<0){lots=lots*2;}
                          //if(slopeM30<0){lots=lots*3;}
                          //if(slopeM15<0 && slopeM30<0){lots=lots*4;}
                       int shortticket=OrderSend(Symbol(),OP_SELL,lots,Bid,slippage,Bid+shortSL,M5_line,DoubleToStr(slopeM1,2)+" "+DoubleToStr(slopeM5,2)+" "+DoubleToStr(slopeM15,2)+" "+DoubleToStr(slopeM30,2)+" "+DoubleToStr(slopeH1,2),magicnumber,0,Green);
                          if(shortticket<0)
                            {
                             Comment("Short OrderSend failed with error #",GetLastError());
                             return(0);
                            }
                   }
              }
         }
  } 
 
  
  //LONG ENTRY
  if(slopeH1>0 && IsTradeAllowed()==true)
  {
        bool longopen=false;
        int ord_cnt=OrdersTotal();
        for (int start=0;start<ord_cnt;start++)   
        {
           OrderSelect(start, SELECT_BY_POS, MODE_TRADES);
           if(OrderMagicNumber()==magicnumber && OrderType()==OP_BUY)
              {longopen=true;}
        }
   if(longopen==false)
   {
     double M5Low=iLow(Symbol(),PERIOD_M5,0);
     if(M5Low<=M5_support)
       {
        double M1Low=iLow(Symbol(),PERIOD_M1,0);
        if(M1Low<=M1_support)
          { 
            double longSL=(M5_line-M5_support)/2;
                //if(slopeM15>0){lots=lots*2;}
                //if(slopeM30>0){lots=lots*3;}
                //if(slopeM15>0 && slopeM30>0){lots=lots*4;}
            int longticket=OrderSend(Symbol(),OP_BUY,lots,Ask,slippage,Ask-longSL,M5_line,DoubleToStr(slopeM1,2)+" "+DoubleToStr(slopeM5,2)+" "+DoubleToStr(slopeM15,2)+" "+DoubleToStr(slopeM30,2)+" "+DoubleToStr(slopeH1,2),magicnumber,0,Green);
        if(longticket<0)
           {
             Comment("Long OrderSend failed with error #",GetLastError());
             return(0);
           } 
          }
       }
   }
  }
   
   
   
}
//----
   return(0);
  }
//+------------------------------------------------------------------+