
//+------------------------------------------------------------------+
//|                                                  Gandalf_PRO.mq4 |
//|                                                          budimir |
//|                                              tartar27@bigmir.net |
//+------------------------------------------------------------------+
#property copyright "budimir"
#property link      "tartar27@bigmir.net"
 
//---- input parameters
//ooooooooooooooooooooooooooooooooooooooooooooooooo
extern bool      In_BUY=true;
extern int       Count_buy=24;
extern double    w_price=0.18;
extern double    w_trend=0.18;
extern int       SL_buy=62;
extern int       Risk_buy=0;
//ooooooooooooooooooooooooooooooooooooooooooooooooo
extern bool      In_SELL=true;
extern int       Count_sell=24;
extern double    m_price=0.18;
extern double    m_trend=0.18;
extern int       SL_sell=62;
extern int       Risk_sell=0;
//ooooooooooooooooooooooooooooooooooooooooooooooooo
//extern int       Risk=0;
//ooooooooooooooooooooooooooooooooooooooooooooooooo
//---- other parameters
   static int  prevtime=0;
          int    ticket=0;
          int Magic_BUY  =123;
          int Magic_SELL =321;
          int x=1;
 
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
      if(Digits == 5) x=10;
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
//oooooooooooooooooooooooooooooooooooooooooooooooooooo
    if (Time[0] == prevtime) return(0); 
                             prevtime = Time[0];
    if (!IsTradeAllowed()) {
     prevtime=Time[1]; MathSrand(TimeCurrent());Sleep(30000 + MathRand());
                           }
//oooooooooooooooooooooooooooooooooooooooooooooooooooo
   if( In_BUY)Trade_BUY ( Magic_BUY, Count_buy,w_price,w_trend, SL_buy);
   if(In_SELL)Trade_SELL(Magic_SELL,Count_sell,m_price,m_trend,SL_sell);
//oooooooooooooooooooooooooooooooooooooooooooooooooooo
   return(0);
  }
//xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
void Trade_BUY(int mn,int num,double factor1,double factor2,int sl) { 
 
         int total=OrdersTotal();
         
    for (int i = 0; i < total; i++) {   OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
    
                if (OrderSymbol() == Symbol() && OrderMagicNumber() == mn) { return(0);
                                                                           } 
                                    }
 //ooooooooooooooooooooooooooooooooooooooooooooooooooo
      ticket = -1;  
      
      double target=Out(num,factor1,factor2);  
 
  if (target>(Bid+15*x*Point)  && IsTradeAllowed()) { 
  
    ticket= OrderSend(Symbol(), OP_BUY,lot(Risk_buy),Ask,5,Bid-x*sl*Point,target,DoubleToStr(mn,0),mn,0,Blue);
        
   
                 RefreshRates();   
      
              if ( ticket < 0) { Sleep(30000);   prevtime = Time[1]; } 
                                           
                                           } //-- Exit ---
 
       return(0); } 
       
       
 //xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
 void Trade_SELL(int mn,int num,double factor1,double factor2,int sl) { 
 
         int total=OrdersTotal();
         
    for (int i = 0; i < total; i++) {   OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
    
                if (OrderSymbol() == Symbol() && OrderMagicNumber() == mn) { return(0);
                                                                           } 
                                    }
 //ooooooooooooooooooooooooooooooooooooooooooooooooooo
      ticket = -1; 
       
      double target=Out(num,factor1,factor2);  
 
     if (target<(Ask-15*x*Point)  && IsTradeAllowed()) { 
     
     ticket= OrderSend(Symbol(),OP_SELL,lot(Risk_sell),Bid,5,Ask+x*sl*Point,target,DoubleToStr(mn,0),mn,0, Red);
  
   
                 RefreshRates();   
      
              if ( ticket < 0) { Sleep(30000);   prevtime = Time[1]; } 
                                           
                                           } //-- Exit ---
 
       return(0); } 
       
       
 //xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
 double Out(int n,double l1,double l2) {    double t[120],
                                                   s[120],
                                        lm=iMA(NULL,0,n,0,MODE_LWMA,PRICE_CLOSE,1),
                                        sm=iMA(NULL,0,n,0, MODE_SMA,PRICE_CLOSE,1); 
 //ooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo  
        t[n]=(6*lm-6*sm)/(n-1);s[n]=4*sm-3*lm-t[n]; 
              for (int k = n-1; k>0; k--) {    
           s[k]=l1*Close[k]+(1-l1)*(s[k+1]+t[k+1]);
           t[k]=l2*(s[k]-s[k+1])+(1-l2)*t[k+1];
                                          }//--end--for-
   return  (NormalizeDouble(s[1]+t[1],MarketInfo(Symbol(),MODE_DIGITS)));}
 
//xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
double lot(int R) { double minlot = MarketInfo(Symbol(), MODE_MINLOT);
                       int o = MathAbs(MathLog(minlot) *0.4343) + 0.5;  
                  double lot = minlot;
//ooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo
   lot = NormalizeDouble(AccountFreeMargin() * 0.00001*R, o);//---
   if (AccountFreeMargin() < lot * MarketInfo(Symbol(), MODE_MARGINREQUIRED)) {
   lot = NormalizeDouble(AccountFreeMargin() / MarketInfo(Symbol(), MODE_MARGINREQUIRED), o);
                                                                               }
//ooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo
   if(lot < minlot) lot = minlot;
   double maxlot =MarketInfo(Symbol(), MODE_MAXLOT);
   if(lot > maxlot) lot = maxlot;
   return(lot);    } 
//xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx_end_film_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx


   

