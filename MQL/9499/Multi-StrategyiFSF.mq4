//+------------------------------------------------------------------+
//|                                                     Combo EA.mq4 |
//|                                        Copyright © 2010, Farshad |
//|                                           http://www.vizhish.com |
//+------------------------------------------------------------------+

#property copyright "Copyright © 2010, Farshad Saremifar"
#property link      "farshad.saremifar@gmail.com"

extern string Copyright="----------Copyright © 2010, Farshad Saremifar----------";
extern string Copyright2="----------Farshad.Saremifar@Gmail.com----------";
extern string    Factor ="--------Combo Trader Factor---------";
extern string    Factor2 ="--------Factor:1 to 3-------";
extern int Combo_Trader_Factor=1;
extern string    MA_Cross2 ="--------Moving Average Cross---------";
extern string    MA_Cross3 ="--------Ma Mode:1 to 5-------";
extern bool    USE_MA = true;//option 4
extern bool    USE_Last_MA_Signal = true;
extern int    MA_MODE = 5;//option 4
extern int    MA_TIMEFRAME = 3;//1,2,3,4,5,6
extern int    FastMa_Period=5;                             
extern int    MidMa_Period=13;
extern int    SlowMa_Period=38;                             

extern string MA_Cross4 =  "-------(SMA=0,EMA=1,SMMA=2,LWMA=3)------";                           
extern int    FastMa_Method=1;                              //  (SMA=0,EMA=1,SMMA=2,LWMA=3)
extern int    MidMa_Method=1;  
extern int    SlowMa_Method=1;                              //  (SMA=0,EMA=1,SMMA=2,LWMA=3)
extern string MA_Price =  "-----(PRICE_CLOSE=1,PRICE_OPEN=1,PRICE_HIGH=2,PRICE_LOW=3)-----";   
extern string MA_Price2 = "-----(PRICE_MEDIAN=4,PRICE_TYPICAL=5,PRICE_WEIGHTED=6)----";   
extern int    FastMa_Price=0;                         
extern int    MidMa_Price=0;  
extern int    SlowMa_Price=0;                             
extern int    FastMa_Shift=0;                         
extern int    MidMa_Shift=0;   
extern int    SlowMa_Shift=0;    
extern int    FastMa_Shifting=0;                         
extern int    MidMa_Shifting=0;   
extern int    SlowMa_Shifting=0;      
  
extern string RSI_DES = "-------------RSI-------------";
extern bool   USE_RSI=true;//option 1
extern bool   USE_Last_RSI_Signal = true;
extern int    RSI_TIMEFRAME = 3;//1,2,3,4,5,6

extern int    RSIperiod=14;
extern int    RSIShift=0;
extern string    RSI_DES2 = "----------Select RSI MODE :1-4";
extern string    RSI_DES3 = "1 FOR OVERBOGHT/OVERSOLD -2 FOR RSI TREND 3-BOTH 1&2 4-FOR RSI AT THE ZONE";
extern int    RSI_MODE = 1;
extern string    RSI_DES4 = "-------This is for Overbought/Oversold zone";
extern string    RSI_DES5 = "-------For RSI MODE 1 & 3";
extern int    RSIBuyOp          = 12;     
extern int    RSISellOp         = 88;
extern string RSI_DES6 = "-------zone for buy and sell";
extern string    RSI_DES7 = "-------For RSI MODE 4";
extern int    RSIBuyZone          = 55;     
extern int    RSISellZone         = 45;

extern string    MACD_1 = "--------------MACD----------------";
extern string    MACD_2 = "-----MACD signal , mode: 1=trend,2=Buy/sell at sell/buy zone,3=use both 1&2 ";
extern bool    USE_MACD = true;//option 5
extern bool   USE_Last_MACD_Signal = true;
extern int    MACD_TIMEFRAME = 3;//1,2,3,4,5,6

extern int     MACD_MODE = 2;
extern int     MACD_Fast = 12;
extern int     MACD_Slow = 24;
extern int     Signal_Period = 9;
extern int     MACD_Price = 0;              // PRICE_CLOSE
extern int     MACD_Shift = 0;
extern string    MACD_3 = "--------------Choose MACD Indicator----------------";
extern bool    Zerolag_MACD =True;
extern bool    Classic_MACD =false;



extern string    STO1_DES = "------- Stochastic--------";
extern bool      USE_STO = true;//option 2
extern bool   USE_Last_STO_Signal = true;
extern int    STO_TIMEFRAME =3;//1,2,3,4,5,6

extern int       STOK=5;
extern int       STOD=3;
extern int       STOL=3;
extern string    STO3_DES = "-----Stochastic Overbough/OverSold Zone--";
extern bool      useSTOHighLow =false;
extern int       STOHigh=80;
extern int       STOLow=20;
extern string    SAR_DES = "--------Parabolic SARS------------";
extern bool      USE_SAR = false;//option 3
extern bool   USE_Last_SAR_Signal = true;
extern int    SAR_TIMEFRAME = 3;//1,2,3,4,5,6

extern double    SARStep = 0.02;
extern double    SARMax = 0.2;
extern bool   Open_at_newbar=true;
extern string    Desc = "-----END OF Opening MODES------";

extern string  _Money_management    = "--------Money Management-------";
extern bool   Use_Static_Lot=true;
extern double Static_Lot=0.1;
extern double Risk=5;
extern string  _Position_Control    = "--------Position Control-------";
extern bool    useTrailingStop = true;
extern int    TrailingStop = 198;
extern double Static_TP=100; 
extern double Static_SL=100; 


extern double Max_orders=4;
extern double Max_same_orders=2;


extern string  _Trend_Control    = "-----------Select trend detecting values----------";
extern bool Trenddetect = true;
extern bool Filtering_Noise = true; 
extern int    NOISE_TIMEFRAME = 2;//1,2,3,4,5,6
 
extern int ADX_PERIOD = 14;
extern int ADX_MAINLEVEL = 20;
extern string  _ADX_TIMEFRAME    = "---Use This To Check Trend On Any Time Frame";
extern string  _ADX_TIMEFRAME2    = "1,2,3,4,5,6,7";
extern int ADX_TIMEFRAME=5;
extern string  _BOLLINGER_BAND    = "-----Use Bollinger Bands To Trade On Range Markets--------";
extern bool Use_BBand=True;
extern int    BBAND_TIMEFRAME = 3;//1,2,3,4,5,6
extern int BB1_Period = 20;
extern int BB2_Period = 20;
extern int BB3_Period = 20;
extern int Range_Parameter = 6;

extern string  _Close_Modes    = "-------------Closing Modes-----------";
extern string  _Close_Modes2   = "--------Choose How to Close a Position-----";
extern bool      AutoClose =True;
extern bool      USE_MA_CLOSING =True;
extern int      MA_MODE_CLOSING=1;
extern int    MA_TIMEFRAME_CLOSING = 3;//1,2,3,4,5,6
extern bool      USE_MACD_CLOSING =True;
extern int      MACD_MODE_CLOSING=3;
extern int    MACD_TIMEFRAME_CLOSING = 3;//1,2,3,4,5,6
extern bool      USE_RSI_CLOSING =False;
extern int      RSI_MODE_CLOSING=3;
extern int    RSI_TIMEFRAME_CLOSING = 3;//1,2,3,4,5,6
extern bool      USE_STO_CLOSING =false;
extern int    STO_TIMEFRAME_CLOSING = 3;//1,2,3,4,5,6
extern bool      USE_SAR_CLOSING=false;
extern int    SAR_TIMEFRAME_CLOSING = 3;//1,2,3,4,5,6
extern bool      USE_NOISE_CLOSING=True;
extern int    NOISE_TIMEFRAME_CLOSING = 3;//1,2,3,4,5,6
extern double   NOISE_LOSS=90;

extern bool      Open_opposite_after_close =False;
extern string EXT="----------Extras--- Protecting Positions---";
extern bool Protect_Profit=false;
extern double Profit_to_Close=40;
extern bool Loss_Contorl=false;
extern double Loss_to_Close=40;
extern bool Cancel_Trading_On_Profit=false;
extern bool Cancel_Trading_On_Loss=false;
extern bool  Send_Mail=false;
extern int       MagicNumber = 1080; // Magic Number  
 









double  LOT;
int options[];
double SLBUY,TPBUY,SLSELL,TPSELL,lotb,lots;
bool cantrade=true;    
 int Periodo = 7;
int Signal = 6 ;
double Buffer=3.8;
 int Shift1=1;
int Shift2=6;
double Buy_StopLoss ,Sell_StopLoss,Buy_TakeProfit,Sell_TakeProfit, SL_Sell,SL_Buy;                       

int New_Bar;                                             
int Time_0;                                              
int PosOpen;                                          
int PosClose;                                           
int total;                                             
double FastMaCurrent,MidMaCurrent,FastMaPrevious,MidMaPrevious,SlowMaCurrent,SlowMaPrevious,MA1CrossBuffer,MA2CrossBuffer;                                          
                                        
                                    
double MACD_Main_0;
double MACD_Main_1;
double MACD_Signal_0;
double MACD_Signal_1;                 
double macdMain;
double macdSignal;
double macdMainPrev ;
double macdSignalPrev;                           
double RSIValue,RSIcurrent,RSIprevious;
double SARSignal;
double STOValue = 0;
double STOSignal = 0;
int countoption,notused,uptrend,downtrend,o = 0; //1 = buy, 2 = sell
int MA,MACD,RSI,STO,SAR,MACD2,RSI2;   


