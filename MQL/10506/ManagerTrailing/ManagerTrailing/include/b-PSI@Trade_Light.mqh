//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                              b-PSI@TradeLight.mqh |
//|                                       Copyright © 2012, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//| 17.03.2012  Библиотека торговых операций (облегчённый вариант).                   |
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
#define WM_COMMAND  0x0111
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                 *****        Параметры библиотеки         *****                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern int   Slippage             = 2;             // Проскальзывание
extern int   NumberOfTry          = 10;            // Количество попыток на совершение торговых операций
/*extern*/ color colSend_BUY      = Blue;
/*extern*/ color colSend_SELL     = Red;
/*extern*/ color colClose_BUY     = Green;
/*extern*/ color colClose_SELL    = Magenta;
/*extern*/ color colModify_BUY    = Gold;
/*extern*/ color colModify_SELL   = Aqua;
/*extern*/ //bool Semaphore_ON      = false;
//IIIIIIIIIIIIIIIIIII========Глобальные переменные модуля=======IIIIIIIIIIIIIIIIIIIIII+
color        bca_Send[2],                          // массив цветов для ордеров при открытии
             bca_Close[2],                         // массив цветов для ордеров при закрытии\удалении
             bca_Modify[2];                        // массив цветов для ордеров при модификации
//IIIIIIIIIIIIIIIIIII=========Подключение внешних модулей=======IIIIIIIIIIIIIIIIIIIIII+
//#include     <b-PSI@MineGV.mqh>                    // Библиотека работы с базовыми GV-переменными
//#include     <b-PSI@Time.mqh>                      // Библиотека функций работы со временем
#include     <b-PSI@Comment.mqh>                   // Библиотека работы с комментариями
#import "user32.dll"
    int GetAncestor (int hWnd, int gaFlags);
    int PostMessageA (int hWnd, int Msg, int wParam, string lParam);
#import
//IIIIIIIIIIIIIIIIIII===========Перечень функций модуля=========IIIIIIIIIIIIIIIIIIIIII+
//void fInit_Trade()                 // Инициализация модуля
//int fOrderSend (string fs_Symbol,                // OrderSymbol()
                //int fi_Type,                     // OrderType()
                //double fd_Lot,                   // OrderLots()
                //double fd_Price,                 // OrderOpenPrice()
                //int fi_Slip,                     // Slippage
                //double fd_SL,                    // OrderStopLoss()
                //double fd_TP,                    // OrderTakeProfit()
                //string fs_Comm = "",             // OrderComment()
                //int fi_MG = 0,                   // OrderMagicNumber()
                //datetime fdt_Expiration = 0);    // Срок истечения отложенного ордера
                                     // Функция устанавливает ордера
//int fOrderModify (int fi_Ticket,                 // OrderTicket()
                  //double fd_NewOpenPrice,        // OpenPrice
                  //double fd_NewSL,               // новый StopLoss
                  //double fd_NewTP)               // новый TakeProfit
                  //datetime fdt_Expiration = 0,   // Время истечения отложенного ордера
                  //color fc_Arrow = CLR_NONE)     // Цвет стрелок модификации StopLoss и/или TakeProfit на графике
                                     // Функция модифицирует\устанавливает СТОПы у выделенного ордера
//bool fOrderClose (int fi_Ticket,                 // OrderTicket()
                  //double fd_Lots,                // OrderLots()
                  //double fd_Price,               // текущая цена
                  //int fi_Slippage,               // проскальзывание
                  //color fc_Arrow = CLR_NONE)     // Цвет стрелки закрытия на графике
                                     // Функция закрывает выделенный рыночный ордер
//bool fOrderDelete (int fi_Ticket)                // OrderTicket()
                   //color fc_Arrow = CLR_NONE)    // Цвет стрелки на графике
                                     // Функция удаляет выделенный отложенный ордер
//int fClose_AllOrders (string fs_SymbolList,      // Лист управляемых валютных пар
                      //string fs_MagicList,       // Лист управляемых Магиков
                      //double& fd_Pribul,         // Возвращаемый профит закрытых ордеров
                      //int fi_Type = -1,          // тип закрываемых ордеров
                      //int fi_NBars_Life = 0,     // минимальная "жизнь" ордера в барах на fi_Period: 0 - параметр не учитывается
                      //int fi_Period = 0,         // Период
                      //int fi_OrderProfit = 0)    // Идентификатор профитности ордера: > 0 - профитный; < 0 - лоссовый
                                     // Функция закрытия "своих" ордеров по признаку
//bool fControl_MAXLife (int fi_curLife,           // текущая "продолжительность жизни" ордера
                       //int fi_MAXLife = -1)      // максимальная жизнь ордера в барах (0 - до конца текущих суток)
                                     // Организуем закрытие по МАКС жизни ордера
//bool fCheck_TypeOrder (int fi_Type,              // -1 - все; -2 - рыночные; 6 - отложенные
                       //int fi_CheckType)         // OrderType
                                     // Организуем фильтр типов ордеров
//bool fCheck_MyMagic (string fs_List, string fs_Delimiter = ",")
                                     // Проверяем из строки валидность Магика
//bool fCheck_ValidStops (double fd_Price,         // текущая цена
                        //int fi_Type,             // OrderType()
                        //double& fd_SL,           // StopLoss
                        //double& fd_TP,           // TakeProfit
                        //bool fb_IsNewOrder = true)// открытие ордера
                                     // Проверяем правильность задания СТОПов
//void fCheck_ValidSTOPOrders (string fs_Symbol,   // OrderSymbol()
                             //int fi_Type,        // OrderType()
                             //double& fd_OpenPrice,// OrderOpenPrice()
                             //double& fd_SL,      // StopLoss
                             //double& fd_TP,      // TakeProfit
                             //double fd_Price,    // Текущая цена по инструменту
                             //bool& fb_FixInvalidPrice,// флаг первоначальной коррекции
                             //bool fb_IsNewOrder = false)// открытие ордера
                                     // Проверяем правильность цены открытия у отложенных ордеров
//void fGet_MarketInfo (string fs_Symbol, int fi_Ticket)
                                     // Получаем торговую информацию по символу и текущему ордеру
//bool fGet_OrderDetails (int fi_Ticket)           // OrderTicket()
                                     // Получаем актуальную информацию о текущем ордере
//|***********************************************************************************|
//| РАЗДЕЛ: Работа с ошибками                                                         |
//|***********************************************************************************|
//bool fErrorHandling (int fi_Error, bool& fb_InvalidSTOP)
                                     // Функция обрабатывает ошибки
//bool fCheck_LevelsBLOCK (int fi_Mode,            // Тип проводимой операции: 1 - Close/Del; 2 - Send; 3 - Modify;
                         //string fs_Symbol,       // OrderSymbol()
                         //int fi_Type,            // OrderType()
                         //double& fd_NewOpenPrice,// OpenPrice
                         //double& fd_NewSL,       // StopLoss
                         //double& fd_NewTP,       // TakeProfit
                         //bool& fb_FixInvalidPrice)// флаг первоначальной коррекции отложки
                                     // Проверка корректности на FREEZELEVEL и STOPLEVEL
