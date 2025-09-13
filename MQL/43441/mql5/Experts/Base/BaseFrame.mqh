#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>    
#include <Trade\PositionInfo.mqh>
#include <Trade\OrderInfo.mqh>
#include <Trade\AccountInfo.mqh>
#include <MovingAverages.mqh>
#include <Arrays\ArrayDouble.mqh>

int MA_BUFFER_LENGTH = 100;
datetime Current_Time;
MqlDateTime Time_Structure;

struct TradeState
{
   bool isondealing;
   bool ispending;
   int direction;                // 1 or -1
   ulong ticket;
   int magic;
   ENUM_ORDER_TYPE positiontype; //
   double lots;
   double addlots;
   double dealprice; // if pending order.
   double latestdealprice; // useful when here is many orders.
   int order_open_ma_diff;
   double takeprofit;
   double profit;
   double stoploss;
   double martingale_lots;
   datetime closetime;
   datetime dealtime;
   int deal_h1_bars;
   int will_close_bars;
   datetime willclosetime;
   string comment;
   datetime expiration;
   int takeprofit_mode; // 0 for point, 1 for price.   
   int stoploss_mode; // 0 for point, 1 for price.   
};

enum INDICATOR_TOGGLE{
    RSI,
    STO,
    MACD
};

enum DIPP_RectION_DEAL{
    BOTH,
    UP,
    DOWN
};

class EABaseFrame{
protected: 
    CTrade ThisTrade;               // trading object
    CSymbolInfo ThisSymbol;         // symbol info object
    CPositionInfo ThisPosition;     // trade position object
    COrderInfo ThisOrder;           // trade pending order object
    CAccountInfo ThisAccount;       // account info wrapper    
    TradeState state;              // trade state  

    int Last_W1_Bars;
    int Last_Current_Bars;

    bool had_executed_once;
    double masrc[], mash1src[], masw1src[];
    //MA group
    CArrayDouble MAS2[2]; 
    CArrayDouble MAS4[4]; 
    CArrayDouble MAS6W1[6]; 
    CArrayDouble MAS6[6];


    //user input parameters
public:
    string  _symbol;
    double  lot_amplifier;
    double  Trade_Init_Lots;           //初始默认手数
    int     Auto_Close_After_X_H1;
    bool    Is_Reverse;
    DIPP_RectION_DEAL Deal_Direction_Allow;  
    
    double USTime_LeftBound;   //美盘货币活跃时间起始
    double USTime_RightBound; //美盘货币活跃时间终止
    double NUSTime_LeftBound;   //非美盘货币活跃时间起始
    double NUSTime_RightBound; //非美盘货币活跃时间终止

public:
    EABaseFrame(void);
    ~EABaseFrame(void){};
    virtual bool Init()=0;
    void Deinit(void);
    bool Processing(void);

protected:
    //--operation
    bool CloseOrder(const double rate = 1.0);
    bool ReverseOrder();
    bool OpenOrder();
    bool ModiffyOrder();
    bool ResetState();
    bool RefreshState();
    bool RefreshIndicator();

    //calcalution
    void CalcOneMA(const double &source[], double &output[], const ENUM_MA_METHOD Ma_Method, const int period,  const int start=0);
    void UpdateOneMA(const double &source[], CArrayDouble &MA, const ENUM_MA_METHOD Ma_Method, const int period, const bool is_insert=false);
    void CalMAS6W1();
    void CalMAS6();
    void CalMAS2();
    void UpdateMAS6W1(bool is_insert=false);
    void UpdateMAS6(bool is_insert = false);
    void UpdateMAS2(bool is_insert = false);
    int MA_Direction(CArrayDouble &maObj[], int comfirm_times, int from_1 = 0);
    double PTGGradient(const int count=2);
    
    //interface
    virtual bool LongOpen()=0;
    virtual bool ShortOpen()=0;
    virtual bool LongClose()=0;
    virtual bool ShortClose()=0;
    virtual bool CheckModiffy()=0;
    virtual bool CheckReverse()=0;
    virtual bool CheckAddLong()=0;
    virtual bool CheckAddShort()=0;
};

