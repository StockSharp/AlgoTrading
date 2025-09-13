//+------------------------------------------------------------------+
//|                                                     Combo EA.mq4 |
//|                                        Copyright © 2010, Farshad |
//|                                           http://www.vizhish.com |
//+------------------------------------------------------------------+

#property copyright "Copyright © 2010, Farshad"
#property link      "farshad.saremifar@gmail.com"
extern string   MA_Cross = "Select 1 if do you want to use";
extern string    MA_Cross2 = "Moving Average Cross trend signal";
extern string    MA_Cross3 = "Ma Mode:1 to 5";
extern int    USE_MA = 1;//option 4
extern int    MA_MODE = 5;//option 4

extern int    MA1_Period=5;                             
extern int    MA2_Period=13;
extern int    MA3_Period=62;                             
extern int CrossBuffer1 = 14; //based by atr period 
extern int CrossBuffer2 = 14;//for mode 2 based by atr period 
 
extern string MA_Cross4 = "(SMA=0,EMA=1,SMMA=2,LWMA=3)";                           
extern int    MA1_Method=1;                              //  (SMA=0,EMA=1,SMMA=2,LWMA=3)
extern int    MA2_Method=1;  
extern int    MA3_Method=1;                              //  (SMA=0,EMA=1,SMMA=2,LWMA=3)
                           
extern int    MA1_Price=0;                         
extern int    MA2_Price=0;  
extern int    MA3_Price=0;                             
extern int    MA1_Shift=0;                         
extern int    MA2_Shift=0;   
extern int    MA3_Shift=0;      
extern string RSI_DES = "Select 1 if do you want to use RSI to check the trend signal Buy op for overbought";
extern int    USE_RSI=1;//option 1
extern int    RSIperiod=21;
extern string    RSI_DES2 = "Select RSI MODE :1-4";
extern string    RSI_DES3 = "1 FOR OVERBOGHT/OVERSOLD -2 FOR RSI TREND 3-BOTH 1&2 4-FOR RSI AT THE ZONE";

extern int    RSI_MODE = 1;
extern string    RSI_DES4 = "High / Low to determine the trend:This is for Overbought/Oversold zone";
extern string    RSI_DES5 = "For RSI MODE 1 & 3";
extern int    RSIBuyOp          = 12;     
extern int    RSISellOp         = 88;
extern string RSI_DES6 = "High / Low to determine the trend:zone for buy and sell";
extern string    RSI_DES7 = "For RSI MODE 4";
extern int    RSIBuyZone          = 55;     
extern int    RSISellZone         = 45;

extern string    MACD_1 = "Select true if do you want to use";
extern string    MACD_2 = "MACD trend signal , two mode 0=trend mode 1=mode 2";
extern int     USE_MACD = 1;//option 5
extern int     MACD_Fast = 12;
extern int     MACD_Slow = 24;
extern int     Signal_Period = 9;
extern int     MACD_Price = 0;              // PRICE_CLOSE
extern int     MACD_Shift = 0;
extern int     MACD_MODE = 2;

extern string    STO1_DES = "Select 1  if do you want to use Stochastic to check the trend signal";
extern int      USE_STO = 1;//option 2
extern int       STOK=5;
extern int       STOD=3;
extern int       STOL=3;
extern string    STO2_DES = "Select 1 if do you want to use Stochastic";
extern string    STO3_DES = "High / Low to determine the trend";
extern int      useSTOHighLow = 0;
extern int       STOHigh=80;
extern int       STOLow=20;
extern string    SAR_DES = "Select 1 if do you want to use Parabolic SARS to check the trend signal";
extern int      USE_SAR = 1;//option 3
extern double    SARStep = 0.02;
extern double    SARMax = 0.2;

extern string    Desc = "END OF MODES";
extern int       useTrailingStop = 0;
extern int       TrailingStop = 198;

extern string  _Money_management    = "Select Money Management";
extern bool Use_Static_Lot=false;
extern double Static_Lot=0.1;
extern double  // stoploss             = 150,
               //  takeprofit           = 350,
               
                  Risk                 = 5,
                  Max_orders           =10,
                  Max_same_orders      =5;
extern string  _SL_TP   = "This system use ATR to Set TP&SL";
                  
extern int      ATR_PERIOD= 191;   
extern int      ATR_Multiple=7;
extern string  _Trade_Control    = "Select Money Management";
 
extern int      AutoClose =1;
extern int      Open_opposite_after_close =0;
extern string  _Auto_Close_    = "Auto_Close_signaling";

extern int      USE_MA_CLOSING =0;
extern int      MA_MODE_CLOSING=1;
extern int      USE_MACD_CLOSING =1;
extern int      MACD_MODE_CLOSING=3;
 
