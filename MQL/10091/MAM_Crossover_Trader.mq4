//--------------------------------------------------------------------
// MAM_Crossover_Trader.mq4 
//
// Developed based on MAM_Crossover indicator by pramono72
// Code Base: http://codebase.mql4.com/7113
//
//
// I hope that you would earn a lot of money from this EA!
// Also I should thank the guy who provided that idea of MAM_Crossover through his indicator.

//--------------------------------------------------------------------
#property copyright "CyrusTheGreat"
#property link      "www.metaquotes.com"
//--------------------------------------------------------------- 1 --
                                  
extern double StopLoss   =40;      // SL for an opened order
extern double TakeProfit =190;     // TP for an opened order
extern int    Period_MA  =20;      // Period of MA 1
extern double Lots       =0.1;     // Strictly set amount of lots
double Prots             =0.07;    // Percent of Free Margin

bool Work=true;                    // EA will work
string Symb;                       // Symbol Name
//--------------------------------------------------------------- 2 --
int start()
  {
   int
   Total,                           // Amount of orders in a window 
   Tip=-1,                          // Type of selected order (B=0,S=1)
   Ticket;                          // Order Number
   double
   A,B,C,D,E,F,x1,y1,x2,y2,x3,y3,   // Variables which are used by MAM Crossover
   Lot,                             // Amount of lots in a selected order
   Lts,                             // Amount of lots in an opened order
   Min_Lot,                         // Minimal amount of lots
   Step,                            // Step of lot size change
   Free,                            // Current free margin
   One_Lot,                         // Price of one lot
   Price,                           // Price of a selected order
   SL,                              // SL of a selected order
   TP;                              // TP OF a selected order
   bool
   Ans  =false,                     // Server response after closing
   Cls_B=false,                     // Criterion for closing Buy
   Cls_S=false,                     // Criterion for closing Sell
   Opn_B=false,                     // Criterion for opening Buy
   Opn_S=false;                     // Criterion for opening Sell
//--------------------------------------------------------------- 3 --
   //  Preliminary processing
   if(Bars < Period_MA)                       // Not enough bars
     {
      Alert("Not enough bars in the window. EA does not work");
      return;                                   // Exit start ()
     }
   if(Work==false)                              // Critical Error
     {
      Alert("Critical error. EA does not work");
      return;                                   // Exit Start ()
     }
//--------------------------------------------------------------- 4 --
   // Orders accounting
   Symb=Symbol();                               // Security Name
   Total=0;                                     // Amount of orders
   for(int i=1; i<=OrdersTotal(); i++)          // Loop through orders
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // If there is the next one
        {                                       // Analyzing orders
         if (OrderSymbol()!=Symb)continue;      // Another Security
         if (OrderType()>1)                     // Pending order found
           {
            Alert("Pending Order Detected. EA does not work");
            return;                             // Exit Start ()
           }
         Total++;                               // Counter of market orders
         if (Total>1)                           // No more than one order
           {
            Alert("Several Marker Orders. EA does not work");
            return;                             // Exit Start ()
           }
         Ticket=OrderTicket();                  // Number of selected order
         Tip   =OrderType();                    // Type of selected order
         Price =OrderOpenPrice();               // Price of selected order
         SL    =OrderStopLoss();                // SL of selected order
         TP    =OrderTakeProfit();              // TP of selected order
         Lot   =OrderLots();                    // Amount of Lots
        }
     }
//--------------------------------------------------------------- 5 --
   //  Trading criteria
      A=iMA(NULL,0,Period_MA,0,MODE_SMA,PRICE_CLOSE,1);
      B=iMA(NULL,0,Period_MA,0,MODE_SMA,PRICE_OPEN,1);
      C=iMA(NULL,0,Period_MA,0,MODE_SMA,PRICE_CLOSE,2);
      D=iMA(NULL,0,Period_MA,0,MODE_SMA,PRICE_OPEN,2);
      E=iMA(NULL,0,Period_MA,0,MODE_SMA,PRICE_CLOSE,0);
      F=iMA(NULL,0,Period_MA,0,MODE_SMA,PRICE_OPEN,0);
      
      x1 = (A-B);
      y1 = (B-A);
      x2 = (C-D);
      y2 = (D-C);
      x3 = (E-F);
      y3 = (F-E);

   if ((x1>y1)&& (x2<y2)&&(x3>y3))              // If Difference between
     {                                          // MA 1 & 2 is large
      Opn_B=true;                               // Criterion for opening Buy
      Cls_S=true;                               // Criterion for closing Sell
     }
   if ((x1<y1)&& (x2>y2)&&(x3<y3))              // If difference between 
     {                                          // MA 1 & 2 is large
      Opn_S=true;                               // Criterion for opening sell
      Cls_B=true;                               // Criterion for closing buy
     }
//--------------------------------------------------------------- 6 --
   // Closing orders
   while(true)                                  // Loop of closing orders
     {
      if (Tip==0 && Cls_B==true)                // Order Buy is opened
        {                                       //and there is Criterion to close
         Alert("Attempt to close buy ",Ticket,"Waiting for response");
         RefreshRates();                        // Refresh Rates
         Ans=OrderClose(Ticket,Lot,Bid,2);      // Closing Buy
         if (Ans==true)                         // Success :)
           {
            Alert ("Close order BUY",Ticket);
            break;                              // Exit closing loop
           }
         if (Fun_Error(GetLastError())==1)      // Preocessing Error
            continue;                           // Retrying
         return;                                // Exit Start ()
        }

      if (Tip==1 && Cls_S==true)                // Order Sell is opened
        {                                       // and there is Criterion to close
         Alert("Attemp to close sell",Ticket,"Waiting for response");
         RefreshRates();                        // Refresh rates
         Ans=OrderClose(Ticket,Lot,Ask,2);      // Closing Sell
         if (Ans==true)                         // Success :)
           {
            Alert ("Close order SELL",Ticket);
            break;                              // Exit closing loop
           }
         if (Fun_Error(GetLastError())==1)      // Processing Errors
            continue;                           // Retrying 
         return;                                // Exit Start()
        }
      break;                                    // Exit While
     }
