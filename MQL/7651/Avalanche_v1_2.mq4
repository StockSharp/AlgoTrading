//+---------------------------------------------------------------------------+
//|                                                         Avalanche_v1.0.mq4|
//|                                                    Version 1.0: 03-04-2006|
//|                                                    Version 1.1: 03-15-2006| Added more error trapping
//|                                                    Version 1.2: 03-16-2006| Corrected some bugs and ERP Buffer
//|                                                Copyright © 2006, HoggZilla|
//|http://www.strategybuilderfx.com/forums/showthread.php?t=16102&page=1&pp=40|
//+---------------------------------------------------------------------------+
#property copyright "Copyright © 2006, HoggZilla"
#property link      "http://www.strategybuilderfx.com/forums/showthread.php?t=16102&page=1&pp=40"
//----
int nOrderMagicNumber                     =     12345;
//----
extern double dBaseLotSize                =     0.1;     // This is the "x" Value (base Lot Size)
extern int dX_Away                        =     1;     // X Value for Away Trades
extern int dX_Toward                      =     2;     // X Value for Toward Trades
extern int dX_Toward_Int                  =     3;     // X Value for Toward Trades paying Interest
//----
double dLots_Toward;                                     // Trade Lots after multiplier
double dLots_Away;   
double dPrice_Toward;
double dPrice_Away; 
//----
extern double dStopLoss_Toward            =     0;       // StopLoss if desired, not reccommended on Towards
extern double dStopLoss_Away              =     10;   
//----
double dStopLossPrice_Toward              =     0;
double dStopLossPrice_Away                =     0;                  
//----
extern int nSlippage                      =     20;
string strComment                         =     "Avalanche";
//----
int strArrowColor_Toward;
int strArrowColor_Away;
//----
extern int nERP_ChartPeriod               =     480;     // The chart used in the ERP, 240 = 4hour
extern int nERP_NumberOfPeriods           =     100;     // The number of periods used in the SMA on the chart
extern int nERP_ChangeBuffer              =     50;      // This is the buffer before changing ERP Position
extern int nInterval_Toward               =     10;       // Interval between Toward Trade Orders
extern int nInterval_Away                 =     10;       // Interval between Away Trade Orders
extern double dTakeProfit_Toward          =     10;       // Take Profit setting on Toward Trades
extern double dTakeProfit_Away            =     8;       // Take Profit setting on Away Trades
//----
double dTakeProfitPrice_Toward            =     0;
double dTakeProfitPrice_Away              =     0;
//----
extern int nStackBuffer_Toward            =     5;       // Stacking Buffer on Toward Trades
extern int nStackBuffer_Away              =     5;       // Stacking Buffer on Away Trades
extern bool bCancelOpenOrders             =     true;    // Cancel all open orders, the program will set new orders 
extern bool bCloseOpenTrades              =     false;   // Are you sure you want to set this to true?
extern bool bOpenStartingOrders           =     true;    // Should the program automatically open new orders when initiated
//----
int ticket                                =     0;
int nTradeOperation_Toward;                              // Buy or Sell - the number for OrderSend 
int nTradeOperation_Away;
//----
string strOp_Toward;                                     // Buy or Sell - the word for Printing
string strOp_Away;
//----
double dCurrentOrderPrice_Toward;
double dCurrentOrderPrice_Away;
//----
string strInterestDirection;
//----
double dCurrentPrice_Toward;
double dCurrentPrice_Away;
double dDistCurrentOrder_Away;
double dDistCurrentOrder_Toward;
double dDistCurrentStackOrder_Away;
double dDistCurrentStackOrder_Toward;
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
   static int nFirstRun;
   //
   Print("*****START******* Bid:",Bid," Ask:",Ask);
