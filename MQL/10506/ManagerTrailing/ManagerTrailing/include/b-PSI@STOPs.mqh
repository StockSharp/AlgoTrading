//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                                   b-PSI@STOPs.mqh |
//|                                    Copyright © 2011-12, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//|   20.05.2012  Библиотека создания и контроля СТОПов.                              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|   Данный продукт предназначен для некомерческого использования. Публикация разре- |
//|шена только при указании имени автора (TarasBY). Редактирование исходного кода до- |
//|пустима только при условии сохранения данного текста, ссылок и имени автора.  Про- |
//|дажа библиотеки или отдельных её частей ЗАПРЕЩЕНА.                                 |
//|   Автор не несет ответственности за возможные убытки, полученные в результате ис- |
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
//IIIIIIIIIIIIIIIIIII==========Внешние параметры модуля=========IIIIIIIIIIIIIIIIIIIIII+
extern string SETUP_STOPs           = "================ Make STOP`s ==================";
extern int    N_STOPs                  = 0;                // N (номер) используемых СТОПов (0 - 3)
extern bool   USE_Dinamic_SL           = TRUE;             // Динамический SL
extern bool   USE_Dinamic_TP           = FALSE;            // Динамический TP
extern int    TF_STOPs                 = PERIOD_H1;        // Таймфрейм на котором рассчитываем СТОПы
extern int    MIN_StopLoss             = 30;               // Минимальный SL
extern int    MIN_TakeProfit           = 30;               // Минимальный TP
extern int    LevelFIBO_SL             = 0;                // уровень по FIBO выше\ниже пика (-2 - 5)
extern int    LevelFIBO_TP             = 0;                // уровень по FIBO выше\ниже пика (-2 - 5)
extern string Setup_STATIC          = "-----------------  N0 - STATIC ----------------";
extern int    StopLoss                 = 100;
extern int    TakeProfit               = 500;
#include      <b-PSI@VirtualSTOPs.mqh>                     // Библиотека виртуальных СТОпов
#include      <b-PSI@LEVELs_Light.mqh>                     // Библиотека расчёта уровней
//IIIIIIIIIIIIIIIIIII=========Подключение внешних модулей=======IIIIIIIIIIIIIIIIIIIIII+

//IIIIIIIIIIIIIIIIIII========Глобальные переменные модуля=======IIIIIIIIIIIIIIIIIIIIII+
double        bd_MIN_SL, bd_MIN_TP;
int           bia_LevelFIBO[2];
//IIIIIIIIIIIIIIIIIII===========Перечень функций модуля=========IIIIIIIIIIIIIIIIIIIIII+
//bool fInit_STOPs()                 // Инициализация модуля
/*bool fCreat_STOPs (int fi_NSTOPs,                // N (номер) расчитываемых СТОПов
                     int fi_Ticket,                // Ticket ордера
                     double fd_Price,              // текущая цена по инструменту
                     int fi_Period,                // TF с которого производятся расчёты
                     bool fb_USE_VirtualSTOPs = false)*/// флаг использования виртуальных СТПов
                                     // Рассчитываем и модифицируем СТОПы
/*void fCreat_LevelsByFIBO (double ar_Extrem[],    // массив экстремумов
                            double& ar_STOPs[],    // возвращаемый массив СТОПов
                            int ar_LevelFIBO[])*/  // массив номеров уровней (0 - для SL; 1 - для TP)
                                     // Расчитываем СТОПы с учётом уровней FIBO (заданные в настройках).
