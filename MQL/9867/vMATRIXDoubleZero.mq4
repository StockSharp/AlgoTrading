//+------------------------------------------------------------------+
//|                                                 Matrix.mq4       |
//|                                                mich99@o2.pl      |
//|                                                                  |        
//+------------------------------------------------------------------+
#property link      " "

extern int    Key =1;
extern int    StartTime =0;
extern int    EndTime   =23;
extern double lot       =0.1;
extern bool   Trail     =false;
extern bool   CB     =false;
 extern bool  BUY=true; 
extern int    slb=80,tpb=50; 
 extern bool  SELL=true;
extern int    sls=80,tps=50;

extern string Filters="------------Extra Filters";

extern bool  fi=true; 


extern int   bk = 1;
extern int   bt = 3;
extern int   br = 3;
extern int   bu = 2;
extern int   be = 4;
extern  int  bo = 4;
//extern double   qbb = 2;
extern double   qbc = 4;
extern double   qbg = 4;
 
  



extern int   k = 1;
extern int   t = 3;
extern int   r = 3;
extern int   u = 2;
extern int   e = 4;
extern int   o = 4;
//extern double   qb = 2;
extern double   qc = 4;
extern double   qg = 4;
 
extern bool Range = false;
extern int    MinimumBars= 15;
extern int    MaximumPipRange = 70;

 extern bool mod_vol = false;
extern int vlm = 1000; 
 extern bool mod_vsa = false;
extern int cs = 2;
extern int ap = 2;
  extern bool mod_cciD1 = false;
extern int ccid1p = 15;

extern bool Mod_TP = false;
extern int    sn1 = 100;    
extern int    sn2 = 100;
extern int    sn3=  100;
extern int    sn4=  100;
extern int    snp2 = 10;

extern bool     second = false;
extern double   xb2 = 100; 
extern double   yb2 = 100;  
extern double   xs2 = 100; 
extern double   ys2 = 100;
extern  int     p2=5;
extern int      mn=552770;

 
   
 double tp = 1.0;
 double mpoit = 1.0;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
 //HideTestIndicators(TRUE);
 Comment(mn);
 setdigits();
 settp();
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
  Comment(mn); 
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
//--------------------------------------------------------------------
   static int  prevtime = 0;    int ticket=0;
//-----------------------------------------------------------------
int start(){  



if ( (Key != 1 && !IsDemo()) 
     ||  (TimeDay(TimeCurrent()) >  1 
     && TimeMonth(TimeCurrent()) >= 9 
     &&  TimeYear(TimeCurrent()) >= 2010)  ) {  Alert("EA "+WindowExpertName()+" >> KEY ONLY FOR DEMO OR EXPIRED.contact:mich99@o2.pl");



   }else{



          if (Time[0] == prevtime) return(0);    prevtime = Time[0];

          if (! IsTradeAllowed()) { prevtime = Time[1]; MathSrand(TimeCurrent());Sleep(30000 + MathRand());}
    
//-------------------------------------------------------------------------------------------------

          int total = OrdersTotal(); if(BUY)BBB(total );if( SELL)SSS(total ); 
 
        }
 return(0); }
 
 
  //--end_start--
 void setdigits() {if (Digits == 5 || Digits == 3) mpoit = 10;}
                   
 void settp() {if (Trail) tp =10;}
 
//-------------------------------------------------------------------------------------------
void BBB(int tot) {  int sprb = MarketInfo(Symbol(), MODE_SPREAD)+2*slb;  int h=Hour();
      
                          
          for (int i = 0; i < tot; i++) { OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
  
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == mn) {
    //----------------------------------------------------------------------------------
     if(OrderType()==OP_BUY && OPEN_POS()<0 && CB)OrderClose(OrderTicket(),OrderLots(),Bid,10);
     
         if(Bid >(OrderStopLoss() + sprb * Point*mpoit)  && OrderType()==OP_BUY && Trail)
     {   if(!OrderModify(OrderTicket(), OrderOpenPrice(),Bid-slb*Point*mpoit,0,0,Blue))
                           {Sleep(30000);prevtime=Time[1];}}
    //----------------------------------------------------------------------------------
    
             
                  
         return(0);
     } 
 }//-----------------------------------------------------------------------------------------
       ticket = -1;  RefreshRates();
  //-----------------------------------------------------------------------------------------
    double   ccid1  = iCCI(NULL, 1440, ccid1p, 0, 0);
   
  if (OPEN_POSb()>0 && (!mod_vol || Volume[1]>vlm)&&(!mod_cciD1 || ccid1>0)&&(!second ||POS3()>0)&&
      h>=StartTime && h<=EndTime && IsTradeAllowed()&& (!mod_vsa || VS(cs,ap)>0)&&(!Range ||RangeExists()>0)) { 
  
      ticket=OrderSend(Symbol(),OP_BUY,lot,Ask,4,Ask-slb*Point*mpoit,Ask+(tpb*Point*mpoit+TPb()/100)*tp,WindowExpertName(), mn, 0, Blue); 
      
                              
      
              if (ticket < 0) { Sleep(30000);prevtime = Time[1]; }
                     }    //-- Exit -


 return(0); } 
