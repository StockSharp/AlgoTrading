//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                            b-PSI@LEVELs_Light.mqh |
//|                                    Copyright © 2011-12, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//|   24.05.2012  Библиотека расчёта уровней.                                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|   Данный продукт предназначен для некомерческого использования. Публикация разре- |
//|шена только при указании имени автора (TarasBY). Редактирование исходного кода до- |
//|пустима только при условии сохранения данного текста, ссылок и имени автора.  Про- |
//|дажа библиотеки или отдельных её частей ЗАПРЕЩЕНА.                                 |
//|   Автор не несет ответственности за возможные убытки, полученные в результате ис- |
//|пользования библиотеки.                                                            |
//|   По всем вопросам, связанным с работой библиотеки, замечаниями или предложениями |
//|по её доработке обращаться на Skype: TarasBY или e-mail.                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
#property copyright "Copyright © 2008-12, TarasBY WM R418875277808; Z670270286972"
#property link      "taras_bulba@tut.by"
//IIIIIIIIIIIIIIIIIII==================CONSTANS=================IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII==========Внешние параметры модуля=========IIIIIIIIIIIIIIIIIIIIII+
extern string Setup_EExtremum       = "---------------N1 from Extremum ---------------";
extern int    E.cnt_Bars               = 3;                // количество баров для поиска экстремума
extern string Setup_EATR            = "------------------N2 from ATR -----------------";
extern int    E.ATR_Period1            = 5;                // период первого ATR (больше 0; может быть равен ATR_Period2, но лучше отличен от последнего)
extern int    E.ATR_Period2            = 15;               // период второго ATR (больше 0)
extern double E.ATR_coeff              = 2.0;              // коэффициент "волатильности" инструмента
extern string Setup_EZZ             = "----------------N3 from ZigZag ----------------";
extern double E.ChannelPercent         = 0.6;
//IIIIIIIIIIIIIIIIIII=========Подключение внешних модулей=======IIIIIIIIIIIIIIIIIIIIII+

//IIIIIIIIIIIIIIIIIII========Глобальные переменные модуля=======IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII===========Перечень функций модуля=========IIIIIIIIIIIIIIIIIIIIII+
/*bool fInit_LEVELs (int fi_N,                     // Номер варианта расчёта экстремумов
                     string fs_Name = "N_STOPs")*/ // имя библиотеки
                                     // Инициализация модуля
/*bool fCheck_LEVELsParameters (int fi_N,          // Номер варианта расчёта экстремумов
                                string fs_Name = "N_STOPs")*/// имя библиотеки
                                     // Проверяем переданные в функцию внешние параметры
//|***********************************************************************************|
//| РАЗДЕЛ: ФУНКЦИИ РАССЧЁТА LEVELs                                                   |
//|***********************************************************************************|
/*bool fGet_Extremum (double& ar_Extrem[],         // возвращаемый массив экстремумов
                      string fs_Symbol = "",       // Symbol
                      int fi_TF = 0,               // TF
                      int fi_cntBars = 1)*/        // количество просматриваемых баров (от 0)
                                     // Получаем в массив экстремумы за fi_cntBars баров на fi_TF периоде.
/*bool fGet_ATR (double& ar_Extrem[],              // возвращаемый массив экстремумов
                 string fs_Symbol = "",            // Symbol
                 int fi_TF = 0,                    // TF
                 int fi_cntBars = 2)*/             // количество просматриваемых баров (от 0)
                                     // Получаем в массив экстремумы по ATR за fi_cntBars баров на fi_TF периоде.
/*bool fGet_ZZP (double& ar_Extrem[],              // возвращаемый массив пиков
                 string fs_Symbol = "",            // Symbol
                 int fi_TF = 0)*/                  // TF
                                     // Получаем в массив экстр. по ZZP за fi_cntBars баров на fi_TF периоде.
