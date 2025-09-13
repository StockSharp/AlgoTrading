//--------------------------------------------------------------------------------
//                 This EA Programed And Developed By Akhmad FX
//         Contact Me To akhmad.sna@gmail.com For Your Custom EA Request
//                    Powered By Forex EA Generator Software
//          Etasoft Inc. Forex EA and Script Generator version 4.5   EA
//                        http://www.forexgenerator.com
//--------------------------------------------------------------------------------
// Keywords: MT4, Forex EA builder, create EA, expert advisor developer

#property copyright "Copyright © 2013, Akhmad FX"
#property link      "http://www.Eamarket.blogspot.com/"

#include <stdlib.mqh>
#include <WinUser32.mqh>

// exported variables
extern int BuyStoploss30 = 100;
extern int BuyTakeprofit30 = 200;
extern double BalanceRiskPercent30 = 5;


// local variables
double PipValue=1;    // this variable is here to support 5-digit brokers
bool Terminated = false;
string LF = "\n";  // use this in custom or utility blocks where you need line feeds
int NDigits = 4;   // used mostly for NormalizeDouble in Flex type blocks
int ObjCount = 0;  // count of all objects created on the chart, allows creation of objects with unique names
int current = 0;

int Count2 = 0;
datetime BarTime4 = 0;


int init()
{
    NDigits = Digits;
    
    if (false) ObjectsDeleteAll();      // clear the chart
    
    
    Comment("");    // clear the chart
}

// Expert start
int start()
{
    if (Bars < 10)
    {
        Comment("Not enough bars");
        return (0);
    }
    if (Terminated == true)
    {
        Comment("EA Terminated.");
        return (0);
    }
    
    OnEveryTick20();
    
}

void OnEveryTick20()
{
    if (true == false && true) PipValue = 10;
    if (true && (NDigits == 3 || NDigits == 5)) PipValue = 10;
    
    PrintInfoToChart_rev42();
    TechnicalAnalysis1aplus28();
    TechnicalAnalysis1aplus24();
    TechnicalAnalysis1aminus33();
    
}

void PrintInfoToChart_rev42()
{
    string temp = "C Factor\nExecuted : " + Count2 + "\n"
    + "Spread: " + DoubleToStr(MarketInfo(Symbol(), MODE_SPREAD)/PipValue, 2)+ "\n"
    + "................................................\n"
    + "ACCOUNT INFORMATION:\n"
    + "\n"
    + "Account Name   :     " + AccountName()+ "\n"
    + "Broker Name    :     " + AccountCompany()+ "\n"
    + "Account Number :     " + DoubleToStr(AccountNumber(), 0)+ "\n"
    + "Account Profit :     " + DoubleToStr(AccountProfit(), 2)+ "\n"
    + "\n"
    + "Account Leverage :     " + DoubleToStr(AccountLeverage(), 0)+ "\n"
    + "Account Balance  :     " + DoubleToStr(AccountBalance(), 2)+ "\n"
    + "Account Equity   :     " + DoubleToStr(AccountEquity(), 2)+ "\n"
    + "Free Margin      :     " + DoubleToStr(AccountFreeMargin(), 2)+ "\n"
    + "Used Margin      :     " + DoubleToStr(AccountMargin(), 2)+ "\n"
    + "Stop Out Level   :     " + DoubleToStr(AccountStopoutLevel(), 0)+ " %\n"
    + "\n"
    + "Date                :     " + TimeToStr(TimeCurrent(),TIME_DATE)+ "\n"
    + "Broker Server Time  :     " + TimeToStr(TimeCurrent(),TIME_SECONDS)+ "\n"
    + "Computer Local Time :     " + TimeToStr(TimeLocal(),TIME_SECONDS)+ "\n"
    + "................................................\n"
    + "\n"
    + "---------------------------------------------------------------\n"
    + "HAVE  A  NICE  TRADE  &  PROFIT  EVERYDAY  ! ! ! !\n"
    + "EA Provide By Akhmad FX , akhmad.sna@gmail.com\n"
    + "EA Programer And Custom EA Developer \n"
    + "Powered By Etasoft Inc. Forex EA and Script Generator Software \n"
    + "http://www.forexgenerator.com \n"
    + "---------------------------------------------------------------\n";
    
    
    Comment(temp);
    Count2++;
    
    
    
    
    
    
}

