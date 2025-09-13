//+------------------------------------------------------------------+
//|                                 WithdrawalTracker.mq5            |
//+------------------------------------------------------------------+
#property copyright "Wamek EA-2025"
#property link      "eawamek@gmail.com"
#property version   "1.00"
#property strict


//+------------------------------------------------------------------+
//| Global variables                                                 |
//+------------------------------------------------------------------+
ulong  lastDealId = 0;
double LastBalanceChecked = 0;
string MyInitialDeposit;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
  P_GloabalInfo();  // initialise global variable

   EventSetTimer(5);    // Set high-frequency timer (5s)

   
   return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   EventKillTimer();
//  GlobalVariableDel(MyInitialDeposit);
}

//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
{
 TrackWithdrawals();
}

//+------------------------------------------------------------------+
//| Calculate profit from new deals since last check                 |
//+------------------------------------------------------------------+
double CalculateNewDealsProfit()
{
   double totalProfit = 0.0;
   ulong currentLastDealId = 0;
   
   // Select all deals in history
   if(!HistorySelect(0, TimeCurrent())) return 0.0;

   int totalDeals = HistoryDealsTotal();
   
   for(int i = 0; i < totalDeals; i++)
   {
      ulong ticket = HistoryDealGetTicket(i);
      if(ticket > lastDealId)
      {
         currentLastDealId = MathMax(currentLastDealId, ticket);
         if(HistoryDealGetInteger(ticket, DEAL_ENTRY) == DEAL_ENTRY_OUT)
         {
            double profit = HistoryDealGetDouble(ticket, DEAL_PROFIT) +
                            HistoryDealGetDouble(ticket, DEAL_SWAP) +
                            HistoryDealGetDouble(ticket, DEAL_COMMISSION);
            totalProfit += profit;
         }
      }
   }
   
   return totalProfit;
}


//+------------------------------------------------------------------+
//| Track last deal ID                                               |
//+------------------------------------------------------------------+
void UpdateLastDealId()
{
   if(HistorySelect(0, TimeCurrent()))
   {
      int totalDeals = HistoryDealsTotal();
      if(totalDeals > 0)
         lastDealId = MathMax(lastDealId, HistoryDealGetTicket(totalDeals-1));
   }
}

//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//|  Set global variables                                            |
//+------------------------------------------------------------------+

void  P_GloabalInfo(){

//Check to skip code in the Strategy Tester
   if(MQLInfoInteger(MQL_TESTER)) return;

   // Create unique global variable name
   MyInitialDeposit = "myDeposit_" + IntegerToString(AccountInfoInteger(ACCOUNT_LOGIN));
   
   // Initialize or load initial deposit
   if(!GlobalVariableCheck(MyInitialDeposit))
   {
      double initialDeposit = AccountInfoDouble(ACCOUNT_BALANCE);
      GlobalVariableSet(MyInitialDeposit, initialDeposit);
      Print("Initial deposit set to: ", initialDeposit);
   }
   else
   {
      Print("Loaded existing deposit: ", GlobalVariableGet(MyInitialDeposit));
   }

   // Initialize tracking variables
   LastBalanceChecked = AccountInfoDouble(ACCOUNT_BALANCE);
   UpdateLastDealId();

}




//+------------------------------------------------------------------+
//|   Track withdrawals from the account                             |
//+------------------------------------------------------------------+

void TrackWithdrawals() {
   if(MQLInfoInteger(MQL_TESTER)) return; // Check to skip code in the Strategy Tester
   
   double currentBalance = AccountInfoDouble(ACCOUNT_BALANCE);
   
   if(!NormalizeDouble(currentBalance, 2) != NormalizeDouble(LastBalanceChecked, 2))
   {
      double tradingProfit = CalculateNewDealsProfit();
      double expectedBalance = LastBalanceChecked + tradingProfit;
      double withdrawalAmount = currentBalance - expectedBalance;
      
      // Detect withdrawal (negative change not from trading)
      if(withdrawalAmount < 0.0)
      {
        double newInitialDeposit=currentBalance;                 
         GlobalVariableSet(MyInitialDeposit, newInitialDeposit);
         Print("Updated initial deposit to: ", newInitialDeposit);
           
      }
      
      // Update tracking variables
      LastBalanceChecked = currentBalance;
      UpdateLastDealId();
   }
}