int signals[];
int MyPoint;   
bool canbuy,cansell,trenddetect;



//CLOSING VARIABLES
double CFastMaCurrent,CMidMaCurrent,CFastMaPrevious,CMidMaPrevious,CSlowMaCurrent,CSlowMaPrevious,CMA1CrossBuffer,CMA2CrossBuffer;                                          
                                          
double value1;
double value2;
double value3;                                
double CMACD_Main_0;
double CMACD_Main_1;
double CMACD_Signal_0;
double CMACD_Signal_1;                 
double CmacdMain;
double CmacdSignal;
double CmacdMainPrev ;
double CmacdSignalPrev;                           
double CRSIValue,CRSIcurrent,CRSIprevious;
double CSARSignal,upperenv,lowerenv;
double CSTOValue = 0;
double CSTOSignal = 0;
static datetime LastTradeBarTime;
static int last_ma_signal,last_macd_signal,last_sto_signal,last_sar_signal,last_rsi_signal;
static int last_ma2_signal,last_macd2_signal,last_rsi2_signal;
static int last_ma3_signal,last_macd3_signal,last_rsi3_signal;
static int last_ma4_signal,last_macd4_signal,last_rsi4_signal;
static int last_ma5_signal;



//============================================================================================
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
  
if(Digits==3||Digits==5)MyPoint=10; else MyPoint=1;
//----

LastTradeBarTime = Time[1]; // initialise the variable
last_ma_signal=0;last_macd_signal=0;last_sto_signal=0;last_sar_signal=0;last_rsi_signal=0;
last_ma2_signal=0;last_macd2_signal=0;last_rsi2_signal=0;
last_ma3_signal=0;last_macd3_signal=0;last_rsi3_signal=0;
last_ma4_signal=0;last_macd4_signal=0;last_rsi4_signal=0;
last_ma5_signal=0;

   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
  ObjectsDeleteAll(); 
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+