//bool fCheck_STOPsParameters()      // Проверяем переданные в библиотеку внешние параметры
//|***********************************************************************************|
//| РАЗДЕЛ: ОБЩИХ ФУНКЦИЙ                                                             |
//|***********************************************************************************|
//void fSet_ValuesSTOPs()            // Получаем рабочие переменные в соответствии с разрядностью
//void fCheck_DecimalSTOPs()         // Учитываем разрядность котировок
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Инициализация модуля                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fInit_STOPs()
{
//----
    //---- Производим проверку передаваемых в библиотеку значений
    if (!fCheck_STOPsParameters())
    {
        Alert ("Проверьте параметры выбранного Вами СТОПа !!!");
        bb_OptimContinue = true;
        return (false);
    }
    //---- Инициализируем переменные библиотеки виртуальных СТОПов
    fInit_VirtualSTOPs();
    //---- Инициализируем переменные библиотеки расчёта уровней
    if (!fInit_LEVELs (N_STOPs)) return (false);
    //---- Приводим внешние переменные в соответствии с разрядностью котировок ДЦ
    fCheck_DecimalSTOPs();
    bia_LevelFIBO[0] = LevelFIBO_SL;
    bia_LevelFIBO[1] = LevelFIBO_TP;
    bb_ClearGV = true;
    string lsa_Array[1];
    lsa_Array[0] = "_#STOP";
    fCreat_ArrayGV (bsa_prefGV, lsa_Array);
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, "fInit_STOPs()", bi_indERR);
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Рассчитываем и модифицируем СТОПы                                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCreat_STOPs (int fi_NSTOPs,                    // N (номер) расчитываемых СТОПов
                   int fi_Ticket,                    // Ticket ордера
                   double fd_Price,                  // текущая цена по инструменту
                   int fi_Period,                    // TF с которого производятся расчёты
                   bool fb_USE_VirtualSTOPs = false) // флаг использования виртуальных СТОПов
{
    //---- Если не нужна динамическая модификация - выходим
    string ls_Name = StringConcatenate (fi_Ticket, "_#STOP");
    if (GlobalVariableCheck (ls_Name)) {if (GlobalVariableGet (ls_Name) == 1) return (false);}
//----
    int    err = GetLastError(), li_Type = OrderType(), li_cmd = 1;
    double lda_STOPs[] = {0.0,0.0}, lda_Levels[2];
    string ls_Symbol = OrderSymbol();
    static string lsa_NameSTOPs[] = {"Classic","Extremum","ATR","ZZ"};
    bool lba_Modify[] = {false,false};
//----
    bs_libNAME = "b-STOPs_Light";
    bs_fName = lsa_NameSTOPs[fi_NSTOPs];
    //ArrayInitialize (ar_STOPs, 0.0);
    if (li_Type == 1) {li_cmd = -1;}
    //---- Получаем актуальные параметры по инструменту
    fSet_ValuesSTOPs();
    //---- Определяем текущиий SL
    ls_Name = StringConcatenate (fi_Ticket, "_#VirtSL");
    bd_curSL = fGet_ValueFromGV (ls_Name, OrderStopLoss(), fb_USE_VirtualSTOPs);
    //---- Определяем текущиий TP
    ls_Name = StringConcatenate (fi_Ticket, "_#VirtTP");
    bd_curTP = fGet_ValueFromGV (ls_Name, OrderTakeProfit(), fb_USE_VirtualSTOPs);
    //---- Формируем СТОПы
    switch (fi_NSTOPs)
    {
        case 0: // Классические СТОПы
            if (StopLoss > 0)
            {
                if (bd_curSL > 0.0) lba_Modify[0] = true;
                lda_STOPs[0] = fd_Price - li_cmd * bd_SL;
            }
            if (TakeProfit > 0)
            {
                if (bd_curTP > 0.0) lba_Modify[1] = true;
                lda_STOPs[1] = fd_Price + li_cmd * (bd_TP + bd_Spread);
            }
            //---- Ничего модифицировать не нужно
            if (lba_Modify[0] && lba_Modify[1]) {GlobalVariableSet (StringConcatenate (fi_Ticket, "_#STOP"), 1); return (false);}
            break;
        case 1: // СТОПы по экстремумам на fi_Period за S.cnt_Bars барах
        case 2: // СТОПы по ATR на fi_Period
        case 3: // СТОПы по ZZP (ZigZag) на fi_Period
            //---- Расчитываем минимальные СТОПы
            lda_STOPs[0] = fd_Price - li_cmd * bd_MIN_SL;
            lda_STOPs[1] = fd_Price + li_cmd * (bd_MIN_TP + bd_Spread);
            //---- Получаем экстремумы
            if (fi_NSTOPs == 1) if (!fGet_Extremum (lda_Levels, ls_Symbol, fi_Period, E.cnt_Bars)) return (false);
            if (fi_NSTOPs == 2) if (!fGet_ATR (lda_Levels, ls_Symbol, fi_Period, E.cnt_Bars)) return (false);
            if (fi_NSTOPs == 3) if (!fGet_ZZP (lda_Levels, ls_Symbol, fi_Period)) return (false);
            //---- Корректируем СТОПы с учётом уровней FIBO
            fCreat_LevelsByFIBO (lda_Levels, lda_STOPs, bia_LevelFIBO);
            break;
    }
    //---- Вычисляем расстояние от SL до текущей цены
    bd_Trail = MathAbs (fd_Price - lda_STOPs[0]);
    bd_Price = fd_Price;
    ArrayInitialize (lba_Modify, 0);
    //---- Заполняем СТОПы новыми значениями
    if (bd_curSL == 0.0 || (USE_Dinamic_SL && li_cmd * (OrderOpenPrice() - bd_curSL) > 0.0))
    {
        bd_NewSL = NDD (lda_STOPs[0]);
        //---- Если SL изменился фиксируем этот факт
        if (NDD (bd_NewSL - bd_curSL) != 0.0) lba_Modify[0] = true;
    }
    else bd_NewSL = bd_curSL;
    if (bd_curTP == 0.0 || USE_Dinamic_TP)
    {
        bd_NewTP = NDD (lda_STOPs[1]);
        //---- Если TP изменился фиксируем этот факт
        if (NDD (bd_NewTP - bd_curTP) != 0.0) lba_Modify[1] = true;
    }
    else bd_NewTP = bd_curTP;
    //---- Если ничего модифицировать не нужно - выходим
    if (!lba_Modify[0] && !lba_Modify[1]) return (false);
    int li_result = 1;
    //---- Организуем возможность первоначальной (внеочередной) модификации
    if (USE_Dinamic_SL || USE_Dinamic_TP) li_result = 0;
    //---- Двигаем СТОПы с модификацией или без
    if (!fb_USE_VirtualSTOPs)
    {
        //---- Модифицируем СТОПы
        int li_modify = fOrderModify (fi_Ticket, OrderOpenPrice(), bd_NewSL, bd_NewTP);
        if (li_modify >= 0) fSet_Comment (bdt_curTime, fi_Ticket, 20, "fCreat_STOPs()", li_modify != 0, fb_USE_VirtualSTOPs);
        //---- Помечаем "последствия" первичной модификации
        if (li_modify == 1) {GlobalVariableSet (StringConcatenate (fi_Ticket, "_#STOP"), li_result);}
        return (li_modify == 1);
    }
    else
    {
        //---- Если SL изменился вносим изменения в GV-переменную
        if (lba_Modify[0]) {GlobalVariableSet (StringConcatenate (fi_Ticket, "_#VirtSL"), bd_NewSL);}
        //---- Если TP изменился вносим изменения в GV-переменную
        if (lba_Modify[1]) {GlobalVariableSet (StringConcatenate (fi_Ticket, "_#VirtTP"), bd_NewTP);}
        if (!bb_VirtualTrade)
        {
            fSet_Comment (bdt_curTime, fi_Ticket, 20, "b-STOPs[Virt]", true, fb_USE_VirtualSTOPs);
            if (bb_CreatVStopsInChart)
            {
                if (lba_Modify[0]) fDraw_VirtSTOP (li_cmd, 0, bd_NewSL);
                if (lba_Modify[1]) fDraw_VirtSTOP (li_cmd, 1, bd_NewTP);
            }
        }
        //---- Помечаем "последствия" первичной модификации
        GlobalVariableSet (StringConcatenate (fi_Ticket, "_#STOP"), li_result);
    }
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fCreat_STOPs()", bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Расчитываем СТОПы с учётом уровней FIBO (заданные в настройках)            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCreat_LevelsByFIBO (double ar_Extrem[],       // массив экстремумов
                          double& ar_STOPs[],       // возвращаемый массив СТОПов
                          int ar_LevelFIBO[])       // массив номеров уровней (0 - для SL; 1 - для TP)
{
    int    li_CMD, li_Type = OrderType(), li_Level;
    double ld_Delta = (ar_Extrem[0] - ar_Extrem[1]);
//----
    for (int li_IND = 0; li_IND < 2; li_IND++)
    {
        if (li_Type == 0) {if (li_IND == 0) li_CMD = 1; else li_CMD = 0;} else {li_CMD = li_IND;}
        li_Level = MathAbs (ar_LevelFIBO[li_IND]);
        int li_cmd = 1;
        if (ar_LevelFIBO[li_IND] < 0) {if (li_CMD == 0) li_cmd = -1;} else {if (li_CMD == 1) li_cmd = -1;}
        if (li_Type == li_IND)
        {ar_STOPs[li_IND] = MathMin (ar_STOPs[li_IND], ar_Extrem[li_CMD] + li_cmd * ld_Delta * bda_FIBO[li_Level]);}
        else {ar_STOPs[li_IND] = MathMax (ar_STOPs[li_IND], ar_Extrem[li_CMD] + li_cmd * ld_Delta * bda_FIBO[li_Level]);}
        //if (li_IND == 0) Print (fGet_NameOP (li_Type), ": SL[", ar_LevelFIBO[li_IND], "] = ", DSD (ar_STOPs[li_IND]), " | HIGH = ", DSD (ar_Extrem[li_IND]));
        //if (li_IND == 1) Print (fGet_NameOP (li_Type), ": TP[", ar_LevelFIBO[li_IND], "] = ", DSD (ar_STOPs[li_IND]), " | LOW = ", DSD (ar_Extrem[li_IND]));
    }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Проверяем переданные в библиотеку внешние параметры.                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_STOPsParameters()
{
    int err = GetLastError();
//----
    if (N_STOPs == 0)
    {
        if (StopLoss < 0) {Print ("Поставьте StopLoss >= 0 !!!"); return (false);}
        if (TakeProfit < 0) {Print ("Поставьте TakeProfit >= 0 !!!"); return (false);}
    }
    else
    {
        if (LevelFIBO_SL < -2 || LevelFIBO_SL > 5) {Print ("Поставьте -2 <= LevelFIBO_SL <= 5 !!!"); return (false);}
        if (LevelFIBO_TP < -2 || LevelFIBO_TP > 5) {Print ("Поставьте -2 <= LevelFIBO_TP <= 5 !!!"); return (false);}
    }
    //---- Производим проверку на правильность задания TF
    if (TF_STOPs == 0) TF_STOPs = Period();
    if (fGet_NumPeriods (TF_STOPs) < 0) {return (false);}
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, "fCheck_STOPsParameters()", bi_indERR);
//----
     return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: ОБЩИХ ФУНКЦИЙ                                                             |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|         Получаем рабочие переменные в соответствии с разрядностью                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fSet_ValuesSTOPs()
{
    static string ls_Symbol = "";
//----
	 if (ls_Symbol != OrderSymbol())
	 {
		  ls_Symbol = OrderSymbol();
		  bd_SL = NDP (StopLoss);
		  bd_TP = NDP (TakeProfit);
		  bd_MIN_SL = NDP (MIN_StopLoss);
		  bd_MIN_TP = NDP (MIN_TakeProfit);
	 }
    bd_Spread = NDP (MarketInfo (bs_Symbol, MODE_SPREAD));
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Учитываем разрядность котировок                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCheck_DecimalSTOPs()
{
//----
    if (StopLoss > 0) StopLoss *= bi_Decimal;
    if (TakeProfit > 0) TakeProfit *= bi_Decimal;
    if (MIN_StopLoss > 0) MIN_StopLoss *= bi_Decimal;
    if (MIN_TakeProfit > 0) MIN_TakeProfit *= bi_Decimal;
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

