//+------------------------------------------------------------------+
//|                                                          pp2.mq4 |
//|                      Copyright © 2011,                           |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright " www.FxAutomated.com"
#property link      "http://www.FxAutomated.com"


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------
extern string ToDonateAndMoreVisit ="www.FxAutomated.com";
extern string ForexSignals    ="www.TradingBug.com";
extern string ManagedAccounts  ="www.TradingBug.com";
extern int StopLoss   =90;     // SL for an opened order
extern int TakeProfit =20;     // ?? for an opened order
extern bool Trailing =false;  // turn trailing on or off
extern int Trail_Stop=10;                        // Trailing distance
extern double Lots    =0.1;    // Strictly set amount of lots
extern int Slippage   =5;      // Price Slippage
extern string Caution  ="Advanced settings follow. Dont change if you dont know what you are doing.";
extern double Step    =0.001;   //Parabolic setting
extern double Maximum =0.2;    //Parabolic setting
extern int    OpenDel =0;     // wont open if current price has already covered this in the positive from bar open price
extern int    OpenAft =0;     // wont open if movement hasnt covered this peeps in the positive
extern bool SARclose =true;    // enable or disable pSAR signals to close orders on signal shift
extern bool   Reverse =false; // enable or disable



/*****************************************************-----READ THIS-------******************************************************
 *******************************************************************************************************************************/
 //-----------------------------------------------------------------------------------------------------------------------------
/*DONATE TO SUPPORT MY FREE PROJECTS AND TO RECEIVE NON OPEN PROJECTS AND ADVANCED VERSIONS OF EXISTING PROJECTS WHEN AVAILABLE: 
//------------------------------------------------------------------------------------------------------------------------------
__my paypal and moneybookers email is admin@forexyangu.com anyone can easily join moneybookers at www.moneybookers.com or paypal at www.paypal.com and pay people via their 
email through numerous payment methods__*/
//------------------------------------------------------------------------------------------------------------------------------
//SUPPORT AND INQUIRIES EMAIL:        admin@forexyangu.com
//------------------------------------------------------------------------------------------------------------------------------
/*******************************************************************************************************************************
 *************************************************--------END------*************************************************************/


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
OpenDelay=OpenDel*StopMultd;
OpenAfter=OpenAft*StopMultd;
Tral_Stop=Trail_Stop*StopMultd;

string csymb=Symbol();
double bidp   =MarketInfo(csymb,MODE_BID);
double askp   =MarketInfo(csymb,MODE_ASK);

OpenDelayBuy=bidp-iLow(NULL,0,0)*Point;
OpenDelaySell=iHigh(NULL,0,0)-askp*Point;
OpenAfterBuy=bidp-iLow(NULL,0,0)*Point;
OpenAfterSell=iHigh(NULL,0,0)-askp*Point;

//-------------------------------------------------------------------+
//Check open orders
//-------------------------------------------------------------------+
if(OrdersTotal()>0){
  for(int i=1; i<=OrdersTotal(); i++)          // Cycle searching in orders
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // If the next is available
        {
          if(OrderMagicNumber()==110001) {int halt1=1;}
 
        }
     }
}
//-------------------------------------------------------------------+
/*-----------------------------------------------------------------------------------------------------------------------*/
if(Reverse==false){         //start reverse is false
/*-----------------------------------------------------------------------------------------------------------------------*/
 
 if ((iSAR(NULL, 0,Step,Maximum, 0)<iClose(NULL,0,0))&&(iSAR(NULL, 0,Step,Maximum, 1)>iClose(NULL,0,1))) //Signal Buy
 {
 /*****************************************************/
 if((SARclose==true)&&(OrdersTotal()>0)){
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
 if(((OpenDelay>=OpenDelayBuy)||(OpenDelay==0))&&(OpenAfter<OpenAfterBuy)){    
     //open buy
    if(halt1!=1)
    {
    int openbuy=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,NormalizeDouble(Ask-SL*Point,Digits),NormalizeDouble(Ask+TP*Point,Digits),"psar bug 6 order ",110001);//Opening Buy
     if(openbuy==TRUE) {Alert("Order opened !!!"); PlaySound("alert.wav"); }
    }
  }
 }
 
 
 if ((iSAR(NULL, 0,Step,Maximum, 0)>iClose(NULL,0,0))&&(iSAR(NULL, 0,Step,Maximum, 1)<iClose(NULL,0,1))) //Signal Sell
 {
 /*************************************************/
 if((SARclose==true)&&(OrdersTotal()>0)){
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
  if(((OpenDelay>=OpenDelaySell)||(OpenDelay==0))&&(OpenAfter<OpenAfterSell)){
      //open sell
    if(halt1!=1)
    {
     int opensell=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,NormalizeDouble(Bid+SL*Point,Digits),NormalizeDouble(Bid-TP*Point,Digits),"psar bug 6 order ",110001);//Opening Sell
     if(opensell==TRUE) {Alert("Order opened !!!"); PlaySound("alert.wav"); }
    }
  }

}
/*-----------------------------------------------------------------------------------------------------------------------*/
}//end reverse false
/*-----------------------------------------------------------------------------------------------------------------------*/


