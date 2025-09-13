//+------------------------------------------------------------------+
//|                                                        X bug.mq4 |
//|                             Copyright © 2010, www.Forexyangu.com |
//|                                        http://www.Forexyangu.com |
//+------------------------------------------------------------------+
#property copyright " www.Forexyangu.com"
#property link      "http://www.ForexYangu.com"

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------
extern string ToDonateAndMoreVisit ="www.ForexYangu.com";
extern string FullDocumentation    ="www.forexyangu.com";
extern int StopLoss   =70;     // SL for an opened order
extern int TakeProfit =5000;     // ?? for an opened order
extern bool Trailing =true;  // turn trailing on or off
extern int Trail_Stop=90;                        // Trailing distance
extern double Lots    =0.1;    // Strictly set amount of lots
extern int Slippage   =5;      // Price Slippage
extern string Caution  ="Advanced settings follow. Dont change if you dont know what you are doing.";
extern int MA1    =1;   //ma setting
extern int Shift1 =0;    //ma setting
extern int MA2    =14;   //ma setting
extern int Shift2 =10;    //ma setting
//extern int    OpenDel =0;     // wont open if current price has already covered this in the positive from bar open price
//extern int    OpenAft =0;     // wont open if movement hasnt covered this peeps in the positive
extern bool MAclose =true;    // enable or disable pSAR signals to close orders on signal shift
extern bool   Reverse =false; // enable or disable

extern string ContactMe ="admin@forexyangu.com"; // support email

/*****************************************************-----READ THIS-------******************************************************
 *******************************************************************************************************************************/
 //-----------------------------------------------------------------------------------------------------------------------------
/*DONATE TO SUPPORT MY FREE PROJECTS AND TO RECEIVE NON OPEN PROJECTS AND ADVANCED VERSIONS OF EXISTING PROJECTS WHEN AVAILABLE: 
//------------------------------------------------------------------------------------------------------------------------------
__my moneybookers email is admin@forexyangu.com anyone can easily join moneybookers at www.moneybookers.com and pay people via their 
email through numerous payment methods__*/
//------------------------------------------------------------------------------------------------------------------------------
//SUPPORT AND INQUIRIES EMAIL:        admin@forexyangu.com
//------------------------------------------------------------------------------------------------------------------------------
/*******************************************************************************************************************************
 *************************************************--------END------*************************************************************/

/* Expert designed to open and close trades at first Parabolic SAR dots. Performance depends on your custom parameters 
Contact me at tonnyochieng@gmail.com */
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
  //trading criteria
    bool   result;
   double price;
   int    cmd,error,TP,SL,StopMultd,OpenDelayBuy,OpenDelaySell,OpenAfterBuy,OpenAfterSell,OpenAfter,OpenDelay,Tral_Stop;
   
int    digits=MarketInfo("EURUSD",MODE_DIGITS);

if(digits==5){StopMultd=10;} else{StopMultd=1;}

TP=NormalizeDouble(TakeProfit*StopMultd,Digits);
SL=NormalizeDouble(StopLoss*StopMultd,Digits);
//OpenDelay=OpenDel*StopMultd;
//OpenAfter=OpenAft*StopMultd;
Tral_Stop=Trail_Stop*StopMultd;

string csymb=Symbol();
double bidp   =MarketInfo(csymb,MODE_BID);
double askp   =MarketInfo(csymb,MODE_ASK);

//OpenDelayBuy=bidp-iLow(NULL,0,0)*Point;
//OpenDelaySell=iHigh(NULL,0,0)-askp*Point;
//OpenAfterBuy=bidp-iLow(NULL,0,0)*Point;
//OpenAfterSell=iHigh(NULL,0,0)-askp*Point;

double MA1_bc=iMA(NULL,0,MA1,Shift1,0,4,0);
double MA2_bc=iMA(NULL,0,MA2,Shift2,0,4,0);
double MA1_bp=iMA(NULL,0,MA1,Shift1,0,4,1);
double MA2_bp=iMA(NULL,0,MA2,Shift2,0,4,1);
double MA1_bl=iMA(NULL,0,MA1,Shift1,0,4,2);
double MA2_bl=iMA(NULL,0,MA2,Shift2,0,4,2);

