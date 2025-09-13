//+------------------------------------------------------------------+
//|                                           CloseBasketPairs.mq4   |
//|                        Copyright 2024, MetaQuotes Software Corp. |
//|                                           https://www.mql4.com   |
//+------------------------------------------------------------------+
#property copyright     "Copyright 2024, MetaQuotes Ltd."
#property link          "https://www.mql5.com"
#property version       "1.01"
#property description   "persinaru@gmail.com"
#property description   "IP 2024 - free open source"
#property description   "Close Basket Pairs"
#property description   ""
#property description   "WARNING: Use this software at your own risk."
#property description   "The creator of this script cannot be held responsible for any damage or loss."
#property description   ""
#property strict
#property show_inputs
#property script_show_inputs


// Define the pairs in the basket and their respective order types
extern string basketPairs = "EURUSD|BUY,GBPUSD|SELL,USDJPY|BUY";
extern int orderProfitThreshold = 0; // Default profit threshold value
extern int orderLossThreshold = 0;   // Default loss threshold value

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){
    // If both thresholds are 0, close the strategy
    if(orderProfitThreshold == 0 && orderLossThreshold == 0){
        Print("Both profit and loss thresholds are set to 0. Closing the strategy.");
        ExpertRemove();
    }
    return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason){
    PrintStrategyInfo();
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){
    CloseBasketPairs();
}
//+------------------------------------------------------------------+
//| Function to close positions for basket pairs                     |
//+------------------------------------------------------------------+
void CloseBasketPairs(){
    // Loop through all open positions
    for(int i = OrdersTotal() - 1; i >= 0; i--){
        if(OrderSelect(i, SELECT_BY_POS) && OrderType() <= OP_SELL){
            string symbol = OrderSymbol();
            int orderType = OrderType();
            
            // Parse basketPairs string and check if the position belongs to the basket
            string pair;
            string action;
            int startPos = 0;
            int separatorPos = StringFind(basketPairs, ",", startPos);
            while(separatorPos >= 0){
                pair = StringSubstr(basketPairs, startPos, separatorPos - startPos);
                action = StringSubstr(pair, StringFind(pair, "|") + 1);
                pair = StringSubstr(pair, 0, StringFind(pair, "|"));
                
                if(StringFind(symbol, pair) >= 0){
                    if((action == "BUY" && orderType == OP_BUY) || (action == "SELL" && orderType == OP_SELL)){
                        // Calculate profit
                        double profit = OrderProfit();
                        
                        // Close the position if profit exceeds the profit threshold or loss exceeds the loss threshold
                        if(orderProfitThreshold > 0 && profit > 0 && profit >= orderProfitThreshold){
                            if(!OrderClose(OrderTicket(), OrderLots(), MarketInfo(symbol, MODE_BID), 3)){
                                Print("Error closing order ", OrderTicket(), " Error code: ", GetLastError());
                            }
                            else{
                                Print("Closed order ", OrderTicket(), " Profit: ", profit);
                            }
                        }
                        else if(orderLossThreshold < 0 && profit < 0 && profit <= -orderLossThreshold){
                            if(!OrderClose(OrderTicket(), OrderLots(), MarketInfo(symbol, MODE_BID), 3)){
                                Print("Error closing order ", OrderTicket(), " Error code: ", GetLastError());
                            }
                            else{
                                Print("Closed order ", OrderTicket(), " Loss: ", profit);
                            }
                        }
                    }
                }
                
                startPos = separatorPos + 1;
                separatorPos = StringFind(basketPairs, ",", startPos);
            }
            
            // Process the last pair
            pair = StringSubstr(basketPairs, startPos);
            action = StringSubstr(pair, StringFind(pair, "|") + 1);
            pair = StringSubstr(pair, 0, StringFind(pair, "|"));
            
            if(StringFind(symbol, pair) >= 0){
                if((action == "BUY" && orderType == OP_BUY) || (action == "SELL" && orderType == OP_SELL)){
                    // Calculate profit
                    double profit = OrderProfit();
                    
                    // Close the position if profit exceeds the profit threshold or loss exceeds the loss threshold
                    if(orderProfitThreshold > 0 && profit > 0 && profit >= orderProfitThreshold){
                        if(!OrderClose(OrderTicket(), OrderLots(), MarketInfo(symbol, MODE_BID), 3)){
                            Print("Error closing order ", OrderTicket(), " Error code: ", GetLastError());
                        }
                        else{
                            Print("Closed order ", OrderTicket(), " Profit: ", profit);
                        }
                    }
                    else if(orderLossThreshold < 0 && profit < 0 && profit <= -orderLossThreshold){
                        if(!OrderClose(OrderTicket(), OrderLots(), MarketInfo(symbol, MODE_BID), 3)){
                            Print("Error closing order ", OrderTicket(), " Error code: ", GetLastError());
                        }
                        else{
                            Print("Closed order ", OrderTicket(), " Loss: ", profit);
                        }
                    }
                }
            }
        }
    }
}
//+------------------------------------------------------------------+
//| Function to print strategy information                           |
//+------------------------------------------------------------------+
void PrintStrategyInfo(){
    Print("Basket Pairs: ", basketPairs);
    Print("Order Profit Threshold: ", orderProfitThreshold);
    Print("Order Loss Threshold: ", orderLossThreshold);
    // Add any other strategy information you want to print here
}
//+------------------------------------------------------------------+
