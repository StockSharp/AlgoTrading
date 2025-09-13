//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                                    e-PSI@PROC.mq4 |
//|                                      Copyright © 2011, TarasBY & Evgeniy Trofimov |
//|                                                                taras_bulba@tut.by |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
#property copyright "Copyright © 2010-11, TarasBY & Evgeniy Trofimov"
#property link      "taras_bulba@tut.by"
/* * После установки эксперта на график менять таймфрейм не рекомендуется!
   * Рекомендуемые Таймфреймы H4 - D1 !!!
   * Допускается добавлять в код прочие торговые системы, способные стабильно
   * работать на таком же таймфрейме, что и остальные торговые стратегии.
   * ВНИМАНИЕ: Параметры указаны для 4-ёх знаков!*/
//IIIIIIIIIIIIIIIIIII==================CONSTANS=================IIIIIIIIIIIIIIIIIIIIII+
#define  MAX_TC                   8    // максимальное количество используемых стратегий
#define  Trend_UP                 0    // Константа для работы TraillingProfit
#define  Trend_DW                 1    // Константа для работы TraillingProfit
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  *****         Параметры советника         *****                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern string SETUP_Expert    = "==================== SETUP EXPERT ===================";
extern string file               = "virtual.csv"; // файл виртуальной торговли
extern int    MinRating          = 50;            // Минимальный рейтинг в процентах для открытия реальной позиции
extern int    Base.Magic		   = 878;           // Базовый рабочий магик для торговых систем
extern int    NewBarInPeriod     = 1;             // <= 0 - работаем на начале периода нового бара, -1 - работаем на каждом тике
extern int    Variant_TradePrice = 1;             // Вариант цен, с которыми будет работать советник
extern int    MAX_OrdersOnTC	   = 1;             // Максимальное число открытых ордеров на одну Торговую Систему
extern int    Period.New.Send	   = 1440;          // Периодичность проверки на открытие следующего ордера одной стратегией, если MAX_OrdersOnTC > 1
extern int    Period.Indicators  = 1440;          // Рабочий период для индикаторов
extern int    STOP.Period        = 70;            // Период для расчёта Стопов по МА
extern string Setup_TraillingMA  = "---- Trailling By MA ----";
extern int    MAMode             = 1;             // 0 = SMA, 1 = EMA, 2 = SSMA, 3 = LWMA
extern int    XFactor            = 84;
extern int    TrailPeriod        = 83;
extern string SETUP_TP        = "================= Trailling Profit ==================";
extern bool   TrailProfit_ON     = TRUE;          // Включение трейлинга общего Профита
extern bool   TrailProfitByMA    = TRUE;          // Трейлинг общего Профита по MA
extern int    TrailProfitPeriod  = 50;            // Период MA для трейлинга по MA
extern double TrailProfit_Start  = 300;           // Величина профита при котором начинается перемещение уровня закрытия 
extern double TrailProfit_Level  = 200;           // Величина обратного движения профита, при котором выполняется закрытие 
extern string SETUP_TC_1      = "================= TRADE SYSTEM № 1 ==================";
extern int    T1.Enabled         = 1;             // 1 - вкл.; 0 - выкл. 
extern double T1.lot             = 0.10;          // Величина фиксированного лота
extern int    T1.Var.STOP        = 1;             // Вариант Стопов: 0 - classic; 1 - By MA
extern int    T1.SL              = 300;           // Стоп лосс
extern int    T1.TP              = 300;           // Тэйк профит
extern int    T1.Var.TS          = 1;             // Вариант Трейлинг стопа: 0 - classic; 1 - By MA
extern int    T1.TS              = 100;           // Трейлинг стоп
extern bool   T1.OnlyBU          = False;         // TraillinStop только в БУ
extern int    T1.Fast            = 10;            // Период быстрой МА
extern int    T1.Slow            = 100;           // Период медленной МА
extern int    T1.PeriodRating    = 5;             // Период усреднения рейтинга
extern string SETUP_TC_2      = "================= TRADE SYSTEM № 2 ==================";
extern int    T2.Enabled         = 1;             // 1 - вкл.; 0 - выкл. 
extern double T2.lot             = 0.10;          // Величина фиксированного лота
extern int    T2.Var.STOP        = 1;             // Вариант Стопов: 0 - classic; 1 - By MA
extern int    T2.SL              = 500;           // Стоп лосс
extern int    T2.TP              = 500;           // Тэйк профит
extern int    T2.Var.TS          = 1;             // Вариант Трейлинг стопа: 0 - classic; 1 - By MA
extern int    T2.TS              = 150;           // Трейлинг стоп
extern bool   T2.OnlyBU          = False;         // TraillinStop только в БУ
extern int    T2.PeriodCCI       = 30;
extern int    T2.LevelCCI        = 200;
extern int    T2.PeriodRating    = 3;             // Период усреднения рейтинга
extern string SETUP_TC_3      = "================= TRADE SYSTEM № 3 ==================";
extern int    T3.Enabled         = 1;             // 1 - вкл.; 0 - выкл. 
extern double T3.lot             = 0.10;          // Величина фиксированного лота
extern int    T3.Var.STOP        = 1;             // Вариант Стопов: 0 - classic; 1 - By MA
extern int    T3.SL              = 300;           // Стоп лосс
extern int    T3.TP              = 300;           // Тэйк профит
extern int    T3.Var.TS          = 1;             // Вариант Трейлинг стопа: 0 - classic; 1 - By MA
extern int    T3.TS              = 80;            // Трейлинг стоп
extern bool   T3.OnlyBU          = False;         // TraillinStop только в БУ
extern int    T3.Fast            = 30;            // Период быстрой МА
extern int    T3.Slow            = 200;           // Период медленной МА
extern int    T3.PeriodRating    = 5;             // Период усреднения рейтинга
extern string SETUP_TC_4      = "================= TRADE SYSTEM № 4 ==================";
extern int    T4.Enabled         = 1;             // 1 - вкл.; 0 - выкл. 
extern double T4.lot             = 0.10;          // Величина фиксированного лота
extern int    T4.Var.STOP        = 1;             // Вариант Стопов: 0 - classic; 1 - By MA
extern double T4.SL              = 500;           // Стоп лосс
extern int    T4.TP              = 500;           // Тэйк профит
extern int    T4.Var.TS          = 1;             // Вариант Трейлинг стопа: 0 - classic; 1 - By MA
extern double T4.TS              = 150;           // Трейлинг стоп
extern bool   T4.OnlyBU          = False;         // TraillinStop только в БУ
extern double T4.LimitMACD       = 0.002;
extern int    T4.PeriodRating    = 5;             // Период усреднения рейтинга
extern string SETUP_TC_5      = "================= TRADE SYSTEM № 5 ==================";
extern int    T5.Enabled         = 1;             // 1 - вкл.; 0 - выкл. 
extern double T5.lot             = 0.10;          // Величина фиксированного лота
extern int    T5.Var.STOP        = 1;             // Вариант Стопов: 0 - classic; 1 - By MA
extern int    T5.SL              = 500;           // Стоп лосс
extern int    T5.TP              = 500;           // Тэйк профит
extern int    T5.Var.TS          = 1;             // Вариант Трейлинг стопа: 0 - classic; 1 - By MA
extern int    T5.TS              = 300;           // Трейлинг стоп
extern bool   T5.OnlyBU          = True;          // TraillinStop только в БУ
extern int    T5.PeriodCCI       = 90;
extern int    T5.LevelCCI        = 100;
extern int    T5.TralingCCI      = 10;
extern int    T5.PeriodRating    = 5;             // Период усреднения рейтинга
extern string SETUP_TC_6      = "================= TRADE SYSTEM № 6 ==================";
extern int    T6.Enabled         = 1;             // 1 - вкл.; 0 - выкл. 
extern int    VarPerceptron      = 1;             // 0 - Perceptron на Close\Open; 1 - на iStochastic; 2 - на CCI
extern double T6.lot             = 0.10;          // Величина фиксированного лота
extern int    T6.Var.STOP        = 1;             // Вариант Стопов: 0 - classic; 1 - By MA
extern int    T6.SL              = 500;           // Стоп лосс
extern int    T6.TP              = 500;           // Тэйк профит
extern int    T6.Var.TS          = 1;             // Вариант Трейлинг стопа: 0 - classic; 1 - By MA
extern int    T6.TS              = 150;           // Трейлинг стоп
extern bool   T6.OnlyBU          = False;         // TraillinStop только в БУ
extern int    x1                 = 120;           // 0 - 200: D = 1
extern int    x2                 = 172;           // 0 - 200: D = 1
extern int    x3                 = 39;            // 0 - 200: D = 1
extern int    x4                 = 172;           // 0 - 200: D = 1
extern double Per_BUY            = 20.0;
extern double Per_Close_BUY      = -5.0;
extern double Per_SELL           = -20.0;
extern double Per_Close_SELL     = 5.0;
extern string PerceptronBars     = "1,3,5,10,15";
extern string Setup_TC_6_Stoch   = "---- Настройки iStochastic ----";
extern int    T6.K_Period        = 92;
extern int    T6.D_Period        = 5;
extern int    T6.Slowing         = 11;
extern string Setup_TC_6_CCI     = "-------- Настройки iCCI -------";
extern int    T6.Period_CCI      = 14;
extern int    T6.Price_CCI       = 0;
extern int    T6.PeriodRating    = 5;             // Период усреднения рейтинга
extern string SETUP_TC_7      = "================= TRADE SYSTEM № 7 ==================";
extern int    T7.Enabled         = 1;             // 1 - вкл.; 0 - выкл. 
extern double T7.lot             = 0.10;          // Величина фиксированного лота
extern int    T7.Var.STOP        = 1;             // Вариант Стопов: 0 - classic; 1 - By MA
extern int    T7.SL              = 500;           // Стоп лосс
extern int    T7.TP              = 500;           // Тэйк профит
extern int    T7.Var.TS          = 1;             // Вариант Трейлинг стопа: 0 - classic; 1 - By MA
extern int    T7.TS              = 150;           // Трейлинг стоп
extern bool   T7.OnlyBU          = False;         // TraillinStop только в БУ
extern int    Warp               = -42;           // -100 - 100: D = 1
extern double Deviation          = 0.14;          // 0 - 2:      D = 0.01
extern int    Amplitude          = 8;             // 0 - 200:    D = 1
extern double Distortion         = 0.52;          // -1 - 1:     D = 0.01
extern double Dir_BUY            = 1.0;
extern double Dir_Close_BUY      = -0.8;
extern double Dir_SELL           = -1.0;
extern double Dir_Close_SELL     = 0.8;
extern string DirectionBars      = "1,3,5,10";
extern int    T7.PeriodRating    = 5;             // Период усреднения рейтинга
extern string SETUP_TC_8      = "================= TRADE SYSTEM № 8 ==================";
extern int    T8.Enabled         = 1;             // 1 - вкл.; 0 - выкл. 
extern double T8.lot             = 0.10;          // Величина фиксированного лота
extern int    T8.Var.STOP        = 1;             // Вариант Стопов: 0 - classic; 1 - By MA
extern int    T8.SL              = 300;           // Стоп лосс
extern int    T8.TP              = 300;           // Тэйк профит
extern int    T8.Var.TS          = 1;             // Вариант Трейлинг стопа: 0 - classic; 1 - By MA
extern int    T8.TS              = 120;           // Трейлинг стоп
extern bool   T8.OnlyBU          = False;         // TraillinStop только в БУ
extern int    T8.MA              = 40;            // Период быстрой МА
extern int    T8.PeriodRating    = 5;             // Период усреднения рейтинга
//IIIIIIIIIIIIIIIIIII======Глобальные переменные советника======IIIIIIIIIIIIIIIIIIIIII+
double        gda_Price[2],               // массив цен инструмента
              // gda_Price[0] - Bid
              // gda_Price[1] - Ask
              gda_TC.SL[MAX_TC],
              gda_TC.TP[MAX_TC],
              gda_TC.TS[MAX_TC],
              gda_TC.lot[MAX_TC],
              gd_Point, /*gd_MaxLot, gd_MinLot, gd_StepLot*/
              gd_Profit,
              gd_XFactor;
