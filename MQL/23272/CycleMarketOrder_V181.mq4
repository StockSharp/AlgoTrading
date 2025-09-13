//+------------------------------------------------------------------+
//|                                             CycleMarketOrder.mq4 |
//|                             Copyright (c) 2013, stock.yumokin.jp |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2013, stock.yumokin.jp"
#property link      "https://stock.yumokin.jp"
#property version     "1.81"
#property strict

#include <stderror.mqh>
#include <stdlib.mqh>

#define WAIT_TIME 5

extern int magic = 20140000;
extern int entry = 1;
extern double maxPrice = 105;
extern int maxCnt = 100;
extern double lots = 0.01;
extern double BreakEven = 40;
extern double TrailingStop = 20;
extern int spanPips = 10;
double pointPerPips;
int slippage = 3;
int slippagePips;
extern int WeekendMode=0;
extern int WeekendHour=4;
extern int WeekstartHour=8;
color ArrowColor[6] = {Red, Green, Red, Green, Red, Green};
string magicArray[];

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init() {
    
    int mult = 1;
    if(Digits == 3 || Digits == 5) {
        mult = 10;
    }
    pointPerPips = Point * mult;
    slippagePips = slippage * mult;
    
    return(0);
}

//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit() {

    return(0);
}

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start(){

    int ret = 0;
    int i = 0;
     
    string symbolCode;

    if(IsTradeAllowed() == false) {
        Print("Trade not Allowed");
        return(0);
    }

    //WEEKEND STOP
    if(WeekendMode==1){
        if(CheckOnTime() == -1){
            for(i=0; i < maxCnt; i++) {
                OrderCancel(-1, magic + i);
            }
            for(i=0; i < maxCnt; i++) {
                OrderCancel(1, magic + i);
            }
            return(0);    
        }
    }
    
    if(setOrderCommnet() == false){
        return(0);
    }

    //MODIFY
    for(i=0; i < maxCnt; i++) {
        MoveTrailingStop(magic + i, BreakEven, TrailingStop, pointPerPips);       
    }
    
    if(entry==0){
        return(0);
    }

    ret = doCycleMarketOrder(lots, maxPrice
                       , maxCnt , spanPips
                       , magic);

    return(0);
}
//+------------------------------------------------------------------+

int doCycleMarketOrder(double lots, double maxPrice, int maxCnt, int spanPips, int magic) {

    bool isBuyFlag;
    double startPrice;
    double endPrice;
    double currentPrice;
    int magicNumber;
        
    for(int i=0; i < maxCnt; i++){
        
        if(entry==1){
            isBuyFlag = true;
            //startPrice > endPrice
            startPrice = maxPrice - spanPips * ((maxCnt-1)-i) * pointPerPips;
            endPrice = maxPrice - spanPips * (maxCnt-i) * pointPerPips;
            currentPrice = Ask;
            magicNumber = magic + ((maxCnt-1)-i);
        }
        else if(entry==-1){
            isBuyFlag = false;
            //startPrice < endPrice 
            startPrice = maxPrice - spanPips * i * pointPerPips;
            endPrice = maxPrice - spanPips * (i-1) * pointPerPips;
            currentPrice = Bid;
            magicNumber = magic + i;
        }
        
        int index = getOrderCommnet(magicNumber);
        if ( index < 0) {
            doMarketOrder(lots, startPrice, endPrice, slippagePips, isBuyFlag, magicNumber);
        }
        
    }
    
    return(0);
}

int doMarketOrder(double lots, double startPrice, double endPrice, int slippage, bool isBuyFlag, int magicNumber) {

    int tradeType = -1;
    double openPrice;
    
    string comment = StringConcatenate(magicNumber," ");

    if(isBuyFlag && startPrice <= Ask) {
        return(-1);
    } else if(isBuyFlag && (startPrice > Ask && Ask > endPrice)) {
        tradeType = OP_BUY;
        openPrice=Ask;    
    } else if(!isBuyFlag && startPrice >= Bid) {
        return(-1);
    } else if(!isBuyFlag && (startPrice < Bid && Bid < endPrice)) {
        tradeType = OP_SELL;
        openPrice=Bid;            
    }
    
    if(tradeType == -1) {
        return(-1);
    }
    
    int errCode = 0;
    int ticket = doOrderSend(tradeType,lots,openPrice,slippage,comment,magicNumber,errCode);

    return(ticket);
    
}

int doOrderSend(int type, double lots, double openPrice, int slippage, string comment, int magicNumber, int &errCode) {

    openPrice = NormalizeDouble(openPrice, Digits);

    int starttime = GetTickCount();

    while(true) {

        if(GetTickCount() - starttime > WAIT_TIME * 1000) {
            Print("OrderSend timeout. Check the experts log.");
            return(false);
        }

        if(IsTradeAllowed() == true) {
            RefreshRates();
            int ticket = OrderSend(Symbol(), type, lots, openPrice, slippage, 0, 0, comment, magicNumber, 0, ArrowColor[type]);
            if( ticket > 0) {
                return(ticket);
            }

            errCode = GetLastError();
            Print("[OrderSendError] : ", errCode, " ", ErrorDescription(errCode));
            Print("price=",openPrice);
            if(errCode == ERR_INVALID_PRICE || errCode == ERR_INVALID_STOPS) {
                break;
            }
        }
        Sleep(100);
    }
    return(-1);
}