//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|---以下为功能实现代码区                                           |
//+------------------------------------------------------------------+
EABaseFrame::EABaseFrame(void): had_executed_once(false)
{
    // ArraySetAsSeries(myindicator_array, true);
}

bool EABaseFrame::RefreshIndicator()
{
    
    if(iBars(_symbol, PERIOD_CURRENT) > Last_Current_Bars){
        CopyClose(_symbol, PERIOD_CURRENT, 0, MA_BUFFER_LENGTH, masrc);
        UpdateMAS6(true);
        Last_Current_Bars = iBars(_symbol, PERIOD_CURRENT);
    }else{
        masrc[ArraySize(masrc)-1] = iClose(_symbol, PERIOD_CURRENT, 0);
        UpdateMAS6(false);
    } 

    if(iBars(_symbol, PERIOD_W1) > Last_W1_Bars){
        CopyClose(_symbol, PERIOD_W1, 0, MA_BUFFER_LENGTH, masw1src);
        UpdateMAS6W1(true);
        Last_W1_Bars = iBars(_symbol, PERIOD_W1);
    }else{
        masw1src[ArraySize(masw1src)-1] = iClose(_symbol, PERIOD_W1, 0);
        UpdateMAS6W1(false);
    }  

    // if (0
    //      || CopyBuffer(my_indicator_dandle, 0, 0, 50, my_indicator1) != 50
    //      || CopyBuffer(my_indicator_dandle, 1, 0, 50, my_indicator2) != 50
         
    // )
    //     return (false);   

    return true;
}

bool EABaseFrame::Processing(void)
{
    if (!ThisSymbol.RefreshRates())
    {
        Print("RefreshRates fail.");
        return (false);
    }

    if(!had_executed_once && iBars(_symbol, PERIOD_W1) > 18){
        had_executed_once = true;
        CopyClose(_symbol, PERIOD_W1, 0, 100, masw1src);
        CopyClose(_symbol, PERIOD_CURRENT, 0, MA_BUFFER_LENGTH, masrc);
        CalMAS6W1();
        CalMAS6();
        // CalMAS2();
        Last_W1_Bars = Bars(_symbol,PERIOD_W1);
        Last_Current_Bars = Bars(_symbol, PERIOD_CURRENT);
    }
    if (!had_executed_once)
        return (false);

    //--- refreshing
    RefreshState();

    if (!RefreshIndicator())
    {
        Print("Refresh indicator fail.");
        return (false);
    }

    // Print("is ptg ok? ", IsPtgOk());
    //--- deal or close or modiffy orders
    if (!state.isondealing)
    { // now no deal
        
        // if (Current_Time < state.closetime + m_const.deal_gap_time * 3600)
        //     return false;
        // string date = TimeToString(Current_Time, TIME_DATE);
        // string limit_start_str = date + " " + Trading_Hour_Start;
        // string limit_end_str = date + " " + Trading_Hour_End;
        // datetime limit_start = StringToTime(limit_start_str);
        // datetime limit_end = StringToTime(limit_end_str);
        //  // Print("start: ",limit_start, "end: ",limit_end, "  str: ",limit_start_str,"  date: ",date);
        // if(!(Current_Time > limit_start && Current_Time < limit_end)){
        //     Print("不在交易时间范围内。");
        //     return false;
        // }

        double lots = Trade_Init_Lots;

        if (LongOpen())
        {
            state.direction = 1;
            state.addlots = lots;
            state.positiontype = state.direction==1?ORDER_TYPE_BUY:ORDER_TYPE_SELL;            
            state.latestdealprice = ThisSymbol.Ask();
            state.will_close_bars = iBars(_symbol, PERIOD_H1) + Auto_Close_After_X_H1;
            return OpenOrder();
        }
        else if (ShortOpen())
        {
            {
                state.direction = -1;
                state.addlots = lots;
                state.positiontype = state.direction==1?ORDER_TYPE_BUY:ORDER_TYPE_SELL;            
                state.latestdealprice = ThisSymbol.Ask();
                state.will_close_bars = iBars(_symbol, PERIOD_H1) + Auto_Close_After_X_H1;
                return OpenOrder();
            }
        }
    }else{ // dealing state
        if ((state.direction == -1 && ShortClose()) || (state.direction ==1 && LongClose()))
        {
            CloseOrder();
            return ResetState();
        }
        if (CheckModiffy()){
            return ModiffyOrder();
        }        
        if(Is_Reverse && CheckReverse()){
            Print("before deal time: ", state.dealtime," willclosebar: ",state.will_close_bars," willclosetime: ",state.willclosetime==0);
            ReverseOrder();
            Print("after deal time: ", state.dealtime," willclosebar: ",state.will_close_bars," willclosetime: ",state.willclosetime);
        } 
        if(CheckAddLong() || CheckAddShort()){
            OpenOrder();
        }        
    }    

    return true;
}