//============================================================================================
int start()  
 {
  // ------

    
 
   // ------
 if (Protect_Profit)Save_Profit();
 if (Loss_Contorl)Save_Loss();
if (Cancel_Trading_On_Profit||Cancel_Trading_On_Loss)checkcanTrading();



 


//================================
//----
double price;  
int i;
   int total = OrdersTotal();
 canbuy=false;cansell=false;   
 trenddetect=trend_detect();
 if (Combo_Trader_Factor==1)
{
 if (Trenddetect||Use_BBand)
   {
       if (trenddetect==3||trenddetect==4)
            {
                if (bband()==1){canbuy=true;cansell=false;}
                  if (bband()==2){canbuy=false;cansell=true;}
  
                  if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}
             }
      else {
  
  
             if (trenddetect==1&&Signal()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
             if (trenddetect==2&&Signal()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
      
  
  
          }
  }
   if (!Trenddetect&&!Use_BBand)
  {
  if (Signal()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
  if (Signal()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
   
  
  
  }
 if (!Trenddetect&&Use_BBand)
   {
      if (bband()==1||Signal()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
      else if (bband()==2||Signal()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
      else {canbuy=false;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
   }
 }//-------------------------COmbo Trader Factor 1
 
else if (Combo_Trader_Factor==2)
 {
 if (Trenddetect&&!Use_BBand)
 {
      if (trenddetect==1&&Signal()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
      if (trenddetect==2&&Signal()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
      if (trenddetect==3&&Signal()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
      if (trenddetect==4&&Signal()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
  
  } 

       
else if (Trenddetect&&Use_BBand)
{
if (trenddetect==1&&Signal()==1&&bband()!=1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
else if (trenddetect==2&&Signal()==2&&bband()!=2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
else if (trenddetect==3&&Signal()==2&&bband()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
else if (trenddetect==4&&Signal()==1&&bband()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
   


}
 else if (!Trenddetect&&!Use_BBand)
 {
  if (Signal()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
  if (Signal()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}

 }
  else if (!Trenddetect&&Use_BBand)
   {
      if (bband()==1&&Signal()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
      else if (bband()==2&&Signal()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
      else {canbuy=false;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
   }
}   
//---------------------------------Combo Trader Factor 2
 else if (Combo_Trader_Factor==3)
 {
       if (Trenddetect&&Use_BBand)
            {
            
            if (trenddetect==1&&Signal()==1&&bband()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
            else if (trenddetect==2&&Signal()==2&&bband()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
            else if (trenddetect==3&&Signal()==2&&bband()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
            else if (trenddetect==4&&Signal()==1&&bband()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
            
            }
         
         else if (!Trenddetect&&!Use_BBand)
         {
             if (Signal()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
              if (Signal()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
            }
         else if (Trenddetect&&!Use_BBand)
            {
                  if (trenddetect==1&&Signal()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
                  if (trenddetect==2&&Signal()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
                  if (trenddetect==3&&Signal()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
                  if (trenddetect==4&&Signal()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
  
             }      
    else if (!Trenddetect&&Use_BBand)
            {
      if (bband()==1){canbuy=true;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
      else if (bband()==2){canbuy=false;cansell=true;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
      else {canbuy=false;cansell=false;if (Filtering_Noise&&signalTRIX()==1){canbuy=false;cansell=false;}}
         }      
 
  

 }  
//closing the and openning opposite====================================================  
  
   int openOrders=0;
   

   if (useTrailingStop==1){Trail(TrailingStop);}                                    
   for(i=total-1;i>=0;i--)
      {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)    
         {
         if(OrderType()==OP_BUY&& OrderMagicNumber() == MagicNumber)                             
            {
            if (USE_NOISE_CLOSING)
            {
            if ((signalTRIX()==1&&OrderProfit()>=0)||(signalTRIX()==1&&OrderProfit()<=(0-NOISE_LOSS))){price=MarketInfo(Symbol(),MODE_BID);
               OrderClose(OrderTicket(),OrderLots(),Bid,4,CLR_NONE);continue;}
            
            }
            if(CloseSignal()==2&&AutoClose==1)                    
               {                                            
               price=MarketInfo(Symbol(),MODE_BID);
               OrderClose(OrderTicket(),OrderLots(),Bid,4,CLR_NONE);
               if (Open_opposite_after_close==1&&checksellorders()<Max_same_orders&&Signal()==2&&cansell)
                     {
            
                    
                       //settpsl(); 
                     
                 LOT = LOT(Risk,OrdersTotal()+1);
                      OPENORDER ("Sell");
                           
                          
                     }//if (open opposite)
               }//if close signal ==2
            }//if order type == buy
         if(OrderType()==OP_SELL&& OrderMagicNumber() == MagicNumber)                            
            {
                if (USE_NOISE_CLOSING)
            {
            if ((signalTRIX()==1&&OrderProfit()>=0)||(signalTRIX()==1&&OrderProfit()<=(0-NOISE_LOSS))){price=MarketInfo(Symbol(),MODE_BID);
               OrderClose(OrderTicket(),OrderLots(),Bid,4,CLR_NONE);continue;}
            
            }
            if(CloseSignal()==1&&AutoClose==1)                     
               {                                             
               price=MarketInfo(Symbol(),MODE_ASK);
               OrderClose(OrderTicket(),OrderLots(),Bid,4,CLR_NONE);
                  if (Open_opposite_after_close==1&&checkbuyorders()<Max_same_orders&&Signal()==1&&canbuy)
                     {
                                  
                          //settpsl();    
                          
                     LOT = LOT(Risk,OrdersTotal()+1);
                           OPENORDER ("Buy"); 
                            
                     }//if opposite
                     
               
               
               }//if close signal = 1
            }//if oreder type=sell
         }//if order select
      }//for
 
//check for new bar===========================================================   
   New_Bar=0;                                           
   if (Time_0 != Time[0])                                  
      {
      New_Bar= 1;      
      //calculate (); 
                                             
      Time_0 = Time[0]; 
      }
  

       
//calculating data===================================================

calculate();
display();



bool cantradings=false;
if (checkmaxorders()<Max_orders)
 {
  if (Open_at_newbar&&New_Bar==1)cantradings=true;
  if (Open_at_newbar&&New_Bar==0)cantradings=false;
  if (Open_at_newbar==false)cantradings=True;
  
 if (checkbuyorders()<Max_same_orders&&canbuy&&cantradings)
      {   //settpsl();
 
                    LOT = LOT(Risk,OrdersTotal()+1);
                      OPENORDER ("Buy"); 
                   
      }     
   
   
    else if (checksellorders()<Max_same_orders&&cansell&&cantradings)
     
      {

            //settpsl();
                    LOT = LOT(Risk,OrdersTotal()+1);
          OPENORDER ("Sell");
          
      }     

} 

  
   
 
 return(0);
}  

//**********************************END OF START ********************************



//============================================================================================

int checkmaxorders()
{
int maxtrades=0;

   for (int i=OrdersTotal()-1; i>=0; i--)
   {                                               
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
      {
         if (OrderSymbol()!=Symbol()) continue;
         else if (OrderMagicNumber() == MagicNumber){maxtrades=maxtrades+1;}
      }   
   }
 return(maxtrades);  
}
//========================================================================================
int checkbuyorders()
{
int buytrades=0;

   for (int i=OrdersTotal()-1; i>=0; i--)
   {                                               
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
      {
         if (OrderSymbol()!=Symbol()) continue;
         else if(OrderMagicNumber() == MagicNumber && OrderType()==OP_BUY){buytrades=buytrades+1;}
      }   
   }
 return(buytrades);  
}

//====================================================================================================
int checksellorders()
{
int selltrades=0;

   for (int i=OrdersTotal()-1; i>=0; i--)
   {                                               
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
      {
         if (OrderSymbol()!=Symbol()) continue;
         else if(OrderMagicNumber() == MagicNumber && OrderType()==OP_SELL){selltrades=selltrades+1;}
      }   
   }
 return(selltrades);  
}
//=================================================================================================
void Trail(int TrailingStop0)
{
    for (int i=OrdersTotal()-1; i >= 0; i--)
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
    {
        if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber)
        {
            if (OrderType() == OP_BUY && Bid - OrderOpenPrice() > NormalizeDouble(TrailingStop0*Point*MyPoint,Digits))
            {
                if (OrderStopLoss() < NormalizeDouble(Bid-(TrailingStop0+3)*Point*MyPoint,Digits))
                    OrderModify(OrderTicket(), OrderOpenPrice(), NormalizeDouble(Bid-TrailingStop0*Point*MyPoint,Digits),NormalizeDouble( Bid+( TrailingStop0)*Point*MyPoint,Digits), OrderExpiration(), White);
               
              
            }
            if (OrderType() == OP_SELL && OrderOpenPrice() - Ask > NormalizeDouble(TrailingStop0*Point*MyPoint,Digits))
            {
                   
                if (OrderStopLoss() > NormalizeDouble(Ask+(TrailingStop0+3)*Point*MyPoint,Digits))

                    OrderModify(OrderTicket(), OrderOpenPrice(),NormalizeDouble( Ask+TrailingStop0*Point*MyPoint,Digits), NormalizeDouble(Ask-(TrailingStop0)*Point*MyPoint,Digits), OrderExpiration(), White);
            }
                                               
            
        }
    }
 
}

//===============================================================================================================

//============================================================================================
int MAMethod(int MA_Method)
   {
      switch(MA_Method)
        {
         case 0: return(0);                                   // MODE_SMA=0
         case 1: return(1);                                   // MODE_EMA=1
         case 2: return(2);                                   // MODE_SMMA=2
         case 3: return(3);                                  // MODE_LWMA=3
        }
   }
//============================================================================================
int MAPrice(int MA_Price)
   {
      switch(MA_Price)
        {
         case 0: return(PRICE_CLOSE);                        // PRICE_CLOSE=0        
         case 1: return(PRICE_OPEN);                         //PRICE_OPEN=1
         case 2: return(PRICE_HIGH);                         // PRICE_HIGH=2
         case 3: return(PRICE_LOW);                           // PRICE_LOW=3
         case 4: return(PRICE_MEDIAN);                        // PRICE_MEDIAN=4
         case 5: return(PRICE_TYPICAL);                       // PRICE_TYPICAL=5
         case 6: return(PRICE_WEIGHTED);                      // PRICE_WEIGHTED=6
        }
   }
   
//=============================================================================================   
void ShowERROR(int Ticket,double SL,double TP)
{
   int err=GetLastError();
   switch ( err )
   {                  
      case 1:                                                                               return;
      case 2:   Alert("Error Connection with trade server absent    ",Ticket," ",Symbol());return;
      case 3:   Alert("Error Invalid trade parameters   Ticket ",  Ticket," ",Symbol());return;
      case 130: Alert("Error Invalid stops   Ticket ",             Ticket," ",Symbol());return;
      case 134: Alert("Error Not enough money   ",                 Ticket," ",Symbol());return;
      case 146: Alert("Error Trade context is busy. ",             Ticket," ",Symbol());return;
      case 129: Alert("Error Invalid price ",                      Ticket," ",Symbol());return;
      case 131: Alert("Error Invalid volume ",                     Ticket," ",Symbol());return;
      case 4051:Alert("Error Invalid function parameter value ", Ticket," ",Symbol());return;
      case 4105:Alert("Error No order selected ",                Ticket," ",Symbol());return;
      case 4063:Alert("Error Integer parameter expected ",       Ticket," ",Symbol());return;
      case 4200:Alert("Error Îáúåêò óæå ñóùåñòâóåò ",            Ticket," ",Symbol());return;
      default:  Alert("Error Object already exists " ,err,"   Ticket ", Ticket," ",Symbol());return;
   }
}

//===================================================================================================
double LOT(int risk,int ord)
{  if (Use_Static_Lot==true&&Static_Lot>=MarketInfo(Symbol(),MODE_MINLOT))LOT=Static_Lot;
  else if(Use_Static_Lot==false&& risk>0)  {

   double MINLOT = MarketInfo(Symbol(),MODE_MINLOT);
   LOT = AccountFreeMargin()*risk/100/MarketInfo(Symbol(),MODE_MARGINREQUIRED)/ord;
   if (LOT>MarketInfo(Symbol(),MODE_MAXLOT)) LOT = MarketInfo(Symbol(),MODE_MAXLOT);
   if (LOT<MINLOT) LOT = MINLOT;
   if (MINLOT<0.1) LOT = NormalizeDouble(LOT,2); else LOT = NormalizeDouble(LOT,1);
   }
   else {Alert("Error You Have Choosed Wrong Lots ",Symbol());return;}
   return(LOT);
}
//===========================================================================================================

//========================================================================================================
void OPENORDER(string ord)
{
        int error;
  
  
   


   if (ord=="Buy" )Buy_StopLoss=NormalizeDouble(Ask-Static_SL*Point*MyPoint,Digits);
   if (ord=="Sell")Sell_StopLoss=NormalizeDouble(Bid+Static_SL*Point*MyPoint,Digits);
   
 
   
  
   if (ord=="Buy" )Buy_TakeProfit=NormalizeDouble(Ask+Static_TP*Point*MyPoint,Digits);
   if (ord=="Sell")Sell_TakeProfit=NormalizeDouble(Bid-Static_TP*Point*MyPoint,Digits);
   
   
   

 
  if (LastTradeBarTime == Time[0]) return(0);
  else {
 
   if (ord=="Buy" ) error=OrderSend(Symbol(),OP_BUY,LOT,Ask,4,Buy_StopLoss,Buy_TakeProfit,"FSF Semi COMBO Trader "+Symbol()+Period(),MagicNumber, 0,Blue);
   if (ord=="Sell") error=OrderSend(Symbol(),OP_SELL,LOT,Bid,4,Sell_StopLoss,Sell_TakeProfit,"FSF Semi COMBO Trader "+Symbol()+Period(),MagicNumber,0,Red);
   if (error==-1) //operation failed
   {  
      ShowERROR(error,0,0);
   }
    LastTradeBarTime = Time[0]; 
    
    
   if (error!=-1&&Send_Mail==True) SendMail("[Signal Alert]", "[" + Symbol() + "] " + DoubleToStr(Bid, Digits) + ord);   

   }
return;

}    
//===========================================================================================================
   
//Calculate==================================
void calculate ()
   {


if (Filtering_Noise==true||NOISE_TIMEFRAME_CLOSING==true){    
value1=iCustom(Symbol(),gettimeframe(NOISE_TIMEFRAME),"Damiani_volatmeter",0,0);
value2=iCustom(Symbol(),gettimeframe(NOISE_TIMEFRAME),"Damiani_volatmeter",1,0);
value3=iCustom(Symbol(),gettimeframe(NOISE_TIMEFRAME),"Damiani_volatmeter",2,0);
}    
if(USE_MACD){    //MACD
   if (Zerolag_MACD&&!Classic_MACD) {
   MACD_Main_0 = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift);
   MACD_Main_1 = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift+1);
   MACD_Signal_0 = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift);
   MACD_Signal_1 = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift+1);
   macdMain = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift);
   macdSignal = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift);
   macdMainPrev = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift+1);
   macdSignalPrev = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift+1);  
  } 
  else if (!Zerolag_MACD&&Classic_MACD) {
   MACD_Main_0 = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift);
   MACD_Main_1 = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift+1);
   MACD_Signal_0 = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift);
   MACD_Signal_1 = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift+1);
   macdMain = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift);
   macdSignal = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift);
   macdMainPrev = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift+1);
   macdSignalPrev = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift+1);  
  } 
  else Alert("You Have To choose Zerolag MACD Or Classic MACD");
} 
if(USE_MA){  
FastMaCurrent = iMA(NULL, gettimeframe(MA_TIMEFRAME), FastMa_Period, FastMa_Shift,MAMethod( FastMa_Method), MAPrice(FastMa_Price),FastMa_Shifting);
MidMaCurrent = iMA(NULL, gettimeframe(MA_TIMEFRAME), MidMa_Period, MidMa_Shift, MAMethod(MidMa_Method), MAPrice(MidMa_Price), MidMa_Shifting);
SlowMaCurrent = iMA(NULL, gettimeframe(MA_TIMEFRAME), SlowMa_Period, SlowMa_Shift, MAMethod(SlowMa_Method), MAPrice(SlowMa_Price), SlowMa_Shifting);

FastMaPrevious = iMA(NULL, gettimeframe(MA_TIMEFRAME), FastMa_Period, FastMa_Shift, MAMethod(FastMa_Method), MAPrice(FastMa_Price), FastMa_Shifting+1);
MidMaPrevious = iMA(NULL, gettimeframe(MA_TIMEFRAME), MidMa_Period, MidMa_Shift, MAMethod(MidMa_Method), MAPrice(MidMa_Price),MidMa_Shifting+1);
SlowMaPrevious = iMA(NULL, gettimeframe(MA_TIMEFRAME), SlowMa_Period, SlowMa_Shift, MAMethod(SlowMa_Method), MAPrice(SlowMa_Price),SlowMa_Shifting+1);

} 
   
if(USE_RSI){     //RSI
    RSIcurrent=iRSI(NULL,gettimeframe(RSI_TIMEFRAME),RSIperiod,PRICE_CLOSE,RSIShift);
   RSIprevious=iRSI(NULL,gettimeframe(RSI_TIMEFRAME),RSIperiod,PRICE_CLOSE,RSIShift+1);
  }
   
 if(USE_SAR){    //SAR
   SARSignal = iSAR(Symbol(),gettimeframe(SAR_TIMEFRAME),SARStep,SARMax,0);
}
   
  if(USE_STO){   //STOCHASTIC
    STOValue = iStochastic(Symbol(),gettimeframe(STO_TIMEFRAME),STOK,STOD,STOL,0,0,MODE_MAIN,0);
    STOSignal = iStochastic(Symbol(),gettimeframe(STO_TIMEFRAME),STOK,STOD,STOL,0,0,MODE_SIGNAL,0);
  }
    
  
 
if(USE_MACD_CLOSING){     
   //--------------------CLOSING VARIABLES---------------------------------------
   if (Zerolag_MACD&&!Classic_MACD) {
   CMACD_Main_0 = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift);
   CMACD_Main_1 = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift+1);
   CMACD_Signal_0 = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift);
   CMACD_Signal_1 = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift+1);
   CmacdMain = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift);
   CmacdSignal = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift);
   CmacdMainPrev = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift+1);
   CmacdSignalPrev = iCustom(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift+1);  
  } 
  else if (!Zerolag_MACD&&Classic_MACD) {
   CMACD_Main_0 = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift);
   CMACD_Main_1 = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift+1);
   CMACD_Signal_0 = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift);
   CMACD_Signal_1 = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift+1);
   CmacdMain = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift);
   CmacdSignal = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift);
   CmacdMainPrev = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,MACD_Shift+1);
   CmacdSignalPrev = iMACD(Symbol(),gettimeframe(MACD_TIMEFRAME_CLOSING),MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,MACD_Shift+1);  
  } 
  else Alert("You Have To choose Zerolag MACD Or Classic MACD");
} 
if(USE_MA_CLOSING){     
CFastMaCurrent = iMA(NULL, gettimeframe(MA_TIMEFRAME_CLOSING), FastMa_Period, FastMa_Shift,MAMethod( FastMa_Method), MAPrice(FastMa_Price),FastMa_Shifting);
CMidMaCurrent = iMA(NULL, gettimeframe(MA_TIMEFRAME_CLOSING), MidMa_Period, MidMa_Shift, MAMethod(MidMa_Method), MAPrice(MidMa_Price), MidMa_Shifting);
CSlowMaCurrent = iMA(NULL, gettimeframe(MA_TIMEFRAME_CLOSING), SlowMa_Period, SlowMa_Shift, MAMethod(SlowMa_Method), MAPrice(SlowMa_Price), SlowMa_Shifting);

CFastMaPrevious = iMA(NULL, gettimeframe(MA_TIMEFRAME_CLOSING), FastMa_Period, FastMa_Shift, MAMethod(FastMa_Method), MAPrice(FastMa_Price), FastMa_Shifting+1);
CMidMaPrevious = iMA(NULL, gettimeframe(MA_TIMEFRAME_CLOSING), MidMa_Period, MidMa_Shift, MAMethod(MidMa_Method), MAPrice(MidMa_Price),MidMa_Shifting+1);
CSlowMaPrevious = iMA(NULL, gettimeframe(MA_TIMEFRAME_CLOSING), SlowMa_Period, SlowMa_Shift, MAMethod(SlowMa_Method), MAPrice(SlowMa_Price),SlowMa_Shifting+1);
}
 
   
 if(USE_RSI_CLOSING){       //RSI
   CRSIcurrent=iRSI(NULL,gettimeframe(RSI_TIMEFRAME_CLOSING),RSIperiod,PRICE_CLOSE,0);
   CRSIprevious=iRSI(NULL,gettimeframe(RSI_TIMEFRAME_CLOSING),RSIperiod,PRICE_CLOSE,1);
  }
   
  if(USE_SAR_CLOSING){      //SAR
   CSARSignal = iSAR(Symbol(),gettimeframe(SAR_TIMEFRAME_CLOSING),SARStep,SARMax,0);

   }
 if(USE_STO_CLOSING){       //STOCHASTIC
    CSTOValue = iStochastic(Symbol(),gettimeframe(STO_TIMEFRAME_CLOSING),STOK,STOD,STOL,0,0,MODE_MAIN,0);
   CSTOSignal = iStochastic(Symbol(),gettimeframe(STO_TIMEFRAME_CLOSING),STOK,STOD,STOL,0,0,MODE_SIGNAL,0);
  }
    
  
   
   
   
   
   
   }
   
  
   