bool setOrderCommnet(){

    int orderCount=0;
    int j;
    for(j=0; j < OrdersTotal(); j++) { 
        if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES) == false) return(false);
        if(OrderSymbol() == Symbol()) {
            orderCount++;
        }
    }
    
    ArrayResize(magicArray,orderCount);
    orderCount=0;

    for(j=0; j < OrdersTotal(); j++) { 
        if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES) == false) return(false);
        if(OrderSymbol() == Symbol()) {
            magicArray[orderCount] = OrderComment();
            orderCount++;
        }
    }
    
    return(true);
    
}

int getOrderCommnet(int magicNumber){

    int index = -1;
    
    for(int i=0; i < ArraySize(magicArray); i++) {
        index = StringFind(magicArray[i],DoubleToStr(magicNumber,0),0);
        if(index >= 0){
            break;
        }
    }
    
    return(index);
}

int TicketClose(int mode,int magicNumber)
{ 
    int total=OrdersTotal();
    
    for(int i=total-1; i>=0; i--){
        if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES) ==false){
            continue;
        }
        if(OrderSymbol() == Symbol() && OrderMagicNumber() == magicNumber){
            if(mode == 1){
                if(OrderType() == OP_BUY){
                    if(OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),3,Green) != TRUE){
                        Print("LastError = ", ErrorDescription(GetLastError()));
                    }
                    return(0);
                }
                else if(OrderType() == OP_BUYLIMIT || OrderType() == OP_BUYSTOP){
                    if( OrderDelete(OrderTicket()) !=TRUE ){
                        Print("LastError = ", ErrorDescription(GetLastError()));
                    }
                    return(0);
                }
            }
            else if(mode == -1){
                if(OrderType() == OP_SELL){
                    if(OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),3,Green) != TRUE){
                        Print("LastError = ", ErrorDescription(GetLastError()));
                    }
                    return(0);
                }
                else if(OrderType() == OP_SELLLIMIT || OrderType() == OP_SELLSTOP){
                    if( OrderDelete(OrderTicket()) !=TRUE ){
                        Print("LastError = ", ErrorDescription(GetLastError()));
                    }
                    return(0);
                }
            }
        }
    }
    return(0);
}

int OrderCancel(int mode,int magicNumber)
{
    int total=OrdersTotal();

    for(int i=total-1; i>=0; i--){
        if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES) ==false){
            continue;
        }
        if(OrderSymbol() == Symbol() && OrderMagicNumber() == magicNumber){
            if(mode == 1){
                if(OrderType() ==  2 || OrderType() == 4){
                    if( OrderDelete(OrderTicket()) !=TRUE ){
                        Print("LastError = ", ErrorDescription(GetLastError()));
                    }
                    return(0);
                }
            }
            else if(mode == -1){
                if(OrderType() == 3 || OrderType() == 5){
                    if( OrderDelete(OrderTicket()) !=TRUE ){
                        Print("LastError = ", ErrorDescription(GetLastError()));
                    }
                    return(0);
                }
            }
        }
    }
    return(0);
}

int MoveTrailingStop(int magicNumber, double BreakEven, double TrailingStop, double pointPerPips){

    int DigitsNum;
    double bp = BreakEven*pointPerPips;
    double tp = TrailingStop*pointPerPips;
    int index;
    
    if(Digits == 3 || Digits == 5){
        DigitsNum=Digits-2;
    }
    else{
        DigitsNum=Digits-1;
    }

    int total=OrdersTotal();
   
    for(int i=total-1; i>=0; i--){
        if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) continue;
        
        if(OrderSymbol()==Symbol()&&OrderMagicNumber()==magicNumber){
            
            if(OrderType()==OP_BUY)
            {
                if(TrailingStop>0)  
                {
                    if(NormalizeDouble(OrderOpenPrice()+bp,DigitsNum) <= NormalizeDouble(Bid-tp,DigitsNum)){
                        if((NormalizeDouble(OrderStopLoss(),DigitsNum) < NormalizeDouble(Bid-tp,DigitsNum)) || OrderStopLoss()==0)
                        {
                            OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Bid-tp,DigitsNum),OrderTakeProfit(),0,Blue);
                            return(0);
                        }
                    }
                }
            }
            else if(OrderType()==OP_SELL)
            {
                if(TrailingStop>0)  
                {
                    if(NormalizeDouble(OrderOpenPrice()-bp,DigitsNum) >= NormalizeDouble(Ask+tp,DigitsNum)){
                        if((NormalizeDouble(OrderStopLoss(),DigitsNum) > NormalizeDouble(Ask+tp,DigitsNum)) || OrderStopLoss()==0)
                        {
                            OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Ask+tp,DigitsNum),OrderTakeProfit(),0,Red);
                            return(0);
                        }
                    }
                }
            }            
        }
    }
    
    return(0);
}

int CheckOnTime(){

    datetime CurrentDateTime = TimeLocal();   
    int CurrentHour=TimeHour(CurrentDateTime);   
    int CurrentWeek=TimeDayOfWeek(CurrentDateTime);
    
    if((CurrentWeek==6 && CurrentHour>=WeekendHour) || CurrentWeek==0 || (CurrentWeek==1 && CurrentHour<WeekstartHour)){
        return(-1);
    }
    else{
        return(1);
    }
}