//----
 if(nFirstRun==0)
 {  
   Print(Symbol());
//----   
   strInterestDirection = func_InterestDirection(StringSubstr(Symbol(),0,6));
   Print("Interest Direction: ",strInterestDirection);
   // Where is the ERP and Price to ERP Position
   double dERP = iMA(NULL,nERP_ChartPeriod,nERP_NumberOfPeriods,0,0,5,0);
   static string strERPPosition;
   strERPPosition = func_ERPPosition(dERP,Bid,"NONE",nERP_ChangeBuffer);
   static string strLastERPPosition;
   strLastERPPosition=strERPPosition;
   // 1. Cancel Pending Orders 
   // 2. Open Staring Positions if none exist 
   // 3. Open First Stop Orders for Away and Towards
   // --------------------------------------------------------------------------
   // 1. Cancel Pending Orders 
   if(bCancelOpenOrders==true)
      {
         func_ClosePendingOrders();
      }
   if(bCloseOpenTrades==true)
      {
         func_CloseOpenTrades();
      }
   // 2. Open Starting Positions if none exist
   int nCurrentOpen = func_CountOpenPositions();
   Print("Current Open Positions: ",nCurrentOpen);  
   //----
   if(strERPPosition=="Below")
         {
            nTradeOperation_Toward   = 0; // Buy
            nTradeOperation_Away     = 1; // Sell
            //   
            dPrice_Toward              = Ask;
            dPrice_Away                = Bid;
            //
            Print("Program is Starting: Bid: ",Bid," Ask: ",Ask);
//----
            dTakeProfitPrice_Toward    = (Ask + (dTakeProfit_Toward * Point));
            dTakeProfitPrice_Away      = (Bid - (dTakeProfit_Away * Point));
            //
            dStopLossPrice_Toward = 0;
            dStopLossPrice_Away = 0;
            if(dStopLoss_Toward != 0) dStopLossPrice_Toward = (Ask - (dStopLoss_Toward * Point));
            if(dStopLoss_Away != 0) dStopLossPrice_Away = (Bid + (dStopLoss_Away * Point));
            //
            strArrowColor_Toward       = Green;
            strArrowColor_Away         = Red;
            //
            if(strInterestDirection=="BUY")
               {
                  dLots_Toward = (dBaseLotSize * dX_Toward_Int);
               }
               else
               {
                  dLots_Toward = (dBaseLotSize * dX_Toward);
               }
            dLots_Away = (dBaseLotSize * dX_Away);
         }
      else
         {
            nTradeOperation_Toward   = 1;
            nTradeOperation_Away     = 0;
            //  
            dPrice_Toward              = Bid;
            dPrice_Away                = Ask;
            //
            dTakeProfitPrice_Toward    = (Bid - (dTakeProfit_Toward*Point));
            dTakeProfitPrice_Away      = (Ask + (dTakeProfit_Away * Point));
            //
            dStopLossPrice_Toward = 0;
            dStopLossPrice_Away = 0;
            if(dStopLoss_Toward != 0) dStopLossPrice_Toward = (Bid + (dStopLoss_Toward * Point));
            if(dStopLoss_Away != 0) dStopLossPrice_Away = (Ask - (dStopLoss_Away * Point));
            //
            strArrowColor_Toward       = Red;
            strArrowColor_Away         = Green;
            if(strInterestDirection=="SELL")
               {
                  dLots_Toward = (dBaseLotSize * dX_Toward_Int);
               }
               else
               {
                  dLots_Toward = (dBaseLotSize * dX_Toward);
               }
            dLots_Away = (dBaseLotSize * dX_Away);
         }
   dCurrentOrderPrice_Toward    = dPrice_Toward;
   dCurrentOrderPrice_Away      = dPrice_Away;
   if((nCurrentOpen < 1) && (bOpenStartingOrders == true))
      {        
         // Open the Market Orders 
         // Toward
        while(true)
        {
         ticket = OrderSend(Symbol(), nTradeOperation_Toward, dLots_Toward, dPrice_Toward, nSlippage, dStopLossPrice_Toward, dTakeProfitPrice_Toward, strComment, nOrderMagicNumber, 0, strArrowColor_Toward);
         if(ticket<0)
            {
               Print("Opening Order ERROR: ",GetLastError());
               int error = GetLastError();
               if(error==134) break; // not enough money
               RefreshRates(); // prices might have already changed
            }
         else
            {
               break;
            }
        } //end While
        while(true)
        {
         // Away
         ticket = OrderSend(Symbol(), nTradeOperation_Away, dLots_Away, dPrice_Away, nSlippage, dStopLossPrice_Away, dTakeProfitPrice_Away, strComment, nOrderMagicNumber, 0, strArrowColor_Away);
         if(ticket<0)
            {
               Print("Opening Order ERROR: ",GetLastError());  
            }
         else
            {
               break;
            }
        } //end While
      }
   // 3. Open the first Stop Orders 
   if(strERPPosition=="Below")
      {
         nTradeOperation_Toward   = 4; // Buy Stop
         nTradeOperation_Away     = 5; // Sell Stop
         //
         strOp_Toward               = "BUY";
         strOp_Away                 = "SELL";
         //   
         dPrice_Toward              = (Ask + (nInterval_Toward * Point));
         dPrice_Away                = (Bid - (nInterval_Away * Point));
         //   
         dTakeProfitPrice_Toward    = (dPrice_Toward + (dTakeProfit_Toward * Point));
         dTakeProfitPrice_Away      = (dPrice_Away - (dTakeProfit_Away * Point));
         //
         dStopLossPrice_Toward = 0;
         dStopLossPrice_Away = 0;
         if(dStopLoss_Toward != 0) dStopLossPrice_Toward = (Ask - (dStopLoss_Toward * Point));
         if(dStopLoss_Away != 0) dStopLossPrice_Away = (Bid + (dStopLoss_Away * Point));
         // 
         strArrowColor_Toward       = Green;
         strArrowColor_Away         = Red;   
         if(strInterestDirection=="BUY")
            {
               dLots_Toward = (dBaseLotSize * dX_Toward_Int);
            }
         else
            {
               dLots_Toward = (dBaseLotSize * dX_Toward);
            }
         dLots_Away = (dBaseLotSize * dX_Away);
      }
   else
      {
         nTradeOperation_Toward   = 5;
         nTradeOperation_Away     = 4;
         //  
         strOp_Toward               = "SELL";
         strOp_Away                 = "BUY";
         //   
         dPrice_Toward              = (Bid - (nInterval_Toward * Point));
         dPrice_Away                = (Ask + (nInterval_Away * Point));
         //   
         dTakeProfitPrice_Toward    = (dPrice_Toward - (dTakeProfit_Toward*Point));
         dTakeProfitPrice_Away      = (dPrice_Away + (dTakeProfit_Away * Point));
         //
         dStopLossPrice_Toward = 0;
         dStopLossPrice_Away = 0;
         if(dStopLoss_Toward != 0) dStopLossPrice_Toward = (Bid + (dStopLoss_Toward * Point));
         if(dStopLoss_Away != 0) dStopLossPrice_Away = (Ask - (dStopLoss_Away * Point));
         //   
         strArrowColor_Toward       = Red;
         strArrowColor_Away         = Green; 
         if(strInterestDirection=="SELL")
            {
               dLots_Toward = (dBaseLotSize * dX_Toward_Int);
            }
         else
            {
               dLots_Toward = (dBaseLotSize * dX_Toward);
            }
         dLots_Away = (dBaseLotSize * dX_Away);
      }
   dCurrentOrderPrice_Toward    = dPrice_Toward;
   dCurrentOrderPrice_Away      = dPrice_Away;
   // Toward
   ticket = OrderSend(Symbol(), nTradeOperation_Toward, dLots_Toward, dPrice_Toward, nSlippage, dStopLossPrice_Toward, dTakeProfitPrice_Toward, strComment, nOrderMagicNumber, 0, strArrowColor_Toward);
   if(ticket<0)
      {
         Print("Opening Order ERROR: ",GetLastError());  
      }
   // Away
   ticket = OrderSend(Symbol(), nTradeOperation_Away, dLots_Away, dPrice_Away, nSlippage, dStopLossPrice_Away, dTakeProfitPrice_Away, strComment, nOrderMagicNumber, 0, strArrowColor_Away);
   if(ticket<0)
      {
         Print("Opening Order ERROR: ",GetLastError());  
      }
 } 
   nFirstRun=nFirstRun+1;   
