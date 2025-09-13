//+------------------------------------------------------------------+
//|                                                   EA框架布局.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2022, MetaQuotes Ltd."
#property link "https://www.mql5.com"
#property version "1.00"
//+------------------------------------------------------------------+
//| Include 文件，只能包含官方库文件                                 |
//+------------------------------------------------------------------+
#define  __MAGIC__ 50000

#include "Base/BaseFrame.mqh"
// #include <Xrk\utils.mqh>

input group "------------------GLOBAL------------------"
input double  lot_amplifier = 1;   //下单手数放大倍数,以2000美金回撤10%为基准
input double  Trade_Init_Lots = 0.01; 
input int     Auto_Close_After_X_H1  =  4;
input bool    Is_Reverse  = false;
input DIPP_RectION_DEAL Deal_Direction_Allow = BOTH;

input group "------------------Time------------------"
input double USTime_LeftBound = 2;   //美盘货币活跃时间起始
input double USTime_RightBound = 23; //美盘货币活跃时间终止
input double NUSTime_LeftBound = 0;   //非美盘货币活跃时间起始
input double NUSTime_RightBound = 0; //非美盘货币活跃时间终止


class MyTrade : public EABaseFrame{
public:
    virtual bool Init();
protected:
    //--judgement 
    int  IsMAEverFit(); 
    bool IsInDealTime();
    virtual bool CheckModiffy();
    virtual bool CheckReverse();
    virtual bool CheckAddLong();
    virtual bool CheckAddShort();
    virtual bool LongOpen();
    virtual bool ShortOpen();
    virtual bool LongClose();
    virtual bool ShortClose();
};