/*-----------------------------------------------------------------------------------------------------------------------*/
if(Reverse==false){         //start reverse is false
/*-----------------------------------------------------------------------------------------------------------------------*/
 
 if ((MA1_bc>MA2_bc)&&(MA1_bl<MA2_bl)) //Signal Buy
 {
 /*****************************************************/
 if((MAclose==true)&&(OrdersTotal()>0)){
   if(OrderSelect(0,SELECT_BY_POS,MODE_TRADES))
     {
      cmd=OrderType();
      //---- open order is sell
      if(cmd==OP_SELL)
        {
         while(true)
           {
            if(cmd==OP_BUY) price=Bid;
            else            price=Ask;
            result=OrderClose(OrderTicket(),OrderLots(),price,Slippage,CLR_NONE);
            if(result!=TRUE) { error=GetLastError(); Print("LastError = ",error); }
            else error=0;
            if(error==135) RefreshRates();
            else break;
           }
        }
     }
   else Print( "Error when order select ", GetLastError());
 }
 /******************************************************/
     // Closing Sell 
// if(((OpenDelay>=OpenDelayBuy)||(OpenDelay==0))&&(OpenAfter<OpenAfterBuy)){    
     //open buy
    if(OrdersTotal()==0)
    {
    int openbuy=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,NormalizeDouble(Ask-SL*Point,Digits),NormalizeDouble(Ask+TP*Point,Digits));//Opening Buy
     if(openbuy==TRUE) {Alert("Order opened !!!"); PlaySound("alert.wav"); }
    }
 // }
 }
 
 
 if ((MA1_bc<MA2_bc)&&(MA1_bl>MA2_bl)) //Signal Sell
 {
 /*************************************************/
 if((MAclose==true)&&(OrdersTotal()>0)){
    if(OrderSelect(0,SELECT_BY_POS,MODE_TRADES))
     {
      cmd=OrderType();
      //---- open order is buy
      if(cmd==OP_BUY)
        {
         while(true)
           {
            if(cmd==OP_BUY) price=Bid;
            else            price=Ask;
            result=OrderClose(OrderTicket(),OrderLots(),price,Slippage,CLR_NONE);
            if(result!=TRUE) { error=GetLastError(); Print("LastError = ",error); }
            else error=0;
            if(error==135) RefreshRates();
            else break;
           }
        }
     }
   else Print( "Error when order select ", GetLastError());
  }
 /****************************************************************/
      // Closing Buy 
  //if(((OpenDelay>=OpenDelaySell)||(OpenDelay==0))&&(OpenAfter<OpenAfterSell)){
      //open sell
    if(OrdersTotal()==0)
    {
     int opensell=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,NormalizeDouble(Bid+SL*Point,Digits),NormalizeDouble(Bid-TP*Point,Digits));//Opening Sell
     if(opensell==TRUE) {Alert("Order opened !!!"); PlaySound("alert.wav"); }
    }
  //}

}
/*-----------------------------------------------------------------------------------------------------------------------*/
}//end reverse false
/*-----------------------------------------------------------------------------------------------------------------------*/


/*-----------------------------------------------------------------------------------------------------------------------*/
else{ // reverse true
/*-----------------------------------------------------------------------------------------------------------------------*/

if ((MA1_bc>MA2_bc)&&(MA1_bl<MA2_bl)) //Signal Buy
 {
 
  /*************************************************/
 if((MAclose==true)&&(OrdersTotal()>0)){
    if(OrderSelect(0,SELECT_BY_POS,MODE_TRADES))
     {
      cmd=OrderType();
      //---- open order is buy
      if(cmd==OP_BUY)
        {
         while(true)
           {
            if(cmd==OP_BUY) price=Bid;
            else            price=Ask;
            result=OrderClose(OrderTicket(),OrderLots(),price,Slippage,CLR_NONE);
            if(result!=TRUE) { error=GetLastError(); Print("LastError = ",error); }
            else error=0;
            if(error==135) RefreshRates();
            else break;
           }
        }
     }
   else Print( "Error when order select ", GetLastError());
  }
 /****************************************************************/
      // Closing Buy 
//if(((OpenDelay>=OpenDelaySell)||(OpenDelay==0))&&(OpenAfter<OpenAfterSell)){
      //open sell
    if(OrdersTotal()==0)
    {
     int opensellr=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,NormalizeDouble(Bid+SL*Point,Digits),NormalizeDouble(Bid-TP*Point,Digits));//Opening Sell
     if(opensellr==TRUE) {Alert("Order opened !!!"); PlaySound("alert.wav"); }
    }
  //}

 
 }
 
 
 if ((MA1_bc<MA2_bc)&&(MA1_bl>MA2_bl)) //Signal Sell
 {
  
  /*****************************************************/
 if((MAclose==true)&&(OrdersTotal()>0)){
   if(OrderSelect(0,SELECT_BY_POS,MODE_TRADES))
     {
      cmd=OrderType();
      //---- open order is sell
      if(cmd==OP_SELL)
        {
         while(true)
           {
            if(cmd==OP_BUY) price=Bid;
            else            price=Ask;
            result=OrderClose(OrderTicket(),OrderLots(),price,Slippage,CLR_NONE);
            if(result!=TRUE) { error=GetLastError(); Print("LastError = ",error); }
            else error=0;
            if(error==135) RefreshRates();
            else break;
           }
        }
     }
   else Print( "Error when order select ", GetLastError());
 }
 /******************************************************/
     // Closing Sell 
 //if(((OpenDelay>=OpenDelayBuy)||(OpenDelay==0))&&(OpenAfter<OpenAfterBuy)){
     //open buy
    if(OrdersTotal()==0)
    {
    int openbuyr=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,NormalizeDouble(Ask-SL*Point,Digits),NormalizeDouble(Ask+TP*Point,Digits));//Opening Buy
     if(openbuyr==TRUE) {Alert("Order opened !!!"); PlaySound("alert.wav"); }
    }
  //}
 

 }