/*-----------------------------------------------------------------------------------------------------------------------*/
else{ // reverse true
/*-----------------------------------------------------------------------------------------------------------------------*/

if ((iSAR(NULL, 0,Step,Maximum, 0)<iClose(NULL,0,0))&&(iSAR(NULL, 0,Step,Maximum, 1)>iClose(NULL,0,1))) //Signal Buy
 {
 
  /*************************************************/
 if((SARclose==true)&&(OrdersTotal()>0)){
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
if(((OpenDelay>=OpenDelaySell)||(OpenDelay==0))&&(OpenAfter<OpenAfterSell)){
      //open sell
    if(halt1!=1)
    {
     int opensellr=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,NormalizeDouble(Bid+SL*Point,Digits),NormalizeDouble(Bid-TP*Point,Digits),"psar bug 6 order ",110001);//Opening Sell
     if(opensellr==TRUE) {Alert("Order opened !!!"); PlaySound("alert.wav"); }
    }
  }

 
 }
 
 
 if ((iSAR(NULL, 0,Step,Maximum, 0)>iClose(NULL,0,0))&&(iSAR(NULL, 0,Step,Maximum, 1)<iClose(NULL,0,1))) //Signal Sell
 {
  
  /*****************************************************/
 if((SARclose==true)&&(OrdersTotal()>0)){
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
 if(((OpenDelay>=OpenDelayBuy)||(OpenDelay==0))&&(OpenAfter<OpenAfterBuy)){
     //open buy
    if(halt1!=1)
    {
    int openbuyr=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,NormalizeDouble(Ask-SL*Point,Digits),NormalizeDouble(Ask+TP*Point,Digits),"psar bug 6 order ",110001);//Opening Buy
     if(openbuyr==TRUE) {Alert("Order opened !!!"); PlaySound("alert.wav"); }
    }
  }
 

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
   for(i=1; i<=OrdersTotal(); i++)          // Cycle searching in orders
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
            break;
           }                                   // End of modification cycle
         //---------------------------------------------------------------------- 8 --
        }                                       // End of order analysis
     }                                          // End of order search
//------------------------------------------------------------------------------- 9 --
            int Error=GetLastError();           // Failed :(
            switch(Error)                       // Overcomable errors
              {
               case 130:Alert("Wrong stops. Retrying.");
                  RefreshRates();               // Update data
                                      // At the next iteration
               case 136:Alert("No prices. Waiting for a new tick..");
                  while(RefreshRates()==false)  // To the new tick
                     Sleep(1);                  // Cycle delay
                                      // At the next iteration
               case 146:Alert("Trading subsystem is busy. Retrying ");
                  Sleep(500);                   // Simple solution
                  RefreshRates();               // Update data
                                      // At the next iteration
                  // Critical errors
               case 2 : Alert("Common error.");
                                         // Exit 'switch'
               case 5 : Alert("Old version of the client terminal.");
                                        // Exit 'switch'
               case 64: Alert("Account is blocked.");
                                        // Exit 'switch'
               case 133:Alert("Trading is prohibited");
                                         // Exit 'switch'
               default: Alert("Occurred error ",Error);//Other errors
              }
                                       // From modification cycle
//------------------------------------------------------------------------------------------------------------------------
// end trail
//--------------------------------------------------------------------------------------------------------------------------
}
//----------
return(0);
  }
//------------------------------------------------------------------+