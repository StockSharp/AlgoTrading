//+------------------------------------------------------------------+
//|                                            WithdrawalTracker.mq4 |
//+------------------------------------------------------------------+
#property copyright "Wamek EA-2025"
#property link      "eawamek@gmail.com"
#property version   "1.00"
#property strict


string MyInitialDeposit;
double LastBalanceChecked;
int PreviousClosedCount;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   P_GloabalInfo(); //   initialise global variable
   
   EventSetTimer(5); // Set a timer to check every 5 seconds
   
   return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   EventKillTimer();
   
  // GlobalVariableDel(MyInitialDeposit);
}

//+------------------------------------------------------------------+
//| Expert timer function                                            |
//+------------------------------------------------------------------+
void OnTimer()
{
TrackWithdrawals();
}
//+------------------------------------------------------------------+



//+------------------------------------------------------------------+
//|  Set global variables                                            |
//+------------------------------------------------------------------+

void  P_GloabalInfo(){

 if(IsTesting())return;
 
   MyInitialDeposit = "myDeposit_" + IntegerToString(AccountNumber());
   
   // Load initial deposit from global variable
   if(GlobalVariableCheck(MyInitialDeposit))
   { 
      double initialDeposit = GlobalVariableGet(MyInitialDeposit);
      Print("Loaded initial deposit from global variable: ", initialDeposit);
   }
   else
   {  
      double initialDeposit = AccountBalance();
      GlobalVariableSet(MyInitialDeposit, initialDeposit);
      Print("Initial deposit set to current balance: ", initialDeposit);
   }
   
   LastBalanceChecked = AccountBalance();
   PreviousClosedCount = OrdersHistoryTotal();

}


//+------------------------------------------------------------------+
//|   Track withdrawals from the account                             |
//+------------------------------------------------------------------+

void TrackWithdrawals(){
 if(IsTesting())return;

   double currentBalance = AccountBalance();
   
   if(currentBalance != LastBalanceChecked)
   {
      int currentClosedCount = OrdersHistoryTotal();
      double sumProfit = 0;
      
      // Calculate sum of profits from new closed trades since last check
      if(currentClosedCount > PreviousClosedCount)
      {
         for(int i = PreviousClosedCount; i < currentClosedCount; i++)
         {
            if(OrderSelect(i, SELECT_BY_POS, MODE_HISTORY))
            {
               // Check if the order is a closed trade (OP_BUY or OP_SELL)
               if(OrderType() == OP_BUY || OrderType() == OP_SELL)
               {
                  sumProfit += OrderProfit() + OrderSwap() + OrderCommission();
               }
            }
         }
      }
      
      // Calculate expected balance change from closed trades
      double expectedBalance = LastBalanceChecked + sumProfit;
      double AmountWithdraw = currentBalance - expectedBalance;
      
      // If withdrawal detected
      if(AmountWithdraw < 0)
      {
         double newInitialDeposit = currentBalance;
         GlobalVariableSet(MyInitialDeposit, newInitialDeposit);
         Print("Withdrawal detected. Initial deposit updated to: ", newInitialDeposit);
      }
      
      // Update last known values
      LastBalanceChecked = currentBalance;
      PreviousClosedCount = currentClosedCount;
   }

}