//-------------------------------------------------------------------------------------------
void SSS(int tot) {   int sprs = MarketInfo(Symbol(), MODE_SPREAD)+2*sls;  int h=Hour();
                          
          for (int i = 0; i < tot; i++) { OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
  
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == mn*2) {
      
    //----------------------------------------------------------------------------------
    if(OrderType()==OP_SELL && OPEN_POSb()>0 && CB)OrderClose(OrderTicket(),OrderLots(),Ask,10);
    
               if(Ask<(OrderStopLoss()-sprs * Point*mpoit) && OrderType()==OP_SELL && Trail)
     {     if(!OrderModify(OrderTicket(),OrderOpenPrice(),Ask+sls*Point*mpoit,0,0,Blue))
                           {Sleep(30000); prevtime=Time[1];}}
                  
         return(0);
     } 
 }//-----------------------------------------------------------------------------------------
      ticket = -1; RefreshRates();
  //-----------------------------------------------------------------------------------------
     double   ccid1  = iCCI(NULL, 1440, ccid1p, 0, 0);
         
   if (OPEN_POS()<0 && (!mod_vol || Volume[1]>vlm)&&(!mod_cciD1 || ccid1<0)&&(!second ||POS3()<0)&&
        h>=StartTime && h<=EndTime && IsTradeAllowed()&& (!mod_vsa || VS(cs,ap)>0)&&(!Range ||RangeExists()>0)) { 
    
      ticket = OrderSend(Symbol(),OP_SELL,lot,Bid,4,Bid+sls*Point*mpoit,Bid-(tps*Point*mpoit+TPb()/100)*tp,WindowExpertName(), mn*2, 0, Red); 
      
                           
      
                       if (ticket < 0) { Sleep(30000); prevtime = Time[1];}
                         }   //-- Exit -


 return(0); } 
//=====================================================================================================

double OPEN_POS()
{  
    double  klon1 = Close[k]-(High[k] + Low[k])/2;
    double  klon2 = Close[u]-(High[u] + Low[u])/2;
    double  klon3 = Close[t]-(High[t] + Low[t])/2;
    double  klon4 = Close[e]-(High[e] + Low[e])/2;
    double  klon5 = Close[r]-(High[r] + Low[r])/2;
    double  klon6 = Close[o]-(High[o] + Low[o])/2;
 
   
   
    
 double L = iLow(NULL, 0, 1);
   double H = iHigh(NULL, 0, 1);
   double O = iOpen(NULL, 0, 1);
   double C = iClose(NULL, 0, 1);
   double OC = MathAbs(O-C);
   double mid = H-(H-L)/2;
   
   
   double L2 = iLow(NULL, 0, 2);
   double H2 = iHigh(NULL, 0, 2);
   double O2 = iOpen(NULL, 0, 2);
   double C2 = iClose(NULL, 0, 2);
   double OC2 = MathAbs(O2-C2);
   double mid2 = H2-(H2-L2)/2;
  
   double L3 = iLow(NULL, 0, 3);
   double H3 = iHigh(NULL, 0, 3);
   double O3 = iOpen(NULL, 0, 3);
   double C3 = iClose(NULL, 0, 3);
   double OC3 = MathAbs(O3-C3);
   double mid3 = H3-(H3-L3)/2;  
      
  
   if(  (!fi ||( klon1 > klon2   && klon3>qc*klon4  &&   klon5 >qg*klon6))   &&   C2>DZ() && C<DZ() ) { return(-1);}  
   
  
   
   return(0);
}


