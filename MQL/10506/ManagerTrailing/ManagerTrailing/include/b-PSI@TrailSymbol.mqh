//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                            b-PSI@TrailSymbol.mqh  |
//|                                    Copyright © 2011-12, Igor Stepovoi aka TarasBY |
//|                                   Сделана на "базе" функций от I_D / Юрий Дзюбан  |
//|                                                 http://codebase.mql4.com/ru/1101  |
//|  12.04.2011  Библиотека функций трейлинга "в одном флаконе".                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|   Данный продукт предназначен для некомерческого использования. Публикация разре- |
//|шена только при указании имени автора (TarasBY). Редактирование исходного кода до- |
//|пустима только при условии сохранения данного текста, ссылок и имени автора.  Про- |
//|дажа библиотеки или отдельных её частей ЗАПРЕЩЕНА.                                 |
//|   Автор не несёт ответственности за возможные убытки, полученные в результате ис- |
//|пользования библиотеки.                                                            |
//|   По всем вопросам, связанным с работой библиотеки, замечаниями или предложениями |
//|по её доработке обращаться на Skype: TarasBY или e-mail.                           |
//+-----------------------------------------------------------------------------------+
//|   This product is intended for non-commercial use.  The publication is only allo- |
//|wed when you specify the name of the author (TarasBY). Edit the source code is va- |
//|lid only under condition of preservation of the text, links and author's name.     |
//|   Selling a module or(and) parts of it PROHIBITED.                                |
//|   The author is not liable for any damages resulting from the use of a module.    |
//|   For all matters relating to the work of the module, comments or suggestions for |
//|their improvement in the contact Skype: TarasBY or e-mail.                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
#property copyright "Copyright © 2008-12, TarasBY WM R418875277808; Z670270286972"
#property link      "taras_bulba@tut.by"
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                 *****        Параметры библиотеки         *****                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern string SETUP_Trailing        = "================== TRAILING ===================";
extern int    N_Trailing               = 0;          // Вариант используемого трейлинга
extern int    Trail_TF             = PERIOD_H1;  // Период (графика), на котором производим расчёты
extern bool   TrailLOSS_ON             = FALSE;      // Включение трейлинга на LOSS`e
extern int    TrailLossAfterBar        = 12;         // После какого по счёту бара (после открытия) начинаем тралить на ЛОССе
extern int    TrailStep                = 5;          // Шаг трейлинга (минимальное приращение)
extern int    BreakEven                = 30;         // Уровень, на котором срабатывает БезУбыток до размера в ProfitMIN
extern string Setup_TrailStairs     = "------------- N0 - TrailingStairs -------------";
extern int    TrailingStop             = 50;         // Скользящий тейк-профит, ноль чтобы отключить его
extern string Setup_TrailByFractals = "----------- N1 - TrailingByFractals -----------";
extern int    BarsInFractal            = 0;          // Количество баров в фрактале
extern int    Indent_Fr                = 0;          // Отступ (пп.) - расстояние от макс\мин свечи, на которое переносится SL (от 0)
extern string Setup_TrailByShadows  = "----------- N2 - TrailingByShadows ------------";
extern int    BarsToShadows            = 0;          // Количество баров, по теням которых необходимо трейлинговать (от 1 и больше)
extern int    Indent_Sh                = 0;          // Отступ (пп.) - расстояние от макс\мин свечи, на которое переносится SL (от 0)
extern string Setup_TrailUdavka     = "------------- N3 - TrailingUdavka -------------";
extern int    Level_0                  = 40;         // На этом уровне трейлинг включается
extern int    Distance_1               = 70;         // До этой дистаниции размер трейлинга = Level_0
extern int    Level_1                  = 30;         // Размер трейлинга м\у Distance_1 до Distance_2
extern int    Distance_2               = 100;        // От Distance_1 до Distance_2 размер трейлинга = Level_1
extern int    Level_2                  = 20;         // После дистанции Distance_2 размер трейлинга = Level_2
extern string Setup_TrailByTime     = "------------- N4 - TrailingByTime -------------";
extern int    Interval                 = 60;         // Интервал (минут), с которым передвигается SL
extern int    TimeStep                 = 5;          // Шаг трейлинга (на сколько пунктов) перемещается SL
extern string Setup_TrailByATR      = "------------- N5 - TrailingByATR --------------";
extern int    ATR_Period1              = 9;          // период первого ATR (больше 0; может быть равен ATR_Period2, но лучше отличен от последнего)
extern int    ATR_shift1               = 2;          // для первого ATR сдвиг "окна" (неотрицательное целое число)
extern int    ATR_Period2              = 14;         // период второго ATR (больше 0)
extern int    ATR_shift2               = 3;          // для второго ATR сдвиг "окна", (неотрицательное целое число)
extern double ATR_coeff                = 2.5;        // 
extern string Setup_TrailRatchetB   = "------------ N6 - TrailingRatchetB ------------";
extern int    ProfitLevel_1            = 20;
extern int    ProfitLevel_2            = 30;
extern int    ProfitLevel_3            = 50;
extern int    StopLevel_1              = 2;
extern int    StopLevel_2              = 5;
extern int    StopLevel_3              = 15;
extern string Setup_TrailByPriceCh  = "--------- N7 - TrailingByPriceChannel ---------";
extern int    BarsInChannel            = 10;        // период (кол-во баров) для рассчета верхней и нижней границ канала
extern int    Indent_Pr                = 15;        // отступ (пунктов), на котором размещается стоплосс от границы канала
extern string Setup_TrailByMA       = "-------------- N8 - TrailingByMA --------------";
extern int    MA_Period                = 14;        // 2-infinity, целые числа
extern int    MA_Shift                 = 0;         // целые положительные или отрицательные числа, а также 0
extern int    MA_Method                = 1;         // 0 (MODE_SMA), 1 (MODE_EMA), 2 (MODE_SMMA), 3 (MODE_LWMA)
extern int    MA_Price                 = 0;         // 0 (PRICE_CLOSE), 1 (PRICE_OPEN), 2 (PRICE_HIGH), 3 (PRICE_LOW), 4 (PRICE_MEDIAN), 5 (PRICE_TYPICAL), 6 (PRICE_WEIGHTED)
extern int    MA_Bar                   = 0;         // 0-Bars, целые числа
extern int    Indent_MA                = 10;        // 0-infinity, целые числа
extern string Setup_TrailFiftyFifty = "----------- N9 - TrailingFiftyFifty -----------";
extern double FF_coeff                 = 0.05;      // "коэффициент поджатия", в % от 0.01 до 1 (в последнем случае SL будет перенесен (если получится) вплотную к тек. курсу и позиция, скорее всего, сразу же закроется)
extern string Setup_TrailKillLoss   = "----------- N10 - TrailingKillLoss ------------";
extern double SpeedCoeff               = 0.5;       // "скорость" движения курса
extern string Setup_TrailPips       = "--------------- N11 - TrailPips ---------------";
extern int    Average_Period           = PERIOD_D1; // На каком периоде вычисляем среднюю свечу
//IIIIIIIIIIIIIIIIIII=========Подключение внешних модулей=======IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII========Глобальные переменные модуля========IIIIIIIIIIIIIIIIIIIII+
double        bd_IndentFr, bd_IndentSh, bd_IndentMA, bd_IndentPr, bda_ProfitLevel[3],
              bda_StopLevel[3], bd_TimeStep, bda_Distance[2];
bool          bb_TrailLOSS;
string        bs_ComTrail = "",
              bsa_NameTral[] = {"TrailingStairs()","TrailingByFractals()","TrailingByShadows()",
              "TrailingUdavka()","TrailingByTime()","TrailingByATR()","TrailingRatchetB()",
              "TrailingByPriceChannel()","TrailingByMA()","TrailingFiftyFifty()","KillLoss()","TrailPips()"};
