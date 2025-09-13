//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                               b-PSI@ManagerPA.mqh |
//|                                       Copyright © 2012, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//| 17.03.2012  Библиотека функций управления общим профитом советника.               |
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
//IIIIIIIIIIIIIIIIIII==================CONSTANS=================IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                 *****        Параметры библиотеки         *****                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern string Setup_TrailingProfit  = "=============== Trailing Profit ==============="; 
extern bool   TrailProfit_ON           = FALSE;            // Включения трейлинга общего Профита
extern bool   ValueInCurrency          = FALSE;            // По умолчанию расчёты ведутся в процентах к балансу
extern double TrailProfit_StartPercent = 10.0;             // Процент Проофит/Баланс при котором начинается перемещение уровня закрытия 
extern double TrailProfit_LevelPercent = 5.0;              // Величина снижения процента Проофит/Баланс, при котором выполняется закрытие 
extern double TrailProfit_Start        = 50.0;             // Величина профита при котором начинается перемещение уровня закрытия 
extern double TrailProfit_Level        = 25.0;             // Величина обратного движения профита, при котором выполняется закрытие 
extern string Setup_TakeProfit      = "============ Take Profit Advisor =============="; 
extern bool   TakeProfit_ON            = FALSE;            // Включения тейкпрофита для общего Профита
extern double TP_AdvisorPercent        = 15.0;             // Процент гарантированной прибыли 
extern double TP_Advisor               = 50.0;             // Гарантированная прибыль 
extern string Setup_StopLoss        = "============= Stop Loss Advisor ==============="; 
extern bool   StopLoss_ON              = FALSE;            // Включения стоплосс для общего Профита
extern double SL_AdvisorPercent        = 20.0;             // Процент гарантированного убытка 
extern double SL_Advisor               = 50.0;             // Гарантированный убыток 
//IIIIIIIIIIIIIIIIIII=========Подключение внешних модулей=======IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII========Глобальные переменные модуля=======IIIIIIIIIIIIIIIIIIIIII+
double        bd_cur_Profit,              // общий профит советника в выбранных единицах (ValueInCurrency)
              bd_TS_Profit,               // текущий уровень срабатывания трала в выбранных единицах (ValueInCurrency)
              bd_TS_Profit_Percent,       // текущий уровень срабатывания трала в процентах
              bd_TS_Profit_CUR,           // текущий уровень срабатывания трала в валюте Депо
              bd_TrailProfit_Start,       // заданный уровень срабатывания трала в выбранных единицах (ValueInCurrency)
              bd_TrailProfit_Level;       // заданный размер (обратный ход) трала в выбранных единицах (ValueInCurrency)
bool          bb_TSProfit = false;        // флаг работы трейлинга общего профита
string        bs_NameGVflag;              // имя флага
//IIIIIIIIIIIIIIIIIII===========Перечень функций модуля=========IIIIIIIIIIIIIIIIIIIIII+
//bool fInit_ManagerPA ()            // Инициализация модуля
//bool fManagerPA (double fd_Profit,               // текущий профит
                 //bool fb_Conditions)             // Условие включения (наличие ордеров)
                                     // Manager Profit Advisor
//bool fTrail_PA (double fd_Profit,                // текущий профит
                //double fd_TrailStart)            // уровень срабатывания трала
                                     // Trailing Profit Advisor
//bool fControl_PA.STOPs (double fd_Profit)        // Текущий профит
                                     // Закрываем все ордера по достижению профитом уровней SL или TP
//int fNULL_STOPs (bool fb_Conditions = true)      // Условие "пропуска" функции, например, отсутствие СТОПов
                                     // Функция обнуления СТОПов
//void fReset_flTSProfit (bool& fb_flTSProfit)
                                     // Сброс флага трейлинга общего профита
