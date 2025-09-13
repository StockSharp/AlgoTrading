//+------------------------------------------------------------------+
//|                                                      PROphet.mq4 |
//|                                                        PraVedNiK |
//|                                                  taa-34@mail.ru  |        
//+------------------------------------------------------------------+
#property link      "taa-34@mail.ru"
//--------------------------------------------------------------------
 extern bool  daBUY=true;  extern int       x1=9,x2=29,x3=94,x4=125,slb=68; 
//---------------------------------------------------------------------------
  extern bool  daSELL=true; extern int    y1=61,y2=100,y3=117,y4=31,sls=72;       

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
      //-----–”À‹ Ì‡ M5_EURUSD------
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
//--------------------------------------------------------------------
   static int  prevtime = 0;    int ticket=0;   double lot=0.1;
//-----------------------------------------------------------------
int start(){    if (Time[0] == prevtime) return(0);    prevtime = Time[0];

    if (! IsTradeAllowed()) { prevtime = Time[1]; MathSrand(TimeCurrent());Sleep(30000 + MathRand());}
//-------------------------------------------------------------------------------------------------

 int total = OrdersTotal(); if(daBUY)BBB(total );if( daSELL)SSS(total ); return(0); } //--end_start--
//-------------------------------------------------------------------------------------------
void BBB(int tot) {  int sprb = MarketInfo(Symbol(), MODE_SPREAD)+2*slb;  int h=Hour();
      
                          
          for (int i = 0; i < tot; i++) { OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
  
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == 74) {//--------------------
      
      if(OrderType()==OP_BUY && h>18)OrderClose(OrderTicket(),OrderLots(),Bid,3);
      
    //----------------------------------------------------------------------------------
    
         if(Bid >(OrderStopLoss() + sprb * Point)  && OrderType()==OP_BUY)
     {   if(!OrderModify(OrderTicket(), OrderOpenPrice(),Bid-slb*Point,0,0,Blue))
                           {Sleep(30000);prevtime=Time[1];}}
    //----------------------------------------------------------------------------------
    
             
                  
         return(0);
     } 
 }//-----------------------------------------------------------------------------------------
       ticket = -1;  RefreshRates();
  //-----------------------------------------------------------------------------------------
  if (Qu(x1,x2,x3,x4)>0 && h>=10 && h<=18 && IsTradeAllowed()) { 
  
      ticket=OrderSend(Symbol(),OP_BUY,lot,Ask,4,Bid-slb*Point,0,"ƒÎËÌÌˇ ", 74, 0, Blue); 
      
                               PlaySound("bil.wav"); 
      
              if (ticket < 0) { Sleep(30000);prevtime = Time[1]; }
                     }    //-- Exit -


 return(0); } 
//-------------------------------------------------------------------------------------------
void SSS(int tot) {   int sprs = MarketInfo(Symbol(), MODE_SPREAD)+2*sls;  int h=Hour();
                          
          for (int i = 0; i < tot; i++) { OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
  
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == 81) {//--------------------
      
    
      if(OrderType()==OP_SELL && h>18)OrderClose(OrderTicket(),OrderLots(),Ask,3);
      
    //----------------------------------------------------------------------------------
    
    
               if(Ask<(OrderStopLoss()-sprs * Point) && OrderType()==OP_SELL)
     {     if(!OrderModify(OrderTicket(),OrderOpenPrice(),Ask+sls*Point,0,0,Blue))
                           {Sleep(30000); prevtime=Time[1];}}
                  
         return(0);
     } 
 }//-----------------------------------------------------------------------------------------
      ticket = -1; RefreshRates();
  //-----------------------------------------------------------------------------------------
   if (Qu(y1,y2,y3,y4)>0 && h>=10 && h<=18 && IsTradeAllowed()) { 
    
      ticket = OrderSend(Symbol(),OP_SELL,lot,Bid,4,Ask+sls*Point,0," ÓÓ“˚ÿ", 81, 0, Red); 
      
                          PlaySound("bil.wav"); 
      
                       if (ticket < 0) { Sleep(30000); prevtime = Time[1];}
                         }   //-- Exit -


 return(0); } 
//=====================================================================================================
     double Qu(int q1,int q2,int q3,int q4) {    return ((q1-100)*MathAbs(High[1]-Low[2])+
 (q2-100)*MathAbs(High[3]-Low[2])+(q3-100)*MathAbs(High[2]-Low[1])+(q4-100)*MathAbs(High[2]-Low[3]));}

