//+------------------------------------------------------------------+
//|                                                EURUSD trader.mq4 |
//|                      Copyright © 2011, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "sergey antipin"
#property link      "http://www.metaquotes.net"



extern double buffer=0;           
extern double stoploss=20;            
extern double takeprofit=350;                
extern double filter=50;               
extern double ma_value=218;  
double max_account_balance=0;
double max_spread=4;   
extern double Z=2;
bool price_above=false;


double M=1;  // risk 

bool trade_ok=true;
int ticket1=-1;


//------------------------------------------------------------------------------------------------~~~~~~!!!!!!!!!!!!!!!!






//------------------------------------------------------------------------------------------------~~~~~~!!!!!!!!!!!!!!!!

     
double entryprice=0;
//++++ These are adjusted for 5 digit brokers.
int     pips2points;    // slippage  3 pips    3=points    30=points
double  pips2dbl;       // Stoploss 15 pips    0.0015      0.00150
int     Digits.pips;    // DoubleToStr(dbl/pips2dbl, Digits.pips)

int     init(){
    if (Digits == 5 || Digits == 3)
    {    // Adjust for five (5) digit brokers.
                pips2dbl    = Point*10; pips2points = 10;   Digits.pips = 1;
    } 
    else {    pips2dbl    = Point;    pips2points =  1;   Digits.pips = 0; }
    // OrderSend(... Slippage.Pips * pips2points, Bid - StopLossPips * pips2dbl
    
    
if (Bid>iMA(Symbol(),15,ma_value,0,MODE_SMA,PRICE_TYPICAL,0))
price_above=true;
else
price_above=false;
}



int deinit()
  {
   return(0);
  }
  
  
  
  
int start()
  {
  
//Alert(price_above);
  
  
double SMA_10day=iMA(Symbol(),15,ma_value,0,MODE_SMA,PRICE_TYPICAL,0);  //  simple ma_value day moving average value 
double ATR=iATR(Symbol(),60,200,0);




//-----risk management  (X * (1-0.005*Z)^lose-trades= how much left)   - http://mathhelpforum.com/calculus/212440-find-general-formula-n-reductions.html#post767362

M=(AccountBalance()*0.005*Z)/(0.1*stoploss) ;  // x% risk per trade

if (M<1) M=1;




if (AccountBalance()>max_account_balance)
max_account_balance=AccountBalance();

//-----end of risk



//---------------- open new trades



//--- * short trade




if  (trade_ok)

{

if ( ( Bid- SMA_10day  < buffer* pips2dbl ) && (price_above) && (ATR<40*pips2dbl) )     
  
  {  

     ticket1=-1;
     
   Alert(MarketInfo(Symbol(),MODE_ASK)-MarketInfo(Symbol(),MODE_BID),"short trade"," atr=", ATR);
      
    
      while (Ask-Bid < max_spread* pips2dbl) //spread < 4

     {
  
     RefreshRates();
     ticket1 = OrderSend(Symbol() , OP_SELL , 0.01*M , Bid , 2*pips2points , 0 ,0); 
Alert("ALERT!!!!!!   ",GetLastError(), "<======");
     Sleep(2000);
     Alert("ALERT!!!!!!   ",GetLastError(), "<======");
     if (ticket1>0)
     { 
     OrderModify(ticket1,Bid,Ask+stoploss* pips2dbl , Ask-takeprofit* pips2dbl,0,White);
   Alert("ALERT!!!!!!   ",GetLastError(), "<======");
     entryprice=Bid; 
     break;
     }
     }

trade_ok=false; 

  }
  
    //--- * end of short trade
     
     else 
     
    
       
//--- * long trade

if ( ( SMA_10day -  Ask < buffer* pips2dbl ) && (!price_above) && (ATR<40*pips2dbl))     
  
  {   
  
          Alert(MarketInfo(Symbol(),MODE_ASK)-MarketInfo(Symbol(),MODE_BID),"long trade"," atr=", ATR,price_above);
     ticket1=-1;
     

     

     while (Ask-Bid<max_spread* pips2dbl) //spread < 4

     {

     RefreshRates();     
     ticket1 = OrderSend(Symbol() , OP_BUY , 0.01*M , Ask , 2*pips2points , 0 , 0);
     
Alert("ALERT!!!!!!   ",GetLastError(), "<======");
     Sleep(2000);
     Alert("ALERT!!!!!!   ",GetLastError(), "<======");
     if (ticket1>0)
     {
     OrderModify(ticket1,Ask,Bid-stoploss* pips2dbl , Bid+takeprofit* pips2dbl,0,White);
     entryprice=Ask;
     Alert("ALERT!!!!!!   ",GetLastError(), "<======");
     break;
     } 
     }
     

     trade_ok=false;    
  }

//--- * end of long trade
  
}
  
  
  

//---- end of open new trades






//-----x pip noice filter
          
       if (!trade_ok)
      {
         if  (price_above)  //-- price currently above 
           
           {
                if (Bid < entryprice - filter* pips2dbl)       //-- price moved below and is not random noice
                
                  {
                       price_above=false;
                       trade_ok=true;
                  }
                  
                  
              else
              
               
                if (Ask > entryprice + filter* pips2dbl)     //-- price stayed above and is not random noice
                   trade_ok=true;
               
           }

      else


         if (!price_above)  //-- price currently below
  
           {

                if (Ask > entryprice + filter* pips2dbl)  //-- price moved above and is not random noice
  
                  {
                      price_above=true;
                      trade_ok=true; 
                  }   
   
             else
               
               if (Bid < entryprice - filter* pips2dbl)  //-- price stayed below and is not random noice
               trade_ok=true;
          
           }
        }   
  
  //----- end of filter
  
  
   return(0);
  }
  