/*------------------------------------------------------------------------------------------------------------------------*/
}  // end reverse true
/*------------------------------------------------------------------------------------------------------------------------*/

if((Trailing==true)&&(OrdersTotal()>0)){
//-----------------------------------------------------------------------------------------------------------------------
// Trail
/*-----------------------------------------------------------------------------------------------------------------------*/
string Symb=Symbol();                        // Symbol
//------------------------------------------------------------------------------- 2 --
   for(int i=1; i<=OrdersTotal(); i++)          // Cycle searching in orders
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // If the next is available
        {                                       // Analysis of orders:
         int Tip=OrderType();                   // Order type
         if(OrderSymbol()!=Symb||Tip>1)continue;// The order is not "ours"
         double SLt=OrderStopLoss();             // SL of the selected order
         //---------------------------------------------------------------------- 3 --
         while(true)                            // Modification cycle
           {
            double TS=Tral_Stop;                // Initial value
            int Min_Dist=MarketInfo(Symb,MODE_STOPLEVEL);//Min. distance
            if (TS < Min_Dist)                  // If less than allowed
               TS=Min_Dist;                     // New value of TS
            //------------------------------------------------------------------- 4 --
            bool Modify=false;                  // Not to be modified
            switch(Tip)                         // By order type
              {
               case 0 :                         // Order Buy
                  if (NormalizeDouble(SLt,Digits)< // If it is lower than we want
                     NormalizeDouble(Bid-TS*Point,Digits))
                    {
                     SLt=Bid-TS*Point;           // then modify it
                     string Text="Buy ";        // Text for Buy 
                     Modify=true;               // To be modified
                    }
                  break;                        // Exit 'switch'
               case 1 :                         // Order Sell
                  if (NormalizeDouble(SLt,Digits)> // If it is higher than we want
                     NormalizeDouble(Ask+TS*Point,Digits)
                     || NormalizeDouble(SLt,Digits)==0)//or equal to zero
                    {
                     SLt=Ask+TS*Point;           // then modify it
                     Text="Sell ";              // Text for Sell 
                     Modify=true;               // To be modified
                    }
              }                                 // End of 'switch'
            if (Modify==false)                  // If it is not modified
               break;                           // Exit 'while'
            //------------------------------------------------------------------- 5 --
            double TPt    =OrderTakeProfit();    // TP of the selected order
            double Price =OrderOpenPrice();     // Price of the selected order
            int    Ticket=OrderTicket();        // Ticket of the selected order
 
            Alert ("Modification ",Text,Ticket,". Awaiting response..");
            bool Ans=OrderModify(Ticket,Price,SLt,TPt,0);//Modify it!
            //------------------------------------------------------------------- 6 --
            if (Ans==true)                      // Got it! :)
              {
               Alert ("Order ",Text,Ticket," is modified:)");
               break;                           // From modification cycle.
              }
            //------------------------------------------------------------------- 7 --
            int Error=GetLastError();           // Failed :(
            switch(Error)                       // Overcomable errors
              {
               case 130:Alert("Wrong stops. Retrying.");
                  RefreshRates();               // Update data
                  continue;                     // At the next iteration
               case 136:Alert("No prices. Waiting for a new tick..");
                  while(RefreshRates()==false)  // To the new tick
                     Sleep(1);                  // Cycle delay
                  continue;                     // At the next iteration
               case 146:Alert("Trading subsystem is busy. Retrying ");
                  Sleep(500);                   // Simple solution
                  RefreshRates();               // Update data
                  continue;                     // At the next iteration
                  // Critical errors
               case 2 : Alert("Common error.");
                  break;                        // Exit 'switch'
               case 5 : Alert("Old version of the client terminal.");
                  break;                        // Exit 'switch'
               case 64: Alert("Account is blocked.");
                  break;                        // Exit 'switch'
               case 133:Alert("Trading is prohibited");
                  break;                        // Exit 'switch'
               default: Alert("Occurred error ",Error);//Other errors
              }
            break;                              // From modification cycle
           }                                    // End of modification cycle
         //---------------------------------------------------------------------- 8 --
        }                                       // End of order analysis
     }                                          // End of order search
//------------------------------------------------------------------------------- 9 --
//------------------------------------------------------------------------------------------------------------------------
// end trail
//--------------------------------------------------------------------------------------------------------------------------
}
//----------
return(0);
  }
//------------------------------------------------------------------+