bool EABaseFrame::ResetState()
{
    state.isondealing = false;
    state.ispending = false;
    state.lots = 0.0;
    state.addlots = 0.0;
    state.direction = 0;
    state.positiontype = -1;
    state.ticket = -1;
    state.dealprice = 0.0;
    state.takeprofit = 0.0;
    state.profit = 0.0;
    state.stoploss = 0;
    state.expiration = 0;
    state.deal_h1_bars = 0;
    state.dealtime = 0;
    state.closetime = Current_Time;
    state.will_close_bars = 0;
    state.willclosetime = 0;
    state.martingale_lots = Trade_Init_Lots;
    return true;
}



bool EABaseFrame::RefreshState()
{
    Current_Time = TimeCurrent();
    TimeToStruct(Current_Time, Time_Structure);

    int order_or_position = 0; // 1 for position, 2 for order.
    uint total = PositionsTotal() + OrdersTotal();
    if (OrdersTotal() > 0)
        state.ispending = true;
    else
        state.ispending = false;
    double lots = 0.0, profit = 0.0;
    for (uint i = 0; i < total; i++)
    {
        if (!ThisPosition.SelectByIndex(i) && !ThisOrder.SelectByIndex(i))
        {
            Print("选取订单失败。");
            continue;
        }
        if (ThisPosition.SelectByIndex(i))
            order_or_position = 1;
        else
            order_or_position = 2;

        // Print("order.symbol: ",ThisPosition.Symbol());
        if (order_or_position == 1 && (ThisPosition.Symbol() != _symbol || ThisPosition.Comment() != state.comment))
            continue;
        if (order_or_position == 2 && (ThisOrder.Symbol() != _symbol || ThisOrder.Comment() != state.comment))
            continue;

        if (state.isondealing == false)
            state.isondealing = true;
        int odt = order_or_position == 1 ? int(ThisPosition.PositionType()) : int(ThisOrder.OrderType());
        if (odt % 2 == 0)
        {
            state.direction = 1;
        }
        if (odt % 2 == 1)
        {
            state.direction = -1;
        }
        state.positiontype = odt;
        lots += order_or_position == 1 ? ThisPosition.Volume() : ThisOrder.VolumeInitial();
        profit += order_or_position == 1 ? ThisPosition.Profit() : 0.0;
        if(state.dealtime<=0) state.dealtime = order_or_position == 1 ? ThisPosition.Time() : ThisOrder.TimeSetup();
        if (state.deal_h1_bars <= 0)
            state.deal_h1_bars = iBars(_symbol, PERIOD_H1) - iBarShift(_symbol, PERIOD_H1, state.dealtime);
        state.dealprice = order_or_position == 1 ? ThisPosition.PriceOpen() : ThisOrder.PriceOpen();
        state.ticket = order_or_position == 1 ? ThisPosition.Ticket() : ThisOrder.Ticket();
    }
    state.lots = lots;
    state.profit = profit;
    /*
    Print("state lots: ", state.lots);
    Print("state addlots: ", state.addlots);
    Print("state direction: ", state.direction);
    Print("state isondealing: ", state.isondealing);
    Print("state comment: ", state.comment);
    */
    //平仓条件是 “下单一定时间后自动平仓” 时触发平仓操作。
    if (lots > 0 && state.willclosetime > 0 && Current_Time > state.willclosetime && CloseOrder())
    {
        return ResetState();
    }

    //止损情况出现
    if (lots == 0 && state.isondealing)
    {
        ResetState();
    }

    return true;
}