extern int      USE_RSI_CLOSING =0;
extern int      RSI_MODE_CLOSING=3;
extern int      USE_STO_CLOSING =1;
extern int      USE_SAR_CLOSING=1;
extern bool  Send_Mail=false;


extern int       MagicNumber = 1080; // Magic Number   



double  LOT;
int options[];
double SL,TP,ATR;
 

                        

int New_Bar;                                             
int Time_0;                                              
int PosOpen;                                          
int PosClose;                                           
int total;                                             
double MA1_0,MA1Current,MA2Current,MA1Previous,MA2Previous,MA3Current,MA3Previous,MA1CrossBuffer,MA2CrossBuffer;                                          
double MA1_1;                                           
double MA2_0;                                           
double MA2_1;       
double MA3_0;                                           
double MA3_1;                                           
                                    
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
int MACDTrend,RSITrend,SARTrend,STOTrend,selected,j;
int signals[];
int MyPoint;   
//============================================================================================
 
//============================================================================================
int start()  
 {
 
if(Digits==3||Digits==5)MyPoint=10; else MyPoint=1;
//closing the and openning opposite====================================================  
  double price;  
   int openOrders=0;
   int total=OrdersTotal();
   if (useTrailingStop==1){Trail(TrailingStop);}                                    
   for(int i=total-1;i>=0;i--)
      {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)    
         {
         if(OrderType()==OP_BUY&& OrderMagicNumber() == MagicNumber)                             
            {
            
            if(CloseSignal()==2&&AutoClose==1)                    
               {                                            
               price=MarketInfo(Symbol(),MODE_BID);
               OrderClose(OrderTicket(),OrderLots(),price,4,CLR_NONE);
               if (Open_opposite_after_close==1&&checksellorders()<Max_same_orders)
                     {
            
                    
                      TP =NormalizeDouble( Bid-(ATR*ATR_Multiple- 2*Point*MyPoint),Digits);
                      SL = NormalizeDouble(Bid+(ATR*ATR_Multiple+ 2*Point*MyPoint),Digits);    
                      LOT = LOT(Risk,1);
                      OPENORDER ("Sell");
                           
                          
                     }//if (open opposite)
               }//if close signal ==2
            }//if order type == buy
         if(OrderType()==OP_SELL&& OrderMagicNumber() == MagicNumber)                            
            {
           
            if(CloseSignal()==1&&AutoClose==1)                     
               {                                             
               price=MarketInfo(Symbol(),MODE_ASK);
               OrderClose(OrderTicket(),OrderLots(),price,4,CLR_NONE);
                  if (Open_opposite_after_close==1&&checkbuyorders()<Max_same_orders)
                     {
                                  
                          TP  = NormalizeDouble(Ask+(ATR*ATR_Multiple+ 2*Point*MyPoint),Digits);
                           SL  = NormalizeDouble(Ask-(ATR*ATR_Multiple- 2*Point*MyPoint),Digits);    
                           LOT = LOT(Risk,1);
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

//diverge = divergence(MA1_Period, MA2_Period, MA1_Price, MA2_Price, 0);
//Check for positions========================================================================
// if(diverge >= DVBuySell && diverge <= DVStayOut)
//       BUYme = true;
//   if(diverge <= (DVBuySell*(-1)) && diverge >= (DVStayOut*(-1))) 
 //      SELLme = true;


if (checkmaxorders()<Max_orders)
 {
  
 if (Signal()==1&& New_Bar==1&&checkbuyorders()<Max_same_orders)
      {
                      TP  =NormalizeDouble( Ask+(ATR*ATR_Multiple+ 2*Point*MyPoint),Digits);
                      SL  = NormalizeDouble(Ask-(ATR*ATR_Multiple- 2*Point*MyPoint),Digits);   
                      LOT = LOT(Risk,1);
                      OPENORDER ("Buy"); 
                   
      }     
   
   
    else if (Signal()==2&& New_Bar==1&&checksellorders()<Max_same_orders)
     
      {
          TP = NormalizeDouble(Bid-(ATR*ATR_Multiple- 2*Point*MyPoint),Digits);
          SL = NormalizeDouble(Bid+(ATR*ATR_Multiple+ 2*Point*MyPoint),Digits);            
          LOT = LOT(Risk,1);
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
      case 4200:Alert("Error Объект уже существует ",            Ticket," ",Symbol());return;
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
   if (ord=="Buy" ) error=OrderSend(Symbol(),OP_BUY, LOT,Ask,4,SL,TP,"FSF FX COMBO Trader "+Symbol()+Period(),MagicNumber, 1,3);
   if (ord=="Sell") error=OrderSend(Symbol(),OP_SELL,LOT,Bid,4,SL,TP,"FSF FX COMBO Trader "+Symbol()+Period(),MagicNumber,-1,3);
   if (error==-1) //operation failed
   {  
      ShowERROR(error,0,0);
   }
   if (error!=-1&&Send_Mail==True) SendMail("[Signal Alert]", "[" + Symbol() + "] " + DoubleToStr(Bid, Digits) + ord);   

   
return;

}    
//===========================================================================================================
   
//Calculate==================================
void calculate ()
   {
    
    
    //MACD
   MACD_Main_0 = iCustom(Symbol(),0,"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,0);
   MACD_Main_1 = iCustom(Symbol(),0,"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,1);
   MACD_Signal_0 = iCustom(Symbol(),0,"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,0);
   MACD_Signal_1 = iCustom(Symbol(),0,"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,1);
   macdMain = iCustom(Symbol(),0,"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,0);
   macdSignal = iCustom(Symbol(),0,"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,0);
   macdMainPrev = iCustom(Symbol(),0,"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_MAIN,1);
   macdSignalPrev = iCustom(Symbol(),0,"ZeroLag MACD",MACD_Fast,MACD_Slow,Signal_Period,MACD_Price,MODE_SIGNAL,1);  
   
  
MA1Current = iMA(NULL, 0, MA1_Period, MA1_Shift,MAMethod( MA1_Method), MAPrice(MA1_Price), 0);
MA2Current = iMA(NULL, 0, MA2_Period, MA2_Shift, MAMethod(MA2_Method), MAPrice(MA2_Price), 0);
MA3Current = iMA(NULL, 0, MA3_Period, MA3_Shift, MAMethod(MA3_Method), MAPrice(MA3_Price), 0);

MA1Previous = iMA(NULL, 0, MA1_Period, MA1_Shift, MAMethod(MA1_Method), MAPrice(MA1_Price), 1);
MA2Previous = iMA(NULL, 0, MA2_Period, MA2_Shift, MAMethod(MA2_Method), MAPrice(MA2_Price),1);
MA3Previous = iMA(NULL, 0, MA3_Period, MA3_Shift, MAMethod(MA3_Method), MAPrice(MA3_Price),1);

 
   
   //RSI
   RSIcurrent=iRSI(NULL,0,RSIperiod,PRICE_CLOSE,0);
   RSIprevious=iRSI(NULL,0,RSIperiod,PRICE_CLOSE,1);
   
   //SAR
   SARSignal = iSAR(Symbol(),0,SARStep,SARMax,0);
   
   //STOCHASTIC
    STOValue = iStochastic(Symbol(),0,STOK,STOD,STOL,0,0,MODE_MAIN,0);
    STOSignal = iStochastic(Symbol(),0,STOK,STOD,STOL,0,0,MODE_SIGNAL,0);
    
    
   //ATR
   ATR = iATR(Symbol(), 0, ATR_PERIOD, 0); 
   MA1CrossBuffer= iATR(Symbol(), 0, CrossBuffer1, 0); 
   MA2CrossBuffer=iATR(Symbol(), 0, CrossBuffer2, 0); 
   }
  
   
//MA Cross======================================================================================
int MA()
   {
   int PosOpen=0;                                   
   if(MA1Current >= MA2Current + (MA1CrossBuffer) && MA1Previous < MA2Previous + (MA1CrossBuffer )) 
              
      {
      PosOpen=1;
      }  
   if(MA1Current <= MA2Current - (MA1CrossBuffer ) && MA1Previous > MA2Previous - (MA1CrossBuffer )) 
                      
      {
      PosOpen=2;
      }             
    
   return(PosOpen);                                         
   }   
   
//MA2 Cross======================================================================================
int MA2()
   {
   
   int PosOpen=0; 
   if(MA2Current >= MA3Current + (MA2CrossBuffer ) && MA2Previous < MA3Previous + (MA2CrossBuffer )) 
                                                   
      {
      PosOpen=1;
      } 
   if(MA2Current <= MA3Current - (MA2CrossBuffer ) && MA2Previous > MA3Previous - (MA2CrossBuffer)) 
                      
      {
      PosOpen=2;
      }             
    
   return(PosOpen);                                         
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
    
   return(PosOpen);                                         
   }   
      
   
//MA4 Cross======================================================================================
int MA4()
   {
   
   int PosOpen=0; 
   if(MA1Current >= MA3Current + (MA1CrossBuffer ) && MA1Previous < MA3Previous + (MA1CrossBuffer )) 
                                                   
      {
      PosOpen=1;
      } 
   if(MA1Current <= MA3Current - (MA1CrossBuffer ) && MA1Previous > MA3Previous - (MA1CrossBuffer)) 
                      
       {
      PosOpen=2;
      }             
    
   return(PosOpen);                                         
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
    
   return(PosOpen);                                         
   }   
      
   
//MA4 Cross======================================================================================   
//Signals============================================================================================



//============================================================================================
//MACD TREND METHOD============================================================
int MACD()
   {
  MACDTrend=0;
   
   
   
   if(MACD_Main_0>MACD_Main_1 && MACD_Signal_0>MACD_Signal_1 && MACD_Main_0>MACD_Signal_0) {MACDTrend = 1;}    //buy
   if(MACD_Main_0<MACD_Main_1 && MACD_Signal_0<MACD_Signal_1 && MACD_Main_0<MACD_Signal_0) {MACDTrend = 2;}    // sell
   if(MACD_Main_0<MACD_Main_1 && MACD_Signal_0>MACD_Signal_1 && MACD_Main_0>MACD_Signal_0) {MACDTrend = 3;}    // up-down
   if(MACD_Main_0>MACD_Main_1 && MACD_Signal_0<MACD_Signal_1 && MACD_Main_0<MACD_Signal_0) {MACDTrend = 4;}    // down-up
   return(MACDTrend);
   }
//==MACD MODE 2=============================================================
int MACD2()
   {
   MACDTrend = 0;   //1=Buy , 2= Sell


   if(macdMain>macdSignal && macdMainPrev<=macdSignalPrev && macdMain<0 && macdMainPrev<0){MACDTrend = 1;}
   else if(macdMain<macdSignal && macdMainPrev>=macdSignalPrev && macdMain>0 && macdMainPrev>0){MACDTrend = 2;}
   else { MACDTrend=0;}
   return(MACDTrend);

      
   }   
//==MACD Both=============================================================
int MACD3()
   {
   MACDTrend = 0;   //1=Buy , 2= Sell


   if((macdMain>macdSignal && macdMainPrev<=macdSignalPrev && macdMain<0 && macdMainPrev<0)
   ||(MACD_Main_0>MACD_Main_1 && MACD_Signal_0>MACD_Signal_1 && MACD_Main_0>MACD_Signal_0))
   {MACDTrend = 1;}
   else if((macdMain<macdSignal && macdMainPrev>=macdSignalPrev && macdMain>0 && macdMainPrev>0)
   ||(MACD_Main_0<MACD_Main_1 && MACD_Signal_0<MACD_Signal_1 && MACD_Main_0<MACD_Signal_0))
   {MACDTrend = 2;}
   else { MACDTrend=0;}
   return(MACDTrend);

      
   }     
//RSI MODE overbought & oversold==============================================================  
int RSI()
   {   
      int RSITrend = 0;//1 = buy, 2 = sell
      
      if (RSIValue > RSISellOp){RSITrend = 2;}//SELL
      else if (RSIValue < RSIBuyOp){RSITrend = 1;}//BUY
      
      return(RSITrend);
    }  
//RSI MODE 2 trend=======================================================================    
int RSI2()
   {   
     int RSITrend = 0;//1 = buy, 2 = sell
 
     if ((RSIcurrent > RSIprevious)&&(Open[0]>Open[1])){RSITrend=1;}//BUY
     if ((RSIcurrent< RSIprevious)&&(Open[0]<Open[1])){RSITrend=2;}//SELL
   
     return(RSITrend);
     
   }  
//RSI MODE 2 trend=======================================================================    
int RSI3()
   {   
     int RSITrend = 0;//1 = buy, 2 = sell
 
     if (RSI2()==1||RSI()==1){RSITrend=1;}//BUY
     if (RSI2()==2||RSI()==2){RSITrend=2;}//SELL
   
     return(RSITrend);
     
   } 
//====================================================================================     
//RSI Mode 4=======================================================================    
int RSI4()

   {   
     int RSITrend = 0;//1 = buy, 2 = sell
 
    if (((RSIcurrent > RSIprevious)&&(RSIcurrent>50))||((RSIcurrent > RSIprevious)&&(RSIprevious>RSISellZone)) ){RSITrend=1;}//BUY
     if (((RSIcurrent< RSIprevious)&&(RSIcurrent<50))||((RSIcurrent < RSIprevious)&&(RSIprevious<RSIBuyZone))){RSITrend=2;}//SELL
   
     return(RSITrend);
     
   }   
   
//SAR ===================================================================================
int SAR()
   {  
  
      SARTrend = 0;   //1 = buy, 2 = sell
      if (SARSignal<Close[1]){SARTrend = 1;}
      else{SARTrend = 2;}
     
     
     return (SARTrend); 
   }     
 
   
//STOCHASTIC=======================================================================================
int STO()
   {    
     STOTrend = 0; //1 = buy, 2 = sell
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
   return(STOTrend);   
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
 

if (uptrend==SelectedCount())Trend=1;
if (downtrend==SelectedCount() ) Trend=2;
  

   



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
                     case 1: sig[i]=RSI();                               
                     case 2: sig[i]=RSI2();                        
                     case 3: sig[i]=RSI3();
                     case 4: sig[i]=RSI4();                        
                     }
               }
            
     
      if (i==1){sig[i]=STO();}
      if (i==2){sig[i]=SAR();}
      if (i==3)   {
      
      switch(MA_MODE_CLOSING)
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
                     switch(MACD_MODE_CLOSING)
                     {
                     case 1: sig[i]=MACD();                               
                     case 2: sig[i]=MACD2();                        
                     case 3: sig[i]=MACD3();                        
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
 

if (uptrend==SelectedCount())Trend=1;
if (downtrend==SelectedCount() ) Trend=2;
  

   



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
//===========================================================================

void display()
   {
   
      if (IsTesting()==false &&IsOptimization()==false){

   ObjectDelete("mainsignal");     ObjectDelete("masignal");   ObjectDelete("rsisignal");   ObjectDelete("stoignal");ObjectDelete("sarignal");ObjectDelete("macdignal");
         if (Signal()==1) clabel("mainsignal",900,20,"Main Signal : Up-Buy",Blue);
            else if (Signal()==2)clabel("mainsignal",900,20,"Main Signal: Down-Sell",Red);
            else clabel("mainsignal",900,20,"Main Signal:Please Wait For Right Signal",White);

            if (MA()==1)clabel("masignal",900,30,"Moving Average Signal : Up-Buy",Blue);
            else if (MA()==2)clabel("masignal",900,30,"Moving AverageSignal :Down-Sell",Red);
            else clabel("masignal",900,30,"MA:Please Wait For Right Signal",White);

            if (MACD()==1)clabel("macdsignal",900,40,"MACD Signal : Up-Buy",Blue);
            else if (MACD()==2)clabel("macdsignal",900,40,"MACD Signal : Down-Sell",Red);
            else  clabel("macdsignal",900,40,"MACD:Please Wait For Right Signal",White);

            if (RSI4()==1)clabel("rsisignal",900,50,"RSI Signal : Up-Buy",Blue);
            else if (RSI4()==2)clabel("rsisignal",900,50,"RSI Signal : Down-Sell",Red);
            else clabel("rsisignal",900,50,"RSI Signal :Please Wait For Right Signal",White);

         if (STO()==1)clabel("stosignal",900,60,"STO Signal : Up-Buy",Blue);
         else if (STO()==2)clabel("stosignal",900,60,"STO Signal : Down-Sell",Red);
         else clabel("stosignal",900,60,"STO Signal :Please Wait For Right Signal",White);

         if (SAR()==1)clabel("sarsignal",900,70,"SAR Signal : Up-Buy",Blue);
         else if (SAR()==2)clabel("sarsignal",900,70,"SAR Signal : Down-Sell",Red);
         else clabel("sarsignal",900,70,"SAR Signal :Please Wait For Right Signal",White);

 Comment("FSF Combo Trader ver 1\n",
            "Copyright © 2010,Farshad Saremifar , Farshad.Saremifar@gmail.com\n",
            "Forex Account Server:",AccountServer(),"\n",
            "Account Balance:  $",AccountBalance(),"\n",
            "Lots:  ",DoubleToStr(LOT,3),"\n",
            "Symbol: ", Symbol(),"\n",
            "Pip Spread:  ",MarketInfo(Symbol(),MODE_SPREAD),"\n",
            " Server Time: ",Hour(),":",Minute(),":",Seconds(),"\n",
            "Minimum Lot Size: ",MarketInfo(Symbol(),MODE_MINLOT),"\n",
       
            "You Have Choosed: ",SelectedCount()," Option(s)");
      }
      
    
      
}  
///-------------------------------------------------------------------------

void clabel(string lblname,int x,int y,
string txt,color txtcolor){
ObjectCreate(lblname, OBJ_LABEL,0, 0, 0);
ObjectSet(lblname, OBJPROP_CORNER, 0);
ObjectSetText(lblname,txt,7,"Verdana", txtcolor);
ObjectSet(lblname, OBJPROP_XDISTANCE, x);
ObjectSet(lblname, OBJPROP_YDISTANCE, y);


}