//|***********************************************************************************|
//| РАЗДЕЛ: ОБЩИХ ФУНКЦИЙ                                                             |
//|***********************************************************************************|
//         Получаем в массив LEVELs                                                   |
/*bool fGet_LEVELs (int fi_N,                      // Номер способа получения экстремумов
                    int fi_TF,                     // таймфрейм
                    double& ar_Extremum[])*/       // возвращаемый массив с экстремумами
                                     // Получаем в массив LEVELs
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Инициализация модуля                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fInit_LEVELs (int fi_N,                    // Номер варианта расчёта экстремумов
                   string fs_Name = "N_STOPs")  // имя библиотеки
{
//----
    //---- Производим проверку передаваемых в библиотеку значений
    if (!fCheck_LEVELsParameters (fi_N, fs_Name))
    {
        Alert ("Проверьте параметры выбранного Вами способа расчёта уровней !!!");
        bb_OptimContinue = true;
        return (false);
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, "fInit_LEVELs()", bi_indERR);
//----
    return (true);
    double ld_Extr[2];
    fGet_LEVELs (0, 0, ld_Extr);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Проверяем переданные в функцию внешние параметры.                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_LEVELsParameters (int fi_N,                    // Номер варианта расчёта экстремумов
                              string fs_Name = "N_STOPs")  // имя библиотеки
{
    int err = GetLastError();
//----
    switch (fi_N)
    {
        case 0: break;
        case 1:
            if (E.cnt_Bars < 1) {Print ("Поставьте E.cnt_Bars >= 1 !!!"); return (false);}
            break;
        case 2:
            if (E.cnt_Bars < 1) {Print ("Поставьте E.cnt_Bars >= 1 !!!"); return (false);}
            if (E.ATR_Period1 > E.ATR_Period2) {Print ("Поставьте E.ATR_Period2 >= E.ATR_Period1 !!!"); return (false);}
            if (E.ATR_coeff <= 0) {Print ("Поставьте E.ATR_coeff > 0 !!!"); return (false);}
            break;
        case 3:
            if (E.ChannelPercent <= 0 || E.ChannelPercent >= 1) {Print ("Поставьте 0 < E.ChannelPercent < 1 !!!"); return (false);}
            break;
        default: Print ("Поставьте 0 <= ", fs_Name, " >= 3 !!!"); return (false);
    }
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, "fCheck_LEVELsParameters()", bi_indERR);
//----
     return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: ФУНКЦИИ РАССЧЁТА УРОВНЕЙ (ЭКСТРЕМУМОВ)                                    |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Получаем в массив экстремумы за fi_cntBars баров на fi_TF периоде.         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fGet_Extremum (double& ar_Extrem[],      // возвращаемый массив экстремумов
                    string fs_Symbol = "",    // Symbol
                    int fi_TF = 0,            // TF
                    int fi_cntBars = 1)       // количество просматриваемых баров (от 0)
                    
{
    if (fs_Symbol == "") fs_Symbol = Symbol();
//----
    int li_BarLow = iLowest (fs_Symbol, fi_TF, MODE_LOW, fi_cntBars),
        li_BarHigh = iHighest (fs_Symbol, fi_TF, MODE_HIGH, fi_cntBars);
//----
    ArrayInitialize (ar_Extrem, EMPTY_VALUE);
    ar_Extrem[0] = iHigh (fs_Symbol, fi_TF, li_BarHigh);
    ar_Extrem[1] = iLow (fs_Symbol, fi_TF, li_BarLow);
    //---- Производим проверку на корректность полученных значений
    for (int li_IND = 0; li_IND < 2; li_IND++)
    {
        if (ar_Extrem[li_IND] <= 0.0) return (false);
        if (ar_Extrem[li_IND] == EMPTY_VALUE) return (false);
    }
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Получаем в массив экстремумы по ATR за fi_cntBars баров на fi_TF периоде.  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fGet_ATR (double& ar_Extrem[],      // возвращаемый массив экстремумов
               string fs_Symbol = "",    // Symbol
               int fi_TF = 0,            // TF
               int fi_cntBars = 2)       // количество просматриваемых баров (от 0)
{
    if (fs_Symbol == "") fs_Symbol = Symbol();
//----
    double ld_ATR = iATR (fs_Symbol, fi_TF, E.ATR_Period1, 1);
    ld_ATR = MathMax (ld_ATR, iATR (fs_Symbol, fi_TF, E.ATR_Period2, 1)) * E.ATR_coeff;
//----
    ArrayInitialize (ar_Extrem, EMPTY_VALUE);
    fGet_Extremum (ar_Extrem, fs_Symbol, fi_TF, fi_cntBars);
    ar_Extrem[0] += ld_ATR;
    ar_Extrem[1] -= ld_ATR;
    //---- Производим проверку на корректность полученных значений
    for (int li_IND = 0; li_IND < 2; li_IND++)
    {
        if (ar_Extrem[li_IND] <= 0.0) return (false);
        if (ar_Extrem[li_IND] == EMPTY_VALUE) return (false);
    }
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Получаем в массив экстр. по ZZP за fi_cntBars баров на fi_TF периоде.      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fGet_ZZP (double& ar_Extrem[],     // возвращаемый массив пиков
               string fs_Symbol = "",   // Symbol
               int fi_TF = 0)           // TF
{
    int li_Bar = 0;
//----
    if (fs_Symbol == "") fs_Symbol = Symbol();
    ArrayInitialize (ar_Extrem, EMPTY_VALUE);
    //---- Находим вершину
    while (ar_Extrem[0] == EMPTY_VALUE)
    {
        li_Bar++;
        ar_Extrem[0] = iCustom (fs_Symbol, fi_TF, "XLab_ZZP", E.ChannelPercent, 0, li_Bar);
    }
    //Print ("HIGH[", TimeToStr (Time[li_Bar]), "] = ", DSD (ar_Extrem[0]));
    li_Bar = 0;
    //---- Находим впадину
    while (ar_Extrem[1] == EMPTY_VALUE)
    {
        li_Bar++;
        ar_Extrem[1] = iCustom (fs_Symbol, fi_TF, "XLab_ZZP", E.ChannelPercent, 1, li_Bar);
    }
    //Print ("LOW[", TimeToStr (Time[li_Bar]), "] = ", DSD (ar_Extrem[1]));
    //---- Производим проверку на корректность полученных значений
    for (int li_IND = 0; li_IND < 2; li_IND++)
    {
        if (ar_Extrem[li_IND] <= 0.0) return (false);
        if (ar_Extrem[li_IND] == EMPTY_VALUE) return (false);
    }
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
//         Получаем в массив LEVELs                                                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fGet_LEVELs (int fi_N,               // Номер способа получения экстремумов
                  int fi_TF,              // таймфрейм
                  double& ar_Extremum[])  // возвращаемый массив с экстремумами
{
//----
    switch (fi_N)
    {
        //---- Экстремумы на fi_Period за E.cnt_Bars барах
        case 1: if (!fGet_Extremum (ar_Extremum, bs_Symbol, fi_TF, E.cnt_Bars)) return (false); break;
        //---- Уровни по ATR на fi_Period
        case 2: if (!fGet_ATR (ar_Extremum, bs_Symbol, fi_TF, E.cnt_Bars)) return (false); break;
        //---- Уровни по ZZP (ZigZag) на fi_Period
        case 3: if (!fGet_ZZP (ar_Extremum, bs_Symbol, fi_TF)) return (false); break;
    }
//----
    return (true);
}