int           bia_StopLevel[3], bia_ProfitLevel[3];
//IIIIIIIIIIIIIIIIIII===========Перечень функций модуля=========IIIIIIIIIIIIIIIIIIIIII+
//bool fInit_Trail()                 // Инициализация модуля
//bool fTrail_Position (int fi_Ticket,             // Ticket
                      //int fi_Slippage = 2)       // проскальзывание
                                     // Трейлингуем выбранную позицию
//bool fCheck_TrailParameters()      // Проверяем переданные в библиотеку внешние параметры
//bool TrailingByFractals (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг по фракталам
//bool TrailingByShadows (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг по теням N свечей
//bool TrailingStairs (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг стандартный
//bool fMove_ToBreakEven (int fi_Ticket, double& fd_NewSL)
                                     // Переводим ордер в БезУбыток (если задано)
//bool TrailingUdavka (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг "Удавка"
//bool TrailingByTime (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг по времени
//bool TrailingByATR (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг по ATR
//bool TrailingRatchetB (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг по Баришпольцу
//bool TrailingByPriceChannel (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг по ценовому каналу
//bool TrailingByMA (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг по скользящему среднему
//bool TrailingFiftyFifty (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг "Половинящий"
//bool KillLoss (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг KillLoss
//bool TrailPips (int fi_Ticket, double& fd_NewSL)
                                     // Трейлинг "Пипсовочный"
//double fGet_AverageCandle (string fs_Symbol,      // Символ
                           //int fi_Period,         // Период
                           //bool fb_IsCandle = false)// считаем свечу или нет
                                     // Просчитываем среднее значение размера свечи за означенный период