MyTrade  _trad;
//+------------------------------------------------------------------+
//| Expert initialization function  初始化函数                       |
//+------------------------------------------------------------------+
int OnInit()
{   
    _trad._symbol = "EURUSD";
    _trad.Trade_Init_Lots = Trade_Init_Lots;
    _trad.lot_amplifier = lot_amplifier;   //下单手数放大倍数,以2000美金回撤10%为基准
    _trad.Trade_Init_Lots = Trade_Init_Lots; 
    _trad.Auto_Close_After_X_H1  =  Auto_Close_After_X_H1;
    _trad.Is_Reverse  = Is_Reverse;
    _trad.Deal_Direction_Allow = Deal_Direction_Allow;

    _trad.USTime_LeftBound = USTime_LeftBound;   //美盘货币活跃时间起始
    _trad.USTime_RightBound = USTime_RightBound; //美盘货币活跃时间终止
    _trad.NUSTime_LeftBound = NUSTime_LeftBound;   //非美盘货币活跃时间起始
    _trad.NUSTime_RightBound = NUSTime_RightBound; //非美盘货币活跃时间终止


    _trad.Init();
    return (INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function  去初始化函数                   |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
}
//+------------------------------------------------------------------+
//| Expert tick function 主体函数                                    |
//+------------------------------------------------------------------+
void OnTick()
{   
    _trad.Processing();
}


bool MyTrade::Init(){
    ThisSymbol.Name(_symbol); // symbol
    ThisTrade.SetTypeFillingBySymbol(_symbol);
    ThisTrade.SetExpertMagicNumber(__MAGIC__);

    state.comment = _symbol;
    state.takeprofit_mode = 0;
    state.stoploss_mode = 0;
    ResetState();   

    //--- succeed
    return (true);
}

/**
 * Is MA ever fit to deal 
 * @return 1 for up, -1 for down, 0 for not fit either
 **/ 
int MyTrade::IsMAEverFit(){
    CArrayDouble mas[3];
    CArrayDouble *pma;
    int ma_dir = 0;
    for(int i=0;i<3;i++){
        pma = &MAS6[i+3];
        mas[i].AssignArray(pma);
        mas[i].DeleteRange(0, 1);
    }
    // ArrayPrint(MAS6[1].m_data,5," ,");
    // ArrayPrint(mas[1].m_data,5," ;");
    for(int i=0;i<2;i++){        
        ma_dir = MA_Direction(mas, 6, 0);
        if(fabs(ma_dir)>0) return ma_dir;

        for(int j=0;j<3;j++){
            mas[j].Delete(0);
        }
    }
    // ArrayPrint(MAS6[1].m_data,5," ,");
    // ArrayPrint(mas[1].m_data,5," ;");
    delete pma;
    return 0;
}

bool MyTrade::IsInDealTime()
{
    int hour = Time_Structure.hour;
    int min = Time_Structure.min;

    int us_left_hour = int(USTime_LeftBound);
    int us_right_hour = int(USTime_RightBound);
    int us_left_minute = 60*(USTime_LeftBound - us_left_hour);
    int us_right_minute = 60*(USTime_RightBound - us_right_hour);

    int nus_left_hour = int(NUSTime_LeftBound);
    int nus_right_hour = int(NUSTime_RightBound);
    int nus_left_minute = 60*(NUSTime_LeftBound - nus_left_hour);
    int nus_right_minute = 60*(NUSTime_RightBound - nus_right_hour);

    if(hour == us_left_hour && min>= us_left_minute) return true;
    if(hour == us_right_hour && min>0 && min< us_right_hour) return true;
    if(hour >= us_left_hour+1 && hour < us_right_hour) return true;
    // if(hour==15 && min>35) return true;
    // if(hour >15 && hour <= 17) return true;
    // if(hour>=9 && min>= left_minutes_in_fraction && hour<13) return true;
    if(hour == nus_left_hour && min>= nus_left_minute) return true;
    if(hour == nus_right_hour && min>0 && min< nus_right_hour) return true;
    if(hour >= nus_left_hour+1 && hour < nus_right_hour) return true;
    return false;
}

//+------------------------------------------------------------------+
//|  止盈止损判断条件                                                |
//+------------------------------------------------------------------+
bool MyTrade::CheckModiffy(){
    return false;
}

//+------------------------------------------------------------------+
//|  是否有必要逆转方向                                               |
//+------------------------------------------------------------------+
bool MyTrade::CheckReverse(){
    if(!Is_Reverse) return false;
    
    return false;
}

//+------------------------------------------------------------------+
//|  买入加仓信号判断条件，函数返回true表示可以买入加仓              |
//+------------------------------------------------------------------+
bool MyTrade::CheckAddLong(){
    if(state.profit > state.lots * 500){
        state.addlots = Trade_Init_Lots;
        state.dealprice = 0.0;
        return true;
    }
    return false;
}
//+------------------------------------------------------------------+
//|  卖出加仓信号判断条件，函数返回true表示卖出加仓
//+------------------------------------------------------------------+
bool MyTrade::CheckAddShort(){
    if(state.profit > state.lots * 500){
        state.addlots = Trade_Init_Lots;
        state.dealprice = 0.0;
        return true;
    }
    return false;
}
//+------------------------------------------------------------------+
//|  买入信号判断条件，函数返回true表示买入                           |
//+------------------------------------------------------------------+
bool MyTrade::LongOpen(){ 
    if( !(Deal_Direction_Allow==BOTH || Deal_Direction_Allow==UP) )
        return false; 
    // int longdir = MA_Direction(MAS6W1, 3, 0); 
    // if(IsMAEverFit()==1 ){
    //     return true;
    // }
    return false;
}

//+------------------------------------------------------------------+
//|  卖出信号判断条件，函数返回true表示卖出                          |
//+------------------------------------------------------------------+
bool MyTrade::ShortOpen(){
    if( !(Deal_Direction_Allow==BOTH || Deal_Direction_Allow==DOWN) )
        return false; 
    // int longdir = MA_Direction(MAS6W1, 3, 0); 
    // if(IsMAEverFit()==-1 ){
    //     return true;
    // }
    return false;
}

//+------------------------------------------------------------------+
//|  买入出场判断条件，函数返回true表示平仓                          |
//+------------------------------------------------------------------+
bool MyTrade::LongClose(){
    int direction = MA_Direction(MAS6, 2, 1);  
    if(iBars(_symbol, PERIOD_H1) >= state.will_close_bars && direction * state.direction != 1){
        return true;
    }
    return false;
}

//+------------------------------------------------------------------+
//|  卖出出场判断条件，函数返回true表示平仓                             |
//+------------------------------------------------------------------+
bool MyTrade::ShortClose(){
    int direction = MA_Direction(MAS6, 2, 1); 
    if(iBars(_symbol, PERIOD_H1) >= state.will_close_bars && direction * state.direction != 1){
        return true;
    }
    return false;
}