// *********************
// This section runs each time the price changes
// *********************
   static double dLastStackOrder_Toward;
   static double dLastStackOrder_Away;
   static double dLastOrder_Toward;
   static double dLastOrder_Away;
//----
   dERP = iMA(NULL,nERP_ChartPeriod,nERP_NumberOfPeriods,0,0,5,0);
   strERPPosition = func_ERPPosition(dERP,Bid,strLastERPPosition,nERP_ChangeBuffer);
   Print("Curr ERP: ",strERPPosition);
   Print("Last ERP: ",strLastERPPosition);
   if(strERPPosition!=strLastERPPosition)
      {
         Print("************ ERP CHANGE: ",strLastERPPosition," to ",strERPPosition);
         //Leave trades Open but Cancel all Orders 
         func_ClosePendingOrders();
         //Set distances so that new orders will be set 
         dLastOrder_Toward=9999;
         dLastOrder_Away=9999;
         dLastStackOrder_Toward=0;
         dLastStackOrder_Away=0;
         nFirstRun=1;
      }
   if(strERPPosition=="Below")
      {
         nTradeOperation_Toward   = 4; // Buy Stop
         nTradeOperation_Away     = 5; // Sell Stop
         //  
         strOp_Toward               = "BUY";
         strOp_Away                 = "SELL";
         //   
         dPrice_Toward              = (Ask + (nInterval_Toward * Point));
         dPrice_Away                = (Bid - (nInterval_Away * Point));
         //
         dCurrentPrice_Toward       = Ask;
         dCurrentPrice_Away         = Bid;
         //   
         dTakeProfitPrice_Toward    = (dPrice_Toward + (dTakeProfit_Toward * Point));
         dTakeProfitPrice_Away      = (dPrice_Away - (dTakeProfit_Away * Point));
         //
         dStopLossPrice_Toward = 0;
         dStopLossPrice_Away = 0;
         if(dStopLoss_Toward != 0) dStopLossPrice_Toward = (dPrice_Toward - (dStopLoss_Toward * Point));
         if(dStopLoss_Away != 0) dStopLossPrice_Away = (dPrice_Away + (dStopLoss_Away * Point)); 
         strArrowColor_Toward       = Green;
         strArrowColor_Away         = Red;
         if(strInterestDirection=="BUY")
            {
               dLots_Toward = (dBaseLotSize * dX_Toward_Int);
            }
         else
            {
               dLots_Toward = (dBaseLotSize * dX_Toward);
            }
         dLots_Away = (dBaseLotSize * dX_Away);
      }
   else
      {
         nTradeOperation_Toward   = 5;
         nTradeOperation_Away     = 4;
         //  
         strOp_Toward               = "SELL";
         strOp_Away                 = "BUY";
         //   
         dPrice_Toward              = (Bid - (nInterval_Toward * Point));
         dPrice_Away                = (Ask + (nInterval_Away * Point));
         //
         dCurrentPrice_Toward       = Bid;
         dCurrentPrice_Away         = Ask;
         //   
         dTakeProfitPrice_Toward    = (dPrice_Toward - (dTakeProfit_Toward*Point));
         dTakeProfitPrice_Away      = (dPrice_Away + (dTakeProfit_Away * Point));
         //
         dStopLossPrice_Toward = 0;
         dStopLossPrice_Away = 0;
         if(dStopLoss_Toward != 0) dStopLossPrice_Toward = (dPrice_Toward + (dStopLoss_Toward * Point));
         if(dStopLoss_Away != 0) dStopLossPrice_Away = (dPrice_Away - (dStopLoss_Away * Point));   
         strArrowColor_Toward       = Red;
         strArrowColor_Away         = Green;
         if(strInterestDirection=="SELL")
            {
               dLots_Toward = (dBaseLotSize * dX_Toward_Int);
            }
         else
            {
               dLots_Toward = (dBaseLotSize * dX_Toward);
            }
         dLots_Away = (dBaseLotSize * dX_Away);
      }
   // Should we open a new Order 
   if(dLastOrder_Toward==0) dLastOrder_Toward=dCurrentOrderPrice_Toward;
   if(dLastOrder_Away==0) dLastOrder_Away=dCurrentOrderPrice_Away;
   if(dLastOrder_Toward==9999) dCurrentOrderPrice_Toward=dCurrentPrice_Toward;
   if(dLastOrder_Away==9999) dCurrentOrderPrice_Away=dCurrentPrice_Away;
   if(dLastOrder_Toward==9999) dLastOrder_Toward=dCurrentPrice_Toward;
   if(dLastOrder_Away==9999) dLastOrder_Away=dCurrentPrice_Away;
   if(nFirstRun==1)
  {
   if((dLastStackOrder_Toward==0) &&  (nTradeOperation_Toward==4)) 
      {
         dLastStackOrder_Toward=(dCurrentOrderPrice_Toward-(nInterval_Toward*Point));
      }
   else
      {
         dLastStackOrder_Toward=(dCurrentOrderPrice_Toward+(nInterval_Toward*Point));
      }
   if((dLastStackOrder_Away==0) &&  (nTradeOperation_Away==4)) 
      {
         dLastStackOrder_Away=(dCurrentOrderPrice_Away-(nInterval_Away*Point));
      }
   else
      {
         dLastStackOrder_Away=(dCurrentOrderPrice_Away+(nInterval_Away*Point));
      }
   Print("LastStackAway:",dLastStackOrder_Away," ","LastStackToward:",dLastStackOrder_Toward);
  }
   // Has the price changed enough to open a new order
   // This is for orders when the price is moving in favor of our current position
   if(nTradeOperation_Toward==4) //Buy
      {
         dDistCurrentOrder_Toward = ((dLastOrder_Toward - dCurrentPrice_Toward)/Point);
         Print("Distance Toward BUY: ",dDistCurrentOrder_Toward);
         dDistCurrentStackOrder_Toward = ((dLastStackOrder_Toward - dCurrentPrice_Toward)/Point);
         Print("Stack Distance Toward BUY: ",dLastStackOrder_Toward," -P",dCurrentPrice_Toward," = ",dDistCurrentStackOrder_Toward);
      }
   else //Sell
      {
         dDistCurrentOrder_Toward = ((dCurrentPrice_Toward - dLastOrder_Toward)/Point);
         Print("Distance Toward SELL: ",dDistCurrentOrder_Toward);
         dDistCurrentStackOrder_Toward = ((dCurrentPrice_Toward - dLastStackOrder_Toward)/Point);
         Print("Stack Distance Toward SELL: ",dCurrentPrice_Toward,"P- ",dLastStackOrder_Toward," = ",dDistCurrentStackOrder_Toward);
      }   
   if(nTradeOperation_Away==4) //Buy
      {
         dDistCurrentOrder_Away = ((dLastOrder_Away - dCurrentPrice_Away)/Point);
         Print("Distance Away BUY: ",dDistCurrentOrder_Away);
         dDistCurrentStackOrder_Away = ((dLastStackOrder_Away - dCurrentPrice_Away)/Point);
         Print("Stack Distance Away BUY: ",dLastStackOrder_Away," -P",dCurrentPrice_Away," = ",dDistCurrentStackOrder_Away);
      }
   else //Sell
      {
         dDistCurrentOrder_Away = ((dCurrentPrice_Away - dLastOrder_Away)/Point);
         Print("Distance Away SELL: ",dDistCurrentOrder_Away);
         dDistCurrentStackOrder_Away = ((dCurrentPrice_Away - dLastStackOrder_Away)/Point);
         Print("Stack Distance Away SELL: ",dCurrentPrice_Away,"P- ",dLastStackOrder_Away," = ",dDistCurrentStackOrder_Away);
      }
   if(dDistCurrentOrder_Toward <= 0) //time to set a new Stop Order Toward
      {
         if(nTradeOperation_Toward==4) 
            {
               // Toward - Buy
               dPrice_Toward                 = (dLastOrder_Toward + (nInterval_Toward*Point));
               dTakeProfitPrice_Toward       = (dPrice_Toward + (dTakeProfit_Toward*Point));
               dStopLossPrice_Toward = 0;
               if(dStopLoss_Toward != 0) dStopLossPrice_Toward = (dPrice_Toward - (dStopLoss_Toward * Point));
//----
               Print("Open Toward BUY: ",strOp_Toward,": ",dPrice_Toward," TP: ",dTakeProfitPrice_Toward);
               ticket = OrderSend(Symbol(), nTradeOperation_Toward, dLots_Toward, dPrice_Toward, nSlippage, dStopLossPrice_Toward, dTakeProfitPrice_Toward, strComment, nOrderMagicNumber, 0, strArrowColor_Toward);
               if(ticket<0)
                  {
                     Print("Opening Toward BUY Order ERROR: ",GetLastError());  
                  }
               else
                  {
                     dLastOrder_Toward             = dPrice_Toward;  //this is now the 'new' price for Last Order Toward
                     dLastStackOrder_Toward        = dPrice_Toward - (nInterval_Toward*Point);
                     Print("Created NEW Toward at: ",dLastOrder_Toward);
                  }
            }
         else
            {
               // Toward - Sell
               dPrice_Toward                 = (dLastOrder_Toward - (nInterval_Toward*Point));
               dTakeProfitPrice_Toward       = (dPrice_Toward - (dTakeProfit_Toward*Point));
               dStopLossPrice_Toward = 0;
               if(dStopLoss_Toward != 0) dStopLossPrice_Toward = (dPrice_Toward + (dStopLoss_Toward * Point));
//----               
               Print("Open Toward SELL: ",strOp_Toward,": ",dPrice_Toward," TP: ",dTakeProfitPrice_Toward);
               ticket = OrderSend(Symbol(), nTradeOperation_Toward, dLots_Toward, dPrice_Toward, nSlippage, dStopLossPrice_Toward, dTakeProfitPrice_Toward, strComment, nOrderMagicNumber, 0, strArrowColor_Toward);
               if(ticket<0)
                  {
                     Print("Opening Toward SELL Order ERROR: ",GetLastError());  
                  }
               else
                  {
                     dLastOrder_Toward             = dPrice_Toward;  //this is now the 'new' price for Last Order Toward
                     dLastStackOrder_Toward        = dPrice_Toward + (nInterval_Toward*Point);
                     Print("Created NEW Toward at: ",dLastOrder_Toward);
                  }
            }   
      }
   if(dDistCurrentOrder_Away <= 0) //time to set a new Stop Order Away
      {
         if(nTradeOperation_Away==4) 
            {
               // Away - Buy
               dPrice_Away                   = (dLastOrder_Away + (nInterval_Away*Point));
               dTakeProfitPrice_Away         = (dPrice_Away + (dTakeProfit_Away*Point));
               dStopLossPrice_Away = 0;
               if(dStopLoss_Away != 0) dStopLossPrice_Away = (dPrice_Away - (dStopLoss_Away * Point));
//----               
               Print("Open Away BUY: ",strOp_Away,": ",dPrice_Away," TP: ",dTakeProfitPrice_Away);
               ticket = OrderSend(Symbol(), nTradeOperation_Away, dLots_Away, dPrice_Away, nSlippage, dStopLossPrice_Away, dTakeProfitPrice_Away, strComment, nOrderMagicNumber, 0, strArrowColor_Away);
               if(ticket<0)
                  {
                     Print("Opening Away BUY Order ERROR: ",GetLastError());  
                  }
               else
                  {
                     dLastOrder_Away               = dPrice_Away;  //this is now the 'new' price for Last Order Away
                     dLastStackOrder_Away          = dPrice_Away - (nInterval_Away*Point);
                     Print("Created NEW Away at: ",dLastOrder_Away);
                  }
            }
         else
            {
               // Away - Sell
               dPrice_Away                   = (dLastOrder_Away - (nInterval_Away*Point));
               dTakeProfitPrice_Away         = (dPrice_Away - (dTakeProfit_Away*Point));
               dStopLossPrice_Away = 0;
               if(dStopLoss_Away != 0) dStopLossPrice_Away = (dPrice_Away + (dStopLoss_Away * Point));
//----         
               Print("Open Away SELL: ",strOp_Away,": ",dPrice_Away," TP: ",dTakeProfitPrice_Away);
               ticket = OrderSend(Symbol(), nTradeOperation_Away, dLots_Away, dPrice_Away, nSlippage, dStopLossPrice_Away, dTakeProfitPrice_Away, strComment, nOrderMagicNumber, 0, strArrowColor_Away);
               if(ticket<0)
                  {
                     Print("Opening Away SELL Order ERROR: ",GetLastError());  
                  }
               else
                  {
                     dLastOrder_Away               = dPrice_Away;  //this is now the 'new' price for Last Order Away
                     dLastStackOrder_Away          = dPrice_Away + (nInterval_Away*Point);
                     Print("Created NEW Away at: ",dLastOrder_Away);
                  }
            }      
      }
   // Should we open a new Stack Order
   // This is an order when the price is going against the current position
   if(dDistCurrentStackOrder_Toward >= (nInterval_Toward + nStackBuffer_Toward)) //time to set a new Stack Order Toward
      {
         if(nTradeOperation_Toward==4) 
            {
               // Toward - Buy
               dPrice_Toward                 = (dLastStackOrder_Toward - (nInterval_Toward*Point));
               dTakeProfitPrice_Toward       = (dPrice_Toward + (dTakeProfit_Toward*Point));
               dStopLossPrice_Toward = 0;
               if(dStopLoss_Toward != 0) dStopLossPrice_Toward = (dPrice_Toward - (dStopLoss_Toward * Point));
//----               
               Print("Open Toward Stack BUY: ",strOp_Toward,": ",dPrice_Toward," TP: ",dTakeProfitPrice_Toward);
               ticket = OrderSend(Symbol(), nTradeOperation_Toward, dLots_Toward, dPrice_Toward, nSlippage, dStopLossPrice_Toward, dTakeProfitPrice_Toward, strComment, nOrderMagicNumber, 0, strArrowColor_Toward);
               if(ticket<0)
                  {
                     Print("Opening Stack BUY Toward Order ERROR: ",GetLastError());  
                  }
               else
                  {
                     dLastStackOrder_Toward        = dPrice_Toward;  //this is now the 'new' price for Last Stack Order Toward
                     Print("Create NEW Stack BUY Toward at: ",dLastStackOrder_Toward," TP:",dTakeProfitPrice_Toward);
                  }
            }
         else
            {
               // Toward - Sell
               dPrice_Toward                 = (dLastStackOrder_Toward + (nInterval_Toward*Point));
               dTakeProfitPrice_Toward       = (dPrice_Toward - (dTakeProfit_Toward*Point));
               dStopLossPrice_Toward = 0;
               if(dStopLoss_Toward != 0) dStopLossPrice_Toward = (dPrice_Toward + (dStopLoss_Toward * Point));
//----               
               Print("Open Toward Stack SELL: ",strOp_Toward,": ",dPrice_Toward," TP: ",dTakeProfitPrice_Toward);
               ticket = OrderSend(Symbol(), nTradeOperation_Toward, dLots_Toward, dPrice_Toward, nSlippage, dStopLossPrice_Toward, dTakeProfitPrice_Toward, strComment, nOrderMagicNumber, 0, strArrowColor_Toward);
               if(ticket<0)
                  {
                     Print("Opening Stack SELL Toward Order ERROR: ",GetLastError());  
                  }
               else
                  {
                     dLastStackOrder_Toward        = dPrice_Toward;  //this is now the 'new' price for Last Stack Order Toward
                     Print("Create NEW Stack SELL Toward at: ",dLastStackOrder_Toward," TP:",dTakeProfitPrice_Toward);
                  }
            }      
      }
   if(dDistCurrentStackOrder_Away >= (nInterval_Away + nStackBuffer_Away)) //time to set a new Stack Order Away
      {
         if(nTradeOperation_Away==4) 
            {
               // Away - Buy
               dPrice_Away                   = (dLastStackOrder_Away - (nInterval_Away*Point));
               dTakeProfitPrice_Away         = (dPrice_Away + (dTakeProfit_Away*Point));
               dStopLossPrice_Away = 0;
               if(dStopLoss_Away != 0) dStopLossPrice_Away = (dPrice_Away - (dStopLoss_Away * Point));
//----               
               Print("Open Away Stack BUY: ",strOp_Away,": ",dPrice_Away," TP: ",dTakeProfitPrice_Away);
               ticket = OrderSend(Symbol(), nTradeOperation_Away, dLots_Away, dPrice_Away, nSlippage, dStopLossPrice_Away, dTakeProfitPrice_Away, strComment, nOrderMagicNumber, 0, strArrowColor_Away);
               if(ticket<0)
                  {
                     Print("Opening Stack BUY Away Order ERROR: ",GetLastError());  
                  }
               else
                  {
                     dLastStackOrder_Away          = dPrice_Away;  //this is now the 'new' price for Last Stack Order Away
                     Print("Created NEW Stack BUY Away at: ",dLastStackOrder_Away," TP:",dTakeProfitPrice_Away);
                  }
            }
         else
            {
               // Away - Sell
               dPrice_Away                   = (dLastStackOrder_Away + (nInterval_Away*Point));
               dTakeProfitPrice_Away         = (dPrice_Away - (dTakeProfit_Away*Point));
               dStopLossPrice_Away = 0;
               if(dStopLoss_Away != 0) dStopLossPrice_Away = (dPrice_Away + (dStopLoss_Away * Point));
//----               
               Print("Open Away Stack SELL: ",strOp_Away,": ",dPrice_Away," TP: ",dTakeProfitPrice_Away);
               ticket = OrderSend(Symbol(), nTradeOperation_Away, dLots_Away, dPrice_Away, nSlippage, dStopLossPrice_Away, dTakeProfitPrice_Away, strComment, nOrderMagicNumber, 0, strArrowColor_Away);
               if(ticket<0)
                  {
                     Print("Opening Stack SELL Away Order ERROR: ",GetLastError());  
                  }
               else
                  {
                     dLastStackOrder_Away          = dPrice_Away;  //this is now the 'new' price for Last Stack Order Away
                     Print("Created NEW Stack SELL Away at: ",dLastStackOrder_Away," TP:",dTakeProfitPrice_Away);
                  }
            }      
      }
   strLastERPPosition=strERPPosition;
   Print("*****END*******");
