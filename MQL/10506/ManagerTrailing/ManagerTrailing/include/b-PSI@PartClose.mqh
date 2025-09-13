//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                               b-PSI@PartClose.mqh |
//|                                       Copyright © 2012, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//| 09.04.2012  Библиотека частичного закрытия ордеров.                               |
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
extern string Setup_PartClose       = "================== PartClose =================="; 
extern bool   PartClose_ON             = TRUE;          // Включение функции частичного закрытия для ордеров
extern string PartClose_Levels         = "20/50/30";    // Уровни закрытия или % прохождения цены до TP.
// При отсутствии у ордера TP. Например, при параметрах 10/20/30 первое закрытие выполняется при прохождении 
// ценой 10 пунктов, второе - при прохождении 20 пунктов и последнее при дрстижении 50 пунктов.
// Если у ордера есть TP, то его размер разбивается на части (части должны вместе составлять 100 %)
extern int    MoveBUInPart             = 1;             // На какой части закрытия двигаем SL в БезУбыток (0 - не двигаем)
extern string PartClose_Percents       = "50/25/25";    // Процент закрытия (через разделитель "/") для соответствующего уровня. Здесь отсчет идет от лота первого ордера. Если исходный ордер открыт с лотом 1.0 лот, закрывается 50% - 0.5, затем 25% от 1.0 - 0.3 и наконец 0.2
//IIIIIIIIIIIIIIIIIII=========Подключение внешних модулей=======IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII========Глобальные переменные модуля=======IIIIIIIIIIIIIIIIIIIIII+
int           bi_cntPartClose;
//IIIIIIIIIIIIIIIIIII===========Перечень функций модуля=========IIIIIIIIIIIIIIIIIIIIII+
//void fInit_PartClose()             // Инициализация модуля
//int fControl_PartClose (string fs_SymbolList,    // Лист управляемых валютных пар
                        //string fs_MagicList,     // Лист управляемых Магиков
                        //bool fb_Conditions,      // Условие включения (наличие ордеров)
                        //int fi_Slippage,         // проскальзывание
                        //bool fb_VirtualWork = false,// флаг виртуальной работы
                        //string fs_Delimiter = ",")// Разделитель переменных в листе fs_MagicList
                                     // Функция организует частичное закрытие ордеров
//int fPartClose (int fi_Ticket,                   // OrderTicket()
                //int fi_Slippage,                 // проскальзывание
                //bool fb_VirtualWork = false)     // флаг виртуальной работы
                                     // Производим частичное закрытие ордера по Ticket
                                     // Функция закрывает выделенный рыночный ордер
//int fGet_ParentTicket (string fs_Comment)        // OrderComment()
                                     // Получаем Ticket родительского ордера