//MA Cross======================================================================================
int MA()
   {
   int PosOpen=0; 
   double curr,prev;
   curr=FastMaCurrent - MidMaCurrent;
  prev=FastMaPrevious - MidMaPrevious;                                  
   if((curr*prev<=0)&&(curr>0)) 
              
      {
      
      PosOpen=1;
      }  
   if((curr*prev<=0)&&(curr<0)) 
                      
      {
      PosOpen=2;
      }             
    if (USE_Last_MA_Signal){
            
            
            if (last_ma_signal!=PosOpen&&PosOpen>0){last_ma_signal=PosOpen;return(last_ma_signal);}
            else return(last_ma_signal);
            }
     else return(PosOpen);                                         
   }   
   
//MA2 Cross======================================================================================
int MA2()
   {
   
   int PosOpen=0; 
    double curr,prev;
   curr=MidMaCurrent - SlowMaCurrent;
  prev=MidMaPrevious - SlowMaPrevious;      
   if((curr*prev<=0)&&(curr>0)) 
                                                   
      {
      PosOpen=1;
      } 
   if((curr*prev<=0)&&(curr<0)) 
                      
      {
      PosOpen=2;
      }             
     if (USE_Last_MA_Signal){
            
            
            if (last_ma2_signal!=PosOpen&&PosOpen>0){last_ma2_signal=PosOpen;return(last_ma2_signal);}
            else return(last_ma2_signal);
            }
  else return(PosOpen);                                          
   }   
   
//MA3 Cross======================================================================================
int MA3()
   {
   
   int PosOpen=0;                                                 
   if (MA2()==1||MA()==1)    //buy signal
      {
      PosOpen=1;
      }                  
   if (MA2()==2||MA()==2) // sell signall
      {
      PosOpen=2;
      }             
     if (USE_Last_MA_Signal){
            
            
            if (last_ma3_signal!=PosOpen&&PosOpen>0){last_ma3_signal=PosOpen;return(last_ma3_signal);}
             else return(last_ma3_signal);
            }
   else return(PosOpen);                                         
   }   
      
   
//MA4 Cross======================================================================================
int MA4()
   {
   
   int PosOpen=0; 
    double curr,prev;
   curr=FastMaCurrent - SlowMaCurrent;
  prev=FastMaPrevious - SlowMaPrevious;   
   if((curr*prev<=0)&&(curr>0)) 
                                                   
      {
      PosOpen=1;
      } 
   if((curr*prev<=0)&&(curr<0)) 
                      
       {
      PosOpen=2;
      }             
     if (USE_Last_MA_Signal){
            
            
            if (last_ma4_signal!=PosOpen&&PosOpen>0){last_ma4_signal=PosOpen;return(last_ma4_signal);}
             else return(last_ma4_signal);
            }
   else return(PosOpen);                                         
   }   
//MA5 Cross======================================================================================
int MA5()
   {
   
   int PosOpen=0;                                                 
   if (MA3()==1||MA4()==1)    //buy signal
      {
      PosOpen=1;
      }                  
   if (MA3()==2||MA4()==2) // sell signall
      {
      PosOpen=2;
      }             
    if (USE_Last_MA_Signal){
           
            
            if (last_ma5_signal!=PosOpen&&PosOpen>0){last_ma5_signal=PosOpen;return(last_ma5_signal);}
             else return(last_ma5_signal);
            } 
   else return(PosOpen);                                         
   }   
      
   





