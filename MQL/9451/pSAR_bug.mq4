//+------------------------------------------------------------------+
//|                                                          pp2.mq4 |
//|                      Copyright © 2010,                           |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright " www.Forexyangu.com"
#property link      "http://www.ForexYangu.com"

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------
extern int StopLoss   =40;     // SL for an opened order
extern int TakeProfit =70;      // ?? for an opened order
extern double Lots       =0.1;     // Strictly set amount of lots

/*****************************************************-----READ THIS-------******************************************************
 *******************************************************************************************************************************/
 //-----------------------------------------------------------------------------------------------------------------------------
/*DONATE TO SUPPORT MY FREE PROJECTS AND TO RECEIVE NON OPEN PROJECTS AND ADVANCED VERSIONS OF EXISTING PROJECTS WHEN AVAILABLE: 
//------------------------------------------------------------------------------------------------------------------------------
__my moneybookers email is admin@forexyangu.com anyone can easily join moneybookers and pay people via their email
through numerous payment methods__*/
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
   int    cmd,error;
   





 //close
 if ((iSAR(NULL, 0, 0.02, 0.2, 0)<iClose(NULL,0,0))&&(iSAR(NULL, 0, 0.02, 0.2, 1)>iClose(NULL,0,1))) //Signal Buy
 {

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
            result=OrderClose(OrderTicket(),OrderLots(),price,3,CLR_NONE);
            if(result!=TRUE) { error=GetLastError(); Print("LastError = ",error); }
            else error=0;
            if(error==135) RefreshRates();
            else break;
           }
        }
     }
   else Print( "Error when order select ", GetLastError());

     // Closing Sell 
     //open buy
    if(OrdersTotal()==0)
    {
    int openbuy=OrderSend(Symbol(),OP_BUY,Lots,Ask,5,Ask-StopLoss*Point,Ask+TakeProfit*Point);//Opening Buy
    }
 
 }
 if ((iSAR(NULL, 0, 0.02, 0.2, 0)>iClose(NULL,0,0))&&(iSAR(NULL, 0, 0.02, 0.2, 1)<iClose(NULL,0,1))) //Signal Sell
 {

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
            result=OrderClose(OrderTicket(),OrderLots(),price,3,CLR_NONE);
            if(result!=TRUE) { error=GetLastError(); Print("LastError = ",error); }
            else error=0;
            if(error==135) RefreshRates();
            else break;
           }
        }
     }
   else Print( "Error when order select ", GetLastError());
 
      // Closing Buy 
      //open sell
    if(OrdersTotal()==0)
    {
     int opensell=OrderSend(Symbol(),OP_SELL,Lots,Bid,5,Bid+StopLoss*Point,Bid-TakeProfit*Point);//Opening Sell
    }
 

}




//----------
return(0);
  }
//------------------------------------------------------------------+