//bool fCheck_PartCloseParameters()  // Проверяем переданные в библиотеку внешние параметры
//void fGet_CurLevels (int ar_PercentClose[],      // массив с процентами движения цены
                     //int fi_SizeTP,              // размер текущего TP в пп.
                     //int& ar_Levels[],           // возвращаемый массив уровней закрытия
                     //bool fb_Condition = true)   // условие на "процентный" расчёт
                                     // Получаем динамические уровни частичного закрытия ордера
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Инициализация модуля                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fInit_PartClose()
{
    string tmpArr[], lsa_txt[2];
    int    err = GetLastError();
//----
    //---- Переносим данные из строк в рабочие массивы
    if (PartClose_ON)
    {
        int lia_size[3];
        lia_size[1] = fSplitStrToStr (PartClose_Levels, tmpArr, "/");
        fCreat_StrToInt (tmpArr, bia_PartClose_Levels, lia_size[1], 1);
        lsa_txt[0] = "Закрываем ордера по частям в ";
        lsa_txt[1] = "при достижении ордером профита в ";
        lia_size[2] = fSplitStrToStr (PartClose_Percents, tmpArr, "/");
        //---- Проверяем размерность полученных массивов
        if (lia_size[1] != lia_size[2])
        {
            lia_size[0] = MathMin (lia_size[1], lia_size[2]);
            if (lia_size[0] != lia_size[1]) ArrayResize (bia_PartClose_Levels, lia_size[0]);
            if (lia_size[0] != lia_size[2]) ArrayResize (bia_PartClose_Percents, lia_size[0]);
            Print ("Размеры массивов не совпали - уменьшаем до ", lia_size[0], " !!!");
        }
        else lia_size[0] = lia_size[1];
        fCreat_StrToInt (tmpArr, bia_PartClose_Percents, lia_size[0], 1);
        for (int li_int = 0; li_int < lia_size[0]; li_int++)
        {
            if (li_int > 0) {bia_PartClose_Levels[li_int] += bia_PartClose_Levels[li_int-1];}
            lsa_txt[0] = StringConcatenate (lsa_txt[0], bia_PartClose_Percents[li_int], IIFs ((li_int == lia_size[0] - 1), " ", ", "));
            lsa_txt[1] = StringConcatenate (lsa_txt[1], bia_PartClose_Levels[li_int], IIFs ((li_int == lia_size[0] - 1), " пп.", ", "));
        }
        lsa_txt[0] = StringConcatenate (lsa_txt[0], " процентах от лота ", lsa_txt[1]);
        Print (lsa_txt[0]);
        //---- Производим проверку передаваемых в библиотеку значений
        if (!fCheck_PartCloseParameters())
        {
            Alert ("Проверьте параметры PartClose !!!");
            bb_OptimContinue = true;
            return (false);
        }
        //---- Добавляем в рабочий массив префиксы временных GV-перемнных
        string lsa_Array[] = {"_#Num","_#Lots","_#BU","_#OP"};
        fCreat_ArrayGV (bsa_prefGV, lsa_Array);
        bb_ClearGV = true;
        bi_cntPartClose = lia_size[0];
    }
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fInit_PartClose()", bi_indERR);
//----
    return (true);
    fControl_PartClose (Symbol(), 0, true, 2);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Организуем частичное закрытие ордеров                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fControl_PartClose (string fs_SymbolList,        // Лист управляемых валютных пар
                         string fs_MagicList,         // Лист управляемых Магиков
                         bool fb_Conditions,          // Условие включения (наличие ордеров)
                         int fi_Slippage,             // проскальзывание
                         bool fb_VirtualWork = false, // флаг виртуальной работы
                         string fs_Delimiter = ",")   // Разделитель переменных в листе fs_MagicList
{
    if (!PartClose_ON) return (false);
    if (!fb_Conditions) return (false);
    int    li_Type, err = GetLastError();
    double ld_Profit;
    string ls_Symbol, ls_fName = "fControl_PartClose()";
    bool lb_result = false;
//----
    bs_libNAME = "b-PartClose";
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {
        if (!OrderSelect (i, SELECT_BY_POS, MODE_TRADES)) continue;
        ls_Symbol = OrderSymbol();
        if (StringFind (fs_SymbolList, ls_Symbol) < 0 && StringLen (fs_SymbolList) > 0) continue;
        if (!fCheck_MyMagic (fs_MagicList, fs_Delimiter)) continue;
        li_Type = OrderType();
        if (li_Type > 1) continue;
        ld_Profit = OrderProfit() + OrderCommission() + OrderSwap();
        //---- Контролируем частичное закрытие выделенных ордеров
        if (ld_Profit > 0.0)
        {
            fGet_MarketInfo (ls_Symbol);
            if (fPartClose (OrderTicket(), fi_Slippage, fb_VirtualWork) >= 0) lb_result = true;
        }
    }
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, ls_fName, bi_indERR);
//----
    return (lb_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Производим частичное закрытие ордера по Ticket                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fPartClose (int fi_Ticket,                // OrderTicket()
                int fi_Slippage,              // проскальзывание
                bool fb_VirtualWork = false)  // флаг виртуальной работы
{
    int    li_ParentTicket = 0, li_cmd, li_Num = 0, li_Type = OrderType(),
           err = GetLastError(), lia_PartClose_Levels[];
    double ld_Lots = OrderLots(), ld_LotsClose, ld_Price, ld_BU, ld_OpenPrice = OrderOpenPrice();
    bool   lb_result = false, lb_close = false, lb_modify = false, lb_BU = false;
    string ls_close, ls_Profit, ls_Name, ls_Comment = OrderComment(), ls_Symbol = OrderSymbol();
//----
    li_ParentTicket = fGet_ParentTicket (ls_Comment);
    if (li_ParentTicket != 0)
    {
        ls_Name = StringConcatenate (li_ParentTicket, "_#Num");
        if (GlobalVariableCheck (ls_Name)) {li_Num = GlobalVariableGet (ls_Name) + 1;}
        ls_Name = StringConcatenate (li_ParentTicket, "_#Lots");
        if (GlobalVariableCheck (ls_Name)) {ld_Lots = GlobalVariableGet (ls_Name);}
        ls_Name = StringConcatenate (li_ParentTicket, "_#OP");
        if (GlobalVariableCheck (ls_Name)) {ld_OpenPrice = GlobalVariableGet (ls_Name);}
        ls_Name = StringConcatenate (li_ParentTicket, "_#BU");
        lb_BU = GlobalVariableCheck (ls_Name);
    }
    else {lb_BU = false;}
    if (li_Num >= bi_cntPartClose) {li_Num = bi_cntPartClose - 1;}
    GlobalVariableSet (StringConcatenate (fi_Ticket, "_#Num"), li_Num);
    GlobalVariableSet (StringConcatenate (fi_Ticket, "_#Lots"), ld_Lots);
    GlobalVariableSet (StringConcatenate (fi_Ticket, "_#OP"), ld_OpenPrice);
    bd_curTP = fGet_ValueFromGV (StringConcatenate (fi_Ticket, "_#VirtTP"), OrderTakeProfit(), fb_VirtualWork);
    //---- Формируем уровни закрытия
    int li_SizeTP = NDPD (MathAbs (bd_curTP - ld_OpenPrice));
    fGet_CurLevels (bia_PartClose_Levels, li_SizeTP, lia_PartClose_Levels, bd_curTP > 0.0);
    RefreshRates();
    ld_Price = NDD (fGet_TradePrice (li_Type, bb_RealTrade, ls_Symbol));
    if (li_Type == OP_BUY) li_cmd = 1; else li_cmd = -1;
    //---- Проверяем условия на закрытие очередной части ордера
    if (li_cmd * (ld_Price - OrderOpenPrice()) >= NDP (lia_PartClose_Levels[li_Num]))
    {
        ld_LotsClose = fLotsNormalize ((bia_PartClose_Percents[li_Num] * ld_Lots / 100.0));
        if (ld_LotsClose > 0.0)
        {
            ld_LotsClose = MathMin (ld_LotsClose, OrderLots());
            //---- Если последняя часть - закрываемся полностью
            if (li_Num + 1 == ArraySize (bia_PartClose_Percents)) ld_LotsClose = OrderLots();
            lb_close = true;
        }
    }
    //---- Закрываем часть выделенного ордера
    if (lb_close)
    {
        //---- После MooveBUInPart части закрытия переводим оставшуюся часть в БУ
        if (MoveBUInPart > 0)
        {
            if (!lb_BU && li_Num + 1 >= MoveBUInPart)
            {
                ld_BU = NDD (OrderOpenPrice() + li_cmd * (bd_ProfitMIN + bd_Spread));
                //---- Если есть чего изменять
                if (NDD (OrderStopLoss() - ld_BU) != 0.0)
                {
                    int li_result = fOrderModify (fi_Ticket, OrderOpenPrice(), ld_BU, OrderTakeProfit(), 0, Gold);
                    if (li_result == 1) {GlobalVariableSet (StringConcatenate (fi_Ticket, "_#BU"), ld_BU);}
                    if (li_result >= 0) fSet_Comment (bdt_curTime, fi_Ticket, 51, "", li_result != 0);
                    if (li_result == 0) return (-1);
                }
            }
        }
        lb_result = fOrderClose (fi_Ticket, ld_LotsClose, ld_Price, fi_Slippage, White);
        fSet_Comment (li_Num, fi_Ticket, 52, "", lb_result, li_ParentTicket, lia_PartClose_Levels[li_Num]);
        if (!lb_result) return (li_Num); else return (-1);
    }
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fPartClose()", bi_indERR);
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Получаем Ticket родительского ордера                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_ParentTicket (string fs_Comment)
{
    int li_N1 = StringFind (fs_Comment, "from");
//----
    if (li_N1 >= 0)
    {
        int li_N2 = StringFind (fs_Comment, "#");
        if (li_N2 > li_N1)
        {return (StrToInteger (StringSubstr (fs_Comment, li_N2 + 1, StringLen (fs_Comment) - li_N2 - 1)));}
    }
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Проверяем переданные в библиотеку внешние параметры.                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_PartCloseParameters()
{
    int err = GetLastError(), li_Percent = 0;
//----
    for (int li_int = 0; li_int < ArraySize (bia_PartClose_Percents); li_int++)
    {li_Percent += bia_PartClose_Percents[li_int];}
    if (li_Percent != 100) {Print ("Сумма процентов частичного закрытия (PartClose_Percents) должна == 100 %"); return (false);}
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, "fCheck_PartCloseParameters()", bi_indERR);
//----
     return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Получаем динамические уровни частичного закрытия ордера                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_CurLevels (int ar_PercentClose[],    // массив с процентами движения цены
                     int fi_SizeTP,            // размер текущего TP в пп.
                     int& ar_Levels[],         // возвращаемый массив уровней закрытия
                     bool fb_Condition = true) // условие на "процентный" расчёт
{
//----
    ArrayResize (ar_Levels, bi_cntPartClose);
    for (int li_IND = 0; li_IND < bi_cntPartClose; li_IND++)
    {
        if (fb_Condition) ar_Levels[li_IND] = fi_SizeTP * ar_PercentClose[li_IND] / 100.0;
        else ar_Levels[li_IND] = ar_PercentClose[li_IND] * bi_Decimal;
    }
//----
} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