// double GetRiskLots()
// {
//     double margin = ThisAccount.FreeMargin();
//     double cz = ThisSymbol.ContractSize();
//     int leverage = ThisAccount.Leverage();
// //    double lots = margin * in_times_of_levelage / cz;

//     double lots = Maximum_Risk * margin * leverage / cz;
//     lots = 0.01 * int(lots*100);
//     return lots;
   
// }

bool EABaseFrame::OpenOrder()
{
    state.isondealing = true;
    if (state.addlots <= 0.0)
    {
        Print("add lot value error! addlots is: ", state.addlots);
    }

    if (state.direction == 1)
    {
        double price = state.dealprice == 0.0 ? ThisSymbol.Ask() : state.dealprice;
        double tp = 0.0, st = 0.0;
        if (state.takeprofit > 0.0)
        {
            if (state.takeprofit_mode == 0)
                tp = price + state.takeprofit * ThisSymbol.Point();
            else if (state.takeprofit_mode == 1)
                tp = state.takeprofit;
        }
        if (state.stoploss > 0.0)
        {
            if (state.stoploss_mode == 0)
                st = price - state.stoploss * ThisSymbol.Point();
            else if (state.stoploss_mode == 1)
                st = state.stoploss;
        }
        if (state.dealprice == 0)
        {
            if (ThisTrade.PositionOpen(_symbol, state.positiontype, state.addlots, price, st, tp, state.comment))
            {
                printf("Position by %s to be opened", _symbol);
                state.addlots = 0;
            }
            else
            {
                printf("Error opening BUY position by %s : '%s'", _symbol, ThisTrade.ResultComment());
                printf("Open parameters : price=%f,ST=%f,TP=%f, TYPE=%f", price, st, tp, state.positiontype);
                return false;
            }
        }
        else
        {
            if (ThisTrade.OrderOpen(_symbol, state.positiontype, state.addlots, 0.0, price, st, tp, ThisTrade.RequestTypeTime(), state.expiration, state.comment))
            {
                printf("Position by %s to be opened", _symbol);
                state.addlots = 0;
            }
            else
            {
                printf("Error opening BUY pending order by %s : '%s'", _symbol, ThisTrade.ResultComment());
                printf("Open parameters : price=%f,ST=%f,TP=%f, TYPE=%f", price, st, tp, state.positiontype);
                return false;
            }
        }
    }

    if (state.direction == -1)
    {
        double price = state.dealprice == 0 ? ThisSymbol.Bid() : state.dealprice;
        double tp = 0.0, st = 0.0;
        if (state.takeprofit > 0.0)
        {
            if (state.takeprofit_mode == 0)
                tp = ThisSymbol.Ask() - state.takeprofit * ThisSymbol.Point();
            else if (state.takeprofit_mode == 1)
                tp = state.takeprofit;
        }
        if (state.stoploss > 0.0)
        {
            if (state.stoploss_mode == 0)
                st = ThisSymbol.Bid() + state.stoploss * ThisSymbol.Point();
            else if (state.stoploss_mode == 1)
                st = state.stoploss;
        }
        if (state.dealprice == 0)
        {
            if (ThisTrade.PositionOpen(_symbol, state.positiontype, state.addlots, price, st, tp, state.comment))
            {
                printf("Position by %s to be opened", _symbol);
                state.addlots = 0;
            }
            else
            {
                printf("Error opening SELL position by %s : '%s'", _symbol, ThisTrade.ResultComment());
                printf("Open parameters : price=%f,ST=%f,TP=%f, TYPE=%f", price, st, tp, state.positiontype);
                return false;
            }
        }
        else
        {
            if (ThisTrade.OrderOpen(_symbol, state.positiontype, state.addlots, 0.0, price, st, tp, ThisTrade.RequestTypeTime(), state.expiration, state.comment))
            {
                printf("Position by %s to be opened", _symbol);
                state.addlots = 0;
            }
            else
            {
                printf("Error opening SELL pending order by %s : '%s'", _symbol, ThisTrade.ResultComment());
                printf("Open parameters : price=%f,ST=%f,TP=%f, TYPE=%f", price, st, tp, state.positiontype);
                return false;
            }
        }
    }
    return true;
}