//----
   return(0);
  }
// Which direction pays interest on this pair
string func_InterestDirection(string symbol) 
   {
	  if(symbol=="AUDUSD")     
	  {	
	   return("BUY");
	  }
	  else if(symbol=="AUDNZD") 
	  {	
	   return("SELL");
	  }
	  else if(symbol=="CHFJPY") 
	  {
	  	return("BUY");
	  }
	  else if(symbol=="EURAUD") 
	  {
	 	return("SELL");
	  }
	  else if(symbol=="EURCAD") 
	  {
	  	return("SELL");
	  }
	  else if(symbol=="EURCHF") 
	  {
	  	return("BUY");
	  }
	  else if(symbol=="EURGBP") 
	  {
	  	return("SELL");
	  }
	  else if(symbol=="EURJPY") 
	  {
	  	return("BUY");
	  }
	  else if(symbol=="EURUSD") 
	  {
	  	return("SELL");
	  }
	  else if(symbol=="GBPCHF") 
	  {
	  	return("BUY");
	  }
	  else if(symbol=="GBPJPY") 
	  {
	  	return("BUY");
	  }
	 else if(symbol=="GBPUSD") 
	  {
	   return("BUY");
	  }
	 else if(symbol=="USDCAD") 
	  {
	   return("BUY");
     }
	 else if(symbol=="USDCHF") 
	  {
	   return("BUY");
	  }
	 else if(symbol=="USDJPY") 
	  {
	   return("BUY");
	  }
	 else 
	  {	Comment("Unexpected Symbol - Default"); return("BUY");}
    } 