//============================================================================================
//MACD TREND METHOD============================================================
int MACD()
   {
  int MACDTrend=0;
   
   
   
   if(MACD_Main_0>MACD_Main_1 && MACD_Signal_0>MACD_Signal_1 && MACD_Main_0>MACD_Signal_0) {MACDTrend = 1;}    //buy
   if(MACD_Main_0<MACD_Main_1 && MACD_Signal_0<MACD_Signal_1 && MACD_Main_0<MACD_Signal_0) {MACDTrend = 2;}    // sell
   if(MACD_Main_0<MACD_Main_1 && MACD_Signal_0>MACD_Signal_1 && MACD_Main_0>MACD_Signal_0) {MACDTrend = 3;}    // up-down
   if(MACD_Main_0>MACD_Main_1 && MACD_Signal_0<MACD_Signal_1 && MACD_Main_0<MACD_Signal_0) {MACDTrend = 4;}    // down-up
    if (USE_Last_MACD_Signal){
           
            
            if (last_macd_signal!=MACDTrend&&MACDTrend>0){last_macd_signal=MACDTrend;return(last_macd_signal);}
             else return(last_macd_signal);
            }
  else return(MACDTrend);
   }
//==MACD MODE 2=============================================================
int MACD2()
   {
   int MACDTrend = 0;   //1=Buy , 2= Sell


   if(macdMain>macdSignal && macdMainPrev<=macdSignalPrev && macdMain<0 && macdMainPrev<0){MACDTrend = 1;}
   else if(macdMain<macdSignal && macdMainPrev>=macdSignalPrev && macdMain>0 && macdMainPrev>0){MACDTrend = 2;}
   
     if (USE_Last_MACD_Signal){
            
            
            if (last_macd2_signal!=MACDTrend&&MACDTrend>0){last_macd2_signal=MACDTrend;return(last_macd2_signal);}
            else return(last_macd2_signal);
            }
   else return(MACDTrend);

      
   }   
//==MACD Both=============================================================
int MACD3()
   {
   int MACDTrend = 0;   //1=Buy , 2= Sell


   if((macdMain>macdSignal && macdMainPrev<=macdSignalPrev && macdMain<0 && macdMainPrev<0)
   ||(MACD_Main_0>MACD_Main_1 && MACD_Signal_0>MACD_Signal_1 && MACD_Main_0>MACD_Signal_0))
   {MACDTrend = 1;}
   else if((macdMain<macdSignal && macdMainPrev>=macdSignalPrev && macdMain>0 && macdMainPrev>0)
   ||(MACD_Main_0<MACD_Main_1 && MACD_Signal_0<MACD_Signal_1 && MACD_Main_0<MACD_Signal_0))
   {MACDTrend = 2;}
  
   if (USE_Last_MACD_Signal){
            
            
            if (last_macd3_signal!=MACDTrend&&MACDTrend>0){last_macd3_signal=MACDTrend;return(last_macd3_signal);}
            else return(last_macd3_signal);
            }
   else return(MACDTrend);

      
      
   }  
//==MACD Singal Crossing zero==========================================================
int MACD4()
   {
   int MACDTrend = 0;   //1=Buy , 2= Sell


   if(macdSignal>0 && macdSignalPrev<0)
   {MACDTrend = 1;}
   else if(macdSignal<0 && macdSignalPrev>0)
   {MACDTrend = 2;}
 
  if (USE_Last_MACD_Signal){
           
            
            if (last_macd4_signal!=MACDTrend&&MACDTrend>0){last_macd4_signal=MACDTrend;return(last_macd4_signal);}
            else return(last_macd4_signal);
            }
   else return(MACDTrend);

      
   }           
//RSI MODE overbought & oversold==============================================================  
int RSI()
   {   
      int RSITrend = 0;//1 = buy, 2 = sell
      
      if (RSIValue > RSISellOp){RSITrend = 2;}//SELL
      else if (RSIValue < RSIBuyOp){RSITrend = 1;}//BUY
      if (USE_Last_RSI_Signal){
            
            
            if (last_rsi_signal!=RSITrend&&RSITrend>0){last_rsi_signal=RSITrend;return(last_rsi_signal);}
             else return(last_rsi_signal);
            
            }
      else return(RSITrend);
    }  
//RSI MODE 2 trend=======================================================================    
int RSI2()
   {   
     int RSITrend = 0;//1 = buy, 2 = sell
 
     if ((RSIcurrent > RSIprevious)&&(Open[0]>Open[1])){RSITrend=1;}//BUY
     if ((RSIcurrent< RSIprevious)&&(Open[0]<Open[1])){RSITrend=2;}//SELL
       if (USE_Last_RSI_Signal){
            
            
            if (last_rsi2_signal!=RSITrend&&RSITrend>0){last_rsi2_signal=RSITrend;return(last_rsi2_signal);}
             else return(last_rsi2_signal);
            }
     else return(RSITrend);
     
   }  
//RSI MODE 2 trend=======================================================================    
int RSI3()
   {   
     int RSITrend = 0;//1 = buy, 2 = sell
 
     if (RSI2()==1||RSI()==1){RSITrend=1;}//BUY
     if (RSI2()==2||RSI()==2){RSITrend=2;}//SELL
       if (USE_Last_RSI_Signal){
           
            
            if (last_rsi3_signal!=RSITrend&&RSITrend>0){last_rsi3_signal=RSITrend;return(last_rsi3_signal);}
            else return(last_rsi3_signal);
            }
    else  return(RSITrend);
     
   } 
//====================================================================================     
//RSI Mode 4=======================================================================    
int RSI4()

   {   
     int RSITrend = 0;//1 = buy, 2 = sell
 
    if ((RSIcurrent > RSIprevious)&&(RSIcurrent>=RSIBuyZone)&&(RSIcurrent<=RSISellOp)){RSITrend=1;}//BUY
     if ((RSIcurrent< RSIprevious)&&(RSIcurrent<=RSISellZone)&&(RSIcurrent>=RSIBuyOp)){RSITrend=2;}//SELL
       if (USE_Last_RSI_Signal){
           
            
            if (last_rsi4_signal!=RSITrend&&RSITrend>0){last_rsi4_signal=RSITrend;return(last_rsi4_signal);}
            else return(last_rsi4_signal);
            }
    else  return(RSITrend);
     
   }   
   
//SAR ===================================================================================
int SAR()
   {  
  
      int SARTrend = 0;   //1 = buy, 2 = sell
      if (SARSignal<Close[1]){SARTrend = 1;}
      else{SARTrend = 2;}
        
         if (USE_Last_SAR_Signal){
            
            
            if (last_sar_signal!=SARTrend&&SARTrend>0){last_sar_signal=SARTrend;return(last_sar_signal);}
             else return(last_sar_signal);
            }
     
     else return (SARTrend); 
   }     
 
   
//STOCHASTIC=======================================================================================
int STO()
   {    
     int STOTrend = 0; //1 = buy, 2 = sell
      if (useSTOHighLow == 1)
      {
         if (STOValue > STOSignal && STOValue > STOHigh){STOTrend = 1;}
         if (STOValue < STOSignal && STOValue < STOLow){STOTrend = 2;}
         else {STOTrend=0;}
      }
      else
      {
         if (STOValue > STOSignal){STOTrend = 1;}
         else if (STOValue < STOSignal) {STOTrend = 2;}
         else{STOTrend=0;}
      }
       if (USE_Last_STO_Signal){
            
            
            if (last_sto_signal!=STOTrend&&STOTrend>0){last_sto_signal=STOTrend;return(last_sto_signal);}
              else return(last_sto_signal);
            }
   else return(STOTrend);   
}    

//CALCULATE SIGNALS=======================================================================================
int Signal()
  {
   int Trend; //1 = buy, 2 = sell
   int s=0;
   int select[5];
   int sig[5];
   select[0]=USE_RSI;
   select[1]=USE_STO;
   select[2]=USE_SAR;
   select[3]=USE_MA;
   select[4]=USE_MACD;
    
notused=0;
uptrend=0;
downtrend=0;
countoption=0;  
   
   
for(int i=0;i<5;i++)
{
if (select[i]==1)
   {
      if (i==0){
                   switch(RSI_MODE)
                     {
                     case 1: sig[i]=RSI();                               
                     case 2: sig[i]=RSI2();                        
                     case 3: sig[i]=RSI3();
                     case 4: sig[i]=RSI4();                        
                     }
               }
            
     
      if (i==1){sig[i]=STO();}
      if (i==2){sig[i]=SAR();}
      if (i==3)   {
      
      switch(MA_MODE)
                     {
                     case 1: sig[i]=MA();                               
                     case 2: sig[i]=MA2();                                                     
                     case 3: sig[i]=MA3(); 
                     case 4: sig[i]=MA4();                                                     
                     case 5: sig[i]=MA5();                          
                     }
      
      
      
                  }
      
      
      
      
      
      if (i==4)
                     {
                     switch(MACD_MODE)
                     {
                     case 1: sig[i]=MACD();                               
                     case 2: sig[i]=MACD2();                        
                     case 3: sig[i]=MACD3();   
                     case 4: sig[i]=MACD4();                   
                     }
                     }
      s++;}
}
   
   

     

   //CHECK FOR SELECTED MODES SIGNALS
 for(i=0;i<5;i++) 
 {
     // if (i==2) break;
      if (sig[i]==2){downtrend++;} 
      
      if (sig[i]==1){uptrend++;} 
    //  if (modes[i]==0)notused++;
      
    
 }//for(o=0;o<5;o++) 
 

if (uptrend==SelectedCount()&&SelectedCount()>0)Trend=1;
if (downtrend==SelectedCount()&&SelectedCount()>0 ) Trend=2;

   



return(Trend);
 } 
 //==========================================================================
 //CALCULATE CLOSING SIGNALS=======================================================================================