/**
 * @brief 将当前的订单反向，意味着先平仓原订单，再开仓逆反的订单
 *
 * @param state
 * @return true
 * @return false
 */
bool EABaseFrame::ReverseOrder()
{
    if (CloseOrder())
    {
        // Print("are you kidding: ",state.direction);
        state.direction = -state.direction;
        state.addlots = Trade_Init_Lots;
        state.dealprice = 0;
        // state.positiontype = state.direction==1?ORDER_TYPE_BUY:ORDER_TYPE_SELL;   
        state.positiontype = state.direction > 0 ? ORDER_TYPE_BUY : ORDER_TYPE_SELL;
        // Print("here the dir: ",state.direction, " positiontype: ", state.positiontype," Buy:",ORDER_TYPE_BUY," SELL:",ORDER_TYPE_SELL);
        if (state.stoploss_mode == 1 && state.stoploss > 0)
        {
            double stoploss_gap = fabs(state.dealprice - state.stoploss);
            state.stoploss = ThisSymbol.Ask() - state.direction * stoploss_gap;
        }

        if (state.takeprofit_mode == 1 && state.takeprofit > 0)
        {
            double takeprofit_gap = fabs(state.takeprofit - state.dealprice);
            state.takeprofit = ThisSymbol.Ask() + state.direction * takeprofit_gap;
        }

        if (state.willclosetime > 0)
        {
            state.willclosetime = Current_Time + (state.willclosetime - state.dealtime);
        }
        if (OpenOrder())
        {
            return true;
        }
    }
    Print("SOME THING WRONG!!");
    return false;
}

bool EABaseFrame::CloseOrder(const double rate = 1.0)
{
    uint total = PositionsTotal();
    double volume = state.lots * rate;
    for (uint i = 0; i < total; i++)
    {
        if (!ThisPosition.SelectByIndex(i))
        {
            Print("选取订单失败。");
            continue;
        }
        // Print("order.symbol: ",ThisPosition.Symbol());
        if (ThisPosition.Symbol() != _symbol || ThisPosition.Comment() != state.comment)
            continue;

        if (ThisTrade.PositionClosePartial(ThisPosition.Ticket(), volume))
        {
            printf("position by %s to be closed", _symbol);
            continue;
        }
        else
        {
            printf("Error closing position by %s : '%s', code: %s ", _symbol, ThisTrade.ResultComment(), string(ThisTrade.ResultRetcode()));
            continue;
        }
    }

    total = OrdersTotal();
    for (uint i = 0; i < total; i++)
    {
        if (!ThisOrder.SelectByIndex(i))
        {
            Print("选取订单失败。");
            continue;
        }
        // Print("order.symbol: ",ThisPosition.Symbol());
        if (ThisOrder.Symbol() != _symbol || ThisOrder.Comment() != state.comment)
            continue;

        if (ThisTrade.OrderDelete(ThisOrder.Ticket()))
        {
            printf("Order by %s to be deleted", _symbol);
            continue;
        }
        else
        {
            printf("Error delete order by %s : '%s', code: %s ", _symbol, ThisTrade.ResultComment(), string(ThisTrade.ResultRetcode()));
            continue;
        }
    }
    return true;
}