//void fCheck_LibDecimal()           // Учитываем разрядность котировок
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Инициализация модуля                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fInit_Trail()
{
//----
    //---- Приводим внешние переменные в соответствии с разрядностью котировок ДЦ
    fCheck_LibDecimal();
    //---- Производим проверку передаваемых в библиотеку значений
    if (!fCheck_TrailParameters())
    {
        Alert ("Проверьте параметры выбранного Вами Трейлинга !!!");
        bb_OptimContinue = true;
        return (false);
    }
    //---- Добавляем переменные в массив временных GV-переменных
    string lsa_Array[1]; 
    int    li_cnt = 0;
    if (BreakEven > 0)
    {
        bb_ClearGV = true;
        lsa_Array[li_cnt] = "_#BU";
        li_cnt++;
    }
    if (N_Trailing == 6 || N_Trailing == 10 || N_Trailing == 11)
    {
        bb_ClearGV = true;
        ArrayResize (lsa_Array, li_cnt + 1);
        if (N_Trailing == 6) {lsa_Array[li_cnt] = "_#LastLossLevel";}
        else if (N_Trailing == 10) {lsa_Array[li_cnt] = "_#Delta_SL";}
        else if (N_Trailing == 11) {lsa_Array[li_cnt] = "_#VirtTP";}
        
    }
    //---- Добавляем в рабочий массив префиксы временных GV-перемнных
    if (bb_ClearGV) fCreat_ArrayGV (bsa_prefGV, lsa_Array);
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Трейлингуем выбранную позицию                                              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fTrail_Position (int fi_Ticket,              // Тикет ордера
                     int fi_Slippage = 2)        // проскальзывание
{
    //---- Работаем ТОЛЬКО с символом графика
    if (OrderSymbol() != Symbol()) return (-1);
//----
    bool   lb_Trail = false;
    int    li_Type = OrderType();
//----
    if (TrailLOSS_ON && iBarShift (Symbol(), Trail_TF, OrderOpenTime()) > TrailLossAfterBar)
    {bb_TrailLOSS = true;}
    else {bb_TrailLOSS = false;}
    bd_curSL = OrderStopLoss();
    bd_curTP = OrderTakeProfit();
    bd_Spread = NDP (MarketInfo (Symbol(), MODE_SPREAD));
    //---- Определяем с какой ценой будем работать
    bd_Price = NDD (fGet_TradePrice (li_Type, bb_RealTrade));
    bs_libNAME = "b-TrailSymbol";
    bs_fName = bsa_NameTral[N_Trailing];
    //---- Обрабатываем выбранный номер трейлинга
    switch (N_Trailing)
    {
        //---- СТАНДАРТНЫЙ-СТУПЕНЧАСТЫЙ
        case 0: lb_Trail = TrailingStairs (fi_Ticket, bd_NewSL); break;
        //---- ТРЕЙЛИНГ ПО ФРАКТАЛАМ
        case 1: lb_Trail = TrailingByFractals (fi_Ticket, bd_NewSL); break;
        //---- ТРЕЙЛИНГ ПО ТЕНЯМ N СВЕЧЕЙ
        case 2: lb_Trail = TrailingByShadows (fi_Ticket, bd_NewSL); break;
        //---- ТРЕЙЛИНГ СТАНДАРТНЫЙ-"УДАВКА"
        case 3: lb_Trail = TrailingUdavka (fi_Ticket, bd_NewSL); break;
        //---- ТРЕЙЛИНГ ПО ВРЕМЕНИ
        case 4: lb_Trail = TrailingByTime (fi_Ticket, bd_NewSL); break;
        //---- ТРЕЙЛИНГ ПО ATR
        case 5: lb_Trail = TrailingByATR (fi_Ticket, bd_NewSL); break;
        //---- ТРЕЙЛИНГ RATCHET БАРИШПОЛЬЦА
        case 6: lb_Trail = TrailingRatchetB (fi_Ticket, bd_NewSL); break;
        //---- ТРЕЙЛИНГ ПО ЦЕНВОМУ КАНАЛУ
        case 7: lb_Trail = TrailingByPriceChannel (fi_Ticket, bd_NewSL); break;
        //---- ТРЕЙЛИНГ ПО СКОЛЬЗЯЩЕМУ СРЕДНЕМУ
        case 8: lb_Trail = TrailingByMA (fi_Ticket, bd_NewSL); break;
        //---- ТРЕЙЛИНГ "ПОЛОВИНЯЩИЙ"
        case 9: lb_Trail = TrailingFiftyFifty (fi_Ticket, bd_NewSL); break;
        //---- ТРЕЙЛИНГ KillLoss
        case 10: lb_Trail = KillLoss (fi_Ticket, bd_NewSL); break;
        //---- ТРЕЙЛИНГ "ПИПСОВОЧНЫЙ"
        case 11: lb_Trail = TrailPips (fi_Ticket, bd_NewSL); break;
    }
    bool lb_BreakEven = fMove_ToBreakEven (fi_Ticket, bd_NewSL);
    //---- Модифицируем стоплосс
    if (lb_Trail || lb_BreakEven)
    {
        //---- SL тянем в сторону уменьшения убытка
        if (fCheck_MinProfit (bd_ProfitMIN, bd_NewSL, !bb_TrailLOSS))
        {
            int li_cmd = 1;
            if (li_Type == OP_SELL) li_cmd = -1;
            if (li_cmd * (bd_NewSL - bd_curSL) > bd_TrailStep || bd_curSL == 0
            //---- Перевод SL в БезУбыток
            || lb_BreakEven)
            {
                //---- Вычисляем расстояние от SL до текущей цены
                bd_Trail = MathAbs (bd_Price - bd_NewSL);
                int li_result = fOrderModify (fi_Ticket, OrderOpenPrice(), NDD (bd_NewSL), OrderTakeProfit());
                if (li_result >= 0) fSet_Comment (bdt_curTime, fi_Ticket, 20, "", li_result != 0, 0);
                //---- Помечаем факт срабатыванию БУ
                if (lb_BreakEven) {if (li_result == 1) GlobalVariableSet (StringConcatenate (fi_Ticket, "_#BU"), bd_NewSL);}
                if (li_result == 1) return (1);
            }
        }
    }
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Проверяем переданные в библиотеку внешние параметры                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_TrailParameters()
{
//----
    if (N_Trailing < 0 || N_Trailing > 11)
    {Print ("НЕТ таких трейлингов. N_Trailing >= 0 и N_Trailing <= 11 !!!"); return (false);}
    //---- По умолчанию тралить ЛОСС начинаем спустя сутки
    if (TrailLossAfterBar == 0) {TrailLossAfterBar = PERIOD_D1 / Period();}
    //---- Производим проверки корректности заданных пользователем параметров
    if (TrailLOSS_ON && TrailLossAfterBar < 0)
    {Print ("Поставьте TrailLossAfterBar >= 0 !!!"); return (false);}
    if (BreakEven > 0 && BreakEven <= ProfitMIN_Pips)
    {Print ("Поставьте BreakEven > ProfitMIN_Pips или BreakEven = 0 !!!"); return (false);}
    if (TrailStep < 1 && (N_Trailing == 0 || N_Trailing == 10 || N_Trailing == 11))
    {Print ("Поставьте TrailStep >= 1 !!!"); return (false);}
    //---- Проверяем вводимые параметры выбранного трейлинга
    switch (N_Trailing)
    {
        case 0:
            if (TrailingStop < TrailStep || TrailingStop < BreakEven + ProfitMIN_Pips)
            {Print ("Трейлинг функцией TrailingStairs() невозможен из-за некорректности значений переданных ей аргументов."); return (false);}
            break;
        case 1:
            if (BarsInFractal <= 3 || Indent_Fr < 0)
            {Print ("Трейлинг функцией TrailingByFractals() невозможен из-за некорректности значений переданных ей аргументов."); return (false);}
            break;
        case 2:
            if (BarsToShadows < 1 || Indent_Sh < 0)
            {Print ("Трейлинг функцией TrailingByShadows() невозможен из-за некорректности значений переданных ей аргументов."); return (false);}
            break;
        case 3:
            if (Level_0 >= Distance_1 || Level_1 >= Level_0 || Level_2 >= Level_1 || Distance_1 >= Distance_2)
            {Print ("Трейлинг функцией TrailingUdavka() невозможен из-за некорректности значений переданных ей аргументов."); return (false);}
            //---- БУ не имеет смысла выше Level_0
            if (Level_0 <= BreakEven)
            {Print ("TrailingUdavka(): Поставьте BreakEven < Level_0 !!!"); return (false);}
            break;
        case 4:
            if (Interval < 1 || TimeStep < 1)
            {Print ("Трейлинг функцией TrailingByTime() невозможен из-за некорректности значений переданных ей аргументов."); return (false);}
            break;
        case 5:
            if (ATR_Period1 < 1 || ATR_Period2 < 1 || ATR_coeff <= 0)
            {Print ("Трейлинг функцией TrailingByATR() невозможен из-за некорректности значений переданных ей аргументов."); return (false);}
            break;
        case 6:
            if (ProfitLevel_2 <= ProfitLevel_1 || ProfitLevel_3 <= ProfitLevel_2 || ProfitLevel_3 <= ProfitLevel_1)
            {Print ("Трейлинг функцией TrailingRatchetB() невозможен из-за некорректности значений переданных ей аргументов."); return (false);}
            //---- БУ не имеет смысла выше ProfitLevel_1
            if (ProfitLevel_1 <= BreakEven)
            {Print ("TrailingRatchetB(): Поставьте BreakEven < ProfitLevel_1 !!!"); return (false);}
            break;
        case 7:
            if (BarsInChannel < 1 || Indent_Pr < 0)
            {Print ("Трейлинг функцией TrailingByPriceChannel() невозможен из-за некорректности значений переданных ей аргументов."); return (false);}
            break;
        case 8:
            if (MA_Period < 2 || MA_Method < 0 || MA_Method > 3 || MA_Price < 0 || MA_Price > 6 || MA_Bar < 0 || Indent_MA < 0)
            {Print ("Трейлинг функцией TrailingByMA() невозможен из-за некорректности значений переданных ей аргументов."); return (false);}
            break;
        case 9:
            if (FF_coeff < 0.01 || FF_coeff > 1.0)
            {Print ("Трейлинг функцией TrailingFiftyFifty() невозможен из-за некорректности значений переданных ей аргументов."); return (false);}
            break;
        case 10:
            if (SpeedCoeff < 0.1)
            {Print ("Трейлинг функцией KillLoss() невозможен из-за некорректности значений переданных ей аргументов."); return (0);}
            break;
        case 11:
            if (Average_Period <= Period())
            {Print ("Трейлинг функцией TrailPips() невозможен из-за некорректности значений переданных ей аргументов."); return (false);}
            break;
    }
    //---- Производим проверку на правильность задания TF
    if (Trail_TF == 0) {Trail_TF = Period();}
    if (fGet_NumPeriods (Trail_TF) < 0) {return (false);}
//----
     return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ТРЕЙЛИНГ ПО ФРАКТАЛАМ                                                             |
//| Функции передаётся тикет позиции, количество баров в фрактале, и отступ (пунктов) |
//| - расстояние от макс. (мин.) свечи, на которое переносится стоплосс (от 0),       |
//|  trlinloss - тралить ли в зоне убытков                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByFractals (int fi_Ticket, double& fd_NewSL)
{
    int    i, z,             // counters
           li_ExtremN,       // номер ближайшего экстремума frktl_bars-барного фрактала 
           after_x, be4_x,   // свечей после и до пика соответственно
           ok_be4, ok_after, // флаги соответствия условию (1 - неправильно, 0 - правильно)
           lia_PeakN[2];     // номера экстремумов ближайших фракталов на продажу/покупку соответственно   
    double ld_tmp;           // служебная переменная
//----
    ld_tmp = BarsInFractal;
    if (MathMod (BarsInFractal, 2) == 0) {li_ExtremN = ld_tmp / 2.0;}
    else {li_ExtremN = MathRound (ld_tmp / 2.0);}
    //---- Баров до и после экстремума фрактала
    after_x = BarsInFractal - li_ExtremN;
    if (MathMod (BarsInFractal, 2) != 0) {be4_x = BarsInFractal - li_ExtremN;}
    else {be4_x = BarsInFractal - li_ExtremN - 1;}
    //---- Если OP_BUY, находим ближайший фрактал на продажу (т.е. экстремум "вниз")
    if (OrderType() == OP_BUY)
    {
        //---- Находим последний фрактал на продажу
        for (i = li_ExtremN; i < iBars (Symbol(), Trail_TF); i++)
        {
            ok_be4 = 0;
            ok_after = 0;
            for (z = 1; z <= be4_x; z++)
            {
                if (iLow (Symbol(), Trail_TF, i) >= iLow (Symbol(), Trail_TF, i - z)) 
                {ok_be4 = 1; break;}
            }
            for (z = 1; z <= after_x; z++)
            {
                if (iLow (Symbol(), Trail_TF, i) > iLow (Symbol(), Trail_TF, i + z)) 
                {ok_after = 1; break;}
            }            
            if (ok_be4 == 0 && ok_after == 0)                
            {lia_PeakN[1] = i; break;}
        }
        //---- Проверяем условие на трал покупки
        double ld_Peak = iLow (Symbol(), Trail_TF, lia_PeakN[1]) - bd_IndentFr;
        //---- Если новый стоплосс лучше имеющегося (в т.ч. если стоплосс == 0, не выставлен)
        if (ld_Peak > OrderStopLoss() && (bb_TrailLOSS || (!TrailLOSS_ON && ld_Peak > OrderOpenPrice())))
        {fd_NewSL = ld_Peak; return (true);}
    }
    //---- Если OP_SELL, находим ближайший фрактал на покупку (т.е. экстремум "вверх")
    if (OrderType() == OP_SELL)
    {
        //---- Находим последний фрактал на покупку
        for (i = li_ExtremN; i < iBars (Symbol(), Trail_TF); i++)
        {
            ok_be4 = 0;
            ok_after = 0;
            for (z = 1; z <= be4_x; z++)
            {
                if (iHigh (Symbol(), Trail_TF, i) <= iHigh (Symbol(), Trail_TF, i - z)) 
                {ok_be4 = 1; break;}
            }
            for (z = 1; z <= after_x; z++)
            {
                if (iHigh (Symbol(), Trail_TF, i) < iHigh (Symbol(), Trail_TF, i + z)) 
                {ok_after = 1; break;}
            }            
            if (ok_be4 == 0 && ok_after == 0)                
            {lia_PeakN[0] = i; break;}
        }        
        ld_Peak = iHigh (Symbol(), Trail_TF, lia_PeakN[0]) + bd_IndentFr + bd_Spread;
        //---- Если новый стоплосс лучше имеющегося (в т.ч. если стоплосс == 0, не выставлен)
        if ((ld_Peak < OrderStopLoss() || OrderStopLoss() == 0) && (bb_TrailLOSS || (!TrailLOSS_ON && ld_Peak < OrderOpenPrice())))
        {fd_NewSL = ld_Peak; return (true);}
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ТРЕЙЛИНГ ПО ТЕНЯМ N СВЕЧЕЙ                                                        |
//| Функции использует количество баров, по теням которых необходимо трейлинговать    |
//| (от 1 и больше) и отступ (пунктов) - расстояние от макс. (мин.) свечи, на         |
//| которое переносится стоплосс (от 0), TrailLOSS_ON - тралить ли в лоссе            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByShadows (int fi_Ticket, double& fd_NewSL)
{
    double ld_Extremum;
//----
    //---- Если длинная позиция (OP_BUY), находим минимум BarsToShadows свечей
    if (OrderType() == OP_BUY)
    {
        ld_Extremum = iLow (Symbol(), Trail_TF, iLowest (Symbol(), Trail_TF, MODE_LOW, BarsToShadows, 1)) - bd_IndentSh;
        if ((ld_Extremum > OrderStopLoss())
        && (bb_TrailLOSS || (!TrailLOSS_ON && ld_Extremum > OrderOpenPrice())))
        {fd_NewSL = ld_Extremum; return (true);}
    }
    //---- Если OP_SELL, находим максимум BarsToShadows свечей
    if (OrderType() == OP_SELL)
    {
        ld_Extremum = iHigh (Symbol(), Trail_TF, iHighest (Symbol(), Trail_TF, MODE_HIGH, BarsToShadows, 1)) + bd_IndentSh + bd_Spread;
        //---- Если новый стоплосс лучше имеющегося (в т.ч. если стоплосс == 0, не выставлен)
        if ((ld_Extremum < OrderStopLoss() || OrderStopLoss() == 0)
        && (bb_TrailLOSS || (!TrailLOSS_ON && ld_Extremum < OrderOpenPrice())))
        {fd_NewSL = ld_Extremum; return (true);}
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ТРЕЙЛИНГ СТАНДАРТНЫЙ-СТУПЕНЧАСТЫЙ                                                 |
//| Функции передаётся тикет позиции, расстояние от курса открытия, на котором        |
//| трейлинг запускается (пунктов) и "шаг", с которым он переносится (пунктов)        |
//| Пример: при +30 стоп на +10, при +40 - стоп на +20 и т.д.                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingStairs (int fi_Ticket, double& fd_NewSL)
{
    int li_cmd = 1;
//----
    fd_NewSL = 0.0;
    if (OrderType() == 1) li_cmd = -1;
    if (TrailingStop > 0)
    {
        if (li_cmd * (bd_Price - OrderOpenPrice()) > bd_TrailingStop || bb_TrailLOSS)
        {
            if ((li_cmd * (bd_Price - bd_curSL) > bd_TrailingStop + bd_TrailStep) || bd_curSL == 0.0)
            {fd_NewSL = bd_Price - li_cmd * bd_TrailingStop; return (true);}
        }
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Переводим ордер в БезУбыток (если задано)                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fMove_ToBreakEven (int fi_Ticket,       // Ticket
                        double& fd_NewSL)    // возвращаемый новый SL
{
    string ls_Name = StringConcatenate (fi_Ticket, "_#BU");
//----
    if (GlobalVariableCheck (ls_Name)) return (false);
    //---- Прописываем работу БУ
    if (BreakEven > 0)
    {
        int li_cmd = 1, li_Type = OrderType();
        if (li_Type == 1) li_cmd = -1;
        if (li_cmd * (bd_Price - OrderOpenPrice()) > bd_BreakEven)
        {
            double ld_Profit = OrderProfit() + OrderSwap() + OrderCommission();
            //---- Модификация стопов в БУ выполняется один раз
            if (ld_Profit > 0.0)
            {
                fd_NewSL = OrderOpenPrice() + li_cmd * bd_ProfitMIN;
                //GlobalVariableSet (ls_Name, fd_NewSL);
                fSet_Comment (fi_Ticket, fi_Ticket, 23, "fMove_ToBreakEven()", True, fd_NewSL);
                return (true);
            }
        }    
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, "fMove_ToBreakEven()", bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ТРЕЙЛИНГ СТАНДАРТНЫЙ-"УДАВКА"                                                     |
//| Пример: исходный трейлинг 30 п., при +50 - 20 п., +80 >= - на расстоянии в 10 п.  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingUdavka (int fi_Ticket, double& fd_NewSL)
{
    double ld_Price, ld_MovePrice, ld_TrailStop = 0.0;
//----
    //---- Если длинная позиция (OP_BUY)
    if (OrderType() == OP_BUY)
    {
        ld_MovePrice = bd_Price - OrderOpenPrice();
        if (ld_MovePrice <= 0) {return (false);}
        //---- Определяем расстояние от СТОПа до цены (плавающий размер трейлинга)
        if (ld_MovePrice <= bda_Distance[0]) {ld_TrailStop = Level_0;}
        if (ld_MovePrice > bda_Distance[0] && ld_MovePrice <= bda_Distance[1]) {ld_TrailStop = Level_1;}
        if (ld_MovePrice > bda_Distance[1]) {ld_TrailStop = Level_2;}
        ld_TrailStop = NDP (ld_TrailStop);
        //---- Если стоплосс = 0 или меньше курса открытия, то если тек.цена (Bid) больше/равна дистанции курс_открытия + расст.трейлинга
        if (OrderStopLoss() < OrderOpenPrice())
        {
            if (ld_MovePrice > ld_TrailStop + bd_TrailStep)
            {fd_NewSL = bd_Price - ld_TrailStop; return (true);}
        }
        //---- Иначе: если текущая цена (Bid) больше/равна дистанции текущий_стоплосс + расстояние трейлинга, 
        else
        {
            if (bd_Price - OrderStopLoss() > ld_TrailStop + bd_TrailStep)
            {fd_NewSL = bd_Price - ld_TrailStop; return (true);}
        }
    }
    //---- Если короткая позиция (OP_SELL)
    if (OrderType() == OP_SELL)
    { 
        ld_MovePrice = OrderOpenPrice() - (bd_Price + bd_Spread);
        if (ld_MovePrice <= 0) {return (false);}
        //---- Определяем расстояние от СТОПа до цены (плавающий размер трейлинга)
        if (ld_MovePrice <= bda_Distance[0]) {ld_TrailStop = Level_0;}
        if (ld_MovePrice > bda_Distance[0] && ld_MovePrice <= bda_Distance[1]) {ld_TrailStop = Level_1;}
        if (ld_MovePrice > bda_Distance[1]) {ld_TrailStop = Level_2;}
        ld_TrailStop = NDP (ld_TrailStop);
        // если стоплосс = 0 или меньше курса открытия, то если тек.цена (Ask) больше/равна дистанции курс_открытия+расст.трейлинга
        if (OrderStopLoss() == 0 || OrderStopLoss() > OrderOpenPrice())
        {
            if (ld_MovePrice > ld_TrailStop + bd_TrailStep)
            {fd_NewSL = bd_Price + ld_TrailStop; return (true);}
        }
        //---- Иначе: если текущая цена (Bid) больше/равна дистанции текущий_стоплосс + расстояние трейлинга, 
        else
        {
            if (OrderStopLoss() - (bd_Price + bd_Spread) > ld_TrailStop + bd_TrailStep)
            {fd_NewSL = bd_Price + ld_TrailStop; return (true);}
        }
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ТРЕЙЛИНГ ПО ВРЕМЕНИ                                                               |
//| Функции передаётся тикет позиции, интервал (минут), с которым, передвигается      |
//| стоплосс и шаг трейлинга (на сколько пунктов перемещается стоплосс, TrailLOSS_ON  |
//| - тралим ли в убытке (т.е. с определённым интервалом подтягиваем стоп до курса    |
//| открытия, а потом и в профите, либо только в профите)                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByTime (int fi_Ticket, double& fd_NewSL)
{
    int    li_MinPast;    // кол-во полных минут от открытия позиции до текущего момента 
    double times2change;  // кол-во интервалов Interval с момента открытия позиции (т.е. сколько раз должен был быть перемещен стоплосс) 
//----
    //---- Определяем, сколько времени прошло с момента открытия позиции
    li_MinPast = (TimeCurrent() - OrderOpenTime()) / 60;
    //---- Сколько раз нужно было передвинуть стоплосс
    times2change = MathFloor (li_MinPast / Interval);
    //---- Если длинная позиция (OP_BUY)
    if (OrderType() == OP_BUY)
    {
        //---- Если тралим в убытке, то отступаем от стоплосса (если он не 0, если 0 - от открытия)
        if (bb_TrailLOSS)
        {
            if (OrderStopLoss() == 0) {fd_NewSL = OrderOpenPrice() + times2change * bd_TimeStep;}
            else {fd_NewSL = OrderStopLoss() + times2change * bd_TimeStep;}
        }
        //---- Иначе - от курса открытия позиции
        else {fd_NewSL = OrderOpenPrice() + times2change * bd_TimeStep;}
    }
    //---- Если короткая позиция (OP_SELL)
    if (OrderType() == OP_SELL)
    {
        //---- Если тралим в убытке, то отступаем от стоплосса (если он не 0, если 0 - от открытия)
        if (bb_TrailLOSS)
        {
            if (OrderStopLoss() == 0) {fd_NewSL = OrderOpenPrice() - times2change * bd_TimeStep - bd_Spread;}
            else {fd_NewSL = OrderStopLoss() - times2change * bd_TimeStep - bd_Spread;}
        }
        else {fd_NewSL = OrderOpenPrice() - times2change * bd_TimeStep - bd_Spread;}
    }
    if (times2change > 0) {return (true);}
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ТРЕЙЛИНГ ПО ATR (Average True Range, Средний истинный диапазон)                   |
//| Функции передаётся тикет позиции, период АТR и коэффициент, на который умножается |
//| ATR. Т.о. стоплосс "тянется" на расстоянии ATR х N от текущего курса;             |
//| перенос - на новом баре (т.е. от цены открытия очередного бара)                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByATR (int fi_Ticket, double& fd_NewSL)
{
    double ld_ATR,
           ld_Coeff; // результат умножения большего из ATR на коэффициент
//----
    //---- Текущее значение ATR
    ld_ATR = iATR (Symbol(), Trail_TF, ATR_Period1, ATR_shift1);
    ld_ATR = MathMax (ld_ATR, iATR (Symbol(), Trail_TF, ATR_Period2, ATR_shift2));
    //---- После умножения на коэффициент
    ld_Coeff = ld_ATR * ATR_coeff;
    //---- Если длинная позиция (OP_BUY)
    if (OrderType() == OP_BUY)
    {
        //---- Откладываем от текущего курса (новый стоплосс)
        fd_NewSL = bd_Price - ld_Coeff;
        //---- Если TrailLOSS_ON == true (т.е. следует тралить в зоне лоссов), то
        if ((bb_TrailLOSS && fd_NewSL > OrderStopLoss())
        //---- Иначе тралим от курса открытия
        || (!TrailLOSS_ON && fd_NewSL > OrderOpenPrice()))
        {return (true);}
    }
    //---- Если короткая позиция (OP_SELL)
    if (OrderType() == OP_SELL)
    {
        //---- Откладываем от текущего курса (новый стоплосс)
        fd_NewSL = bd_Price + (ld_Coeff + bd_Spread);
        //---- Если TrailLOSS_ON == true (т.е. следует тралить в зоне лоссов), то
        if ((bb_TrailLOSS && (fd_NewSL < OrderStopLoss() || OrderStopLoss() == 0))
        //---- Иначе тралим от курса открытия
        || (!TrailLOSS_ON && fd_NewSL < OrderOpenPrice()))
        {return (true);}
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ТРЕЙЛИНГ RATCHET БАРИШПОЛЬЦА                                                      |
//| При достижении профитом уровня 1 стоплосс - в bd_ProfitMIN, при достижении профи- |
//| том уровня 2 профита - стоплосс - на уровень 1, когда профит достигает уровня 3   |
//| профита, стоплосс - на уровень 2 (дальше можно трейлить другими методами)         |
//| при работе в лоссовом участке - тоже 3 уровня, но схема работы с ними несколько   |
//| иная,  а именно: если мы опустились ниже уровня, а потом поднялись выше него      |
//| (пример для покупки), то стоплосс ставим на следующий, более глубокий уровень     |
//| (например, уровни -5, -10 и -25, стоплосс -40; если опустились ниже -10, а потом  |
//| поднялись выше -10, то стоплосс - на -25, если поднимемся выще -5, то стоплосс    |
//| перенесем на -10, при -2 (спрэд) стоп на -5                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingRatchetB (int fi_Ticket, double& fd_NewSL)
{
    bool lb_result = false;
//----
    //---- Если длинная позиция (OP_BUY)
    if (OrderType() == OP_BUY)
    {
        double ld_ProfitLevel;
        //---- Работаем на участке профитов
        for (int li_IND = 2; li_IND >= 0; li_IND--)
        {
            //---- Если разница "текущий_курс-курс_открытия" > "ProfitLevel_N+спрэд", SL переносим в "StopLevel_N+спрэд"
            if (bd_Price - OrderOpenPrice() >= bda_ProfitLevel[li_IND])
            {
                if (OrderStopLoss() == 0 || OrderStopLoss() < OrderOpenPrice() + bda_StopLevel[li_IND])
                {fd_NewSL = OrderOpenPrice() + bda_StopLevel[li_IND]; return (true);}
            }
        }
        //---- Работаем на участке лоссов
        if (bb_TrailLOSS)      
        {
            //---- Подчищаем за собой отработанные глобальные переменные
            double ld_LastLossLevel;
            string ls_Name = StringConcatenate (fi_Ticket, "_#LastLossLevel");
            //---- Глобальная переменная терминала содержит значение самого уровня убытка (StopLevel_n), ниже которого опускался курс
            // (если он после этого поднимается выше, устанавливаем SL на ближайшем более глубоком уровне убытка (если это не начальный SL позиции)
            if (!GlobalVariableCheck (ls_Name)) {GlobalVariableSet (ls_Name, 0);}
            else {ld_LastLossLevel = GlobalVariableGet (ls_Name);}
            //---- Убыточным считаем участок ниже курса открытия и до первого уровня профита
            if (bd_Price - OrderOpenPrice() < bda_ProfitLevel[0])
            {
                //---- Если (текущий_курс лучше/равно открытие) и (dpstlslvl>=StopLevel_1), стоплосс - на StopLevel_1
                if (bd_Price >= OrderOpenPrice())
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() < OrderOpenPrice() - bda_StopLevel[0])
                    {fd_NewSL = OrderOpenPrice() - bda_StopLevel[0]; lb_result = true;}
                }
                //---- Если (текущий_курс лучше уровня_убытка_1) и (dpstlslvl>=StopLevel_1), стоплосс - на StopLevel_2
                if (bd_Price >= OrderOpenPrice() - bda_StopLevel[0] && ld_LastLossLevel >= StopLevel_1)
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() < OrderOpenPrice() - bda_StopLevel[1])
                    {fd_NewSL = OrderOpenPrice() - bda_StopLevel[1]; lb_result = true;}
                }
                //---- Если (текущий_курс лучше уровня_убытка_2) и (dpstlslvl>=StopLevel_2), стоплосс - на StopLevel_3
                if (bd_Price >= OrderOpenPrice() - bda_StopLevel[1] && ld_LastLossLevel >= StopLevel_2)
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() < OrderOpenPrice() - bda_StopLevel[2])
                    {fd_NewSL = OrderOpenPrice() - bda_StopLevel[2]; lb_result = true;}
                }
                //---- Проверим/обновим значение наиболее глубокой "взятой" лоссовой "ступеньки"
                //---- Если "текущий_курс-курс открытия + спрэд" меньше 0, 
                if (bd_Price - OrderOpenPrice() + bd_Spread < 0)
                //---- Проверим, не меньше ли он того или иного уровня убытка
                {
                    for (li_IND = 2; li_IND >= 0; li_IND--)
                    {
                        if (bd_Price <= OrderOpenPrice() - bda_StopLevel[li_IND])
                        {
                            if (ld_LastLossLevel < bia_StopLevel[li_IND])
                            {GlobalVariableSet (ls_Name, bia_StopLevel[li_IND]); return (lb_result);}
                        }
                    }
                }
            }
        }
    }
    //---- Если короткая позиция (OP_SELL)
    if (OrderType() == OP_SELL)
    {
        //---- Работаем на участке профитов
        for (li_IND = 2; li_IND >= 0; li_IND--)
        {
            //---- Если разница "текущий_курс-курс_открытия" > "ProfitLevel_N+спрэд", SL переносим в "StopLevel_N+спрэд"
            if (OrderOpenPrice() - bd_Price >= bda_ProfitLevel[li_IND])
            {
                if (OrderStopLoss() == 0 || OrderStopLoss() > OrderOpenPrice() - bda_StopLevel[li_IND])
                {fd_NewSL = OrderOpenPrice() - bda_StopLevel[li_IND]; return (true);}
            }
        }
        //---- Работаем на участке лоссов
        if (bb_TrailLOSS)      
        {
            //---- Подчищаем за собой отработанные глобальные переменные
            ls_Name = StringConcatenate (fi_Ticket, "_#LastLossLevel");
            //---- Глобальная переменная терминала содержит значение самого уровня убытка (StopLevel_n), ниже которого опускался курс
            // (если он после этого поднимается выше, устанавливаем SL на ближайшем более глубоком уровне убытка (если это не начальный SL позиции)
            if (!GlobalVariableCheck (ls_Name)) {GlobalVariableSet (ls_Name, 0);}
            else {ld_LastLossLevel = GlobalVariableGet (ls_Name);}
            //---- Убыточным считаем участок ниже курса открытия и до первого уровня профита
            if (OrderOpenPrice() - bd_Price < bda_ProfitLevel[0])         
            {
                //---- Если (текущий_курс лучше/равно открытие) и (dpstlslvl>=StopLevel_1), SL - на StopLevel_1
                if (bd_Price <= OrderOpenPrice())
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() > OrderOpenPrice() + (bda_StopLevel[0] + bd_Spread))
                    {fd_NewSL = OrderOpenPrice() + (bda_StopLevel[0] + bd_Spread); lb_result = true;}
                }
                //---- Если (текущий_курс лучше уровня_убытка_1) и (dpstlslvl>=StopLevel_1), SL - на StopLevel_2
                if (bd_Price <= OrderOpenPrice() + (bda_StopLevel[0] + bd_Spread) && ld_LastLossLevel >= StopLevel_1)
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() > OrderOpenPrice() + (bda_StopLevel[1] + bd_Spread))
                    {fd_NewSL = OrderOpenPrice() + (bda_StopLevel[1] + bd_Spread); lb_result = true;}
                }
                //---- Если (текущий_курс лучше уровня_убытка_2) и (dpstlslvl>=StopLevel_2), SL - на StopLevel_3
                if (bd_Price <= OrderOpenPrice() + (bda_StopLevel[1] + bd_Spread) && ld_LastLossLevel >= StopLevel_2)
                {
                    if (OrderStopLoss() == 0 || OrderStopLoss() > OrderOpenPrice() + (bda_StopLevel[2] + bd_Spread))
                    {fd_NewSL = OrderOpenPrice() + (bda_StopLevel[2] + bd_Spread); lb_result = true;}
                }
                //---- Проверим/обновим значение наиболее глубокой "взятой" лоссовой "ступеньки"
                //---- Если "текущий_курс-курс открытия+спрэд" меньше 0, 
                if (OrderOpenPrice() - bd_Price + bd_Spread < 0)
                //---- Проверим, не меньше ли он того или иного уровня убытка
                {
                    for (li_IND = 2; li_IND >= 0; li_IND--)
                    {
                        if (bd_Price >= OrderOpenPrice() + (bda_StopLevel[li_IND] + bd_Spread))
                        {
                            if (ld_LastLossLevel < bia_StopLevel[li_IND])
                            {GlobalVariableSet (ls_Name, bia_StopLevel[li_IND]); return (lb_result);}
                        }
                    }
                }
            }
        }
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (lb_result);    
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ТРЕЙЛИНГ ПО ЦЕНВОМУ КАНАЛУ                                                        |
//| Добавлен по совету Nickolay Zhilin (aka rebus) Трейлинг по закрывшимся барам.     |
//| Функции передаётся тикет позиции, период (кол-во баров) для рассчета верхней и    | 
//| нижней границ канала, отступ (пунктов), на котором размещается стоплосс от        |
//| границы канала                                                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByPriceChannel (int fi_Ticket, double& fd_NewSL)
{     
    double ld_ChnlMax, // верхняя граница канала
           ld_ChnlMin; // нижняя граница канала
//----
    //---- Определяем макс.хай и мин.лоу за BarsInChannel баров начиная с [1] (= верхняя и нижняя границы ценового канала)
    ld_ChnlMax = iHigh (Symbol(), Trail_TF, iHighest (Symbol(), Trail_TF, MODE_HIGH, BarsInChannel, 1)) + bd_IndentPr + bd_Spread;
    ld_ChnlMin = iLow (Symbol(), Trail_TF, iLowest (Symbol(), Trail_TF, MODE_LOW, BarsInChannel, 1)) - bd_IndentPr;   
   
    //---- Если длинная позиция, и её стоплосс хуже (ниже нижней границы канала либо не определен, == 0), модифицируем его
    if (OrderType() == OP_BUY)
    {
        if (OrderStopLoss() < ld_ChnlMin)
        {fd_NewSL = ld_ChnlMin; return (true);}
    }
    //---- Если позиция - короткая, и её стоплосс хуже (выше верхней границы канала или не определён, == 0), модифицируем его
    if (OrderType() == OP_SELL)
    {
        if (OrderStopLoss() == 0 || OrderStopLoss() > ld_ChnlMax)
        {fd_NewSL = ld_ChnlMax; return (true);}
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ТРЕЙЛИНГ ПО СКОЛЬЗЯЩЕМУ СРЕДНЕМУ                                                  |
//| Функции передаётся тикет позиции и параметры средней (таймфрейм, период, тип,     | 
//| сдвиг относительно графика, метод сглаживания, составляющая OHCL для построения,  |
//| № бара, на котором берется значение средней.                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingByMA (int fi_Ticket, double& fd_NewSL)
{     
    double ld_MA; // значение скользящего среднего с переданными параметрами
    //---- Определим значение МА с переданными функции параметрами
    ld_MA = iMA (Symbol(), Trail_TF, MA_Period, MA_Shift, MA_Method, MA_Price, MA_Bar);
    //---- Если длинная позиция, и её стоплосс хуже значения среднего с отступом в Indent_MA пунктов, модифицируем его
    if (OrderType() == OP_BUY)
    {
        ld_MA -= bd_IndentMA;
        if (OrderStopLoss() < ld_MA) {fd_NewSL = ld_MA; return (true);}
    }
    //---- Если позиция - короткая, и её стоплосс хуже (выше верхней границы канала или не определён, ==0), модифицируем его
    if (OrderType() == OP_SELL)
    {
        ld_MA += (bd_IndentMA + bd_Spread);
        if (OrderStopLoss() == 0 || OrderStopLoss() > ld_MA) {fd_NewSL = ld_MA; return (true);}
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ТРЕЙЛИНГ "ПОЛОВИНЯЩИЙ"                                                            |
//| По закрытии очередного периода (бара) подтягиваем стоплосс на половину (но можно  |
//| и любой иной коэффициент) дистанции, пройденной курсом (т.е., например, по        |
//| закрытии суток профит +55 п. - стоплосс переносим в 55/2=27 п. Если по закрытии   |
//| след. суток профит достиг, допустим, +80 п., то стоплосс переносим на половину    |
//| (напр.) расстояния между тек. стоплоссом и курсом на закрытии бара -              |
//| 27 + (80-27)/2 = 27 + 53/2 = 27 + 26 = 53 п.                                      |
//| TrailLOSS_ON - стоит ли тралить на лоссовом участке - если да, то по закрытию     |
//| очередного бара расстояние между стоплоссом (в т.ч. "до" безубытка) и текущим     |
//| курсом будет сокращаться в dCoeff раз чтобы посл. вариант работал, обязательно    |
//| должен быть определён стоплосс (не равен 0)                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailingFiftyFifty (int fi_Ticket, double& fd_NewSL)
{ 
    static datetime ldt_NewBar = 0;
//----
    //---- Активируем трейлинг только по закрытии бара
    if (ldt_NewBar == iTime (Symbol(), Trail_TF, 0)) return (0);
    ldt_NewBar = iTime (Symbol(), Trail_TF, 0);             
    //---- Начинаем тралить - с первого бара после открывающего (иначе при bTrlinloss сразу же после открытия 
    // позиции стоплосс будет перенесен на половину расстояния между стоплоссом и курсом открытия)
    // т.е. работаем только при условии, что с момента OrderOpenTime() прошло не менее Trail_TF минут      
    if (TimeCurrent() - Trail_TF * 60 > OrderOpenTime())
    {         
        double ld_NextMove;     
      
        //---- Для длинной позиции переносим стоплосс на FF_coeff дистанции от курса открытия до Bid на момент открытия бара
        // (если такой стоплосс лучше имеющегося и изменяет стоплосс в сторону профита)
        if (OrderType() == OP_BUY)
        {
            if (bb_TrailLOSS && OrderStopLoss() != 0)
            {
                ld_NextMove = FF_coeff * (bd_Price - OrderStopLoss());
                fd_NewSL = OrderStopLoss() + ld_NextMove;            
            }
            else
            {
                //---- Если стоплосс ниже курса открытия, то тралим "от курса открытия"
                if (OrderOpenPrice() > OrderStopLoss())
                {
                    ld_NextMove = FF_coeff * (bd_Price - OrderOpenPrice());                 
                    //Print ("Next Move = ", FF_coeff, " * (", DSD (dBid), " - ", DSD (OrderOpenPrice()), ") = ", DSD (ld_NextMove));
                    fd_NewSL = OrderOpenPrice() + ld_NextMove;
                    //Print ("New SL[", DSD (OrderStopLoss()), "] = (", DSD (OrderOpenPrice()), " + ", DSD (ld_NextMove), ") ", DSD (fd_NewSL), "[", (fd_NewSL - OrderStopLoss()) / Point, "]");
                }
                //---- Если стоплосс выше курса открытия, тралим от стоплосса
                else
                {
                    ld_NextMove = FF_coeff * (bd_Price - OrderStopLoss());
                    fd_NewSL = OrderStopLoss() + ld_NextMove;
                }                                       
            }
            //---- SL перемещаем только в случае, если новый стоплосс лучше текущего и если смещение - в сторону профита
            // (при первом поджатии, от курса открытия, новый стоплосс может быть лучше имеющегося, и в то же время ниже 
            // курса открытия (если dBid ниже последнего) 
            if (ld_NextMove > 0) {return (true);}
        }       
        if (OrderType() == OP_SELL)
        {
            if (bb_TrailLOSS && OrderStopLoss() != 0)
            {
                ld_NextMove = FF_coeff * (OrderStopLoss() - (bd_Price + bd_Spread));
                fd_NewSL = OrderStopLoss() - ld_NextMove;            
            }
            else
            {         
                //---- Если стоплосс выше курса открытия, то тралим "от курса открытия"
                if (OrderOpenPrice() < OrderStopLoss())
                {
                    ld_NextMove = FF_coeff * (OrderOpenPrice() - (bd_Price + bd_Spread));                 
                    fd_NewSL = OrderOpenPrice() - ld_NextMove;
                }
                //---- Если стоплосс нижу курса открытия, тралим от стоплосса
                else
                {
                    ld_NextMove = FF_coeff * (OrderStopLoss() - (bd_Price + bd_Spread));
                    fd_NewSL = OrderStopLoss() - ld_NextMove;
                }                  
            }
            //---- SL перемещаем только в случае, если новый стоплосс лучше текущего и если смещение - в сторону профита
            if (ld_NextMove > 0) {return (true);}
        }               
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//| ТРЕЙЛИНГ KillLoss                                                                 |
//| Применяется на участке лоссов. Суть: стоплосс движется навстречу курсу со ско-    |
//| ростью движения курса х коэффициент (SpeedCoeff). При этом коэффициент можно      |
//| "привязать" к скорости увеличения убытка - так, чтобы при быстром росте лосса     |
//| потерять меньше. При коэффициенте = 1 стоплосс сработает ровно посредине между    |
//| уровнем стоплосса и курсом на момент запуска функции, при коэфф.>1 точка встречи  |
//| курса и стоплосса будет смещена в сторону исходного положения курса, при коэфф.<1 |
//| - наоборот, ближе к исходному стоплоссу.                                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool KillLoss (int fi_Ticket, double& fd_NewSL)
{
    double ld_StopPriceDiff, // расстояние (пунктов) между курсом и стоплоссом
           ld_ToMove,        // кол-во пунктов, на которое следует переместить стоплосс
           ld_curMove, ld_newSL, ld_LastPriceDiff;
    string ls_Name;
    int    li_cmd;
//----
    //---- Текущее расстояние между курсом и стоплоссом
    if (OrderType() == OP_BUY)
    {ld_StopPriceDiff = bd_Price - OrderStopLoss();}
    if (OrderType() == OP_SELL)
    {ld_StopPriceDiff = (OrderStopLoss() + bd_Spread) - bd_Price;}
    ls_Name = StringConcatenate (fi_Ticket, "_#Delta_SL");
    //---- Проверяем, если тикет новый, запоминаем текущее расстояние между курсом и стоплоссом
    if (!GlobalVariableCheck (ls_Name))
    {GlobalVariableSet (ls_Name, ld_StopPriceDiff); return (false);}
    else {ld_LastPriceDiff = GlobalVariableGet (ls_Name);}
    //---- Итак, у нас есть коэффициент ускорения изменения курса
    // на каждый пункт, который проходит курс в сторону лосса, 
    // мы должны переместить стоплосс ему на встречу на fd_SpeedCoeff раз пунктов
    // (например, если лосс увеличился на 3 пункта за тик, fd_SpeedCoeff = 1.5, то
    // стоплосс подтягиваем на 3 х 1.5 = 4.5, округляем - 5 п. Если подтянуть не 
    // удаётся (слишком близко), ничего не делаем.          
        
    //---- Кол-во пунктов, на которое приблизился курс к стоплоссу с момента предыдущей проверки (тика, по идее)
    ld_ToMove = NDPD (ld_LastPriceDiff - ld_StopPriceDiff);
        
    //---- Записываем новое значение, но только если оно уменьшилось
    if (ld_StopPriceDiff + bd_TrailStep < ld_LastPriceDiff)
    {GlobalVariableSet (ls_Name, ld_StopPriceDiff);}
        
    //---- Дальше действия на случай, если расстояние уменьшилось (т.е. курс приблизился к стоплоссу, убыток растет)
    if (ld_ToMove >= TrailStep)
    {
        ld_ToMove = NDP (MathRound (ld_ToMove * SpeedCoeff));
        if (OrderType() == OP_BUY) {li_cmd = 1;} else {li_cmd = -1;}
        //---- Стоплосс, соответственно, нужно также передвинуть на такое же расстояние, но с учетом коэфф. ускорения
        ld_curMove = li_cmd * (bd_Price - (OrderStopLoss() + li_cmd * ld_ToMove));
        fd_NewSL = OrderStopLoss() + li_cmd * ld_ToMove;
        return (true);
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//| ТРЕЙЛИНГ ПИПСОВОЧНЫЙ (для пипсовки)                                               |
//| Применяется для пипсовки, когда не известен тэйк и хочется взять по-максимуму.    |
//| Берём промежуток времени Period_Average и расчитываем средний размер свечи - это  |
//| будет условным тэйком (УТ). При достижении ценой половины УТ, переводим в Без-    |
//| Убыток. А затем применяем "удавку". При достижении ценой условного тэйка,         |
//| "удлиняем" его в 1.5 раза.                                                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool TrailPips (int fi_Ticket, double& fd_NewSL)
{
    double ld_VirtTP, ld_TrailStop = 0.0;
    string ls_Name = StringConcatenate (fi_Ticket, "_#VirtTP");
    int    li_cmd;
//----
    //---- Получаем виртуальный Тэйк
    if (GlobalVariableCheck (ls_Name)) {ld_VirtTP = GlobalVariableGet (ls_Name);}
    else
    {
        if (OrderType() == OP_BUY) {li_cmd = 1;} else {li_cmd = -1;}
        ld_VirtTP = OrderOpenPrice() + li_cmd * fGet_AverageCandle (Symbol(), Average_Period);
        GlobalVariableSet (ls_Name, ld_VirtTP);
    }
    if (OrderType() == OP_BUY)
    {
        //---- Увеличиваем виртуальный тэйк
        if (bd_Price >= ld_VirtTP)
        {
            ld_VirtTP += ((ld_VirtTP - OrderOpenPrice()) / 2.0);
            GlobalVariableSet (ls_Name, ld_VirtTP);
        }
        //---- Рассчитываем TrailStop - расстояние от виртуального стопа до цены
        for (int li_int = 4; li_int >= 2; li_int--)
        {
            ld_TrailStop = NDD ((ld_VirtTP - OrderOpenPrice()) / li_int);
            if (ld_TrailStop >= ld_VirtTP - bd_Price)
            //---- При прохождении ценой половины пути до цели, ставим виртуальный БУ
            {if (li_int == 2) {ld_TrailStop -= bd_ProfitMIN;} break;}
            else {ld_TrailStop = 0.0;}
        }
        if (ld_TrailStop > 0)  
        {
            if (bd_Price - OrderOpenPrice() > ld_TrailStop)
            {
                fd_NewSL = bd_Price - ld_TrailStop;
                if (OrderStopLoss() + bd_TrailStep < fd_NewSL || OrderStopLoss() == 0)
                {return (true);}
            }
        }
    }
    if (OrderType() == OP_SELL)
    {
        //---- Увеличиваем виртуальный тэйк
        if (bd_Price <= ld_VirtTP)
        {
            ld_VirtTP -= ((OrderOpenPrice() - ld_VirtTP) / 2.0);
            GlobalVariableSet (ls_Name, ld_VirtTP);
        }
        //---- Рассчитываем TrailStop - расстояние от виртуального стопа до цены
        for (li_int = 4; li_int >= 2; li_int--)
        {
            ld_TrailStop = NDD ((OrderOpenPrice() - ld_VirtTP) / li_int);
            if (ld_TrailStop >= bd_Price - ld_VirtTP)
            //---- При прохождении ценой половины пути до цели, ставим виртуальный БУ
            {if (li_int == 2) {ld_TrailStop -= bd_ProfitMIN;} break; }
            else {ld_TrailStop = 0.0;}
        }
        if (ld_TrailStop > 0)  
        {
            if (OrderOpenPrice() - bd_Price > ld_TrailStop)
            {
                fd_NewSL = bd_Price + ld_TrailStop;
                if (OrderStopLoss() > fd_NewSL + bd_TrailStep || OrderStopLoss() == 0)
                {return (true);}
            }
        }
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, bs_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Просчитываем среднее значение размера свечи за означенный период           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_AverageCandle (string fs_Symbol,         // Символ
                           int fi_Period,            // Период
                           bool fb_IsCandle = false) // считаем свечу?
{
    double ld_OPEN, ld_CLOSE, ld_HIGH, ld_LOW, ld_AVERAGE = 0;
    datetime ldt_Begin = iTime (fs_Symbol, 0, 0) - fi_Period * 60;
    int      li_cnt_Bar = iBarShift (fs_Symbol, 0, ldt_Begin);
//----
    for (int li_BAR = 1; li_BAR < li_cnt_Bar; li_BAR++)
    {
        if (fb_IsCandle)
        {
            ld_OPEN = iOpen (fs_Symbol, 0, li_BAR);
            ld_CLOSE = iClose (fs_Symbol, 0, li_BAR);
            ld_AVERAGE += MathAbs (ld_OPEN - ld_CLOSE);
        }
        else
        {
            ld_HIGH = iHigh (fs_Symbol, 0, li_BAR);
            ld_LOW = iLow (fs_Symbol, 0, li_BAR);
            ld_AVERAGE += (ld_HIGH - ld_LOW);
        }
    }
    ld_AVERAGE /= li_cnt_Bar;
    ld_AVERAGE = NormalizeDouble (ld_AVERAGE, 4);
    //Print ("Time[", li_cnt_Bar, "] = ", TimeToStr (ldt_Begin), "; Свеча = ", DS0 (ld_AVERAGE / bd_Point));
//----
    return (ld_AVERAGE);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Учитываем разрядность котировок                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCheck_LibDecimal()
{
//----
    TrailingStop *= bi_Decimal;
    bd_TrailingStop = NDP (TrailingStop);
    TrailStep *= bi_Decimal;
    bd_TrailStep = NDP (TrailStep);
    BreakEven *= bi_Decimal;
    bd_BreakEven = NDP (BreakEven);
    Indent_Fr *= bi_Decimal;
    bd_IndentFr = NDP (Indent_Fr);
    Indent_Sh *= bi_Decimal;
    bd_IndentSh = NDP (Indent_Sh);
    Indent_Pr *= bi_Decimal;
    bd_IndentPr = NDP (Indent_Pr);
    Indent_MA *= bi_Decimal;
    bd_IndentMA = NDP (Indent_MA);
    TimeStep *= bi_Decimal;
    bd_TimeStep = NDP (TimeStep);
    //---- Заполняем переменные для трейлинга "Udavka"
    Distance_1 *= bi_Decimal;
    Distance_2 *= bi_Decimal;
    bda_Distance[0] = NDP (Distance_1);
    bda_Distance[1] = NDP (Distance_2);
    Level_0 *= bi_Decimal;
    Level_1 *= bi_Decimal;
    Level_2 *= bi_Decimal;
    //---- Заполняем переменные для трейлинга "Расчёт Баришпольца"
    ProfitLevel_1 *= bi_Decimal;
    StopLevel_1 *= bi_Decimal;
    ProfitLevel_2 *= bi_Decimal;
    StopLevel_2 *= bi_Decimal;
    ProfitLevel_3 *= bi_Decimal;
    StopLevel_3 *= bi_Decimal;
    bia_StopLevel[0] = StopLevel_1;
    bia_StopLevel[1] = StopLevel_2;
    bia_StopLevel[2] = StopLevel_3;
    bia_ProfitLevel[0] = ProfitLevel_1;
    bia_ProfitLevel[1] = ProfitLevel_2;
    bia_ProfitLevel[2] = ProfitLevel_3;
    for (int li_int = 0; li_int < 3; li_int++)
    {
        bda_StopLevel[li_int] = NDP (bia_StopLevel[li_int]);
        bda_ProfitLevel[li_int] = NDP (bia_ProfitLevel[li_int]);
    }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