int CloseSignal()
  {
   int Trend; //1 = buy, 2 = sell
   int s=0;
   int select[5];
   int sig[5];
   select[0]=USE_RSI_CLOSING;
   select[1]=USE_STO_CLOSING;
   select[2]=USE_SAR_CLOSING;
   select[3]=USE_MA_CLOSING;
   select[4]=USE_MACD_CLOSING;
   
    
notused=0;
uptrend=0;
downtrend=0;
countoption=0;  
   
   
for(int i=0;i<5;i++)
{
if (select[i]==1)
   {
      if (i==0){
                   switch(RSI_MODE_CLOSING)
                     {
                     case 1: sig[i]=CRSI();                               
                     case 2: sig[i]=CRSI2();                        
                     case 3: sig[i]=CRSI3();
                     case 4: sig[i]=CRSI4();                        
                     }
               }
            
     
      if (i==1){sig[i]=CSTO();}
      if (i==2){sig[i]=CSAR();}
      if (i==3)   {
      
      switch(MA_MODE_CLOSING)
                     {
                     case 1: sig[i]=CMA();                               
                     case 2: sig[i]=CMA2();                        
                     case 3: sig[i]=CMA3();   
                     case 4: sig[i]=CMA4();                                                     
                     case 5: sig[i]=CMA5();                         
                     }
      
      
      
                  }
      
      
      
      
      
      if (i==4)
                     {
                     switch(MACD_MODE_CLOSING)
                     {
                     case 1: sig[i]=CMACD();                               
                     case 2: sig[i]=CMACD2();                        
                     case 3: sig[i]=CMACD3(); 
                     case 4: sig[i]=CMACD4();                        
                     }
                     }
                    
      s++;}
}
   
   

     

   //CHECK FOR SELECTED MODES SIGNALS
 for(i=0;i<5;i++) 
 {
     // if (i==2) break;
      if (sig[i]==2){downtrend++;} 
      
      if (sig[i]==1){uptrend++;} 
    //  if (modes[i]==0)notused++;
      
    
 }//for(o=0;o<5;o++) 
 

if (uptrend==closeSelectedCount()&&closeSelectedCount()>0)Trend=1;
if (downtrend==closeSelectedCount()&&closeSelectedCount()>0 ) Trend=2;
  
  

   



return(Trend);
 } 
 //==========================================================================
int SelectedCount()
{
int s=0;
int select[5];
select[0]=USE_RSI;
select[1]=USE_STO;
select[2]=USE_SAR;
select[3]=USE_MA;
select[4]=USE_MACD;
for(int i=0;i<5;i++)
{
if (select[i]==1){s++;}
}
return(s);
}
int closeSelectedCount()
{
int s=0;
int select[5];
select[0]=USE_RSI_CLOSING;
   select[1]=USE_STO_CLOSING;
   select[2]=USE_SAR_CLOSING;
   select[3]=USE_MA_CLOSING;
   select[4]=USE_MACD_CLOSING;
for(int i=0;i<5;i++)
{
if (select[i]==true){s++;}
}
return(s);
}
//===========================================================================

void display()
   {
   
      if (IsTesting()==false &&IsOptimization()==false){

        if (canbuy) clabel("mainsignal",500,20,"Main Signal : Up-Buy",Blue);
            else if (cansell)clabel("mainsignal",500,20,"Main Signal: Down-Sell",Red);
            else clabel("mainsignal",500,20,"Main Signal:Please Wait For Right Signal",White);
            
         if (CloseSignal()==1) clabel("closesignal",500,30,"Closing Signal : You Can Close Sell Positions",Blue);
            else if (CloseSignal()==2)clabel("closesignal",500,30,"Closing Signal: You Can Close Buy Positions",Red);
            else clabel("closesignal",500,30,"Closing Signal:Please Wait For Right Signal",White);
            
         if (MA()==1)clabel("masignal",500,40,"Moving Average Mode 1 Signal : Up-Buy",Blue);
            else if (MA()==2)clabel("masignal",500,40,"Moving Average Mode 1 Signal :Down-Sell",Red);
            else clabel("masignal",500,40,"Moving Average Mode 1 Signal :Please Wait For Right Signal",White);
            
       if (MA2()==1)clabel("ma2signal",500,50,"Moving Average Mode 2 Signal : Up-Buy",Blue);
            else if (MA2()==2)clabel("ma2signal",500,50,"Moving Average Mode 2 Signal :Down-Sell",Red);
            else clabel("ma2signal",500,50,"Moving Average Mode 2 Signal :Please Wait For Right Signal",White);
            
       if (MA3()==1)clabel("ma3signal",500,60,"Moving Average Mode 3 Signal : Up-Buy",Blue);
            else if (MA3()==2)clabel("ma3signal",500,60,"Moving Average Mode 3 Signal :Down-Sell",Red);
            else clabel("ma3signal",500,60,"Moving Average Mode 3 Signal :Please Wait For Right Signal",White);
       
       if (MA4()==1)clabel("ma4signal",500,70,"Moving Average Mode 4 Signal : Up-Buy",Blue);
            else if (MA3()==2)clabel("ma4signal",500,70,"Moving Average Mode 4 Signal :Down-Sell",Red);
            else clabel("ma4signal",500,70,"Moving Average Mode 4 Signal :Please Wait For Right Signal",White);
       
       if (MA5()==1)clabel("ma5signal",500,80,"Moving Average Mode 5 Signal : Up-Buy",Blue);
            else if (MA3()==2)clabel("ma5signal",500,80,"Moving Average Mode 5 Signal :Down-Sell",Red);
            else clabel("ma5signal",500,80,"Moving Average Mode 5 Signal :Please Wait For Right Signal",White);
       
       
      

           if (MACD()==1)clabel("macdsignal",500,90,"MACD Mode 1 Signal : Up-Buy",Blue);
            else if (MACD()==2)clabel("macdsignal",500,90,"MACD Mode 1 Signal : Down-Sell",Red);
             else if (MACD()==3)clabel("macdsignal",500,90,"MACD Mode 1 Signal : Up->Down",Red);
              else if (MACD()==4)clabel("macdsignal",500,90,"MACD Mode 1 Signal : Down->Up",Red);
            else  clabel("macdsignal",500,90,"MACD Mode 1 Signal :Please Wait For Right Signal",White);

         if (MACD2()==1)clabel("macd2signal",500,100,"MACD Mode 2 Signal : Up-Buy",Blue);
            else if (MACD2()==2)clabel("macd2signal",500,100,"MACD Mode 2 Signal : Down-Sell",Red);
            else  clabel("macd2signal",500,100,"MACD Mode 2 Signal :Please Wait For Right Signal",White);

          if (MACD3()==1)clabel("macd3signal",500,110,"MACD Mode 3 Signal : Up-Buy",Blue);
            else if (MACD3()==2)clabel("macd3signal",500,110,"MACD Mode 3 Signal : Down-Sell",Red);
            else  clabel("macd3signal",500,110,"MACD Mode 3 Signal :Please Wait For Right Signal",White);
         
         if (MACD4()==1)clabel("macd4signal",500,120,"MACD Mode 4 Signal : Up-Buy",Blue);
            else if (MACD4()==2)clabel("macd4signal",500,120,"MACD Mode 4 Signal : Down-Sell",Red);
            else  clabel("macd4signal",500,120,"MACD Mode 4 Signal :Please Wait For Right Signal",White);


         if (RSI()==1)clabel("rsisignal",500,130,"RSI Mode 1 Signal : Up-Buy",Blue);
            else if (RSI()==2)clabel("rsisignal",500,130,"RSI Mode 1 Signal : Down-Sell",Red);
            else clabel("rsisignal",500,130,"RSI Mode 1 Signal :Please Wait For Right Signal",White);
         
          if (RSI2()==1)clabel("rsisignal2",500,140,"RSI Mode 2 Signal : Up-Buy",Blue);
            else if (RSI2()==2)clabel("rsisignal2",500,140,"RSI Mode 2 Signal : Down-Sell",Red);
            else clabel("rsisignal2",500,140,"RSI Mode 2 Signal :Please Wait For Right Signal",White);
          
          if (RSI3()==1)clabel("rsisignal3",500,150,"RSI Mode 3 Signal : Up-Buy",Blue);
            else if (RSI2()==2)clabel("rsisignal3",500,150,"RSI Mode 3 Signal : Down-Sell",Red);
            else clabel("rsisignal3",500,150,"RSI Mode 3 Signal :Please Wait For Right Signal",White);
           
          if (RSI4()==1)clabel("rsisignal4",500,160,"RSI Mode 4 Signal : Up-Buy",Blue);
            else if (RSI2()==2)clabel("rsisignal4",500,160,"RSI Mode 4 Signal : Down-Sell",Red);
            else clabel("rsisignal4",500,160,"RSI Mode 4 Signal :Please Wait For Right Signal",White);
          
           
         
            
            
         if (STO()==1)clabel("stosignal",500,170,"STO Signal : Up-Buy",Blue);
         else if (STO()==2)clabel("stosignal",500,170,"STO Signal : Down-Sell",Red);
         else clabel("stosignal",500,170,"STO Signal :Please Wait For Right Signal",White);
         if (SAR()==1)clabel("sarsignal",500,180,"SAR Signal : Up-Buy",Blue);
         else if (SAR()==2)clabel("sarsignal",500,180,"SAR Signal : Down-Sell",Red);
         else clabel("sarsignal",500,180,"SAR Signal :Please Wait For Right Signal",White);
        
      
         if (trend_detect()==1)clabel("trend",500,190,"Trend : Bullish",Blue);
         else if (trend_detect()==2)clabel("trend",500,190,"Trend: Bearish",Red);
         else if (trend_detect()==4)clabel("trend",500,190,"Trend : Range->Bullish",DodgerBlue);
         else if (trend_detect()==3)clabel("trend",500,190,"Trend: Range->Bearish",Maroon);
      if (signalTRIX()==1)clabel("noise",500,200,"Noise : Don\'t Trade, Trend Looks Like a Bad Range",Red);
     if (signalTRIX()==0)clabel("noise",500,200,"Noise : You Can Trade Now ",Green);

 Comment("\n\n FSF Semi Combo Trader (BETA)\n",
            "Copyright © 2010,Farshad Saremifar , Farshad.Saremifar@gmail.com\n",
            "Forex Account Server:",AccountServer(),"\n",
            "Account Balance:  $",AccountBalance(),"\n",
            "Lots:  ",DoubleToStr(LOT,3),"\n",
            "Symbol: ", Symbol(),"\n",
            "Pip Spread:  ",MarketInfo(Symbol(),MODE_SPREAD),"\n",
            " Server Time: ",Hour(),":",Minute(),":",Seconds(),"\n",
            "Minimum Lot Size: ",MarketInfo(Symbol(),MODE_MINLOT),"\n",
       
            "You Have Choosed: ",SelectedCount()," Option(s)","\n",
            "If You make too much money with this EA - some gift or donations accepted [:-)");
     
      }
      
    
      
      
}  
///-------------------------------------------------------------------------