bool EABaseFrame::ModiffyOrder()
{
    uint total = PositionsTotal() + OrdersTotal();
    if (OrdersTotal() > 0)
        state.ispending = true;
    double lots = 0.0;
    int order_or_position = 0;
    double st, tp;
    for (uint i = 0; i < total; i++)
    {
        if (!ThisPosition.SelectByIndex(i) && !ThisOrder.SelectByIndex(i))
        {
            Print("选取订单失败。");
            continue;
        }

        if (ThisPosition.SelectByIndex(i))
            order_or_position = 1;
        else
            order_or_position = 2;

        if (order_or_position == 1)
        {
            if (ThisPosition.Symbol() != _symbol || ThisPosition.Comment() != state.comment)
                continue;
            st = state.stoploss == 0 ? ThisPosition.StopLoss() : state.stoploss;
            tp = state.takeprofit == 0 ? ThisPosition.TakeProfit() : state.takeprofit;
            if (st > 0 && state.stoploss_mode == 0)
                st = ThisSymbol.Ask() - state.direction * st * ThisSymbol.Point();
            if (tp > 0 && state.takeprofit_mode == 0)
                tp = ThisSymbol.Ask() + state.direction * tp * ThisSymbol.Point();

            // Print("ticket", ThisPosition.Ticket()," state.stoploss: ", state.stoploss, "ThisPosition.StopLoss(): ",ThisPosition.StopLoss());
            // Print("state.takeprofit: ", state.takeprofit, "ThisPosition.TakeProfit(): ",ThisPosition.TakeProfit());
            // Print("bid:", ThisSymbol.Bid(), "stoploss: ", st, " takeprofit: ", tp);

            if (fabs(st - ThisPosition.StopLoss()) < ThisSymbol.Point() && fabs(tp - ThisPosition.TakeProfit()) < ThisSymbol.Point())
            {
                continue;
            }

            if (!ThisTrade.PositionModify(ThisPosition.Ticket(), st, tp))
            {
                printf("Error modiffy position by %s : '%s'", _symbol, ThisTrade.ResultComment());
                Print("state.stoploss: ", state.stoploss, "ThisPosition.StopLoss(): ", ThisPosition.StopLoss());
                Print("state.takeprofit: ", state.takeprofit, "ThisPosition.TakeProfit(): ", ThisPosition.TakeProfit());
                Print("bid:", ThisSymbol.Bid(), "stoploss: ", st, " takeprofit: ", tp, " error code: ", GetLastError());
                // Print("fuck:!!!: ",fabs(st - ThisPosition.StopLoss()) < ThisSymbol.Point() && fabs(tp - ThisPosition.TakeProfit()) < ThisSymbol.Point());
                return false;
            }
            continue;
        }
        if (order_or_position == 2)
        {
            if (ThisOrder.Symbol() != _symbol || ThisOrder.Comment() != state.comment)
                continue;
            st = state.stoploss == 0 ? ThisOrder.StopLoss() : state.stoploss;
            tp = state.takeprofit == 0 ? ThisOrder.TakeProfit() : state.takeprofit;
            if (st > 0 && state.stoploss_mode == 0)
                st = state.dealprice - state.direction * st * ThisSymbol.Point();
            if (tp > 0 && state.takeprofit_mode == 0)
                st = state.dealprice + state.direction * tp * ThisSymbol.Point();
            if (fabs(st - ThisOrder.StopLoss()) < ThisSymbol.Point() && fabs(tp - ThisOrder.TakeProfit()) < ThisSymbol.Point())
                continue;
            if (!ThisTrade.OrderModify(ThisOrder.Ticket(), state.dealprice, st, tp, ThisOrder.TypeTime(), ThisOrder.TimeExpiration()))
            {
                printf("Error modiffy order by %s : '%s'", _symbol, ThisTrade.ResultComment());
                Print("  stoploss: ", st, " takeprofit: ", tp, " error code: ", GetLastError());
                return false;
            }
            continue;
        }
    }
    return true;
}