//string fGet_StringManagerPA()      // Получаем строкой статистику по работе модуля
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Инициализация модуля                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fInit_ManagerPA()
{
    int er = GetLastError();
//----
    //---- При оптимизации пропускаем бесполезные варианты настроек
    if (fSet_ContinuePA()) if (IsOptimization()) {bb_OptimContinue = true; return (false);}
    if (ValueInCurrency) {bsa_sign[0] = bs_sign; bsa_sign[1] = "";} else {bsa_sign[0] = ""; bsa_sign[1] = " %";}
    //---- Заполняем переменные работы с общим профитом в соответствии с выбранным типом расчётов
    if (!ValueInCurrency)
    {
        bd_TrailProfit_Start = 100.0 + TrailProfit_StartPercent;
        bd_TrailProfit_Level = TrailProfit_LevelPercent;
        bd_TP_Adv = 100.0 + TP_AdvisorPercent;
        bd_SL_Adv = 100.0 - SL_AdvisorPercent;
    }
    else
    {
        bd_TrailProfit_Start = TrailProfit_Start;
        bd_TrailProfit_Level = TrailProfit_Level;
        bd_TP_Adv = TP_Advisor;
        bd_SL_Adv = -SL_Advisor;
    }
    bs_NameGVflag = StringConcatenate (bs_NameGV, "_#fl_TSProfit");
    if (GlobalVariableCheck (bs_NameGVflag)) bb_TSProfit = GlobalVariableGet (bs_NameGVflag);
    else GlobalVariableSet (bs_NameGVflag, bb_TSProfit);
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fInit_ManagerPA()", bi_indERR);
//----
    return (true);
    fGet_StringManagerPA();
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Manager Profit Advisor                                                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fManagerPA (double fd_Profit,          // текущий профит
                 bool fb_Conditions)        // Условие включения (наличие ордеров)
{
//----
    bs_libNAME = "b-ManagerPA";
    //---- Переводим текущий профит в проценты от депозита
    bd_ProfitPercent = NormalizeDouble (100.0 + fd_Profit * 100.0 / AccountBalance(), 2);
    //---- Считаем профит в соответствии с выбранными настройками
    if (ValueInCurrency) {bd_cur_Profit = fd_Profit;} else {bd_cur_Profit = bd_ProfitPercent;}
    //---- Работа основных функций библиотеки
    if (fb_Conditions)
    {
        //---- Проверяем условия срабатывания Trailing Profit для наблюдаемых ордеров
        if (TrailProfit_ON)
        {if (fTrail_PA (bd_cur_Profit, bd_TrailProfit_Start)) {return (true);}}
        //---- Проверяем условия срабатывания Take Profit и Stop Loss для наблюдаемых ордеров
        if (TakeProfit_ON || StopLoss_ON)
        {if (fControl_PA.STOPs (bd_cur_Profit)) {return (true);}}
        //---- Сбрасываем (если не сброшен) флаг трейлинга общего профита
        if (fd_Profit < 0.0) {fReset_flTSProfit (bb_TSProfit);}
    }
    //---- Сбрасываем (если не сброшен) флаг трейлинга общего профита
    else {fReset_flTSProfit (bb_TSProfit);}
    bs_libNAME = "";
    //---- Контролируем возможные ошибки
    //fGet_LastErrorInArray (bsa_Comment, "fManagerPA()", bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Trailing Profit Advisor                                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fTrail_PA (double fd_Profit,      // текущий профит
                double fd_TrailStart)  // уровень срабатывания трала
{
    int    li_result = -1;
    string ls_fName = "fTrail_PA()";
//----
    if (!bb_TSProfit)
    {
        //---- Рассчитываем начальные значения рабочих переменных
        bd_TS_Profit_Percent = bd_ProfitPercent - TrailProfit_LevelPercent;
        bd_TS_Profit_CUR = fd_Profit - TrailProfit_Level;
        if (!ValueInCurrency) {bd_TS_Profit = bd_TS_Profit_Percent;} else {bd_TS_Profit = bd_TS_Profit_CUR;}
        //---- Проверяем начальные условия трейлинга
        if (fd_Profit >= fd_TrailStart)
        {
            bb_TSProfit = true;
            GlobalVariableSet (bs_NameGVflag, bb_TSProfit);
            //---- Обнуляем стопы у всех контролируемых ордеров
            li_result = fNULL_STOPs();
            if (li_result > 0) {fSet_Comment (bdt_curTime, 0, 60, ls_fName, True, li_result); return (false);}
        }
    }
    else
    {
        //---- Рассчитываем рабочие переменные
        bd_TS_Profit_Percent = MathMax (bd_TS_Profit_Percent, bd_ProfitPercent - TrailProfit_LevelPercent);
        bd_TS_Profit_CUR = MathMax (bd_TS_Profit_CUR, fd_Profit - TrailProfit_Level);
        if (!ValueInCurrency) {bd_TS_Profit = bd_TS_Profit_Percent;} else {bd_TS_Profit = bd_TS_Profit_CUR;}
        //---- Условие срабатывания закрытия ордеров по TraillingProfit
        if (fd_Profit <= bd_TS_Profit)
        {
            double ld_Pribul;
            li_result = fClose_AllOrders (bs_SymbolList, bs_MagicList, ld_Pribul, -1);
            if (li_result > 0)
            {fSet_Comment (bdt_curTime, 0, 61, ls_fName, True, li_result, ld_Pribul, bd_TS_Profit, bd_TrailProfit_Start); return (true);}
        }
    }
    //---- Контролируем возможные ошибки
    //fGet_LastErrorInArray (bsa_Comment, ls_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Закрываем все ордера по достижению профитом уровней SL или TP              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fControl_PA.STOPs (double fd_Profit)     // Текущий профит
{
    string ls_fName = "fControl_PA.STOPs()";
    bool   lb_close = false;
    double ld_Pribul;
//----
    //fControl_PA.STOPs (ls_txt, gd_cur_Profit, Symbol.List, Magic.List);
    //---- Проверяем условия срабатывания Take Profit для наблюдаемых ордеров
    if (TakeProfit_ON) {if (fd_Profit >= bd_TP_Adv) lb_close = true;}
    //---- Проверяем условия срабатывания Stop Loss для наблюдаемых ордеров
    if (StopLoss_ON) {if (!lb_close) {if (fd_Profit <= bd_SL_Adv) lb_close = true;}}
    if (lb_close)
    {
        int li_result = 0;
        li_result = fClose_AllOrders (bs_SymbolList, bs_MagicList, ld_Pribul, -1, 0, bs_Delimiter);
        if (li_result > 0)
        {fSet_Comment (bdt_curTime, 0, 62, ls_fName, True, li_result, ld_Pribul, bd_ProfitPercent); return (true);}
    }
    //---- Контролируем возможные ошибки
    //fGet_LastErrorInArray (bsa_Comment, ls_fName, bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция обнуления СТОПов                                                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fNULL_STOPs (bool fb_Conditions = false)  // Условие "пропуска" функции, например, отсутствие СТОПов
{
    //---- Условие "неработы" функции
    if (fb_Conditions) {return (0);}
    int  li_cnt = 0;
    bool lb_result = false;
//----
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {
        if (!OrderSelect (i, SELECT_BY_POS, MODE_TRADES)) continue;
        if (StringFind (bs_SymbolList, OrderSymbol()) < 0 && StringLen (bs_SymbolList) > 0) continue;
        if (!fCheck_MyMagic (bs_MagicList, bs_Delimiter)) continue;
        if (OrderType() > 1) continue;
        if (OrderStopLoss() == 0.0 && OrderTakeProfit() == 0.0) continue;
        if (fOrderModify (OrderTicket(), OrderOpenPrice(), 0.0, 0.0, 0, Goldenrod) == 1) li_cnt++;
    }
    //---- Контролируем возможные ошибки
    //fGet_LastErrorInArray (bsa_Comment, "fNULL_STOPs()", bi_indERR);
//----
    return (li_cnt);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Сброс флага трейлинга общего профита                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fReset_flTSProfit (bool& fb_flTSProfit)
{if (fb_flTSProfit) {fb_flTSProfit = false; GlobalVariableSet (bs_NameGVflag, fb_flTSProfit);}}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Получаем строкой статистику по работе модуля                               |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fGet_StringManagerPA()
{
//----
    if (TrailProfit_ON || TakeProfit_ON || StopLoss_ON)
    {
        string ls_rowR = "                              - - - - - - - - - - - - - - - - - - - - - - - - - - - - -\n",
               ls_StatisticPA = ls_rowR;
        if (TrailProfit_ON)
        {
            string ls_TS_Profit_CUR = "", ls_TS_Profit_Percent = "", ls_flagTS = CheckBOOL (bb_TSProfit);
            if (ValueInCurrency) {ls_TS_Profit_CUR = StringConcatenate (" [ ", bs_sign, DS0 (TrailProfit_Start), " ] ");}
            else {ls_TS_Profit_Percent = StringConcatenate (" [ ", DoubleToStr (TrailProfit_StartPercent, 1), " % ] ");}
            string ls_TrailProfit = StringConcatenate (
                 "                              TRAIL               = ", bs_sign, fSplitField (DoubleToStr (bd_TS_Profit_CUR, 1)), ls_TS_Profit_CUR, " | ", DoubleToStr (bd_TS_Profit_Percent, 1), " % ", ls_TS_Profit_Percent, " - ", ls_flagTS, "\n");
        }
        else {ls_TrailProfit = "";}
        if (TakeProfit_ON)
        {string ls_TP_Adv = StringConcatenate (
                 "                              TAKE                = ", bsa_sign[0], " ", fSplitField (DoubleToStr (bd_cur_Profit, 1)), bsa_sign[1], " [ ", bsa_sign[0], " ", fSplitField (DoubleToStr (bd_TP_Adv, 1)), bsa_sign[1], " ]\n");}
        else {ls_TP_Adv = "";}
        if (StopLoss_ON)
        {string ls_SL_Adv = StringConcatenate (
                 "                              STOP                = ", bsa_sign[0], " ", fSplitField (DoubleToStr (bd_cur_Profit, 1)), bsa_sign[1], " [ ", bsa_sign[0], " ", fSplitField (DoubleToStr (bd_SL_Adv, 1)), bsa_sign[1], " ]\n");}
        else {ls_SL_Adv = "";}
        ls_StatisticPA = StringConcatenate (ls_rowR, ls_TrailProfit, ls_TP_Adv, ls_SL_Adv, ls_rowR);
    }
    else {ls_StatisticPA = "";}
//----
    return (ls_StatisticPA);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Для оптимизации убираем "пустые" варианты                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fSet_ContinuePA()
{
//----
    if (TrailProfit_ON)
    {
        if (!ValueInCurrency)
        {
            if (TrailProfit_StartPercent <= TrailProfit_LevelPercent) return (true);
            if (TakeProfit_ON) {if (TP_AdvisorPercent <= TrailProfit_StartPercent) return (true);}
        }
        else
        {
            if (TrailProfit_Start <= TrailProfit_Level) return (true);
            if (TakeProfit_ON) {if (TP_Advisor <= TrailProfit_Start) return (true);}
        }
    }
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