// Position of Current Price related to ERP
// If the price is above the ERP then our position is Above
// If the price is below the ERP then out position is Below
string func_ERPPosition(double dCurrentERP , double dCurrentPrice, string strLastPosition, int nERPBuffer) 
   {
	  if(strLastPosition=="NONE")
	     {
	     if(dCurrentPrice >= dCurrentERP)      
	        {	
	           return("Above");
	        }
	     else
	        {
	           return("Below");
	        }
	     }    
	  if(strLastPosition=="Above")
	     {
	     if(dCurrentPrice >= (dCurrentERP-nERPBuffer*Point))    
	        {	
	           return("Above");
	        }
	     else
	        {
	           return("Below");
	        }
	     }
	  if(strLastPosition=="Below")
	     {
	     if(dCurrentPrice >= (dCurrentERP+nERPBuffer*Point))     
	        {	
	           return("Above");
	        }
	     else
	        {
	           return("Below");
	        }
	     }
	}
//+------------------------------------------------------------------------+
//| counts the number of open positions                                    |
//+------------------------------------------------------------------------+
int func_CountOpenPositions()
  {  
  int op =0;
  int totalorders = OrdersTotal();
    for(int i=totalorders-1;i>=0;i--)                                // scan all orders and positions...
      {
        OrderSelect(i, SELECT_BY_POS);
        if ( OrderSymbol()==Symbol() && ( (OrderMagicNumber() == 12345) || (OrderComment() == "Test")) )  // only look if mygrid and symbol...
         {  
          int type = OrderType();
          if ( type == OP_BUY ) {op=op+1;} 
          if ( type == OP_SELL ) {op=op+1;} 
         }
      } 
   return(op);
  }