void TechnicalAnalysis1aplus28()
{
    if (Bid >= Low[1] + 100 *PipValue*Point)
    {
        CloseOrderUnlid22();
        
    }
    
    
    
    
    
}

void CloseOrderUnlid22()
{
    int orderstotal = OrdersTotal();
    int orders = 0;
    int ordticket[30][2];
    for (int i = 0; i < orderstotal; i++)
    {
        OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
        if (OrderType() != OP_BUY || OrderSymbol() != Symbol() || OrderMagicNumber() != 777)
        {
            continue;
        }
        ordticket[orders][0] = OrderOpenTime();
        ordticket[orders][1] = OrderTicket();
        orders++;
    }
    if (orders > 1)
    {
        ArrayResize(ordticket,orders);
        ArraySort(ordticket);
    }
    for (i = 0; i < orders; i++)
    {
        if (OrderSelect(ordticket[i][1], SELECT_BY_TICKET) == true)
        {
            bool ret = OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 1, Blue);
            if (ret == false)
            Print("OrderClose() error - ", ErrorDescription(GetLastError()));
        }
    }
    
    
    
    
}

void TechnicalAnalysis1aplus24()
{
    if (Bid >= High[1] + 0 *PipValue*Point)
    {
        OncePerBar4();
        
    }
    
    
    
    
    
}

void OncePerBar4()
{
    
    if (BarTime4 < Time[0])
    {
        // we have a new bar opened
        BarTime4 = Time[0]; // keep the new bar open time
        BuyOrderRiskFixedUnlid30();
        
    }
}

void BuyOrderRiskFixedUnlid30()
{
    double lotsize = MarketInfo(Symbol(),MODE_LOTSIZE) / AccountLeverage();
    double pipsize = 0.1 * 10;
    double maxlots = AccountFreeMargin() / 100 * BalanceRiskPercent30 / lotsize * pipsize;
    if (BuyStoploss30 == 0) Print("OrderSend() error - stoploss can not be zero");
    double lots = maxlots / BuyStoploss30 * 10;
    
    // calculate lot size based on current risk
    double lotvalue = 0.001;
    double minilot = MarketInfo(Symbol(), MODE_MINLOT);
    int powerscount = 0;
    while (minilot < 1)
    {
        minilot = minilot * MathPow(10, powerscount);
        powerscount++;
    }
    lotvalue = NormalizeDouble(lots, powerscount - 1);
    
    if (lotvalue < MarketInfo(Symbol(), MODE_MINLOT))    // make sure lot is not smaller than allowed value
    {
        lotvalue = MarketInfo(Symbol(), MODE_MINLOT);
    }
    if (lotvalue > MarketInfo(Symbol(), MODE_MAXLOT))    // make sure lot is not greater than allowed value
    {
        lotvalue = MarketInfo(Symbol(), MODE_MAXLOT);
    }
    double SL = Ask - BuyStoploss30*PipValue*Point;
    if (BuyStoploss30 == 0) SL = 0;
    double TP = Ask + BuyTakeprofit30*PipValue*Point;
    if (BuyTakeprofit30 == 0) TP = 0;
    
    int ticket = -1;
    if (true)
    ticket = OrderSend(Symbol(), OP_BUY, lotvalue, Ask, 1, 0, 0, "C Factor", 777, 0, Blue);
    else
    ticket = OrderSend(Symbol(), OP_BUY, lotvalue, Ask, 1, SL, TP, "C Factor", 777, 0, Blue);
    if (ticket > -1)
    {
        if (true)
        {
            OrderSelect(ticket, SELECT_BY_TICKET);
            bool ret = OrderModify(OrderTicket(), OrderOpenPrice(), SL, TP, 0, Blue);
            if (ret == false)
            Print("OrderModify() error - ", ErrorDescription(GetLastError()));
        }
            
    }
    else
    {
        Print("OrderSend() error - ", ErrorDescription(GetLastError()));
    }
    
    
    
    
}

void TechnicalAnalysis1aminus33()
{
    if (Bid <= High[1] - 20 *PipValue*Point)
    {
        CloseOrderUnlid22();
        
    }
    
    
    
    
}



int deinit()
{
    if (false) ObjectsDeleteAll();
    
    
}