void clabel(string lblname,int x,int y,
string txt,color txtcolor){
 if (ObjectFind( lblname)>0) ObjectDelete(lblname);
ObjectCreate(lblname, OBJ_LABEL,0, 0, 0);
ObjectSet(lblname, OBJPROP_CORNER, 0);
ObjectSetText(lblname,txt,7,"Verdana", txtcolor);
ObjectSet(lblname, OBJPROP_XDISTANCE, x);
ObjectSet(lblname, OBJPROP_YDISTANCE, y);


}

int trend_detect(){
if (adxSignal(ADX_PERIOD,ADX_MAINLEVEL,0,gettimeframe(ADX_TIMEFRAME))==0)return(3);//range-down
if (adxSignal(ADX_PERIOD,ADX_MAINLEVEL,0,gettimeframe(ADX_TIMEFRAME))==1)return(4);//range-up
if (adxSignal(ADX_PERIOD,ADX_MAINLEVEL,0,gettimeframe(ADX_TIMEFRAME))==2)return(1);//up
if (adxSignal(ADX_PERIOD,ADX_MAINLEVEL,0,gettimeframe(ADX_TIMEFRAME))==3)return(2);//down



else return(0);


}


int adxSignal(int adx_Period, int adx_Main_Level,int shift,int timeframe)
{
   double adxMain = iADX(NULL,timeframe,adx_Period,PRICE_CLOSE,MODE_MAIN,shift);
   double adxPlus = iADX(NULL,timeframe,adx_Period,PRICE_CLOSE,MODE_PLUSDI,shift);
   double adxMinus = iADX(NULL,timeframe,adx_Period,PRICE_CLOSE,MODE_MINUSDI,shift);
   
   int result;
   
   
   if(adxMain < adxPlus && adxMain < adxMinus)
   {
      
      result = 0;
   }
   else if(adxMain < adx_Main_Level)
   {
      
      result = 1;
   }
   else 
   {
      if(adxPlus > adxMinus)
      {
        
         result = 2;
      }
      else if(adxPlus < adxMinus)
      {
         
         result = 3;
      }
   }
   

 return(result);  
}  
int signalTRIX()
{
int signal=0;



if ((value1>value3))signal=1;       
else signal=0;
return(signal);


}
// ------
int Save_Profit(){
if (AccountProfit()>= Profit_to_Close)
   {
    for(int i=OrdersTotal()-1;i>=0;i--)
       {
       OrderSelect(i, SELECT_BY_POS);
       int type   = OrderType();
               
       bool result = false;
              
       switch(type)
          {
          //Close opened long positions
          case OP_BUY  : result = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),3,Pink);
                         break;
               
          //Close opened short positions
          case OP_SELL : result = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),3,Pink);
                          
          }
          
       if(result == false)
          {
            Sleep(3000);
          }
      else if (Cancel_Trading_On_Profit) cantrade=false;     
       }
      Print ("Account Profit Reached. All Open Trades Have Been Closed");
      return(0);
   }  
   


return(0);

}
bool checkcanTrading()
{
int    orders=HistoryTotal(); 
if (cantrade==false)
      {
         
            for(int i=orders-1;i>=0;i--)
                  {
                      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
                      if(OrderSymbol()!=Symbol() || OrderMagicNumber() != MagicNumber) continue;
                      if(TimeDay(OrderCloseTime())!=TimeDay(TimeCurrent())) {cantrade=true; break;}
                      else {cantrade=false; break;}
                  }    
           
      
      
      }
      
 return(cantrade);     

}
int Save_Loss(){
if (AccountProfit()<0-Loss_to_Close)
   {
    for(int i=OrdersTotal()-1;i>=0;i--)
       {
       OrderSelect(i, SELECT_BY_POS);
       int type   = OrderType();
               
       bool result = false;
              
       switch(type)
          {
          //Close opened long positions
          case OP_BUY  : result = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),3,Pink);
                         break;
               
          //Close opened short positions
          case OP_SELL : result = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),3,Pink);
                          
          }
          
       if(result == false)
          {
            Sleep(3000);
          }
      else if (Cancel_Trading_On_Loss) cantrade=false;     
       }
      Print ("Account Loss Reached. All Open Trades Have Been Closed");
      return(0);
   }  
   


return(0);

}

int bband(){
datetime Bar,sBar;int buy,sell,i,a,b,nbb3ok,vbb3ok,nbar;double Sstoploss,stoploss,setup2,okbuy,oksell;

buy=0;sell=0;

   double vbb1 =iBands(NULL,gettimeframe(BBAND_TIMEFRAME),BB1_Period,1,0,PRICE_CLOSE,MODE_UPPER,1);
   double vbb2 =iBands(NULL,gettimeframe(BBAND_TIMEFRAME),BB2_Period,2,0,PRICE_CLOSE,MODE_UPPER,1);
   double vbb3 =iBands(NULL,gettimeframe(BBAND_TIMEFRAME),BB3_Period,3,0,PRICE_CLOSE,MODE_UPPER,1);
   double nbb1 =iBands(NULL,gettimeframe(BBAND_TIMEFRAME),BB1_Period,1,0,PRICE_CLOSE,MODE_LOWER,1);
   double nbb2 =iBands(NULL,gettimeframe(BBAND_TIMEFRAME),BB2_Period,2,0,PRICE_CLOSE,MODE_LOWER,1);
   double nbb3 =iBands(NULL,gettimeframe(BBAND_TIMEFRAME),BB3_Period,3,0,PRICE_CLOSE,MODE_LOWER,1);
  
  
  if(Low[1]<=nbb3 )
  { okbuy=0;  Bar=Time[1];
  
      for( i=Range_Parameter;i>0;i--)
      {
      double rsi=iRSI(NULL,gettimeframe(RSI_TIMEFRAME),RSIperiod,PRICE_CLOSE,i);
      if(rsi<30){nbb3ok=1;}
      }
  }
  
 
  if(okbuy==0 && nbb3ok==1 && Close[1]>nbb2 && buy==0)
  {okbuy=1;nbb3ok=0;a=0;
  // stoplossbb=Low[iLowest(NULL,0,MODE_LOW,iBarShift(NULL,0,Bar,FALSE),1)]-Step_For_Loss*Point;
//   stoplossbbuy=Low[iLowest(NULL,0,MODE_HIGH,Step_For_Loss,1)];
   return(1);
  
  }
  

  
  /**********************************************sell**********************************************************/
  
    if(High[1]>=vbb3 )
  { oksell=0;  sBar=Time[1];
  
      for( i=Range_Parameter;i>0;i--)
      {
      rsi=iRSI(NULL,0,RSIperiod,PRICE_CLOSE,i);
      if(rsi>70){vbb3ok=1;}
      }
  }
 
  if(oksell==0 && vbb3ok==1 && Close[1]<vbb2 && sell==0)
  {oksell=1;vbb3ok=0;b=0;
  //stoplossbb=High[iHighest(NULL,0,MODE_HIGH,iBarShift(NULL,0,sBar,FALSE),1)]+Step_For_Loss*Point;
  //stoplossbsell=High[iHighest(NULL,0,MODE_HIGH,Step_For_Loss,1)];
   return(2);
  }
  
 
 
}