/**
 *该函数从原始的价格数组计算任意给定周期的均线，返回存到ouput里。目前还不支持 LWMA模式 
 **/
void EABaseFrame::CalcOneMA(const double &source[], double &output[], const ENUM_MA_METHOD Ma_Method, const int period,  const int start=0){
    int len = ArraySize(source);
    ArrayResize(output, len);
    if(Ma_Method == MODE_SMA){        
        for(int i = start; i<len; i++){
            if(i<period-1){
                output[i] = source[i];
                continue;
            }
            if(i==period-1){
                output[i] = 0.0;
                for(int j = 0; j<period; j++){
                    output[i] += source[j];
                }
                output[i]=output[i]/period;                
                continue;
            }
            output[i] = output[i-1] + (source[i] - source[i-period])/period;
        }
    }
    if(Ma_Method == MODE_EMA){
        double pr=2.0/(period+1.0);        
        for(int i = start; i<len; i++){
            if(i==0){
                output[i] = source[i];
                continue;
            }            
            output[i] = output[i-1] * (1-pr) + source[i] * pr;
        }
    }
    if(Ma_Method == MODE_SMMA){
        for(int i = start; i<len; i++){
            if(i<period-1){
                output[i] = source[i];
                continue;
            }
            if(i==period-1){
                output[i] = 0.0;
                for(int j = 0; j<period; j++){
                    output[i] += source[j];
                }
                output[i]=output[i]/period;                
                continue;
            }
            output[i] = (output[i-1] * (period-1) + source[i])/period;
        }
    }
}

/**
 * 该函数更新一个CArrayDouble 对象的 MA数据。 因为MQL5目前并没有好的方式支持动态的二维数组。所以这里
 * 引入了官方的封装CArrayDouble类。该类维护动态数组，因此定义CArrayDouble MA[], 将会得到类似2维的动态数组
 * 因为均线组策略对均线的操作有重复性，所以放在二维数组里更适合。
 * 
 * 该函数性能应该是很好的，每次只会计算一个柱子的均线值；并不是对整个数组进行刷新。
 **/ 
void EABaseFrame::UpdateOneMA(const double &source[], CArrayDouble &MA, const ENUM_MA_METHOD Ma_Method, const int period, const bool is_insert=false){  
    // attention that the source is not set as series, so the last element is the current price.
    int last_of_source = ArraySize(source) -1;  
    if(Ma_Method == MODE_SMA){   
        double element = MA.At(1) + (source[last_of_source] - source[last_of_source - period])/period;   
        if(is_insert) MA.Insert(element, 0);
        else MA.Update(0, element); 
    }
    if(Ma_Method == MODE_EMA){
        double pr=2.0/(period+1.0);   
        double element = MA.At(1) * (1-pr) + source[last_of_source] * pr;
        if(is_insert) MA.Insert(element, 0);
        else MA.Update(0, element); 
    }
    if(Ma_Method == MODE_SMMA){
        double element = (MA.At(1) * (period-1) + source[last_of_source])/period;  
        if(is_insert) MA.Insert(element, 0);
        else MA.Update(0, element); 
    }

    if(MA.Total() > MA_BUFFER_LENGTH*2) {
        MA.DeleteRange(MA_BUFFER_LENGTH, MA_BUFFER_LENGTH*100);
    }
}

void EABaseFrame::CalMAS6W1(){
    double ma[]; 
    int periods[]={2, 4, 6 , 8, 12, 16};
    for(int i=0;i<6;i++){   
        ArraySetAsSeries(ma, false);     
        CalcOneMA(masw1src, ma, MODE_EMA, periods[i], 0);
        ArraySetAsSeries(ma, true);
        MAS6W1[i].AssignArray(ma);
    } 
}