//+------------------------------------------------------------------------+
//| cancels all pending orders                                             |
//+------------------------------------------------------------------------+
void func_ClosePendingOrders()
{
  int totalorders = OrdersTotal();
  for(int i=totalorders-1;i>=0;i--)
 {
    OrderSelect(i, SELECT_BY_POS);
    bool result = false;
    if ( OrderSymbol()==Symbol() && ( (OrderMagicNumber() == nOrderMagicNumber) || (OrderComment() == strComment)) )  // only look if mygrid and symbol...
     {
           //Close pending orders
           if ( OrderType() > 1 ) result = OrderDelete( OrderTicket() );
      }
  }
  return;
}
//+------------------------------------------------------------------------+
//| closes all open trades                                                 |
//+------------------------------------------------------------------------+
void func_CloseOpenTrades()
{
  int totalorders = OrdersTotal();
  for(int i=totalorders-1;i>=0;i--)
 {
    OrderSelect(i, SELECT_BY_POS);
    bool result = false;
    if ( OrderSymbol()==Symbol() && ( (OrderMagicNumber() == nOrderMagicNumber) || (OrderComment() == strComment)) )  // only look if mygrid and symbol...
     {
           //Close opened long positions
           if ( OrderType() == OP_BUY )  result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), 5, Red );
           //Close opened short positions
           if ( OrderType() == OP_SELL )  result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), 5, Red );
      }
  }
  return;
}
//+------------------------------------------------