//+----
int gettimeframe(int time){

switch(time)
                     {
                     case 1: return(1);                               
                     case 2: return(5);                   
                     case 3: return(15);                               
                     case 4: return(30);                   
                     case 5: return(60);                               
                     case 6: return(240);
                     case 7: return(1440);                   
                     }

}


//MA Cross======================================================================================
int CMA()
   {
   int PosOpen=0;      
   double curr,prev;
   curr=CFastMaCurrent - CMidMaCurrent;
  prev=CFastMaPrevious - CMidMaPrevious;   
                             
   if((curr*prev<=0)&&(curr>0)) 
              
      {
      PosOpen=1;
      }  
   if((curr*prev<=0)&&(curr<0)) 
                      
      {
      PosOpen=2;
      }             
    
   return(PosOpen);                                         
   }   
   
//MA2 Cross======================================================================================
int CMA2()
   {
   double curr,prev;
   curr=CMidMaCurrent - CSlowMaCurrent;
  prev=CMidMaPrevious - CSlowMaPrevious;   
   int PosOpen=0; 
   if((curr*prev<=0)&&(curr>0)) 
                                                   
      {
      PosOpen=1;
      } 
   if((curr*prev<=0)&&(curr<0)) 
                      
      {
      PosOpen=2;
      }             
    
   return(PosOpen);                                         
   }   
   
//MA3 Cross======================================================================================
int CMA3()
   {
   
   int PosOpen=0;                                                 
   if (CMA2()==1||CMA()==1)    //buy signal
      {
      PosOpen=1;
      }                  
   if (CMA2()==2||CMA()==2) // sell signall
      {
      PosOpen=2;
      }             
    
   return(PosOpen);                                         
   }   
      
   
//MA4 Cross======================================================================================
int CMA4()
   {
   double curr,prev;
   curr=CFastMaCurrent - CSlowMaCurrent;
  prev=CFastMaPrevious - CSlowMaPrevious; 
   int PosOpen=0; 
   if((curr*prev<=0)&&(curr>0)) 
                                                   
      {
      PosOpen=1;
      } 
   if((curr*prev<=0)&&(curr<0)) 
                      
       {
      PosOpen=2;
      }             
    
   return(PosOpen);                                         
   }   
//MA5 Cross======================================================================================
int CMA5()
   {
   
   int PosOpen=0;                                                 
   if (CMA3()==1||CMA4()==1)    //buy signal
      {
      PosOpen=1;
      }                  
   if (CMA3()==2||CMA4()==2) // sell signall
      {
      PosOpen=2;
      }             
    
   return(PosOpen);                                         
   }   
      
   
//MA4 Cross======================================================================================   
//Signals============================================================================================



//============================================================================================
//MACD TREND METHOD============================================================
int CMACD()
   {
  int CMACDTrend=0;
   
   
   
   if(CMACD_Main_0>CMACD_Main_1 && CMACD_Signal_0>CMACD_Signal_1 && CMACD_Main_0>CMACD_Signal_0) {CMACDTrend = 1;}    //buy
   if(CMACD_Main_0<CMACD_Main_1 && CMACD_Signal_0<CMACD_Signal_1 && CMACD_Main_0<CMACD_Signal_0) {CMACDTrend = 2;}    // sell
   if(CMACD_Main_0<CMACD_Main_1 && CMACD_Signal_0>CMACD_Signal_1 && CMACD_Main_0>CMACD_Signal_0) {CMACDTrend = 3;}    // up-down
   if(CMACD_Main_0>CMACD_Main_1 && CMACD_Signal_0<CMACD_Signal_1 && CMACD_Main_0<CMACD_Signal_0) {CMACDTrend = 4;}    // down-up
   return(CMACDTrend);
   }
//==CMACD MODE 2=============================================================
int CMACD2()
   {
   int CMACDTrend = 0;   //1=Buy , 2= Sell


   if(CmacdMain>CmacdSignal && CmacdMainPrev<=CmacdSignalPrev && CmacdMain<0 && CmacdMainPrev<0){CMACDTrend = 1;}
   else if(CmacdMain<CmacdSignal && CmacdMainPrev>=CmacdSignalPrev && CmacdMain>0 && CmacdMainPrev>0){CMACDTrend = 2;}
   else { CMACDTrend=0;}
   return(CMACDTrend);

      
   }   
//==MACD Both=============================================================
int CMACD3()
   {
   int CMACDTrend = 0;   //1=Buy , 2= Sell


   if((CmacdMain>CmacdSignal && CmacdMainPrev<=CmacdSignalPrev && CmacdMain<0 && CmacdMainPrev<0)
   ||(CMACD_Main_0>CMACD_Main_1 && CMACD_Signal_0>CMACD_Signal_1 && CMACD_Main_0>CMACD_Signal_0))
   {CMACDTrend = 1;}
   else if((CmacdMain<CmacdSignal && CmacdMainPrev>=CmacdSignalPrev && CmacdMain>0 && CmacdMainPrev>0)
   ||(CMACD_Main_0<CMACD_Main_1 && CMACD_Signal_0<CMACD_Signal_1 && CMACD_Main_0<CMACD_Signal_0))
   {CMACDTrend = 2;}
   else { CMACDTrend=0;}
   return(CMACDTrend);

      
      
   }  
//==CMACD Singal Crossing zero==========================================================
int CMACD4()
   {
  int CMACDTrend = 0;   //1=Buy , 2= Sell


   if(CmacdSignal>0 && CmacdSignalPrev<0)
   {CMACDTrend = 1;}
   else if(CmacdSignal<0 && CmacdSignalPrev>0)
   {CMACDTrend = 2;}
  else { CMACDTrend=0;}
   return(CMACDTrend);

      
   }           
//RSI MODE overbought & oversold==============================================================  
int CRSI()
   {   
      int RSITrend = 0;//1 = buy, 2 = sell
      
      if (CRSIValue > RSISellOp){RSITrend = 2;}//SELL
      else if (CRSIValue < RSIBuyOp){RSITrend = 1;}//BUY
      
      return(RSITrend);
    }  
//RSI MODE 2 trend=======================================================================    
int CRSI2()
   {   
     int RSITrend = 0;//1 = buy, 2 = sell
 
     if ((CRSIcurrent > CRSIprevious)&&(Open[0]>Open[1])){RSITrend=1;}//BUY
     if ((CRSIcurrent< CRSIprevious)&&(Open[0]<Open[1])){RSITrend=2;}//SELL
   
     return(RSITrend);
     
   }  
//RSI MODE 2 trend=======================================================================    
int CRSI3()
   {   
     int RSITrend = 0;//1 = buy, 2 = sell
 
     if (CRSI2()==1||CRSI()==1){RSITrend=1;}//BUY
     if (CRSI2()==2||CRSI()==2){RSITrend=2;}//SELL
   
     return(RSITrend);
     
   } 
//====================================================================================     
//RSI Mode 4=======================================================================    
int CRSI4()

   {   
     int RSITrend = 0;//1 = buy, 2 = sell
  if ((CRSIcurrent > CRSIprevious)&&(CRSIcurrent>=RSIBuyZone)&&(CRSIcurrent<=RSISellOp)){RSITrend=1;}//BUY
     if ((CRSIcurrent< CRSIprevious)&&(CRSIcurrent<=RSISellZone)&&(CRSIcurrent>=RSIBuyOp)){RSITrend=2;}//SELL
     
    
     return(RSITrend);
     
   }   
   
//SAR ===================================================================================
int CSAR()
   {  
  
      int SARTrend = 0;   //1 = buy, 2 = sell
      if (CSARSignal<Close[1]){SARTrend = 1;}
      else{SARTrend = 2;}
     
     
     return (SARTrend); 
   }     
 
   
//STOCHASTIC=======================================================================================
int CSTO()
   {    
     int STOTrend = 0; //1 = buy, 2 = sell
      if (useSTOHighLow == 1)
      {
         if (CSTOValue > CSTOSignal && CSTOValue > STOHigh){STOTrend = 1;}
         if (CSTOValue < CSTOSignal && CSTOValue < STOLow){STOTrend = 2;}
         
      }
      else
      {
         if (CSTOValue > CSTOSignal){STOTrend = 1;}
         else if (CSTOValue < CSTOSignal) {STOTrend = 2;}
         
      }
   return(STOTrend);   
}    