//|***********************************************************************************|
//| РАЗДЕЛ: Сервисных функций                                                         |
//|***********************************************************************************|
//void fReConnect()                  // Сканирование серверов при разрыве связи
//void fWrite_Log (string fs_Txt)    // Пишем Log-файл
//void fPrintAndShowComment (string& Text,         // Возвращаемая пустая строка
                           //bool Show_Conditions, // Разрешение на показ комментов на графике
                           //bool Print_Conditions,// Разрешение на печать комментов
                           //string& s_Show[],     // Взвращаемый массив комментов
                           //int ind = -1)         // индекс ячейки массива, куда внесли изменения
                                     // Выводим на печать и на график комментарии
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|        Инициализация модуля                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fInit_Trade()
{
//----
    bi_Error = GetLastError();
    bca_Send[0] = colSend_BUY;
    bca_Send[1] = colSend_SELL;
    bca_Close[0] = colClose_BUY;
    bca_Close[1] = colClose_SELL;
    bca_Modify[0] = colModify_BUY;
    bca_Modify[1] = colModify_SELL;
    Slippage *= bi_Decimal;
    if (NumberOfTry == 0) NumberOfTry = 200;
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fInit_Trade()", bi_indERR);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|        Функция устанавливает ордера                                               |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fOrderSend (string fs_Symbol,             // OrderSymbol()
                int fi_Type,                  // OrderType()
                double fd_Lot,                // OrderLots()
                double fd_Price,              // OrderOpenPrice()
                int fi_Slip,                  // Slippage
                double fd_SL,                 // OrderStopLoss()
                double fd_TP,                 // OrderTakeProfit()
                string fs_Comm = "",          // OrderComment()
                int fi_MG = 0,                // OrderMagicNumber()
                datetime fdt_Expiration = 0,  // Срок истечения отложенного ордера
                color fc_Arrow = CLR_NONE)    // Цвет открывающей стрелки на графике
{
    int    li_cnt = 0, li_Ticket = -1, cmd;
    bool   lb_InvalidSTOP = false, lb_FixInvalidPrice = false;
//----
    bs_libNAME = "b-Trade_Light";
    //---- Получаем актуальную информацию по символу
    fGet_MarketInfo (fs_Symbol);
    //---- Проверяем на условия FREEZELEVEL и STOPLEVEL
    if (!fCheck_LevelsBLOCK (2, fs_Symbol, fi_Type, fd_Price, fd_SL, fd_TP, lb_FixInvalidPrice))
    {if (StringLen (bs_ErrorTL) > 0) {fWrite_Log (bs_ErrorTL, bi_indERR);} return (-1);}
    bi_Error = GetLastError();
    fd_Price = NDD (fd_Price);
    if (fc_Arrow == CLR_NONE) fc_Arrow = bca_Send[fi_Type % 2];
    if (!bb_MMM) fd_Lot = MathMin (fd_Lot, bd_MAXLOT);
    else if (fd_Lot > bd_MAXLOT)
    {fSet_Comment (bdt_curTime, 0, 7, "", True, fd_Lot);}
    double ld_Lots = MathMin (fd_Lot, bd_MAXLOT);
    if (!bb_RealTrade)
    {
        //---- Предусматриваем открытие нескольких ордеров с максимальным лотом (экстремальный трейдинг)
        while (fd_Lot > 0.0)
        {
            li_Ticket = OrderSend (fs_Symbol, fi_Type, ld_Lots, fd_Price, fi_Slip, 0, 0, fs_Comm, fi_MG, fdt_Expiration, fc_Arrow);
            if (li_Ticket > 0)
            {  
                if (fd_SL != 0.0 || fd_TP != 0.0)
                {
                    double ld_SL = 0.0, ld_TP = 0.0;
                    if (OrderSelect (li_Ticket, SELECT_BY_TICKET))
                    {fOrderModify (li_Ticket, OrderOpenPrice(), fd_SL, fd_TP, fdt_Expiration, fc_Arrow);}
                }
            }
            else fWrite_Log (StringConcatenate ("fOrderSend(): ", fOrderErrTxt (GetLastError())), bi_indERR);
            fd_Lot -= bd_MAXLOT;
            if (fd_Lot <= 0.0) return (li_Ticket);
            ld_Lots = MathMin (bd_MAXLOT, fd_Lot);
            bi_cntTrades++;
        }
    }
    else
    {
        if (fi_Type % 2 == OP_BUY) {cmd = 1;} else {cmd = 0;}
        //---- Предусматриваем открытие нескольких ордеров с максимальным лотом (экстремальный трейдинг)
        while (fd_Lot > 0.0)
        {
            while (IsTradeAllowed() == true)
            {
                if (!IsExpertEnabled() || IsStopped() || li_cnt > 200)
                {
                    fWrite_Log (StringConcatenate ("Error: Trying to send order ", fGet_NameOP (fi_Type), " | Price: ", DSD (fd_Price), " NOT IsTradeContextBusy"), bi_indERR);
                    if (!IsExpertEnabled()) {fWrite_Log ("Permit ExpertEnabled !!!", bi_indERR);}
                    return (-1);
                }
                li_Ticket = OrderSend (fs_Symbol, fi_Type, ld_Lots, fd_Price, fi_Slip, 0, 0, fs_Comm, fi_MG, fdt_Expiration, fc_Arrow);
                if (li_Ticket == -1)
                {
                    bi_Error = GetLastError();
                    if (fErrorHandling (bi_Error, lb_InvalidSTOP)) {return (-1);}
				        fWrite_Log (StringConcatenate ("Error Occured : ", ErrorDescription (bi_Error)), bi_indERR);
				        fWrite_Log (StringConcatenate ("fOrderSend(): ", fs_Symbol, "/", fGet_NameOP (fi_Type), " | Price = ", DSD (fd_Price)), bi_indERR);
                    li_cnt++;
                    if (NumberOfTry < li_cnt) return (-1);
                    RefreshRates();
                    if (fi_Type < 2) {fd_Price = NDD (fGet_TradePrice (cmd, true, fs_Symbol));}
                    //---- Проводим коррекцию цены открытия для отложенных ордеров
                    else {fCheck_ValidSTOPOrders (fs_Symbol, fi_Type, fd_Price, fd_SL, fd_TP, fGet_TradePrice (cmd, true, fs_Symbol), lb_FixInvalidPrice, true);}
                    continue;
                }
                else {break;}
            }
            if (li_Ticket > 0 && (fd_SL != 0.0 || fd_TP != 0.0))
            {
                li_cnt = 0;
                if (OrderSelect (li_Ticket, SELECT_BY_TICKET))
                {                  
                    if (fi_Type % 2 == 0) {cmd = 1;} else {cmd = -1;}
                    fOrderModify (li_Ticket, OrderOpenPrice(), fd_SL, fd_TP, fdt_Expiration, fc_Arrow);
                }
                else {fWrite_Log (StringConcatenate ("OrderSelectError :: ", GetLastError()), bi_indERR);}
            }
            fd_Lot -= bd_MAXLOT;
            if (fd_Lot <= 0.0) break;
            ld_Lots = MathMin (bd_MAXLOT, fd_Lot);
            bi_cntTrades++;
            GlobalVariableSet (StringConcatenate (bs_NameGV, "_#cntTrades"), bi_cntTrades);
        }
    }
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fOrderSend()", bi_indERR);
//----
    return (li_Ticket);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|        Функция модифицирует\устанавливает СТОПы у выделенного ордера              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fOrderModify (int fi_Ticket,               // OrderTicket()
                  double fd_OpenPrice,         // OpenPrice
                  double fd_NewSL,             // новый StopLoss (УРОВЕНЬ !!!)
                  double fd_NewTP,             // новый TakeProfit (УРОВЕНЬ !!!)
                  datetime fdt_Expiration = 0, // Время истечения отложенного ордера
                  color fc_Arrow = CLR_NONE)   // Цвет стрелок модификации StopLoss и/или TakeProfit на графике
{
    //---- Проверяем необходимость модификации
    if (NDD (fd_NewSL) == OrderStopLoss() && NDD (fd_NewTP) == OrderTakeProfit()) return (false);
//----
    int    li_cnt = 0;
    double ld_Price;
    bool   lb_result = false, lb_InvalidSTOP = false, lb_FixInvalidPrice = false;
//----
    bs_libNAME = "b-Trade_Light";
    //---- Получаем актуальную информацию по символу и текущему ордеру
    fGet_MarketInfo (OrderSymbol(), fi_Ticket);
    //---- Проверяем на условия FREEZELEVEL и STOPLEVEL
    if (!fCheck_LevelsBLOCK (3, bs_Symbol, bi_Type, fd_OpenPrice, fd_NewSL, fd_NewTP, lb_FixInvalidPrice))
    {if (StringLen (bs_ErrorTL) > 0) {fWrite_Log (bs_ErrorTL, bi_indERR);} return (-1);}
    bi_Error = GetLastError();
    //---- Определяем цвет значков модификации ордеров
    if (fc_Arrow == CLR_NONE) fc_Arrow = bca_Modify[OrderType() % 2];
    //---- Выполняем модификацию в тестере
    if (!bb_RealTrade)
    {
        lb_result = OrderModify (fi_Ticket, fd_OpenPrice, fd_NewSL, fd_NewTP, fdt_Expiration, fc_Arrow);
        if (!lb_result) fWrite_Log (StringConcatenate ("fOrderModify(): ", fOrderErrTxt (GetLastError())), bi_indERR);
        return (lb_result);
    }
    //---- Выполняем модификацию в on-line торговле
    while (IsTradeAllowed() == true)
    {
        if (!IsExpertEnabled() || IsStopped() || li_cnt > 200)
        {
            fWrite_Log (StringConcatenate ("Error: Trying to modify ticket #", fi_Ticket, ", which is ", fGet_NameOP (bi_Type), " NOT IsTradeContextBusy"), bi_indERR);
            if (!IsExpertEnabled()) {fWrite_Log ("Permit ExpertEnabled !!!", bi_indERR);}
            return (-1);
        }
        if (OrderModify (fi_Ticket, fd_OpenPrice, fd_NewSL, fd_NewTP, fdt_Expiration, fc_Arrow)) {lb_result = true; break;}
        bi_Error = GetLastError();
        if (fErrorHandling (bi_Error, lb_InvalidSTOP)) break;
		  fWrite_Log (StringConcatenate ("Error Occured : ", ErrorDescription (bi_Error)), bi_indERR);
		  fWrite_Log (StringConcatenate ("fOrderModify(): ", bs_Symbol, "# ", fi_Ticket, "/", fGet_NameOP (bi_Type), " | Price = ", DSD (fd_OpenPrice), " | SL = ", DSD (fd_NewSL), " | TP = ", DSD (fd_NewTP)), bi_indERR);
        if (lb_InvalidSTOP)
        {
            RefreshRates();
            ld_Price = fGet_TradePrice (bi_Type % 2, true, bs_Symbol);
            if (bi_Type < 2) {fCheck_ValidStops (ld_Price, bi_Type, fd_NewSL, fd_NewTP, false);}
            else {fCheck_ValidSTOPOrders (bs_Symbol, bi_Type, fd_OpenPrice, fd_NewSL, fd_NewTP, ld_Price, lb_FixInvalidPrice);}
        }
        li_cnt++;
        if (NumberOfTry < li_cnt) return (false);
    }
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fOrderModify()", bi_indERR);
//----
    return (lb_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|        Функция закрывает выделенный рыночный ордер                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fOrderClose (int fi_Ticket,              // OrderTicket()
                  double fd_Lots,             // OrderLots()
                  double fd_Price,            // текущая цена
                  int fi_Slippage,            // проскальзывание
                  color fc_Arrow = CLR_NONE)  // Цвет стрелки закрытия на графике
{
    //---- Закрываем ТОЛЬКО рыночные ордера
    if (OrderType() > 1) return (false);
    bool   lb_result = false, lb_InvalidSTOP = false;
//----
    bs_libNAME = "b-Trade_Light";
    if (fc_Arrow == CLR_NONE) fc_Arrow = bca_Close[OrderType()];
    //---- Производим закрытие ордера в тестере
    if (!bb_RealTrade)
    {
        lb_result = OrderClose (fi_Ticket, fd_Lots, NDD (fd_Price), fi_Slippage, fc_Arrow);
        if (!lb_result) fWrite_Log (StringConcatenate ("fOrderClose(): ", fOrderErrTxt (GetLastError())), bi_indERR);
        return (lb_result);
    }
    int li_cnt = 0;
    //---- Получаем актуальную информацию по символу и текущему ордеру
    fGet_MarketInfo (OrderSymbol(), fi_Ticket);
    //---- Проверяем на условия FREEZELEVEL
    if (!fCheck_LevelsBLOCK (1, bs_Symbol, bi_Type, fd_Price, bd_curSL, bd_curTP, lb_InvalidSTOP))
    {if (StringLen (bs_ErrorTL) > 0) {fWrite_Log (bs_ErrorTL, bi_indERR);} return (false);}
    bi_Error = GetLastError();
    //---- Производим закрытие ордера в on-line работе
    while (IsTradeAllowed() == true)
    {
        if (!IsExpertEnabled() || IsStopped() || li_cnt > 200)
        {
            fWrite_Log (StringConcatenate ("Error: Trying to close ticket #", fi_Ticket, ", which is ", fGet_NameOP (bi_Type), " NOT IsTradeContextBusy"), bi_indERR);
            if (!IsExpertEnabled()) {fWrite_Log ("Permit ExpertEnabled !!!", bi_indERR);}
            return (false);
        }
        if (OrderClose (fi_Ticket, fd_Lots, NDD (fd_Price), Slippage, fc_Arrow))
        {lb_result = true; break;}
        bi_Error = GetLastError();
        if (fErrorHandling (bi_Error, lb_InvalidSTOP)) break;
		  fWrite_Log (StringConcatenate ("Error Occured : ", ErrorDescription (bi_Error)), bi_indERR);
		  fWrite_Log (StringConcatenate ("fOrderClose(): ", bs_Symbol, "# ", fi_Ticket, "/", fGet_NameOP (bi_Type), " | Lots = ", DSDig (fd_Lots), " | Price = ", DSD (fd_Price), " | SL = ", DSD (bd_curSL), " | TP = ", DSD (bd_curTP)), bi_indERR);
        li_cnt++;
        if (NumberOfTry < li_cnt) return (false);
        RefreshRates();
        fd_Price = fGet_TradePrice (bi_Type, true, bs_Symbol);
    }
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fOrderClose()", bi_indERR);
//----
    return (lb_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|        Функция удаляет выделенный отложенный ордер                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fOrderDelete (int fi_Ticket,              // OrderTicket()
                   color fc_Arrow = CLR_NONE)  // Цвет стрелки на графике
{
    //---- Удаляем ТОЛЬКО отложенные ордера
    if (OrderType() < 2) return (false);
    bool lb_result = false, lb_InvalidSTOP = false;
//----
    bs_libNAME = "b-Trade_Light";
    if (fc_Arrow == CLR_NONE) fc_Arrow = bca_Close[OrderType() % 2];
    //---- Производим удаление ордера в тестере
    if (!bb_RealTrade)
    {
        lb_result = OrderDelete (fi_Ticket, fc_Arrow);
        if (!lb_result) fWrite_Log (StringConcatenate ("fOrderDelete(): ", fOrderErrTxt (GetLastError())), bi_indERR);
        return (lb_result);
    }
    int    li_cnt = 0;
    double ld_SL, ld_TP, ld_Price;
    //---- Получаем актуальную информацию по символу и текущему ордеру
    fGet_MarketInfo (OrderSymbol(), fi_Ticket);
    //---- Проверяем на условия FREEZELEVEL
    if (!fCheck_LevelsBLOCK (1, bs_Symbol, fi_Ticket, ld_Price, ld_SL, ld_TP, lb_InvalidSTOP)) return (false);
    bi_Error = GetLastError();
    //---- Производим удаление ордера в on-line работе
    while (IsTradeAllowed() == true)
    {
        if (!IsExpertEnabled() || IsStopped() || li_cnt > 200)
        {
            fWrite_Log (StringConcatenate ("Error: Trying to close ticket #", fi_Ticket, ", which is ", fGet_NameOP (bi_Type), " NOT IsTradeContextBusy"), bi_indERR);
            if (!IsExpertEnabled()) {fWrite_Log ("Permit ExpertEnabled !!!", bi_indERR);}
            return (false);
        }
        if (OrderDelete (fi_Ticket, fc_Arrow)) {lb_result = true; break;}
        bi_Error = GetLastError();
        if (fErrorHandling (bi_Error, lb_InvalidSTOP)) break;
		  fWrite_Log (StringConcatenate ("Error Occured : ", ErrorDescription (bi_Error)), bi_indERR);
		  fWrite_Log (StringConcatenate ("fOrderDelete(): ", OrderSymbol(), "# ", fi_Ticket, "/", fGet_NameOP (bi_Type), " | Lots = ", DSDig (OrderLots())), bi_indERR);
        li_cnt++;
        if (NumberOfTry < li_cnt) return (false);
    }
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fOrderDelete()", bi_indERR);
//----
    return (lb_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|        Функция закрытия ордеров                                                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fClose_AllOrders (string fs_SymbolList,      // Лист управляемых валютных пар
                      string fs_MagicList,       // Лист управляемых Магиков
                      double& fd_Pribul,         // Возвращаемый профит закрытых ордеров
                      int fi_Type = -1,          // тип закрываемых ордеров
                      int fi_NBars_Life = 0,     // минимальная "жизнь" ордера в барах на fi_Period: 0 - параметр не учитывается
                      int fi_Period = 0,         // Период
                      int fi_TypeProfit = 0)     // Идентификатор профитности ордера: > 0 - профитный; < 0 - лоссовый
{
    int li_Total = OrdersTotal();
    if (li_Total < 1) return (0);
//----
    int    li_Type, li_ord = 0, li_Digits, li_Ticket, li_cnt;
    double ld_ClosePrice, ld_Profit;
    string ls_Symbol;
//----
    bs_libNAME = "b-Trade_Light";
    fd_Pribul = 0.0;
    bi_Error = GetLastError();
    for (int li_ORD = li_Total - 1; li_ORD >= 0; li_ORD--)
    {
        if (!OrderSelect (li_ORD, SELECT_BY_POS)) continue;
        ls_Symbol = OrderSymbol();
        if (StringFind (fs_SymbolList, ls_Symbol) < 0) {if (StringLen (fs_SymbolList) > 0) continue;}
        if (!fCheck_MyMagic (fs_MagicList, bs_Delimiter)) continue;
        li_Type = OrderType();         
        if (!fCheck_TypeOrder (fi_Type, li_Type)) continue;
        //---- Контролируем "жизнь" ордера
        if (fi_NBars_Life > 0)
        {if (fi_NBars_Life >= iBarShift (Symbol(), fi_Period, OrderOpenTime())) continue;}
        li_Ticket = OrderTicket();
        //fGet_MarketInfo (ls_Symbol);
        if (li_Type < 2)
        {
            //---- Фильтруем ордера по профитности
            if (fi_TypeProfit != 0)
            {
                ld_Profit = OrderProfit() + OrderSwap() + OrderCommission();
                if (fi_TypeProfit > 0 && ld_Profit < 0) continue;
                if (fi_TypeProfit < 0 && ld_Profit > 0) continue;
            }
            RefreshRates();
            ld_ClosePrice = fGet_TradePrice (li_Type, bb_RealTrade, ls_Symbol);
            if (fOrderClose (li_Ticket, OrderLots(), ld_ClosePrice, Slippage))
            {
                li_ord++;
                if (!OrderSelect (li_Ticket, SELECT_BY_TICKET, MODE_HISTORY)) continue;
                fd_Pribul += (OrderProfit() + OrderSwap() + OrderCommission());
                continue;
            }
        }
        else {if (fOrderDelete (li_Ticket)) li_ord++;}
     }
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fClose_AllOrders()", bi_indERR);
//----
    return (li_ord);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|        Организуем закрытие по МАКС жизни ордера                                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fControl_MAXLife (int fi_curLife,        // текущая "продолжительность жизни" ордера
                       int fi_MAXLife = -1)   // максимальная жизнь ордера в барах (0 - до конца текущих суток)
{        
    if (fi_MAXLife < 0) return (false);
//----
    int li_LifeBars;
//----
    if (fi_MAXLife > 0) {li_LifeBars = fi_MAXLife - fi_curLife;}
    //---- Максимум ордер живёт до конца текущих суток
    else {li_LifeBars = 1 - iBarShift (bs_Symbol, PERIOD_D1, OrderOpenTime());}
    if (li_LifeBars <= 0)
    {
        int li_Ticket = OrderTicket();
        bool lb_result = fOrderClose (li_Ticket, OrderLots(), bda_Price[OrderType()], Slippage, Aqua);
        fSet_Comment (bdt_curTime, li_Ticket, 40, "MAXLife", lb_result, fi_MAXLife, fi_curLife);
        if (lb_result) return (true);
    }
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|        Организуем фильтр типов ордеров                                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_TypeOrder (int fi_Type,      // -1 - все; -2 - рыночные; 7 - отложенные
                       int fi_CheckType) // OrderType
{
//----
    if (fi_Type >= 0)
    {
        if (fi_Type == 7) {if (fi_CheckType < 2) return (false);}
        else {if (fi_Type != fi_CheckType) return (false);}
    }
    else {if (fi_Type == -2) {if (fi_CheckType > 1) return (false);}}
    
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|       Проверяем из строки валидность Магика                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_MyMagic (string fs_List, string fs_Delimiter = ",")
{
    int li_Len = StringLen (fs_List);
//----
    if (li_Len == 0) return (true);
    //---- Отрицательный Магик подразумевает "любой"
    if (fs_List == "-1") return (true);
    string ls_Magic = OrderMagicNumber();
    //---- Определяем номер начальной позиции Магика в строке
    int li_N = StringFind (fs_List, ls_Magic);
    //---- Если Магик в искомой строке не найден
    if (li_N < 0) return (false);
    //---- Если строка сотоит ТОЛЬКО из нашего Магика
    if (fs_List == ls_Magic) return (true);
    //---- Определяем длину Магика
    int li_LenMagic = StringLen (ls_Magic);
    //---- После Магика в строке должен стоять разделитель (Delimiter)
    if (StringSubstr (fs_List, li_N + li_LenMagic, 1) == fs_Delimiter) return (true);
    //---- Если Магик стоит в конце строки
    if (li_Len == li_N + li_LenMagic) return (true);
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fCheck_MyMagic()", bi_indERR);
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|       Проверяем правильность задания СТОПов                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_ValidStops (double fd_Price,           // текущая цена
                        int fi_Type,               // OrderType()
                        double& fd_SL,             // StopLoss
                        double& fd_TP,             // TakeProfit
                        bool fb_IsNewOrder = true) // открытие ордера
{
    string ls_NAME = "fCheck_ValidStops", lsa_txt[2] = {"",""}, ls_Symbol = bs_Symbol;
    if (!fb_IsNewOrder) ls_Symbol = OrderSymbol(); 
    double ld_OrigSL = fd_SL, ld_OrigTP = fd_TP, ld_NewSL, ld_NewTP;
    bool   lb_result = True;
//----
    bi_Error = GetLastError();
    //---- Если SL не 0
    if (fd_SL != 0.0)
    {
        if (fi_Type % 2 == OP_BUY)
        {
            ld_NewSL = fd_Price - bd_STOPLEVEL;
            lsa_txt[0] = StringConcatenate (ls_NAME, ": SL = ", DSD (ld_OrigSL), " | new SL [", DSD (ld_NewSL), "] = price [", DSD (fd_Price), "] - MinSTOP [", NDPD (bd_STOPLEVEL), "]");
            if (fb_IsNewOrder)
            {
                ld_NewSL -= bd_Spread;
                lsa_txt[1] = StringConcatenate (ls_NAME, ": Minus spread [", DSD (bd_Spread), "]");
            }
            fd_SL = MathMin (fd_SL, ld_NewSL);
            if (!fb_IsNewOrder)
            {
                if (OrderStopLoss() != 0.0)
                {if (NDD (OrderStopLoss() - fd_SL) >= 0.0) lb_result = false;}
            }
        }
        else
        {
            ld_NewSL = fd_Price + bd_STOPLEVEL;
            lsa_txt[0] = StringConcatenate (ls_NAME, ": SL = ", DSD (ld_OrigSL), " | new SL [", DSD (ld_NewSL), "] = price [", DSD (fd_Price), "] + MinSTOP [", NDPD (bd_STOPLEVEL), "]");
            if (fb_IsNewOrder)
            {
                ld_NewSL += bd_Spread;
                lsa_txt[1] = StringConcatenate (ls_NAME, ": Plus spread [", DSD (bd_Spread), "]");
            }
            fd_SL = MathMax (fd_SL, ld_NewSL);
            if (!fb_IsNewOrder)
            {
                if (OrderStopLoss() != 0.0)
                {if (NDD (fd_SL - OrderStopLoss()) >= 0.0) lb_result = false;}
            }
        }
        fd_SL = NDD (fd_SL);
    }
    //---- Если TP не 0
    if (fd_TP != 0.0)
    {
        //---- Проверяем соответствие TP STOPLEVEL
        if (MathAbs (fd_Price - fd_TP) <= bd_STOPLEVEL)
        {
            if (fi_Type % 2 == OP_BUY)
            {
                ld_NewTP = fd_Price + bd_STOPLEVEL;
                fd_TP = MathMax (fd_TP, ld_NewTP);
            }
            else
            {
                ld_NewTP = fd_Price - bd_STOPLEVEL;
                fd_TP = MathMin (fd_TP, ld_NewTP);
            }
            fd_TP = NDD (fd_TP);
            lb_result = true;
        }
    }
    //---- Уведомляем об произведённых изменениях
    if (NDD (fd_SL - ld_OrigSL) != 0.0)
    {
        fWrite_Log (StringConcatenate (ls_NAME, ": Symbol = ", ls_Symbol, " | MinSTOP = ", NDPD (bd_STOPLEVEL), " | spread = ", NDPD (bd_Spread))); 
        fWrite_Log (lsa_txt[0]);
        fWrite_Log (lsa_txt[1]);
        fWrite_Log (StringConcatenate (ls_NAME, ": SL был меньше STOPLEVEL (", NDPD (bd_STOPLEVEL), "). \nПередвинули SL на: ", DSD (fd_SL)), 6);
    }
    if (NDD (fd_TP - ld_OrigTP) != 0.0)
    {fWrite_Log (StringConcatenate (ls_NAME, ": TP был меньше STOPLEVEL (", NDPD (bd_STOPLEVEL), "). \nПередвинули TP на: ", DSD (fd_TP)), 6);}
    //---- Нормализуем СТОПы
    if (fd_SL > 0.0) fd_SL = NDD (fd_SL);
    if (fd_TP > 0.0) fd_TP = NDD (fd_TP);
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, ls_NAME, bi_indERR);
//----
    return (lb_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|       Проверяем правильность цены открытия у отложенных ордеров                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCheck_ValidSTOPOrders (string fs_Symbol,           // OrderSymbol()
                             int fi_Type,                // OrderType()
                             double& fd_OpenPrice,       // OrderOpenPrice()
                             double& fd_SL,              // StopLoss
                             double& fd_TP,              // TakeProfit
                             double fd_Price,            // Текущая цена по инструменту
                             bool& fb_FixInvalidPrice,   // флаг первоначальной коррекции
                             bool fb_IsNewOrder = false) // открытие ордера
{
    string ls_NAME = "fCheck_ValidSTOPOrders()";
    int 	  li_cmd = 1;
    double ld_OrigSL = fd_SL, ld_OrigTP = fd_TP, ld_NewTP, ld_PriceOld;
//----
    bi_Error = GetLastError();
    //fCheck_ValidSTOPOrders (fs_Symbol, li_Type, ld_Price, SL, TP, lb_FixInvalidPrice);
    if (fi_Type == OP_BUYSTOP || fi_Type == OP_BUYLIMIT)
    {
        if (fi_Type == OP_BUYLIMIT) {li_cmd = -1;}
        if (MathAbs (fd_Price - fd_OpenPrice) <= bd_STOPLEVEL)
        {
            if (fb_FixInvalidPrice)
            {
                fd_OpenPrice = fd_OpenPrice + li_cmd * bd_SymPoint;
                if (fd_SL > 0.0) {fd_SL = fd_SL + li_cmd * bd_SymPoint;}
                if (fd_TP > 0.0) {fd_TP = fd_TP + li_cmd * bd_SymPoint;}
                if (fd_SL > 0.0 || fd_TP > 0.0)
                {fWrite_Log (StringConcatenate (ls_NAME, ": CHANGE[", fGet_NameOP (fi_Type), "]: SL (now ", DSD (fd_SL), ") | TP (now ", DSD (fd_TP), ")."), 6);}
            }
            else
            {
                ld_PriceOld = fd_OpenPrice;
                fd_OpenPrice = fd_Price + li_cmd * bd_STOPLEVEL;
                if (fd_SL > 0.0) {fd_SL += (fd_OpenPrice - ld_PriceOld);}
                if (fd_TP > 0.0) {fd_TP += (fd_OpenPrice - ld_PriceOld);}
                if (fd_SL > 0.0 || fd_TP > 0.0)
                {fWrite_Log (StringConcatenate (ls_NAME, ": CHANGE[", fGet_NameOP (fi_Type), "]: new Price = ", DSD (fd_OpenPrice), " | SL (now ", DSD (fd_SL), ") | TP (now ", DSD (fd_TP), ")."), 6);}
                fb_FixInvalidPrice = true;
            }
            fCheck_ValidStops (fd_OpenPrice, fi_Type, fd_SL, fd_TP, fb_IsNewOrder);
        }
    }
    else if (fi_Type == OP_SELLSTOP || fi_Type == OP_SELLLIMIT)
    {
        if (fi_Type == OP_SELLSTOP) {li_cmd = -1;}
        if (MathAbs (fd_Price - fd_OpenPrice) <= bd_STOPLEVEL)
        {
            if (fb_FixInvalidPrice)
            {
                fd_OpenPrice = fd_OpenPrice + li_cmd * bd_SymPoint;
                if (fd_SL > 0.0) {fd_SL = fd_SL + li_cmd * bd_SymPoint;}
                if (fd_TP > 0.0) {fd_TP = fd_TP + li_cmd * bd_SymPoint;}
                if (fd_SL > 0.0 || fd_TP > 0.0)
                {fWrite_Log (StringConcatenate (ls_NAME, ": CHANGE[", fGet_NameOP (fi_Type), "]: SL (now ", DSD (fd_SL), ") | TP (now ", DSD (fd_TP), ")."), 6);}
            }
            else
            {
                if (fi_Type == OP_SELLSTOP) ld_PriceOld = fd_OpenPrice;
                fd_OpenPrice = fd_Price + li_cmd * bd_STOPLEVEL;
                if (fd_SL > 0.0) {fd_SL -= (ld_PriceOld - fd_OpenPrice);}
                if (fd_TP > 0.0) {fd_TP -= (ld_PriceOld - fd_OpenPrice);}
                if (fd_SL > 0.0 || fd_TP > 0.0)
                {fWrite_Log (StringConcatenate (ls_NAME, ": CHANGE[", fGet_NameOP (fi_Type), "]: new Price = ", DSD (fd_OpenPrice), " | SL (now ", DSD (fd_SL), ") | TP (now ", DSD (fd_TP), ")."), 6);}
                fb_FixInvalidPrice = true;
            }
            fCheck_ValidStops (fd_OpenPrice, fi_Type, fd_SL, fd_TP, fb_IsNewOrder);
        }
    }
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, ls_NAME, bi_indERR);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|         Получаем торговую информацию по символу                                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_MarketInfo (string fs_Symbol, int fi_Ticket = 0)
{
//----
	 if (fs_Symbol != bs_Symbol || fi_Ticket < 0)
	 {
		  if (fi_Ticket > 0) {bs_Symbol = OrderSymbol();} else {bs_Symbol = fs_Symbol;}
		  if (bs_Symbol == Symbol())
		  {
		      bi_SymDigits = Digits;
		      bd_SymPoint = Point;
		  }
		  else
		  {
		      bi_SymDigits = MarketInfo (fs_Symbol, MODE_DIGITS);
		      bd_SymPoint = MarketInfo (fs_Symbol, MODE_POINT);
		  }
        if (bd_SymPoint == 0.0) {bd_SymPoint = fGet_Point (fs_Symbol);}
		  bd_ProfitMIN = NDP (ProfitMIN_Pips);
	 }
	 if (fi_Ticket > 0) {fGet_OrderDetails (fi_Ticket);}
    //---- Получаем текущие цены по инструменту
    RefreshRates();
    bda_Price[0] = NDD (fGet_TradePrice (0, bb_RealTrade, bs_Symbol));
    bda_Price[1] = NDD (fGet_TradePrice (1, bb_RealTrade, bs_Symbol));
    bd_Spread = NDD (bda_Price[1] - bda_Price[0]);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|       Получаем актуальную информацию об ордере и о Символе                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fGet_OrderDetails (int fi_Ticket)              // OrderTicket()
{
//----
    //---- На текущий момент может быть выделенным другой ордер
    int li_LastTicket = OrderTicket();
    if (li_LastTicket != fi_Ticket)
    {   if (!OrderSelect (fi_Ticket, SELECT_BY_TICKET))
        {fSet_Comment (fi_Ticket, fi_Ticket, 100, "fGet_OrderDetails()", True, GetLastError()); return (false);}
    }
    //bs_Symbol = OrderSymbol();
    bi_Type = OrderType();
    bd_OpenPrice = NDD (OrderOpenPrice());
    bd_curSL = NDD (OrderStopLoss());
    bd_curTP = NDD (OrderTakeProfit());
    //---- Выделяем первоначальный ордер
    if (li_LastTicket > 0) {if (li_LastTicket != fi_Ticket) OrderSelect (li_LastTicket, SELECT_BY_TICKET);}
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Работа с ошибками                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|        Функция обрабатывает ошибки                                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fErrorHandling (int fi_Error, bool& fb_InvalidSTOP)
{   
//----
    if (fi_Error == 0) return (true);
    switch (fi_Error)
    {
        case 4:   /*ERR_SERVER_BUSY*/
        case 137: /*ERR_BROKER_BUSY*/
        case 139: /*ERR_ORDER_LOCKED*/
        case 146: /*ERR_TRADE_CONTEXT_BUSY*/ Sleep (500); return (false);
        case 6:   /*ERR_NO_CONNECTION*/ fReConnect(); Sleep (1000); return (false);
        case 135: /*ERR_PRICE_CHANGED*/ 
        case 136: /*ERR_OFF_QUOTES*/
        case 138: /*ERR_REQUOTE*/ Sleep (1); return (false);
        case 129: /*ERR_INVALID_PRICE*/
        case 130: /*ERR_INVALID_STOPS*/ fb_InvalidSTOP = true; return (false);
        case 4109: /*ERR_TRADE_NOT_ALLOWED*/
            Print ("TRADE NOT ALLOWED ! SWITCH ON option \' Allow live trading\' (Необходимо включить опцию \'Разрешить советнику торговать\')");
            return (true);
        default: fWrite_Log (StringConcatenate (OrderTicket(), ": Критическая ошибка № ", ErrorDescription (fi_Error)), bi_indERR); return (true);
    }
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY                                                                  |
//+-----------------------------------------------------------------------------------+
//|        Проверка корректности на FREEZELEVEL и STOPLEVEL                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_LevelsBLOCK (int fi_Mode,              // Тип проводимой операции: 1 - Close/Del; 2 - Send; 3 - Modify;
                         string fs_Symbol,         // OrderSymbol()
                         int fi_Type,              // OrderType()
                         double& fd_NewOpenPrice,  // OpenPrice
                         double& fd_NewSL,         // StopLoss
                         double& fd_NewTP,         // TakeProfit
                         bool& fb_FixInvalidPrice) // флаг первоначальной коррекции отложки
{
    double ld_Price, ld_FreezeLevel;
    int    li_cmd, cmd, li_Ticket = 0;
    bool   lb_Freeze = false, lb_MinSTOP = false, lb_NewOrder = false;
//----
    bi_Error = GetLastError();
    //fCheck_LevelsBLOCK (1, fs_Symbol, li_Type, ld_Price, ld_SL, ld_TP, lb_FixInvalidPrice);
    if (fi_Mode == 2) lb_NewOrder = true;
    if (!lb_NewOrder) li_Ticket = OrderTicket();
    //---- Получаем актуальные данные по символу, а также данные об ордере (если открыт)
    //fGet_OrderDetails (li_Ticket, lb_NewOrder);
    bd_STOPLEVEL = NDP (MarketInfo (fs_Symbol, MODE_STOPLEVEL));
    ld_FreezeLevel = NDP (MarketInfo (fs_Symbol, MODE_FREEZELEVEL));
    if (fi_Type == 0) li_cmd = 0; else if (fi_Type == 1) li_cmd = 1;
    else if (fi_Type % 2 == 0) li_cmd = 1; else li_cmd = 0;
    ld_Price = bda_Price[li_cmd];
    //---- Нормализуем СТОПы
    if (fd_NewSL > 0.0) fd_NewSL = NDD (fd_NewSL);
    if (fd_NewTP > 0.0) fd_NewTP = NDD (fd_NewTP);
    //---- Проверяем условия сервера на FREEZELEVEL
    if (fi_Mode == 1 || fi_Mode == 3)
    {
        if (fi_Type < 2)
        {
            if (fi_Type == 0) cmd = 1; else cmd = -1;
            lb_Freeze = ((cmd * (ld_Price - bd_curSL) > ld_FreezeLevel || bd_curSL == 0) && (cmd * (bd_curTP - ld_Price) > ld_FreezeLevel || bd_curTP == 0));
        }
        else {lb_Freeze = (MathAbs (ld_Price - bd_OpenPrice) > ld_FreezeLevel);}
        if (!lb_Freeze)
        {
            bs_ErrorTL = StringConcatenate (fGet_NameOP (fi_Type), "[", OrderTicket(), "]: Не выполнены условия сервера на FreezeLevel !!!");
            return (false);
        }
        //---- Нет СТОПов - ХОРОШО на Close\Delete
        if (fi_Mode == 1) {if (bd_curSL == 0 && bd_curTP == 0) return (true);}
        //---- При попытке произвести модификацию уже имеющимися значениями
        //if (fi_Mode == 3) {if (bd_curSL == fd_NewSL && bd_curTP == fd_NewTP) return (false);}
    }
    //---- Нет СТОПов - ХОРОШО на Send
    else {if (fd_NewSL == 0 && fd_NewTP == 0) return (true);}
    //---- Проверяем условия сервера на STOPLEVEL
    switch (fi_Mode)
    {
        case 1: //---- Close\Delete orders
            return (true);
        case 2: //---- Send orders
            switch (fi_Type)
            {
                case 0: //---- Send BUY
                    lb_MinSTOP = ((bd_STOPLEVEL <= ld_Price - fd_NewSL || fd_NewSL == 0)
                    && (bd_STOPLEVEL <= fd_NewTP - ld_Price || fd_NewTP == 0));
                    break; 
                case 1: //---- Send SELL
                    lb_MinSTOP = ((bd_STOPLEVEL <= fd_NewSL - ld_Price || fd_NewSL == 0)
                    && (bd_STOPLEVEL <= ld_Price - fd_NewTP || fd_NewTP == 0));
                    break;
                case 2: //---- Send BUYLIMIT
                    if ((bd_STOPLEVEL <= ld_Price - fd_NewOpenPrice)
                    && (bd_STOPLEVEL <= fd_NewOpenPrice - fd_NewSL || fd_NewSL == 0)
                    && (bd_STOPLEVEL <= fd_NewTP - fd_NewOpenPrice || fd_NewTP == 0))
                    lb_MinSTOP = true;
                    break;  
                case 3: //---- Send SELLLIMIT
                    if ((bd_STOPLEVEL <= fd_NewOpenPrice - ld_Price)
                    && (bd_STOPLEVEL <= fd_NewSL - fd_NewOpenPrice || fd_NewSL == 0)
                    && (bd_STOPLEVEL <= fd_NewOpenPrice - fd_NewTP || fd_NewTP == 0))
                    lb_MinSTOP = true;
                    break;
                case 4: //---- Send BUYSTOP
                    if ((bd_STOPLEVEL <= fd_NewOpenPrice - ld_Price)
                    && (bd_STOPLEVEL <= fd_NewOpenPrice - fd_NewSL || fd_NewSL == 0)
                    && (bd_STOPLEVEL <= fd_NewTP - fd_NewOpenPrice || fd_NewTP == 0))
                    lb_MinSTOP = true;
                    break;
                case 5: //---- Send SELLSTOP
                    if ((bd_STOPLEVEL <= ld_Price - fd_NewOpenPrice)
                    && (bd_STOPLEVEL <= fd_NewSL - fd_NewOpenPrice || fd_NewSL == 0)
                    && (bd_STOPLEVEL <= fd_NewOpenPrice - fd_NewTP || fd_NewTP == 0))
                    lb_MinSTOP = true;
                    break;
            }
            break;
        case 3: //---- Modify orders
            switch (fi_Type)
            {
                case 0: //---- Modify BUY
                    lb_MinSTOP = ((bd_STOPLEVEL <= ld_Price - fd_NewSL || fd_NewSL == 0)
                    && (bd_STOPLEVEL <= fd_NewTP - ld_Price || fd_NewTP == 0));
                    if (fd_NewSL >= ld_Price) return (false);
                    break; 
                case 1: //---- Modify SELL
                    lb_MinSTOP = ((bd_STOPLEVEL <= fd_NewSL - ld_Price || fd_NewSL == 0)
                    && (bd_STOPLEVEL <= ld_Price - fd_NewTP || fd_NewTP == 0));
                    if (fd_NewSL <= ld_Price) return (false);
                    break;
                case 2: //---- Modify BUYLIMIT
                    if ((bd_STOPLEVEL <= bd_OpenPrice - bd_curSL || bd_curSL == 0)
                    && (bd_STOPLEVEL <= bd_curTP - bd_OpenPrice || bd_curTP == 0))
                    {
                        if ((bd_STOPLEVEL <= ld_Price - fd_NewOpenPrice)
                        && (bd_STOPLEVEL <= fd_NewOpenPrice - fd_NewSL || fd_NewSL == 0)
                        && (bd_STOPLEVEL <= fd_NewTP - fd_NewOpenPrice || fd_NewTP == 0))
                        lb_MinSTOP = true;
                    }
                    break;                 
                case 3: //---- Modify SELLLIMIT
                    if ((bd_STOPLEVEL <= bd_curSL - bd_OpenPrice || bd_curSL == 0)
                    && (bd_STOPLEVEL <= bd_OpenPrice - bd_curTP || bd_curTP == 0)) 
                    {
                        if ((bd_STOPLEVEL <= fd_NewOpenPrice - ld_Price)
                        && (bd_STOPLEVEL <= fd_NewSL - fd_NewOpenPrice || fd_NewSL == 0)
                        && (bd_STOPLEVEL <= fd_NewOpenPrice - fd_NewTP || fd_NewTP == 0))
                        lb_MinSTOP = true;
                    }
                    break;   
                case 4: //---- Modify BUYSTOP
                    if ((bd_STOPLEVEL <= bd_OpenPrice - bd_curSL || bd_curSL == 0)
                    && (bd_STOPLEVEL <= bd_curTP - bd_OpenPrice || bd_curTP == 0))  
                    {
                        if ((bd_STOPLEVEL <= fd_NewOpenPrice - ld_Price)
                        && (bd_STOPLEVEL <= fd_NewOpenPrice - fd_NewSL || fd_NewSL == 0)
                        && (bd_STOPLEVEL <= fd_NewTP - fd_NewOpenPrice || fd_NewTP == 0))
                        lb_MinSTOP = true;
                    }
                    break; 
                case 5: //---- Modify SELLSTOP
                    if ((bd_STOPLEVEL <= bd_curSL - bd_OpenPrice || bd_curSL == 0)
                    && (bd_STOPLEVEL <= bd_OpenPrice - bd_curTP || bd_curTP == 0)) 
                    {
                        if ((bd_STOPLEVEL <= ld_Price - fd_NewOpenPrice)
                        && (bd_STOPLEVEL <= fd_NewSL - fd_NewOpenPrice || fd_NewSL == 0)
                        && (bd_STOPLEVEL <= fd_NewOpenPrice - fd_NewTP || fd_NewTP == 0))
                        lb_MinSTOP = true;
                    }
                    break;
            }
    }
    //---- Если не выполняются условия STOPLEVEL
    if (!lb_MinSTOP)
    {
        if (fi_Type < 2)
        {
            //---- На Send меняем цены местами
            if (lb_NewOrder)
            {
                if (fi_Type == 0) li_cmd = 1; else if (fi_Type == 1) li_cmd = 0;
                else if (fi_Type % 2 == 0) li_cmd = 0; else li_cmd = 1;
            }
            if (fd_NewSL != 0.0) if (li_cmd * (ld_Price - fd_NewSL) < bd_STOPLEVEL)
            {bs_ErrorTL = StringConcatenate ("STOPLEVEL[", NDPD (bd_STOPLEVEL), "] > for NewSL[", li_cmd * NDPD (ld_Price - fd_NewSL), "]");}
            if (fd_NewTP != 0.0) if (li_cmd * (fd_NewTP - ld_Price) < bd_STOPLEVEL)
            {bs_ErrorTL = StringConcatenate ("STOPLEVEL[", NDPD (bd_STOPLEVEL), "] > for NewTP[", li_cmd * NDPD (fd_NewTP - ld_Price), "]");}
            fWrite_Log (StringConcatenate (fGet_NameOP (fi_Type), ": ", bs_ErrorTL));
            //---- Корректируем СТОПы у рыночных ордеров
            if (!fCheck_ValidStops (bda_Price[li_cmd], fi_Type, fd_NewSL, fd_NewTP, lb_NewOrder))
            {fWrite_Log ("fCheck_ValidStops(): Изменить СТОПы не получилось !!!"); return (false);}
            else return (true);
        }
        else
        {
            //---- Корректируем цену открытия (или модификации) и СТОПы у отложенных ордеров
            fCheck_ValidSTOPOrders (fs_Symbol, fi_Type, fd_NewOpenPrice, fd_NewSL, fd_NewTP, ld_Price, fb_FixInvalidPrice, lb_NewOrder);
            return (true);
        }
    }
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fCheck_LevelsBLOCK()", bi_indERR);
//----
    return (lb_MinSTOP);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Сервисных функций                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|          Сканирование серверов при разрыве связи                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fReConnect()
{
    //---- Получаем Handle терминала
    int hMetaTrader = GetAncestor (WindowHandle (Symbol(), Period()), 2);
    //---- Команда пересканировать все сервера
    if (hMetaTrader != 0) {PostMessageA (hMetaTrader, WM_COMMAND, 37400, NULL);} 
    return;
}   
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       Печатаем ошибку                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fOrderErrTxt (int err)
{bi_Error = err; return (StringConcatenate ("ERROR № ", err, "  ::  ", ErrorDescription (err)));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