//--------------------------------------------------------------- 7 --
   // Order value
   RefreshRates();                              // Refresh Rates
   Min_Lot=MarketInfo(Symb,MODE_MINLOT);        // Minimal number of lots
   Free   =AccountFreeMargin();                 // Free Margin
   One_Lot=MarketInfo(Symb,MODE_MARGINREQUIRED);// Price of 1 Lot
   Step   =MarketInfo(Symb,MODE_LOTSTEP);       // Step is changed

   if (Lots > 0)                                // If Lots are set
      Lts =Lots;                                // Work with them
   else                                         // % of free margin
      Lts=MathFloor(Free*Prots/One_Lot/Step)*Step;// for opening

   if(Lts < Min_Lot) Lts=Min_Lot;               // Not less than minimal
   if (Lts*One_Lot > Free)                      // Lot larger than free margin
     {
      Alert("Not Enough Money For", Lts,"Lots");
      return;                                   // Exit Start ()
     }
//--------------------------------------------------------------- 8 --
   // Opening orders
   while(true)                                  // Order Closing loop
     {
      if (Total==0 && Opn_B==true)              // No new orders +
        {                                       // Criterion for opening BUY
         RefreshRates();                        // Refresh rates
         SL=Bid - New_Stop(StopLoss)*Point;     // Calculating SL of opened
         TP=Bid + New_Stop(TakeProfit)*Point;   // Calculating TP of opened
         Alert("Attempt to open BUY. Waiting for response..");
         Ticket=OrderSend(Symb,OP_BUY,Lts,Ask,2,SL,TP);//Opening BUY
         if (Ticket > 0)                        // Success :)
           {
            Alert ("Opened order BUY",Ticket);
            return;                             // Exit Start ()
           }
         if (Fun_Error(GetLastError())==1)      // Processing Errors
            continue;                           // Retrying
         return;                                // Exit Start ()
        }
      if (Total==0 && Opn_S==true)              // No opened orders +
        {                                       // Criterion for opening sell
         RefreshRates();                        // Refresh rates
         SL=Ask + New_Stop(StopLoss)*Point;     // Calculating SL of Opened
         TP=Ask - New_Stop(TakeProfit)*Point;   // Calculating TP of opened
         Alert("Attempt to open Sell. Waiting for response..");
         Ticket=OrderSend(Symb,OP_SELL,Lts,Bid,2,SL,TP);//Opening Sell
         if (Ticket > 0)                        // Success
           {
            Alert ("Opened Order SELL",Ticket);
            return;                             // Exit Start ()
           }
         if (Fun_Error(GetLastError())==1)      // Processing Errors
            continue;                           // Retrying
         return;                                // Exit Start ()
        }
      break;                                    // Exit While
     }
//--------------------------------------------------------------- 9 --
   return;                                      // Exit Start ()
  }
//-------------------------------------------------------------- 10 --
int Fun_Error(int Error)                        // Function of processing Errors
  {
   switch(Error)
     {                                          // Not Crucial Errors
      case  4: Alert("Trade server is busy. Trying once again..");
         Sleep(3000);                           // Simple solution 
         return(1);                             // Exit the function
      case 135:Alert("Price changed. Trying once again..");
         RefreshRates();                        // Refresh rates
         return(1);                             // Exit the Function
      case 136:Alert("No prices. Waiting for a new tick");
         while(RefreshRates()==false)           // Till a new tick
            Sleep(1);                           // Pause in the loop
         return(1);                             // Exit the Function
      case 137:Alert("Broker is busy. Trying once again");
         Sleep(3000);                           // Simple solution
         return(1);                             // Exit the function
      case 146:Alert("Trading sub-system is busy. Trying once again..");
         Sleep(500);                            // Simple solution
         return(1);                             // Exit the function
         // Critical Errors
      case  2: Alert("Common Error");
         return(0);                             // Exit the function
      case  5: Alert("Old terminal version");
         Work=false;                            // Terminate operation
         return(0);                             // Exit the function
      case 64: Alert("Account Blocked.");
         Work=false;                            // Terminate operation
         return(0);                             // Exit the function
      case 133:Alert("Trading Forbidden");
         return(0);                             // Exit the function
      case 134:Alert("Not Enough Money to Execute Operation");
         return(0);                             // Exit the function
      default: Alert("Error occured",Error);    // Other variants 
         return(0);                             // Exit the function
     }
  }
//-------------------------------------------------------------- 11 --
int New_Stop(int Parametr)                      // Checking stop levels
  {
   int Min_Dist=MarketInfo(Symb,MODE_STOPLEVEL);// Minimal distance 
   if (Parametr<Min_Dist)                       // If less than allowed
     {
      Parametr=Min_Dist;                        // Sett allowed
      Alert("Increased Distance of Stop Level");
     }
   return(Parametr);                            // Returning Value
  }
//-------------------------------------------------------------- 12 --