int           gia_TC.Magic[MAX_TC],
              gia_TC.Enable[MAX_TC],
              gia_TC.PeriodRating[MAX_TC],
              gia_TC.Var.TS[MAX_TC],
              gia_TC.Var.STOP[MAX_TC],
              gia_TC.Orders[MAX_TC],      // массив счётчиков открытых ордеров по каждой старатегии
              gia_PerceptronBars[5],
              gia_DirectionBars[4],
              gi_MyOrders,                // счётчик открытых ордеров по всем стратегиям
              gi_flag_TP,                 // флаг Traiiling Profit
              gi_flag_NullStop,           // флаг обнуления стопов
              gi_LastTrend = -1,          // предыдущее показание тренда
              gi_Decimal = 1, gi_Digits, gi_dig = 0;
string        gs_Symbol, gs_NameGV, gs_trade, gs_TradeCom[3], gs_VirtCom[3];
bool          gba_TC.OnlyBU[MAX_TC];
datetime      NewBar;
//IIIIIIIIIIIIIIIIIII==========Подключенные библиотеки==========IIIIIIIIIIIIIIIIIIIIII+
#include      <VirtualTrendForPROC.mqh>   // библиотека виртуальных сделок
#include      <RealTrendForPROC.mqh>      // библиотека торговых опреаций
#include      <b-PSI@ICManagerForPROC.mqh>       // библиотека управления инвест-капиталом
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  Custom expert initialization function                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int init()
{
//----
    gs_NameGV = "PROC";
    //---- Формируем префикс GV-переменных
    if (IsTesting())
    {gs_NameGV = gs_NameGV + "_t";}
    if (IsDemo())
    {gs_NameGV = gs_NameGV + "_d";}
	 gs_Symbol = Symbol();
    gd_MaxLot = MarketInfo (gs_Symbol, MODE_MAXLOT);
    gd_MinLot = MarketInfo (gs_Symbol, MODE_MINLOT);
    gd_StepLot = MarketInfo (gs_Symbol, MODE_LOTSTEP);
    gd_Point = MarketInfo (gs_Symbol, MODE_POINT);
    gi_Digits = MarketInfo (gs_Symbol, MODE_DIGITS);
    if (PrintCom) Print ("minLot = ", gd_MinLot," | stepLot = ", gd_StepLot);
    //---- Расчитываем разрядность лота
    while (MathPow (10, gi_dig) * gd_StepLot < 1)
    {gi_dig += 1;}
    //---- Учитываем работу 5-ти знака
    if (gi_Digits == 3 || gi_Digits == 5)
    {gi_Decimal = 10;}
    Slippage *= gi_Decimal;
    //---- Готовим к работе массивы данных
    fInitialArrays();
    gi_flag_TP = GlobalVariableGet (gs_NameGV + "_#flagTP");
    file = StringSubstr (file, 0, StringFind (file, ".csv"));
    file = StringConcatenate (file, "_", Symbol(), "_", Period(), ".csv");
    if (IsDemo() && !IsTesting())
    {file = "virtual_d.csv";}
    if (PrintCom) Print ("NameFile = ", file);
    gd_XFactor = NDD ((1.6180339887 - XFactor / 1.6180339887) * gd_Point);
    //---- Инициализируем библиотеку
    Init_ICManager (K_Begin);
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "init()");
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  Custor expert deinitialization function                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void deinit()
{
//----
    FastTest = false;
    VirtualFileSave (file);
    //---- Деинициализируем библиотеку по управлению инвест-капиталом
    deInit_ICManager (5, 15);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|               Custom expert iteration function                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void start()
{
    int  err = GetLastError();
    bool lb_result = false;
//----
    //---- Запускаем в работу библиотеку по управлению инвест-капиталом
    if (gb_RealTrade)
    {f_ICManager (5, 15, False, K_Begin);}
    //---- Определяем периодичность работы советника
    if (NewBarInPeriod >= 0)
    {
        if (NewBar == iTime (gs_Symbol, NewBarInPeriod, 0))
        {return (0);}
        NewBar = iTime (gs_Symbol, NewBarInPeriod, 0);
    }
    //---- Получаем цены, с которыми будет работать советник
    fGet_MineTradePrice (Variant_TradePrice, gda_Price);
    //---- Обновляем рыночную информацию виртуальной торговли
    VirtualUpdate (file);
    //---- Проверяем рыночные позиции на закрытие
    fClosings();
    //---- Трейлинг ордеров
    fTrailling();
    //---- Trailling Profit
    if (gi_MyOrders >= 2)
    {
        if (TrailProfitByMA)
        {lb_result = fTrailingProfitByMA (20.0, gd_Profit, gs_NameGV, gi_flag_TP, gi_flag_NullStop);}
        else
        {lb_result = fTrailingProfit (20.0, TrailProfit_Start, TrailProfit_Level, gd_Profit, gs_NameGV, gi_flag_TP, gi_flag_NullStop);}
        if (lb_result)
        {
            if (CloseOrderAll (gia_TC.Magic))
            return;
        }
    }
    //---- Проверяем условия на открытие ордеров
    fOpenings();
    //---- Запускаем в работу библиотеку по управлению инвест-капиталом (для тестирования по ценам открытия)
    if (!gb_RealTrade)
    f_ICManager (5, 15, False, K_Begin);
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "start()");
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Процедура закрытия позиций по сигналу                                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fClosings()
{
    int    i, j, li_total, li_POS, myFilter[], li_IND, err = GetLastError();
    double PriceClose;
    bool   lb_close;
//----
    //---- Перебираем все стратегии по очереди
    for (int li_TC = 0; li_TC < MAX_TC; li_TC++)
    {
        if (!(IsTesting() && gia_TC.Enable[li_TC] != 1))
        {
            li_total = OrdersTotal();
            for (li_POS = li_total - 1; li_POS >= 0; li_POS--)
            {
                if (OrderSelect (li_POS, SELECT_BY_POS))
                {
                    //---- Пропускаем не рыночные ордера
                    if (OrderType() > 1)
                    {continue;}
                    if (OrderMagicNumber() == gia_TC.Magic[li_TC])
                    {
                        lb_close = false;
                        switch (li_TC)
                        {
                            case 0: lb_close = T1_SignalClose (OrderType(), Period.Indicators); break;
                            case 1: lb_close = T2_SignalClose (OrderType(), Period.Indicators); break;
                            case 2: lb_close = T3_SignalClose (OrderType(), Period.Indicators); break;
                            case 3: lb_close = T4_SignalClose (OrderType(), Period.Indicators); break;
                            case 4: lb_close = T5_SignalClose (OrderType(), Period.Indicators); break;
                            case 5: lb_close = T6_SignalClose (OrderType(), Period.Indicators); break;
                            case 6: lb_close = T7_SignalClose (OrderType(), Period.Indicators); break;
                            case 7: lb_close = T8_SignalClose (OrderType(), OrderOpenTime(), Period.Indicators); break;
                        }
                        if (lb_close)
                        {
                            PriceClose = gda_Price[OrderType()];
                            if (!Block()) OrderClose (OrderTicket(), OrderLots(), PriceClose, Slippage);
                            if (PrintCom) Print ("close #", OrderTicket(), "[", li_TC + 1, "]: ", GetNameOP (OrderType()), " ", DSDig (OrderLots()), " at ", DSD (OrderOpenPrice()), " SL: ", DSD (OrderStopLoss()), " TP: ", DSD (OrderTakeProfit()), " Profit: $", DSD (OrderProfit()), " on a signal.");
                        }
                    }
                }
            }// Next li_POS
            VirtualFileLoad (file);
            VirtualFilter (VIRT_TRADES, -1, -1, Symbol(), gia_TC.Magic[li_TC]);
            if (Virt.Filter.Count > 0)
            {
                ArrayResize (myFilter, Virt.Filter.Count);
                for (li_POS = 0; li_POS < Virt.Filter.Count; li_POS++)
                {myFilter[li_POS] = Virt.Filter[li_POS];}// Next li_POS
                for (li_POS = 0; li_POS < ArraySize (myFilter); li_POS++)
                {
                    lb_close = false;
                    li_IND = myFilter[li_POS];
                    switch (li_TC)
                    {
                        case 0: lb_close = T1_SignalClose (Virt.Type[li_IND], Period.Indicators); break;
                        case 1: lb_close = T2_SignalClose (Virt.Type[li_IND], Period.Indicators); break;
                        case 2: lb_close = T3_SignalClose (Virt.Type[li_IND], Period.Indicators); break;
                        case 3: lb_close = T4_SignalClose (Virt.Type[li_IND], Period.Indicators); break;
                        case 4: lb_close = T5_SignalClose (Virt.Type[li_IND], Period.Indicators); break;
                        case 5: lb_close = T6_SignalClose (Virt.Type[li_IND], Period.Indicators); break;
                        case 6: lb_close = T7_SignalClose (Virt.Type[li_IND], Period.Indicators); break;
                        case 7: lb_close = T8_SignalClose (Virt.Type[li_IND], Virt.OpenTime[li_IND], Period.Indicators); break;
                    }
                    if (lb_close)
                    {
                        VirtualClose (Virt.Ticket[li_IND], file);
                        if (PrintCom) Print ("close Virtual #", Virt.Ticket[li_IND], "[", li_TC + 1, "]: ", GetNameOP (Virt.Type[li_IND]), " ", DSDig (Virt.Lots[li_IND]), " at ", Virt.OpenPrice[li_IND], " SL = ", DSD (Virt.StopLoss[li_IND]), " TP = ", DSD (Virt.TakeProfit[li_IND]), " at price = ", DSD (Virt.ClosePrice[li_IND]), " Profit = $", DoubleToStr (Virt.Profit[li_IND], 1));
                    }
                }// Next li_POS
            }
        }
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fClosings()");
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Процедура сопровождения открытых позиций (трал)                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fTrailling()
{
    double ld_TS, ld_Profit, ld_Price,
           ld_TS_step = 3 * gi_Decimal * gd_Point,
           ld_BU = 5 * gi_Decimal * gd_Point;
    int    li_total, li_cmd, li_POS, li_Ticket, myFilter[], cnt = 0, li_IND,
           err = GetLastError();
    bool   lb_modify;
//----
    gd_Profit = 0.0;
    gi_MyOrders = 0;
    //---- Перебираем все стратегии по очереди
    for (int li_TC = 0; li_TC < MAX_TC; li_TC++)
    {
        if (!IsTesting() || gia_TC.Enable[li_TC] == 1)
        {
            li_total = OrdersTotal();
            for (li_POS = li_total - 1; li_POS >= 0; li_POS--)
            {
                if (OrderSelect (li_POS, SELECT_BY_POS))
                {
                    //---- Пропускаем не рыночные ордера
                    if (OrderType() > 1)
                    {continue;}
                    if (OrderMagicNumber() == gia_TC.Magic[li_TC])
                    {
                        //----- Рассчитываем профит для работы TraillingProfit
                        ld_Profit = OrderProfit() + OrderSwap() + OrderCommission();
                        gd_Profit += ld_Profit;
                        li_Ticket = OrderTicket();
                        gi_MyOrders++;
                        if (gi_flag_NullStop == 0)
                        {
                            if (gi_flag_TP == 1)
                            {
                                Modify (li_Ticket, 0.0, 0.0, True);
                                if (PrintCom) {Print ("Delete STOP`s #", li_Ticket, "[", li_TC + 1, "]");}
                                cnt++;
                                continue;
                            }
                        }
                        else
                        {continue;}
                        ld_Price = gda_Price[OrderType()];
                        if (OrderType() == OP_BUY) {li_cmd = 1;} else {li_cmd = -1;}
                        //---- Рассчитываем размер трейлинг стопа
                        switch (gia_TC.Var.TS[li_TC])
                        {
                            case 0: ld_TS = gda_TC.TS[li_TC]; break;
                            case 1: ld_TS = NDD (MathAbs (ld_Price - (iMA (NULL, Period.Indicators, TrailPeriod, 0, MAMode, PRICE_OPEN, 0) + li_cmd * gd_XFactor))); break;
                        }
                        if (OrderType() == OP_BUY)
                        {
                            //---- Прописываем работу БУ
                            if (gba_TC.OnlyBU[li_TC])
                            {
                                if (OrderStopLoss() == 0 || OrderStopLoss() < OrderOpenPrice())
                                {
                                    if (ld_Price - OrderOpenPrice() > ld_TS)
                                    {
                                        Modify (li_Ticket, ld_Price - ld_TS + ld_BU);
                                        if (PrintCom) {Print ("Make BU #", li_Ticket, "[", li_TC + 1, "]: ", GetNameOP (OrderType()));}
                                    }
                                }
                            }
                            else
                            {
                                if (ld_Price - OrderOpenPrice() > ld_TS)
                                {
                                    if (OrderStopLoss() == 0 || ld_Price - OrderStopLoss() > ld_TS + ld_TS_step)
                                    {Modify (li_Ticket, ld_Price - ld_TS);}
                                }
                            }
                        }
                        else if (OrderType() == OP_SELL)
                        {
                            //---- Прописываем работу БУ
                            if (gba_TC.OnlyBU[li_TC])
                            {
                                if (OrderStopLoss() == 0 || OrderStopLoss() > OrderOpenPrice())
                                {
                                    if (OrderOpenPrice() - ld_Price > ld_TS)
                                    {
                                        Modify (li_Ticket, ld_Price + ld_TS - ld_BU);
                                        if (PrintCom) {Print ("Make BU #", li_Ticket, "[", li_TC + 1, "]: ", GetNameOP (OrderType()));}
                                    }
                                }
                            }
                            else
                            {
                                if (OrderOpenPrice() - ld_Price > ld_TS)
                                {
                                    if (OrderStopLoss() == 0 || OrderStopLoss() - ld_Price > ld_TS + ld_TS_step)
                                    {Modify (li_Ticket, ld_Price + ld_TS);}
                                }
                            }
                        }
                    }
                }
            }// Next li_POS
            VirtualFileLoad (file);
            VirtualFilter (VIRT_TRADES, -1, -1, Symbol(), gia_TC.Magic[li_TC]);
            if (Virt.Filter.Count > 0)
            {
                ArrayResize (myFilter, Virt.Filter.Count);
                for (li_POS = 0; li_POS < Virt.Filter.Count; li_POS++)
                {myFilter[li_POS] = Virt.Filter[li_POS];}
                for (li_POS = 0; li_POS < ArraySize (myFilter); li_POS++)
                {
                    lb_modify = false;
                    li_IND = myFilter[li_POS];
                    if (Virt.Type[li_IND] == OP_BUY)
                    {
                        if (gba_TC.OnlyBU[li_TC])
                        {
                            if (Virt.StopLoss[li_IND] == 0 || Virt.StopLoss[li_IND] < Virt.OpenPrice[li_IND])
                            {
                                if (ld_Price - Virt.OpenPrice[li_IND] > ld_TS)
                                {lb_modify = VirtualModify (Virt.Ticket[li_IND], 0, ld_Price - ld_TS + ld_BU, 0, 0, file);}
                            }
                        }
                        else
                        {
                            if (ld_Price - Virt.OpenPrice[li_IND] > ld_TS + ld_TS_step)
                            {
                                if (Virt.StopLoss[li_IND] == 0 || ld_Price - Virt.StopLoss[li_IND] > ld_TS)
                                {lb_modify = VirtualModify (Virt.Ticket[li_IND], 0, ld_Price - ld_TS, 0, 0, file);}
                            }
                        }
                    }
                    else if (Virt.Type[li_IND] == OP_SELL)
                    {
                        if (gba_TC.OnlyBU[li_TC])
                        {
                            if (Virt.StopLoss[li_IND] == 0 || Virt.StopLoss[li_IND] > Virt.OpenPrice[li_IND])
                            {   
                                if (Virt.OpenPrice[li_IND] - ld_Price > ld_TS)
                                {lb_modify = VirtualModify (Virt.Ticket[li_IND], 0, ld_Price + ld_TS - ld_BU, 0, 0, file);}
                            }
                        }
                        else
                        {
                            if (Virt.OpenPrice[li_IND] - ld_Price > ld_TS + ld_TS_step)
                            {
                                if (Virt.StopLoss[li_IND] == 0 || Virt.StopLoss[li_IND] - ld_Price > ld_TS)
                                {lb_modify = VirtualModify (Virt.Ticket[li_IND], 0, ld_Price + ld_TS, 0, 0, file);}
                            }
                        }
                    }
                    if (lb_modify)
                    {if (PrintCom) Print ("modify Virtual #", Virt.Ticket[li_IND], "[", li_TC + 1, "]: ", GetNameOP (Virt.Type[li_IND]), " ", DSDig (Virt.Lots[li_IND]), " at ", DSD (Virt.OpenPrice[li_IND]), " SL: ", DSD (Virt.StopLoss[li_IND]), " TP: ", DSD (Virt.TakeProfit[li_IND]));}
                }// Next li_POS
            }
        }
    }
    //---- Закрываем трейлинг
    if (gi_flag_NullStop == 0 && gi_flag_TP == 1)
    {
        if (PrintCom) Print ("Delete STOP`s on ", cnt, " orders");
        gi_flag_NullStop = 1;
        GlobalVariableSet (gs_NameGV + "_#NULLStops", gi_flag_NullStop);
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fTrailling()");
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Процедура открытия позиций по сигналу                                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fOpenings()
{
    int    li_CMD, li_Magic, li_Ticket, li_cmd, err = GetLastError();
    double ld_R, ld_OpenPrice, ld_StopLoss, ld_TakeProfit, ld_SL, ld_TP, ld_Lot,
           gd_StopLevel = MarketInfo (Symbol(), MODE_STOPLEVEL) * gd_Point;
    string ls_Comment;
//----
    //ArrayInitialize (gia_TC.Orders, 0);
    //---- Перебираем все стратегии по очереди
    for (int li_TC = 0; li_TC < MAX_TC; li_TC++)
    {
        if (gia_TC.Enable[li_TC] == 1)
        {   
            switch (li_TC)
            {
                case 0: li_CMD = T1_SignalOpen (Period.Indicators); break;
                case 1: li_CMD = T2_SignalOpen (Period.Indicators); break;
                case 2: li_CMD = T3_SignalOpen (Period.Indicators); break;
                case 3: li_CMD = T4_SignalOpen (Period.Indicators); break;
                case 4: li_CMD = T5_SignalOpen (Period.Indicators); break;
                case 5: li_CMD = T6_SignalOpen (Period.Indicators); break;
                case 6: li_CMD = T7_SignalOpen (Period.Indicators); break;
                case 7: li_CMD = T8_SignalOpen (Period.Indicators); break;
            }
            if (li_CMD > -1)
            {
                li_Magic = gia_TC.Magic[li_TC];
                VirtualFilter (VIRT_TRADES, -1, -1, Symbol(), li_Magic);
                gia_TC.Orders[li_TC] = Virt.Filter.Count;
                //---- Ограничиваем количество открываемых ордеров одной стратегией
                if (Virt.Filter.Count < MAX_OrdersOnTC)
                {
                    ld_R = VirtualRating (li_Magic, Symbol(), gia_TC.PeriodRating[li_TC], 0, file);
                    gda_R[li_TC] = ld_R;
                    if (!VirtualExist (iTime (Symbol(), Period.New.Send, 0), li_Magic))
                    {
                        if (li_CMD == OP_BUY) {li_cmd = 1;} else {li_cmd = -1;}
                        if (li_CMD == OP_BUY) {ld_OpenPrice = gda_Price[1];} else {ld_OpenPrice = gda_Price[0];}
                        switch (gia_TC.Var.STOP[li_TC])
                        {
                            case 0: 
                                ld_StopLoss = ld_OpenPrice - li_cmd * MathMax (gd_StopLevel, gda_TC.SL[li_TC]);
                                ld_TakeProfit = ld_OpenPrice + li_cmd * MathMax (gd_StopLevel, gda_TC.TP[li_TC]);
                                break;
                            case 1:
                                ld_SL = MathAbs (ld_OpenPrice - (iMA (NULL, Period.Indicators, STOP.Period, 0, MAMode, PRICE_OPEN, 0) + li_cmd * gd_XFactor)); 
                                ld_StopLoss = NDD (ld_OpenPrice - li_cmd * MathMax (gd_StopLevel, ld_SL));
                                if (gda_TC.TP[li_TC] == 0)
                                {
                                    if (li_cmd == 1) {li_cmd = -1;} else {li_cmd = 1;}
                                    ld_TP = MathAbs (ld_OpenPrice - (iMA (NULL, Period.Indicators, STOP.Period, 0, MAMode, PRICE_OPEN, 0) + li_cmd * gd_XFactor)); 
                                    ld_TakeProfit = MathAbs (NDD (ld_OpenPrice - li_cmd * MathMax (gd_StopLevel, ld_TP)));
                                }
                                else
                                {ld_TakeProfit = ld_OpenPrice + li_cmd * MathMax (gd_StopLevel, gda_TC.TP[li_TC]);}
                                break; 
                        }
                        ld_SL = MathMax (gd_StopLevel, ld_SL);
                        ld_Lot = gda_TC.lot[li_TC];
                        if (RatingON) if (PrintCom) Print ("System ", li_TC + 1, ": Rating = ", DS0 (ld_R), "%");
                        li_Ticket = VirtualSend (Symbol(), li_CMD, ld_Lot, ld_OpenPrice, Slippage, ld_StopLoss, ld_TakeProfit, "R = " + DoubleToStr (ld_R, 1) + "%", li_Magic, 0, file);
                        if (PrintCom) Print ("open Virtual #", li_Ticket, "[", li_TC + 1, "]: ", GetNameOP (li_CMD), " ", DSDig (ld_Lot), " at ", DSD (ld_OpenPrice), " SL[", MathAbs (ld_OpenPrice - ld_StopLoss) / gd_Point, "]: ", DSD (ld_StopLoss), " TP[", MathAbs (ld_OpenPrice - ld_TakeProfit) / gd_Point, "]: ", DSD (ld_TakeProfit), " R: ", DS0 (ld_R), "%");
                        //---- Если рейтниг стратегии > минимального рейтинга, открываем рыночный ордер
                        if (ld_R > MinRating        // условие соответсвия рейтинга
                        && gi_flag_TP == 0)         // не работает (включён) Trailling Profit
                        {
                            if (!OrderExist (iTime (Symbol(), Period.New.Send, 0), li_Magic))
                            {
                                li_Ticket = 0;
                                ld_Lot = ld_Lot * ld_R / 100;
                                ls_Comment = StringConcatenate ("System ", li_TC + 1, " (R = ", DoubleToStr (ld_R, 1), "%)");
                                if (li_CMD == OP_BUY)
                                {li_Ticket = BUY_pips (ld_Lot, ld_StopLoss, ld_TakeProfit, li_Magic, ls_Comment);}
                                else if (li_CMD == OP_SELL)
                                {li_Ticket = SELL_pips (ld_Lot, ld_StopLoss, ld_TakeProfit, li_Magic, ls_Comment);}
                                if (li_Ticket > 0)
                                {
                                    //Print ("Open orders #", li_Ticket, "[", ld_Lot, "/", li_Magic, "] from ", li_TC + 1, " strategy");
                                    if (OrderSelect (li_Ticket, SELECT_BY_TICKET))
                                    {OrderPrint();}
                                }
                            }
                        }
                    }// if (!VirtualExist...
                }
            }
        }
    }
    if (gi_MyOrders == 0)
    {
        gi_flag_TP = 0;
        gi_flag_NullStop = 0;
        GlobalVariableSet (gs_NameGV + "_#flagTP", gi_flag_TP);
        GlobalVariableSet (gs_NameGV + "_#NULLStops", gi_flag_NullStop);
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fOpenings()");
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на открытие позиции:                               |
//|         -1 - не открывать позицию                                                 |
//|         0 - покупать                                                              |
//|         1 - продавать                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int T1_SignalOpen (int fi_Period = 0)
{
    double MA_Fast = iMA (NULL, fi_Period, T1.Fast, 0, MODE_EMA, PRICE_CLOSE, 1),
           MA_Fast_Last = iMA (NULL, fi_Period, T1.Fast, 0, MODE_EMA, PRICE_CLOSE, 2),
           MA_Slow = iMA (NULL, fi_Period, T1.Slow, 0, MODE_EMA, PRICE_CLOSE, 1),
           MA_Slow_Last = iMA (NULL, fi_Period, T1.Slow, 0, MODE_EMA, PRICE_CLOSE, 2);
//----
    if (MA_Slow > MA_Slow_Last)
    {
        if (MA_Fast > MA_Slow && MA_Fast_Last < MA_Slow_Last)
        {return (OP_BUY);}
    }
    if (MA_Slow < MA_Slow_Last)
    {
        if (MA_Fast < MA_Slow && MA_Fast_Last > MA_Slow_Last)
        {return (OP_SELL);}
    }
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на закрытие позиции:                               |
//|         false - не закрывать позицию                                              |
//|         true - закрыть                                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool T1_SignalClose (int type, int fi_Period = 0)
{
    if (type == T1_SignalOpen (fi_Period)) return (false);
//----
    double MA_Fast = iMA (NULL, fi_Period, T1.Fast, 0, MODE_EMA, PRICE_CLOSE, 1),
           MA_Slow = iMA (NULL, fi_Period, T1.Slow, 0, MODE_EMA, PRICE_CLOSE, 1);
//----
    if (type == OP_BUY)
    {if (MA_Fast < MA_Slow) return (true);}
    else if (type == OP_SELL)
    {if (MA_Fast > MA_Slow) return (true);}
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на открытие позиции:                               |
//|         -1 - не открывать позицию                                                 |
//|         0 - покупать                                                              |
//|         1 - продавать                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int T2_SignalOpen (int fi_Period = 0, int Candle = 0)
{
    double CCI = iCCI (NULL, fi_Period, T2.PeriodCCI, PRICE_TYPICAL, Candle + 1),
           CCILast = iCCI (NULL, fi_Period, T2.PeriodCCI, PRICE_TYPICAL, Candle + 2);
//----
    if ((CCI < T2.LevelCCI) && (CCILast > T2.LevelCCI)) return (OP_SELL);
    if ((CCI > -T2.LevelCCI) && (CCILast < -T2.LevelCCI)) return (OP_BUY);
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на закрытие позиции:                               |
//|         false - не закрывать позицию                                              |
//|         true - закрыть                                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool T2_SignalClose (int type, int fi_Period = 0)
{
    int i = 0, SS = -1;
//----
    while (SS == -1)
    {
        SS = T2_SignalOpen (fi_Period, i);
        i++;
    }
    if (type == SS) return (false);
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на открытие позиции:                               |
//|         -1 - не открывать позицию                                                 |
//|         0 - покупать                                                              |
//|         1 - продавать                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int T3_SignalOpen (int fi_Period = 0)
{
    double MA_Fast = iMA (NULL, fi_Period, T3.Fast, 0, MODE_EMA, PRICE_CLOSE, 1),
           MA_Fast_Last = iMA (NULL, fi_Period, T3.Fast, 0, MODE_EMA, PRICE_CLOSE, 2),
           MA_Slow = iMA (NULL, fi_Period, T3.Slow, 0, MODE_EMA, PRICE_CLOSE, 1),
           MA_Slow_Last = iMA (NULL, fi_Period, T3.Slow, 0, MODE_EMA, PRICE_CLOSE, 2);
//----
    if (MA_Slow > MA_Slow_Last)
    {
        if (MA_Fast > MA_Slow && MA_Fast_Last < MA_Slow_Last)
        {return (OP_BUY);}
    }
    if (MA_Slow < MA_Slow_Last)
    {
        if (MA_Fast < MA_Slow && MA_Fast_Last > MA_Slow_Last)
        {return (OP_SELL);}
    }
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на закрытие позиции:                               |
//|         false - не закрывать позицию                                              |
//|         true - закрыть                                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool T3_SignalClose (int type, int fi_Period = 0)
{
    if (type == T3_SignalOpen (fi_Period)) return (false);
//----
    double MA_Fast =iMA (NULL, fi_Period, T3.Fast, 0, MODE_EMA, PRICE_CLOSE, 1),
           MA_Slow = iMA (NULL, fi_Period, T3.Slow, 0, MODE_EMA, PRICE_CLOSE, 1);
//----
    if (type == OP_BUY)
    {if (MA_Fast < MA_Slow) return (true);}
    else if (type == OP_SELL)
    {if (MA_Fast > MA_Slow) return (true);}
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на открытие позиции:                               |
//|         -1 - не открывать позицию                                                 |
//|         0 - покупать                                                              |
//|         1 - продавать                                                             |
//|      Стратегия Сергея Лозовика                                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int T4_SignalOpen (int fi_Period = 0, int CurrentCandle = 0)
{
    double LMA200 = iMA (NULL, fi_Period, 200, 0, MODE_EMA, PRICE_OPEN, CurrentCandle + 1),
           MA200 = iMA (NULL, fi_Period, 200, 0, MODE_EMA, PRICE_OPEN, CurrentCandle),
           LMA50 = iMA (NULL, fi_Period, 50, 0, MODE_EMA, PRICE_OPEN, CurrentCandle + 1),
           MA50 = iMA (NULL, fi_Period, 50, 0, MODE_EMA, PRICE_OPEN, CurrentCandle),
           LMA10 = iMA (NULL, fi_Period, 10, 0, MODE_EMA, PRICE_OPEN, CurrentCandle + 1),
           MA10 = iMA (NULL, fi_Period, 10, 0, MODE_EMA, PRICE_OPEN, CurrentCandle),
           LMACD = iMACD (NULL, fi_Period, 12, 26, 9, PRICE_OPEN, MODE_MAIN, CurrentCandle + 1),
           MACD = iMACD (NULL, fi_Period, 12, 26, 9, PRICE_OPEN, MODE_MAIN, CurrentCandle);
//----
    if (MA200 > LMA200)
    {
        if (MA50 > LMA50 && MA50 > MA200)
        {
            if (MA10 > LMA10 && MA10 > MA50)
            {
                if (MACD > LMACD && MACD > T4.LimitMACD)
                {return (0);}
            }
        }
    }
    else if (MA200 < LMA200)
    {
        if (MA50 < LMA50 && MA50 < MA200)
        {
            if (MA10 < LMA10 && MA10 < MA50)
            {
                if (MACD < LMACD && MACD < -T4.LimitMACD)
                {return (1);}
            }
        }
    }
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на закрытие позиции:                               |
//|         false - не закрывать позицию                                              |
//|         true - закрыть                                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool T4_SignalClose (int type, int fi_Period = 0)
{
    double MA50 = iMA (NULL, fi_Period, 50, 0, MODE_EMA, PRICE_OPEN, 0);
//----
    if (type == OP_BUY)
    {
        if (Close[1] < MA50)
        {return (true);}
    }
    else if (type == OP_SELL)
    {
        if (Close[1] > MA50)
        {return (true);}
    }
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на открытие позиции:                               |
//|         -1 - не открывать позицию                                                 |
//|         0 - покупать                                                              |
//|         1 - продавать                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int T5_SignalOpen (int fi_Period = 0, int Candle = 0)
{
    double CCI = iCCI (NULL, fi_Period, T5.PeriodCCI, PRICE_TYPICAL, Candle + 1),
           CCILast = iCCI (NULL, fi_Period, T5.PeriodCCI, PRICE_TYPICAL, Candle + 2);
//----
    if ((CCI > T5.LevelCCI) && (CCILast < T5.LevelCCI)) return (OP_BUY);
    if ((CCI < -T5.LevelCCI) && (CCILast > -T5.LevelCCI)) return (OP_SELL);
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на закрытие позиции:                               |
//|         false - не закрывать позицию                                              |
//|         true - закрыть                                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool T5_SignalClose (int type, int fi_Period = 0)
{
    int i = 0, SS = -1;
    double MyLevel, CCI;
//----
    if (type == OP_BUY)
    {MyLevel = T5.LevelCCI - T5.TralingCCI;}
    else {MyLevel = T5.LevelCCI + T5.TralingCCI;}
    while (SS == -1)  // Поиск сигнала на покупку
    {
        SS = T5_SignalOpen (fi_Period, i);
        i++;
        CCI = iCCI (NULL, fi_Period, T5.PeriodCCI, PRICE_TYPICAL, i);
        if (type == OP_BUY)
        {
            if (CCI > MyLevel + 2 * T5.TralingCCI)
            {MyLevel = MyLevel + T5.TralingCCI;}
        }
        else
        {
            if (CCI < MyLevel - 2 * T5.TralingCCI)
            {MyLevel = MyLevel - T5.TralingCCI;}
        }
    }
    CCI = iCCI (NULL, fi_Period, T5.PeriodCCI, PRICE_TYPICAL, 1);
    if (type == OP_BUY)
    {if (CCI < MyLevel) return (true);}
    else
    {if (CCI > MyLevel) return (true);}
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на открытие позиции:                               |
//|         -1 - не открывать позицию                                                 |
//|         0 - покупать                                                              |
//|         1 - продавать                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int T6_SignalOpen (int fi_Period = 0)
{
    double ld_Perceptron;
//----
    ld_Perceptron = Perceptron (fi_Period, VarPerceptron);
    if (ld_Perceptron >= Per_BUY) return (OP_BUY);
    if (ld_Perceptron <= Per_SELL) return (OP_SELL);
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на закрытие позиции:                               |
//|         false - не закрывать позицию                                              |
//|         true - закрыть                                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool T6_SignalClose (int type, int fi_Period = 0)
{
    double ld_Perceptron;
//----
    ld_Perceptron = Perceptron (fi_Period, VarPerceptron);
    if (type == OP_BUY)
    {
        if (ld_Perceptron <= Per_Close_BUY)
        {return (true);}
    }
    else if (type == OP_SELL)
    {
        if (ld_Perceptron >= Per_Close_SELL)
        {return (true);}
    }
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Расчитываем Perceptron                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double Perceptron (int fi_TF, int fi_varPerceptron)
{
   double lda_PRC[4], lda_X[4], ld_result = 0;
//----
   lda_X[0] = x1 - 100.0;
   lda_X[1] = x2 - 100.0;
   lda_X[2] = x3 - 100.0;
   lda_X[3] = x4 - 100.0;
   for (int li_PRC = 0; li_PRC < 4; li_PRC++)
   {
       switch (fi_varPerceptron)
       {
           case 0: lda_PRC[li_PRC] = iClose (NULL, fi_TF, gia_PerceptronBars[li_PRC]) - iOpen (NULL, fi_TF, gia_PerceptronBars[li_PRC+1]); break;
           case 1: lda_PRC[li_PRC] = iStochastic (NULL, fi_TF, T6.K_Period, T6.D_Period, T6.Slowing, MODE_EMA, 0, MODE_MAIN, gia_PerceptronBars[li_PRC]) - 50.0; break;
           case 2: lda_PRC[li_PRC] = iCCI (NULL, fi_TF, T6.Period_CCI, T6.Price_CCI, gia_PerceptronBars[li_PRC]); break;
       }
       ld_result += lda_X[li_PRC] * lda_PRC[li_PRC];
   }
   return (ld_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на открытие позиции:                               |
//|         -1 - не открывать позицию                                                 |
//|         0 - покупать                                                              |
//|         1 - продавать                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int T7_SignalOpen (int fi_Period = 0)
{
    double ld_Direction;
//----
    ld_Direction = Direction (gia_DirectionBars, Warp, Deviation, Amplitude, Distortion, fi_Period);
    if (ld_Direction >= Dir_BUY) return (OP_BUY);
    if (ld_Direction <= Dir_SELL) return (OP_SELL);
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на закрытие позиции:                               |
//|         false - не закрывать позицию                                              |
//|         true - закрыть                                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool T7_SignalClose (int type, int fi_Period = 0)
{
    double ld_Direction;
//----
    ld_Direction = Direction (gia_DirectionBars, Warp, Deviation, Amplitude, Distortion, fi_Period);
    if (type == OP_BUY)
    {
        if (ld_Direction <= Dir_Close_BUY)
        {return (true);}
    }
    else if (type == OP_SELL)
    {
        if (ld_Direction >= Dir_Close_SELL)
        {return (true);}
    }
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Расчитываем Direction                                                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double Direction (int iar_NBars[],       // массив номеров баров для расчёта Direction
                  int fi_Warp, 
                  double fd_Deviation, 
                  int fi_Amplitude, 
                  double fd_Distortion, 
                  int fi_TF = 0)         // таймфрейм индикатора AC
{
    double ld_iAC[4];
//----
    for (int li_int = 0; li_int < 4; li_int++)
    {ld_iAC[li_int] = iAC (Symbol(), fi_TF, iar_NBars[li_int]);}
//----
    return (fi_Warp * ld_iAC[0] + 100.0 * (fd_Deviation - 1.0) * ld_iAC[1] + (fi_Amplitude - 100) * ld_iAC[2] + 100.0 * fd_Distortion * ld_iAC[3]);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на открытие позиции:                               |
//|         -1 - не открывать позицию                                                 |
//|         0 - покупать                                                              |
//|         1 - продавать                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int T8_SignalOpen (int fi_Period = 0)
{
    double li_Trend;
//----
    li_Trend = fMA_TrendDetection (fi_Period);
    if (gi_LastTrend < 0)
    {gi_LastTrend = li_Trend;}
    if (li_Trend == Trend_UP && gi_LastTrend == Trend_DW)
    {
        gi_LastTrend = Trend_UP;
        return (OP_BUY);
    }
    if (li_Trend == Trend_DW && gi_LastTrend == Trend_UP)
    {
        gi_LastTrend = Trend_DW;
        return (OP_SELL);
    }
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      Функция возвращает сигнал на закрытие позиции:                               |
//|         false - не закрывать позицию                                              |
//|         true - закрыть                                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool T8_SignalClose (int type, datetime fdt_OpenTime, int fi_Period = 0)
{
    if (fdt_OpenTime + Period.New.Send * 60 > TimeCurrent())
    {return (false);}
    double li_Trend;
//----
    li_Trend = fMA_TrendDetection (fi_Period);
    if (type == OP_BUY)
    {
        if (li_Trend == Trend_DW && gi_LastTrend == Trend_UP)
        {return (true);}
    }
    else if (type == OP_SELL)
    {
        if (li_Trend == Trend_UP && gi_LastTrend == Trend_DW)
        {return (true);}
    }
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|      TrendDetection                                                               |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fMA_TrendDetection (int fi_TF = 0)
{
    double ld_MA, ld_MA_next;
    int    lia_Price[] = {PRICE_CLOSE,PRICE_WEIGHTED,PRICE_TYPICAL,PRICE_MEDIAN,PRICE_OPEN},
           liaTrend[] = {0,0};
//-----
    for (int li_MA = 0; li_MA < 4; li_MA++)
    {
        ld_MA = iMA (NULL, fi_TF, T8.MA, 0, MAMode, lia_Price[li_MA], 0);
        ld_MA_next = iMA (NULL, fi_TF, T8.MA, 0, MAMode, lia_Price[li_MA+1], 0);
        if (ld_MA > ld_MA_next)
        {liaTrend[0]++;}
        if (ld_MA < ld_MA_next)
        {liaTrend[1]++;}
    }
    if (liaTrend[0] == 4)
    return (Trend_UP);
    if (liaTrend[1] == 4)
    return (Trend_DW);
//-----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Проверка на возможность осуществления торговых операций                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool Block()
{
//----
    if (!IsTradeAllowed() || IsTradeContextBusy() || !IsConnected())
    {
        Print ("Рынок занят, нет связи, или советнику не разрешено торговать");
        return (true);
    }
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Готовим к работе массивы данных                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fInitialArrays()
{
    int    li_size, li_int;
    string tmpArr[];
//----
    //---- Подготовка компонентов Perceptron
    if (T6.Enabled == 1)
    {
        fSplitStrToStr (PerceptronBars, tmpArr);
        li_size = ArraySize (tmpArr);
        ArrayResize (gia_PerceptronBars, li_size);
        for (li_int = 0; li_int < li_size; li_int++)
        {gia_PerceptronBars[li_int] = StrToInteger (tmpArr[li_int]);}
    }
    //---- Подготовка компонентов Direction
    if (T7.Enabled == 1)
    {
        fSplitStrToStr (DirectionBars, tmpArr);
        li_size = ArraySize (tmpArr);
        ArrayResize (gia_DirectionBars, li_size);
        for (li_int = 0; li_int < li_size; li_int++)
        {gia_DirectionBars[li_int] = StrToInteger (tmpArr[li_int]);}
    }
    //---- Подготовка рабочих массивов
    for (int li_MG = 0; li_MG < MAX_TC; li_MG++)
    {
        gia_TC.Magic[li_MG] = Base.Magic + li_MG;
        switch (li_MG)
        {
            case 0:
                gia_TC.Enable[li_MG] = T1.Enabled;
                gda_TC.SL[li_MG] = T1.SL * gi_Decimal * gd_Point;
                gda_TC.TP[li_MG] = T1.TP * gi_Decimal * gd_Point;
                gda_TC.TS[li_MG] = T1.TS * gi_Decimal * gd_Point;
                gda_TC.lot[li_MG] = T1.lot;
                gia_TC.PeriodRating[li_MG] = T1.PeriodRating;
                gba_TC.OnlyBU[li_MG] = T1.OnlyBU;
                gia_TC.Var.STOP[li_MG] = T1.Var.STOP;
                gia_TC.Var.TS[li_MG] = T1.Var.TS;
                break;
            case 1:
                gia_TC.Enable[li_MG] = T2.Enabled;
                gda_TC.SL[li_MG] = T2.SL * gi_Decimal * gd_Point;
                gda_TC.TP[li_MG] = T2.TP * gi_Decimal * gd_Point;
                gda_TC.TS[li_MG] = T2.TS * gi_Decimal * gd_Point;
                gda_TC.lot[li_MG] = T2.lot;
                gia_TC.PeriodRating[li_MG] = T2.PeriodRating;
                gba_TC.OnlyBU[li_MG] = T2.OnlyBU;
                gia_TC.Var.STOP[li_MG] = T2.Var.STOP;
                gia_TC.Var.TS[li_MG] = T2.Var.TS;
                break;
            case 2:
                gia_TC.Enable[li_MG] = T3.Enabled;
                gda_TC.SL[li_MG] = T3.SL * gi_Decimal * gd_Point;
                gda_TC.TP[li_MG] = T3.TP * gi_Decimal * gd_Point;
                gda_TC.TS[li_MG] = T3.TS * gi_Decimal * gd_Point;
                gda_TC.lot[li_MG] = T3.lot;
                gia_TC.PeriodRating[li_MG] = T3.PeriodRating;
                gba_TC.OnlyBU[li_MG] = T3.OnlyBU;
                gia_TC.Var.STOP[li_MG] = T3.Var.STOP;
                gia_TC.Var.TS[li_MG] = T3.Var.TS;
                break;
            case 3:
                gia_TC.Enable[li_MG] = T4.Enabled;
                gda_TC.SL[li_MG] = T4.SL * gi_Decimal * gd_Point;
                gda_TC.TP[li_MG] = T4.TP * gi_Decimal * gd_Point;
                gda_TC.TS[li_MG] = T4.TS * gi_Decimal * gd_Point;
                gda_TC.lot[li_MG] = T4.lot;
                gia_TC.PeriodRating[li_MG] = T4.PeriodRating;
                gba_TC.OnlyBU[li_MG] = T4.OnlyBU;
                gia_TC.Var.STOP[li_MG] = T4.Var.STOP;
                gia_TC.Var.TS[li_MG] = T4.Var.TS;
                break;
            case 4:
                gia_TC.Enable[li_MG] = T5.Enabled;
                gda_TC.SL[li_MG] = T5.SL * gi_Decimal * gd_Point;
                gda_TC.TP[li_MG] = T5.TP * gi_Decimal * gd_Point;
                gda_TC.TS[li_MG] = T5.TS * gi_Decimal * gd_Point;
                gda_TC.lot[li_MG] = T5.lot;
                gia_TC.PeriodRating[li_MG] = T5.PeriodRating;
                gba_TC.OnlyBU[li_MG] = T5.OnlyBU;
                gia_TC.Var.STOP[li_MG] = T5.Var.STOP;
                gia_TC.Var.TS[li_MG] = T5.Var.TS;
                T5.TralingCCI *= gi_Decimal;
                break;
            case 5:
                gia_TC.Enable[li_MG] = T6.Enabled;
                gda_TC.SL[li_MG] = T6.SL * gi_Decimal * gd_Point;
                gda_TC.TP[li_MG] = T6.TP * gi_Decimal * gd_Point;
                gda_TC.TS[li_MG] = T6.TS * gi_Decimal * gd_Point;
                gda_TC.lot[li_MG] = T6.lot;
                gia_TC.PeriodRating[li_MG] = T6.PeriodRating;
                gba_TC.OnlyBU[li_MG] = T6.OnlyBU;
                gia_TC.Var.STOP[li_MG] = T6.Var.STOP;
                gia_TC.Var.TS[li_MG] = T6.Var.TS;
                break;
            case 6:
                gia_TC.Enable[li_MG] = T7.Enabled;
                gda_TC.SL[li_MG] = T7.SL * gi_Decimal * gd_Point;
                gda_TC.TP[li_MG] = T7.TP * gi_Decimal * gd_Point;
                gda_TC.TS[li_MG] = T7.TS * gi_Decimal * gd_Point;
                gda_TC.lot[li_MG] = T7.lot;
                gia_TC.PeriodRating[li_MG] = T7.PeriodRating;
                gba_TC.OnlyBU[li_MG] = T7.OnlyBU;
                gia_TC.Var.STOP[li_MG] = T7.Var.STOP;
                gia_TC.Var.TS[li_MG] = T7.Var.TS;
                break;
            case 7:
                gia_TC.Enable[li_MG] = T8.Enabled;
                gda_TC.SL[li_MG] = T8.SL * gi_Decimal * gd_Point;
                gda_TC.TP[li_MG] = T8.TP * gi_Decimal * gd_Point;
                gda_TC.TS[li_MG] = T8.TS * gi_Decimal * gd_Point;
                gda_TC.lot[li_MG] = T8.lot;
                gia_TC.PeriodRating[li_MG] = T8.PeriodRating;
                gba_TC.OnlyBU[li_MG] = T8.OnlyBU;
                gia_TC.Var.STOP[li_MG] = T8.Var.STOP;
                gia_TC.Var.TS[li_MG] = T8.Var.TS;
                break;
        }
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fInitialArrays()");
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Общие функции                                                             |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Возвращает массив STRING из строки, разделённой sDelimiter                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
/*int fSplitStrToStr (string aString, string& aArray[], string aDelimiter = ",")
{
    string tmp_str = "", tmp_char = "";
//----
    ArrayResize (aArray, 0);
    for (int i = 0; i < StringLen (aString); i++)
    {
        tmp_char = StringSubstr (aString, i, 1);
        if (tmp_char == aDelimiter)
        {
            if (StringTrimLeft (StringTrimRight (tmp_str)) != "")
            {
                ArrayResize (aArray, ArraySize (aArray) + 1);
                aArray[ArraySize (aArray) - 1] = tmp_str;
            }
            tmp_str = "";
        }
        else
        {
            if (tmp_char != " ")
            {tmp_str = tmp_str + tmp_char;}
        }
    }
    if (StringTrimLeft (StringTrimRight (tmp_str)) != "")
    {
        ArrayResize (aArray, ArraySize (aArray) + 1);
        aArray[ArraySize (aArray) - 1] = tmp_str;
    }
//----
    return (ArraySize (aArray));
}*/
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                          |
//+-----------------------------------------------------------------------------------+
//|  Версия   : 01.09.2005                                                            |
//|  Описание : Возвращает наименование торговой операции                             |
//+-----------------------------------------------------------------------------------+
//|  Параметры:                                                                       |
//|    op - идентификатор торговой операции                                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string GetNameOP (int op) 
{
    switch (op) 
    {
        case OP_BUY      : return ("BUY");
        case OP_SELL     : return ("SELL");
        case OP_BUYLIMIT : return ("BUYLIMIT");
        case OP_SELLLIMIT: return ("SELLLIMIT");
        case OP_BUYSTOP  : return ("BUYSTOP");
        case OP_SELLSTOP : return ("SELLSTOP");
    }
    return (StringConcatenate ("None (", op, ")"));
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Функция, нормализации значения double до Digit                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double NDD (double v) {return (NormalizeDouble (v, gi_Digits));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Функция, перевода значения из double в string c нормализацией по 0         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DS0 (double v) {return (DoubleToStr (v, 0));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Функция, перевода значения из double в string c нормализацией по Digit     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DSD (double v) {return (DoubleToStr (v, gi_Digits));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Функция, перевода значения из double в string c нормализацией по           |
//| минимальной разрядности лота                                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DSDig (double v) {return (DoubleToStr (v, gi_dig));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Возвращает наименование состояния (ДА\НЕТ)                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string CheckBOOL (int M)
{
//----
    switch (M)
    {
        case 0: {return ("Нет");}
        case 1: {return ("Да");}
    }
//----
    return ("Не знаю...");
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Работа с ордерами                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Функция закрытия ордеров по типу (OP) и Magic (iMG)                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool CloseOrderAll (int iar_MG[], int OP = -1)
{
    color  lc_close;
    int    li_Type, li_NUM, err = GetLastError();
    double ld_ClosePrice;
    bool   lb_Close = false;
//----
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {
        if (!OrderSelect (i, SELECT_BY_POS, MODE_TRADES))
        {continue;}
        li_NUM = fCheckMyMagic (OrderMagicNumber(), iar_MG);
        if (li_NUM < 0)
        {continue;}
        if (OrderSymbol() != Symbol() || OrderType() > 1)
        {continue;}
        li_Type = OrderType();
        if (OP == li_Type || OP == -1)
        {
            ld_ClosePrice = gda_Price[li_Type];
            if (li_Type == OP_BUY) {lc_close = Blue;} else {lc_close = Red;}
            lb_Close = OrderClose (OrderTicket(), OrderLots(), ld_ClosePrice, Slippage, lc_close);
        }
    }
    fGetLastError (gs_ComError, "CloseOrderAll()");
//----
    return (lb_Close);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор    : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|  Описание : Получаем цены, с которыми будет работать советник                     |
//+-----------------------------------------------------------------------------------+
//|  Параметры:                                                                       |
//|    iPrice          : 0 - Bid; 1 - Ask                                             |
//|    fi_VariantPrice : 0 - Bid\Ask; 1 - Open[0]; 2 - Close [1]; 3 - Close[0]        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_MineTradePrice (int fi_VariantPrice, double& ar_Price[])
{
//----
    switch (fi_VariantPrice)
    {
        case 0: 
            RefreshRates();
            ar_Price[0] = Bid;
            ar_Price[1] = Ask;
            break;
        case 1: double ld_Price = iOpen (Symbol(), NewBarInPeriod, 0); break;
        case 2: ld_Price = iClose (Symbol(), NewBarInPeriod, 1); break;
        case 3: ld_Price = iClose (Symbol(), NewBarInPeriod, 0); break;
    }
    double ld_spread = MarketInfo (gs_Symbol, MODE_SPREAD) * gd_Point;
    ar_Price[0] = ld_Price;
    ar_Price[1] = ld_Price + ld_spread;
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  UNI:      Тралим профит по счёту по MA                                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fTrailingProfitByMA (int TrailStopMin,     // нижняя граница срабатывания Trailing
                         double dProfit,       // профит (контролируемая величина)
                         string Name_GV,       // префикс для GV-переменных
                         int& flag_TP,         // флаг срабатывания Trailing
                         int& flag_Stop)       // флаг обнуления стопов
{
    static double MaxProfit, ld_Profit_BU, ld_ProfitMIN, ld_ProfitSL;
    static int    li_Trend = -1;
    static bool   lsb_ON = false, lsb_BU = false;
    double ld_MA, ld_ProfitTS, ld_Price;
    string ls_fName = "fTrailingProfitByMA()";
    int    err = GetLastError();
    bool   lb_result = false;
//---- 
    if (dProfit > 0)
    {
        //---- Фиксируем уровень БУ для общего профита
        if (!lsb_BU)
        {
            ld_Profit_BU = gda_Price[0];
            lsb_BU = true;
        }
        if (dProfit > TrailStopMin)
        {
            //---- Фиксируем уровень MinProfit для общего профита
            if (!lsb_ON)
            {
                ld_ProfitMIN = gda_Price[0];
                fCreat_OBJ ("Profit BU", OBJ_HLINE, 0, "Profit BU", 0, Time[0], ld_Profit_BU, false, Gold);
                lsb_ON = true;
                if (ld_ProfitMIN > ld_Profit_BU)
                {li_Trend = Trend_UP;} else {li_Trend = Trend_DW;}
                if (PrintCom) Print ("Profit BU = ", DSD (ld_Profit_BU));
            }
        }
    }
    else if (dProfit < 0)
    {
        ld_Profit_BU = 0.0;
        ld_ProfitMIN = 0.0;
        lsb_BU = false;
        lsb_ON = false;
        li_Trend = -1;
        flag_TP = 0;
        flag_Stop = 0;
        DelObject ("Profit BU");
        DelObject ("Profit SL");
    }
    if (ld_Profit_BU > 0.0)
    {
        ld_MA = iMA (NULL, Period.Indicators, TrailProfitPeriod, 0, MAMode, PRICE_OPEN, 0);
        if (li_Trend == Trend_UP)      // Trend UP
        {
            ld_Price = gda_Price[0];
            //---- Определяем размер трейлинг стопа
            ld_ProfitTS = NDD (MathAbs (ld_Price - (ld_MA + gd_XFactor)));
            if (flag_TP == 0)
            {
                if (ld_Price - ld_Profit_BU > ld_ProfitTS)
                {
                    ld_ProfitSL = NDD (ld_MA + gd_XFactor);
                    flag_TP = 1;
                    ObjectSet ("Profit BU", OBJPROP_COLOR, Blue);
                    GlobalVariableSet (Name_GV + "_#flagTP", flag_TP);
                    gs_trade = fPrepareComment (StringConcatenate (ls_fName, ": TrailingProfit_ON[UP]. flagTrailProfit = ", CheckBOOL (flag_TP), "."), gb_InfoPrint);
                    if (PrintCom) Print ("Trailling Stop = ", ld_ProfitTS / gd_Point, " pips; Profit SL[", (ld_Price - ld_ProfitSL) / gd_Point, "] = ", DSD (ld_ProfitSL), "; Price = ", ld_Price);
                    if (PrintCom) {Print (gs_trade);}
                }
            }
            if (flag_TP == 1)
            {
                if (ld_Price - ld_ProfitSL > ld_ProfitTS)
                {
                    ld_ProfitSL = ld_Price - ld_ProfitTS;
                    MaxProfit = MathMax (MaxProfit, dProfit);
                    DelObject ("Profit BU");
                    fCreat_OBJ ("Profit SL", OBJ_HLINE, 0, "Profit SL", 0, Time[0], ld_ProfitSL, false, Blue);
                    ObjectSet ("Profit SL", OBJPROP_STYLE, DRAW_SECTION);
                    gs_trade = fPrepareComment (StringConcatenate (ls_fName, ": Work TP - MAX = $", DS0 (MaxProfit), "; Profit SL [", (ld_Price - ld_ProfitSL) / gd_Point, "] = ", DSD (ld_ProfitSL), ")."), gb_InfoPrint);
                    if (PrintCom && fCCV_D (ND0 (MaxProfit), 3)) {Print (gs_trade);}
                }
                if (ld_Price < ld_ProfitSL)
                {lb_result = true;}
            }
        }
        if (li_Trend == Trend_DW)      // Trend DW
        {
            ld_Price = gda_Price[1];
            ld_ProfitTS = NDD (MathAbs ((ld_MA - gd_XFactor) - ld_Price));
            if (flag_TP == 0)
            {
                if (ld_Profit_BU - ld_Price > ld_ProfitTS)
                {
                    ld_ProfitSL = NDD (ld_MA - gd_XFactor);
                    flag_TP = 1;
                    ObjectSet ("Profit BU", OBJPROP_COLOR, Red);
                    GlobalVariableSet (Name_GV + "_#flagTP", flag_TP);
                    gs_trade = fPrepareComment (StringConcatenate (ls_fName, ": TrailingProfit_ON[DW]. flagTrailProfit = ", CheckBOOL (flag_TP), "."), gb_InfoPrint);
                    if (PrintCom) Print ("Trailling Stop = ", ld_ProfitTS / gd_Point, " pips; Profit SL[", (ld_ProfitSL - ld_Price) / gd_Point, "] = ", DSD (ld_ProfitSL), "; Price = ", ld_Price);
                    if (PrintCom) {Print (gs_trade);}
                }
            }
            if (flag_TP == 1)
            {
                if (ld_ProfitSL - ld_Price > ld_ProfitTS)
                {
                    ld_ProfitSL = ld_Price + ld_ProfitTS;
                    MaxProfit = MathMax (MaxProfit, dProfit);
                    DelObject ("Profit BU");
                    fCreat_OBJ ("Profit SL", OBJ_HLINE, 0, "Profit SL", 0, Time[0], ld_ProfitSL, false, Red);
                    ObjectSet ("Profit SL", OBJPROP_STYLE, DRAW_SECTION);
                    gs_trade = fPrepareComment (StringConcatenate (ls_fName, ": Work TP - MAX = $", DS0 (MaxProfit), "; Profit SL[", (ld_ProfitSL - ld_Price) / gd_Point, "] = ", DSD (ld_ProfitSL), ")."), gb_InfoPrint);
                    if (PrintCom && fCCV_D (ND0 (MaxProfit), 3)) {Print (gs_trade);}
                }
                if (ld_Price > ld_ProfitSL)
                {lb_result = true;}
            }
        }
    }
    if (lb_result)
    {
        gs_trade = fPrepareComment (StringConcatenate (ls_fName, ": Close from TP, Profit = $", DS0 (dProfit), "."), gb_InfoPrint);
        if (PrintCom) {Print (gs_trade);}
        MaxProfit = 0;
        flag_TP = 0;
        flag_Stop = 0;
        GlobalVariableSet (Name_GV + "_#flagTP", flag_TP);
        GlobalVariableSet (Name_GV + "_#NULLStops", flag_Stop);
        DelObject ("Profit BU");
        DelObject ("Profit SL");
        return (true);
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, ls_fName);
    return (false);
//---- 
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  UNI:      Тралим профит по счёту                                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fTrailingProfit (int TrailStopMin,     // нижняя граница срабатывания Trailing
                      int TrailStart,       // величина срабатывания Trailing
                      int TrailValue,       // величина обратного отката после срабатывания
                      double dProfit,       // профит (контролируемая величина)
                      string Name_GV,       // префикс для GV-переменных
                      int& flag_TP,         // флаг срабатывания Trailing
                      int& flag_Stop)       // флаг обнуления стопов
{
    static double MinProfit, MaxProfit;
    static int cur_TrailValue, cur_TrailStart, cur_TrailStopMin;
    string ls_fName = "fTrailingProfit()";
    int    err = GetLastError();
//---- 
    cur_TrailValue = TrailValue;
    cur_TrailStart = TrailStart;
    if (MinProfit > 0)
    {cur_TrailStopMin = MathMin (TrailStopMin, MinProfit);}
//---- 
    if (flag_TP == 0)
    {
        if (dProfit >= cur_TrailStart)
        {
            MinProfit = dProfit - cur_TrailValue;      // нижняя граница коридора
            MaxProfit = MinProfit + cur_TrailValue;    // верхняя граница коридора
            flag_TP = 1;
            GlobalVariableSet (Name_GV + "_#flagTP", flag_TP);
            gs_trade = fPrepareComment (StringConcatenate (ls_fName, ": TrailingProfit_ON. flagTrailProfit = ", CheckBOOL (flag_TP), "."), gb_InfoPrint);
            if (PrintCom) {Print (gs_trade);}
            return (false);
        }
    }
    if (flag_TP == 1)
    {
        if (dProfit >= cur_TrailStart)
        {
            MinProfit = MathMax (MinProfit, dProfit - cur_TrailValue);
            MaxProfit = MathMax (MaxProfit, dProfit);
            fCreat_OBJ ("Profit SL", OBJ_HLINE, 0, "Profit SL", 0, Time[0], MinProfit, false, Yellow);
            ObjectSet ("Profit SL", OBJPROP_STYLE, DRAW_SECTION);
            gs_trade = fPrepareComment (StringConcatenate (ls_fName, ": Work TP. (UP = $", DS0 (MaxProfit), "; DOWN = $", DS0 (MinProfit), "), ProfitStart = ", DS0 (cur_TrailStart), "."), gb_InfoPrint);
            if (PrintCom && fCCV_D (ND0 (MaxProfit), 3)) {Print (gs_trade);}
        }
        if (dProfit < MinProfit)
        {
            if (MinProfit == cur_TrailStopMin)
            {gs_trade = fPrepareComment (StringConcatenate (ls_fName, ": Close by ProfitMin[", TrailStopMin, "], Profit = $", DS0 (dProfit), "."), gb_InfoPrint);}
            if (MinProfit > cur_TrailStopMin)
            {gs_trade = fPrepareComment (StringConcatenate (ls_fName, ": Close from TP, Profit = $", DS0 (dProfit), "."), gb_InfoPrint);}
            if (PrintCom) {Print (gs_trade);}
            MaxProfit = 0;
            MinProfit = 0;
            flag_TP = 0;
            flag_Stop = 0;
            DelObject ("Profit SL");
            GlobalVariableSet (Name_GV + "_#flagTP", flag_TP);
            GlobalVariableSet (Name_GV + "_#NULLStops", flag_Stop);
            return (true);
        }
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, ls_fName);
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Сервисных функций                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| Готовим к выводу на печать и на график комментарии                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fPrepareComment (string sText = "", bool bConditions = false)
{if (bConditions) if (StringLen (sText) > 0) {return (sText);}}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Выводим на печать и на график комментарии                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fPrintAndShowComment (string Text, bool Show_Conditions, bool Print_Conditions, string& s_Show[], int ind = -1)
{
    if ((Show_Conditions || Print_Conditions) && StringLen (Text) > 0)
    {
        if (ind >= 0 && Show_Conditions)
        {s_Show[ind] = Text;}
        if (Print_Conditions)
        {Print (Text);}
    }
//---- 
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Работа с графическими объектами                                           |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       Рисуем OBJ_TREND, OBJ_ARROW, OBJ_HLINE                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCreat_OBJ (string fs_Name, int fi_OBJ, int fi_ArrowCode, string fs_Text, int fi_FontSize,
                 datetime fdt_Time1, double fd_Price1, bool fb_Ray = true, color fc_Color = Gold,
                 datetime fdt_Time2 = 0, double fd_Price2 = 0)
{
    int err = GetLastError();
    bool lb_result = false;
//----
    if (ObjectFind (fs_Name) == -1)
    {lb_result = ObjectCreate (fs_Name, fi_OBJ, 0, 0, 0);}
    ObjectSet (fs_Name, OBJPROP_TIME1, fdt_Time1);
    ObjectSet (fs_Name, OBJPROP_PRICE1, fd_Price1);
    if (fdt_Time2 != 0 || fd_Price2 != 0)
    {
        ObjectSet (fs_Name, OBJPROP_TIME2, fdt_Time2);
        ObjectSet (fs_Name, OBJPROP_PRICE2, fd_Price2);
    }
    ObjectSet (fs_Name, OBJPROP_COLOR, fc_Color);
    if (fi_OBJ == OBJ_TREND)
    {ObjectSet (fs_Name, OBJPROP_RAY, fb_Ray);}
    if (fi_OBJ == OBJ_ARROW)
    {ObjectSet (fs_Name, OBJPROP_ARROWCODE, fi_ArrowCode);}
    if (StringLen (fs_Text) > 0)
    {ObjectSetText (fs_Name, fs_Text, fi_FontSize, "Calibri", fc_Color);}
    fGetLastError (gs_ComError, "fCreat_OBJ()");
//----
    return (lb_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//          УДАЛЕНИЕ ГРАФИЧЕСКОГО ОБЪЕКТА                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool DelObject (string fs_partTXT)
{
    int    err = GetLastError();
    string ls_Name;
    bool   lb_result = false;
//----
    if (ObjectFind (fs_partTXT) != -1)
    {
        if (ObjectDelete (fs_partTXT))
        {return (true);}
    }
    //---- Удаляем все объекты с заданным префиксом fs_partTXT
    for (int li_OBJ = ObjectsTotal() - 1; li_OBJ >= 0; li_OBJ--)
    {
        ls_Name = ObjectName (li_OBJ);
        if (StringFind (ls_Name, fs_partTXT, 0) > -1)
        {
            if (ObjectDelete (ls_Name))
            {lb_result = true;}
        }
    }
    fGetLastError (gs_ComError, StringConcatenate ("DelObject()[", fs_partTXT, "]"));
//----
    return (lb_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