void EABaseFrame::UpdateMAS6W1(bool is_insert=false){  
    int periods[]={2, 4, 6 , 8, 12, 16};     
    for(int i=0;i<6;i++){        
        UpdateOneMA(masw1src, MAS6W1[i], MODE_EMA, periods[i], is_insert);        
    } 
}

void EABaseFrame::CalMAS6(){
    double ma[]; 
    int periods[]={2, 4, 6 , 8, 12, 16};
    // if(periods[ArraySize(periods)-1] > MA_BUFFER_LENGTH){
    //     Print("Invalid period, out of range of MA_BUFFER_LENGTH!");
    //     ExpertRemove();
    // }
    for(int i=0;i<6;i++){   
        ArraySetAsSeries(ma, false);     
        CalcOneMA(masrc, ma, MODE_EMA, periods[i], 0);
        ArraySetAsSeries(ma, true);
        MAS6[i].AssignArray(ma);
    } 
}

void EABaseFrame::UpdateMAS6(bool is_insert = false){  
    int periods[]={2, 4, 6 , 8, 12, 16};
    for(int i=0;i<6;i++){        
        UpdateOneMA(masrc, MAS6[i], MODE_EMA, periods[i], is_insert);        
    } 
}

void EABaseFrame::CalMAS2(){
    double ma[]; 
    int periods[]={96, 288};
    // if(periods[ArraySize(periods)-1] > MA_BUFFER_LENGTH){
    //     Print("Invalid period, out of range of MA_BUFFER_LENGTH!");
    //     ExpertRemove();
    // }
    for(int i=0;i<2;i++){   
        ArraySetAsSeries(ma, false);     
        CalcOneMA(masrc, ma, MODE_EMA, periods[i], 0);
        ArraySetAsSeries(ma, true);
        MAS2[i].AssignArray(ma);
    } 
}

void EABaseFrame::UpdateMAS2(bool is_insert = false){  
    int periods[]={96, 288};     
    for(int i=0;i<2;i++){        
        UpdateOneMA(masrc, MAS2[i], MODE_EMA, periods[i], is_insert);        
    } 
}

/**
 * @brief 确定均线组是否同向
 *
 * @param maObj 均线数组
 * @param comfirm_times 多少根柱子的均线同向才返回true
 * @param from_1 确认均线同向时是否包含 shift=0 的均线，默认包含。
 * @return 0 或 -1， 1 表示均线同向情况。 1表示最新的`comfirm_times`个柱子，均线同时向上
 **/
int EABaseFrame::MA_Direction(CArrayDouble &maObj[], int comfirm_times, int from_1 = 0)
{   
    
    int direction = 0;
    int ma_len = ArraySize(maObj);
    double tosort[];
    ArrayResize(tosort, ma_len, 0);
    int tmp_direction = 0;
    //确保6条均线从shift=0 ~ shift=1 的方向都是一致的，没有交叉情况
    for (int i = from_1; i < comfirm_times + from_1; i++)
    {
        for (int j = 0; j < ma_len; j++)
        {
            tosort[j] = maObj[j][i];
        }
        if (tosort[0] > tosort[1])
            tmp_direction = 1;
        if (tosort[0] < tosort[1])
            tmp_direction = -1;

        for (int k = 1; k < ma_len - 1; k++)
        {
            if ((tosort[k] - tosort[k + 1]) * tmp_direction < 0)
            {
                tmp_direction = 0;
                break;
            }
        }
        if (direction == 0)
            direction = tmp_direction;
        if (direction * tmp_direction != 1)
        {
            direction = 0;
            break;
        }
    }

    // string s=" ";
    // for(int i=0;i<20;i++){
    //     s+=""+i+" th is: "+maObj[5][i]+"  ";
    // }
    // Print(s);

    for(int i=0; i< ma_len;i++){
        if((maObj[i].At(1) - maObj[i].At(2)) * direction <=0)  return 0; 
    }

    // Print(" now tell me the tosort in MA_Direction is: ");
    // ArrayPrint(tosort);
    // Print("tion--------noit");
    return direction;
}