double OPEN_POSb()
{  
    double  klon1 = Close[bk]-(High[bk] + Low[bk])/2;
    double  klon2 = Close[bu]-(High[bu] + Low[bu])/2;
    double  klon3 = Close[bt]-(High[bt] + Low[bt])/2;
    double  klon4 = Close[be]-(High[be] + Low[be])/2;
    double  klon5 = Close[br]-(High[br] + Low[br])/2;
    double  klon6 = Close[bo]-(High[bo] + Low[bo])/2;
 
   
     
   
  
    
  double L = iLow(NULL, 0, 1);
   double H = iHigh(NULL, 0, 1);
   double O = iOpen(NULL, 0, 1);
   double C = iClose(NULL, 0, 1);
   double OC = MathAbs(O-C);
   double mid = H-(H-L)/2;
   
   
   double L2 = iLow(NULL, 0, 2);
   double H2 = iHigh(NULL, 0, 2);
   double O2 = iOpen(NULL, 0, 2);
   double C2 = iClose(NULL, 0, 2);
   double OC2 = MathAbs(O2-C2);
   double mid2 = H2-(H2-L2)/2;
  
   double L3 = iLow(NULL, 0, 3);
   double H3 = iHigh(NULL, 0, 3);
   double O3 = iOpen(NULL, 0, 3);
   double C3 = iClose(NULL, 0, 3);
   double OC3 = MathAbs(O3-C3);
   double mid3 = H3-(H3-L3)/2; 
   
   if(  (!fi || ( klon1 > klon2   && klon3>qbc*klon4  &&   klon5 >qbg*klon6))  && C2<DZ() && C>DZ() ) { return(1); }  
     
  
   
  
   
   return(0);
}


double DZ() {
   double factor=MathPow(10, 2);
   return(MathRound( Close[1]*factor)/factor);
}


   double VS(int b,int f)   {
  
   if(!mod_vsa) return (0);
  
   double a3 = iATR(NULL,0,f,1);
   double a4 = iATR(NULL,0,f,b);
   
   if( a3>a4) return(1);
   
   
   return(0);
  
 }  
 
 double RangeExists()   {
  
  double RangeHigh = iHigh(NULL, 0, iHighest(NULL, 0, 2, MinimumBars,  1));
  double RangeLow = iLow(NULL, 0, iLowest(NULL, 0, 3, MinimumBars,  1));
  
  
  if(RangeHigh - RangeLow < MaximumPipRange * Point*mpoit) return(1);
     
  
   return(0);
}

 double TPbv()   {
  
  if(!Mod_TP) return (0);
 
   double w1 = sn1 - 50;
   double w2 = sn2 - 50;
   double w3 = sn3 - 50;
   double w4 = sn4 - 50;
   double a1 = iATR(NULL,60,1,1);
   double a2 = iATR(NULL,60,88,1);
   double a3 = High[1] - High[snp2 ];
  
   double a4 = High[snp2] - High[snp2*2 ];
   
   return(w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4);
}  
 double TPbb()   {
  
  if(!Mod_TP) return (0);
 int p2=snp2;
   double w1 = sn1 - 50;
   double w2 = sn2 - 50;
   double w3 = sn3 - 50;
   double w4 = sn4 - 50;
   double a1 = High[1] - High[p2];
   double a2 = High[p2] - High[p2 * 2];
   double a3 = Low[1] - Low[p2];
   double a4 = Low[p2 ] - Low[p2 * 2];
   
   return((w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4));
}  

 double POS3m()   {
 
 
   double a1 = High[1] - High[p2];
   double a2 = High[p2] - High[p2 * 2];
   double a3 = Low[1] - Low[p2];
   double a4 = Low[p2 ] - Low[p2 * 2];
   
   double w1 = 0 ;
   double w2 = 0 ;
   double w3 = 0 ;
   double w4 = 0 ;
   
    if(xb2==1)w1=a1;
    if(xb2==2)w1=a2;
    if(xb2==3)w1=a3;
    if(xb2==4)w1=a4;
    
    if(xs2==1)w2=a1;
    if(xs2==2)w2=a2;
    if(xs2==3)w2=a3;
    if(xs2==4)w2=a4;
  
    if(yb2==1)w3=a1;
    if(yb2==2)w3=a2;
    if(yb2==3)w3=a3;
    if(yb2==4)w3=a4;
    
    if(ys2==1)w4=a1;
    if(ys2==2)w4=a2;
    if(ys2==3)w4=a3;
    if(ys2==4)w4=a4;
    
     if( w1 > w2   && w3  > w4 && w1>w4   ) { return(1); }  
  
   return(0);
}




double POS3()   {
  
  
 
   double w1 = xb2 - 50;
   double w2 = xs2 - 50;
   double w3 = yb2 - 50;
   double w4 = ys2 - 50;
   double a1 = High[1] - High[p2];
   double a2 = High[p2] - High[p2 * 2];
   double a3 = Low[1] - Low[p2];
   double a4 = Low[p2 ] - Low[p2 * 2];
   return(w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4);
}

 double TPb()   {
  
  if(!Mod_TP) return (0);
 
   double w1 = sn1 - 50;
   double w2 = sn2 - 50;
   double w3 = sn3 - 50;
   double w4 = sn4 - 50;
   double a1 = iATR(NULL,60,1,1)-iATR(NULL,60,1,2);
   double a2 = iATR(NULL,60,1,1);//-iATR(NULL,60,1,3);
   double a3 = iATR(NULL,60,25,1);
  
   double a4 = High[snp2] - High[snp2*2 ];
   
   return((w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4));
}  