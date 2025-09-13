//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                               b-PSI@ICManager.mqh |
//|                                                Copyright © 2010-11, Vipro&TarasBY |
//|                                                                taras_bulba@tut.by |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
#property copyright "Vipro&TarasBY"
#property link      "taras_bulba@tut.by"

/*
 * При Var_CalculateLots = 0
 * Если открывается ведущая позиция объемом gd_MinLot * (BaseBalance / Balance_ForMinLot), то на оставшиеся средства
 * открывается ведомая позиция из расчета gd_MinLot на каждый Balance_ForMinLot. При открытии большей
 * или меньшей ведущей позиции
 * 
 * Например, при параметрах по умолчанию, пусть общий баланс равен 10000, gd_MinLot = 0.01.
 * Тогда cредства инвесторов составляют 10000 - 3000 = 7000 = 7 * Balance_ForMinLot
 * Если управляющий открывает позицию на 0.03 лота, то автоматически откроется ведомая позиция на 0.07 лота.
 * Если управляющий открывает позицию на 0.06 лота, то автоматически откроется ведомая позиция на 0.14 лота.
 *
 * При Var_CalculateLots = 1
 * Расчёт ведётся по формуле: InvestLot = IC * Lots / BaseBalance * K
 * К находим по формуле: K = ResultTP / BaseBalance;
 * Если результаты стратегии за отчётный период < 0, то InvestLot = Lots;
 * При ResultTP == 0, K = fd_K_Begin (настраиваемая величина) (я ставлю fd_K_Begin = 0.5)
 */
//IIIIIIIIIIIIIIIIIII==================CONSTANS=================IIIIIIIIIIIIIIIIIIIIII+
#define MAX_ORDERS  100
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                   *****         Параметры модуля         *****                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern string   SETUP_ICExpert     = "============= Общие настройки модуля ============";
extern int      Magic			        = 454;                 // Рабочий магик для ведомых позиций
extern double   Balance_ForMinLot	  = 1000;                // Размер баланса для торговли минимальным лотом
extern double   BaseBalance		     = 3000;                // Капитал управляющего
extern datetime BeginTrade            = D'2011.01.01 00:00'; // Начало работы счёта
extern double   K_Begin               = 0.5;                 // Начальный коэффициент формирования лота для ведомых ордеров
extern int      Var_CalculateLots     = 1;                   // 0 - фракционно-пропорциональный; 1 - "портфельный" (по итогам работы)
extern bool     Increase_Allow 	     = true;		          // Разрешено ли уменьшение позиций при вводе средств?
extern bool     Decrease_Allow	     = true;		          // Разрешено ли уменьшение позиций при выводе средств?
extern string	 Allowed_Pairs	        = "GBPUSD;EURUSD";	    // Разрешенные пары; "" - все разрешены
extern string	 Allowed_Magics	     = "";				       // Разрешенные магические номера; "" - все разрешены
extern string   Setup_ICServices   = "==================== SERVICES ===================";
//extern int      NumberOfTry           = 10;                  // Количество попыток на совершение торговых операций
//extern bool     SemaphoreOn           = TRUE;                // Семафор, определяющий очерёдность доступа к торговому потоку советников на одном счёте
extern bool     PrintCom              = TRUE;                // Печатать комментарий.
extern bool     SoundAlert            = FALSE;               // Звук
extern string   Setup_ICTable      = "=============== Параметры таблицы ===============";
extern bool     Draw_ICObject_ON      = TRUE;                // Рисовать ли на графике объекты (для тестирования и оптимизации ни к чему)
extern bool     Show_ICStatidtic_ON   = TRUE;                // Показывать ли на графике статистику (Max LOSS, Max ZALOG, Min EQUITY, Min NARGIN)
extern color    BaseIC_color          = Lime;                // Основной цвет таблицы
extern color    ADDIC_color           = Gold;                // Дополнительный цвет таблицы
extern color    ProfitIC_color        = Blue;                // Цвет положительной статистики
extern color    LOSSIC_color          = Red;                 // Цвет отрицательной статистики
extern color    TimeIC_color          = Aqua;                // Цвет отображения времени события
extern string   FontIC_Table          = "Calibri";           // Шрифт таблицы
extern string   FontIC_Time           = "Calibri";           // Шрифт отображения времени событий
extern string   Setup_Tester       = "==================== Tester ====================";
extern int		 Test_BalanceChange    = 1000;		          // Для тестирования работы - моделирует периодическое изменение баланса
//IIIIIIIIIIIIIIIIIII========Глобальные переменные модуля========IIIIIIIIIIIIIIIIIIIIII+
double   gd_MinLot = 0, gd_MaxLot = 0, gd_StepLot = 0, gd_NewLot = 0, gd_OldLot = 0,
         gd_Bid = 0, gd_Ask = 0, gd_ChangeBalance = 0, gd_ICPribul, gd_MCPribul,
         gda_K[],                   // массив "портфельных" коэффициентов (корректировки лота)
         gda_R[],
         gda_MCPribul[],            // массив результатов работы по каждой стратегии ведущих ордеров
         gda_ICPribul[],            // массив результатов работы по каждой стратегии ведомых ордеров
         gda_ICMaxLOSS[],           // массив максимальной просадки по каждой стратегии ведомых ордеров
         gda_MCMaxLOSS[],           // массив максимальной просадки по каждой стратегии ведущих ордеров
         gda_ICProfit[],            // массив текущих итогов по всем открытым ведомым ордерам
         gda_MCProfitTP[],          // массив текущих итогов по всем открытым ведущим ордерам за последний торговый период
         gda_MCProfit[],            // массив текущих итогов по всем открытым ведущим ордерам
         gd_SL, gd_TP, gd_SlaveSL, gd_SlaveTP, gd_ICBalance, gd_MICEquity,
         gd_MICEquityPercent, gd_MICMarginPercent, gd_MICZalogPercent, gd_ICMaxLOSS, gd_MCMaxLOSS,
         gd_MICMinEquity,           // значение минимального эквити счёта в валюте Депо
         gd_MICMinEquityPercent,    // значение минимального эквити счёта в процентах
         gd_MICMinMargin,           // минимальное значение свободных средств, разрешенных для открытия позиций
         gd_MICMinMarginPercent,    // минимальное значение свободных средств, разрешенных для открытия позиций в процентах
         gd_MICMaxZalog,            // значение максимального залога в валюте Депо
         gd_MICMaxZalogPercent,     // значение максимального залога в процентах
         gd_MICBeginBalance,        // начальное значение Баланса
         gd_ICProfit,               // текущий итог по всем открытым ведомым ордерам
         gd_MCProfit,               // текущий итог по всем открытым ведущим ордерам
         gd_BaseBalance;            // текущее состояние баланса управляющего
int      spread = 0, gi_StopLevel = 0, cnt_curSlaveOrders = 0,// DAY,
         cnt_MG = 10,               // счётчик Магиков управляющих стратегий
         gia_Tickets[MAX_ORDERS],   // массив Тикетов ордеров управляющих стратегий
         gia_Magic[],               // массив Магиков управляющих стратегий
         gia_MCHistoryOrdersTP[],   // массив счётчиков закрытых ведомых ордеров по стратегиям за TP
         gia_MCOrders[],            // массив счётчиков открытых ведомых ордеров по стратегиям
         gia_ICOrders[],            // массив счётчиков открытых ведущих ордеров по стратегиям
         gia_MCHistoryOrders[],     // массив счётчиков закрытых ведомых ордеров по стратегиям
         gi_MCHistoryTotal,         // счётчик закрытых ведомых ордеров
         gia_ICHistoryOrders[],     // массив счётчиков закрытых ведущих ордеров по стратегиям
         gi_ICHistoryTotal,         // счётчик закрытых ведущих ордеров
         gi_HistoryTotal,           // счётчик всех закрытых ордеров
         gi_ICOrders,               // счётчик открытых ведущих ордеров
         gi_MCOrders,               // счётчик открытых ведомых ордеров
         gi_TP,                     // торговый период
         gi_MasterTicket, gi_SlaveTicket, slip, j/*, gi_Digits, gi_dig = 0*/;
string   ds_SYM = "", gs_Comment = "",
         gs_ComError = "",          // переменная для вывода ошибок
         gs_ICNameGV;               // префикс GV-переменных
bool     gb_redraw = false,
         gb_RealTrade = true,       // идентификатор работы в тестере/не в тестере
         gb_InfoPrint,            // флаг разрешения вывода комментариев или на печать, или на график
         gb_ICVirtualTrade;         // статус работы советника в тестере
datetime gdt_LastBalanceTime, gdt_curTime, gdta_CommTime[7],
         gdt_LastBegin_TP;          // начало последнего отчётного периода
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  Custom expert initialization function                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int Init_ICManager (double fd_KBegin = 0.5)
{
    string tmpArr[];
//----
    gs_ICNameGV = "ICManager";
	 ds_SYM = Symbol();
    //gd_MaxLot = MarketInfo (ds_SYM, MODE_MAXLOT);
    //gd_MinLot = MarketInfo (ds_SYM, MODE_MINLOT);
    //gd_StepLot = MarketInfo (ds_SYM, MODE_LOTSTEP);
    //Print ("minLot = ", gd_MinLot," | stepLot = ", gd_StepLot);
    //DAY = PERIOD_D1 * 60;
    //while (MathPow (10, gi_dig) * gd_StepLot < 1)
    //{gi_dig += 1;}
    //---- Расчитываем продолжительность Торгового Периода
    gi_TP = 30 * DAY;
    //---- Идентифицируем статус работы модуля и задействуем GV-переменные
    fGetIsStatusTrade (PrintCom, Draw_ICObject_ON, gb_InfoPrint, SoundAlert, gb_RealTrade, gb_ICVirtualTrade);
    gd_BaseBalance = BaseBalance + gd_MCPribul;
    //---- Готовим массив Магиков управляющих стратегий
    if (Allowed_Magics != "")
    {
        fSplitStrToStr (Allowed_Magics, tmpArr, ";");
        cnt_MG = ArraySize (tmpArr);
        ArrayResize (gia_Magic, cnt_MG);
        for (int li_int = 0; li_int < cnt_MG; li_int++)
        {gia_Magic[li_int] = StrToInteger (tmpArr[li_int]);}
        ArraySort (gia_Magic);
        ArrayResize (gia_ICHistoryOrders, cnt_MG);
        ArrayResize (gia_ICOrders, cnt_MG);
        ArrayResize (gda_ICPribul, cnt_MG);
        ArrayResize (gda_ICProfit, cnt_MG);
        ArrayResize (gia_MCHistoryOrders, cnt_MG);
        ArrayResize (gia_MCOrders, cnt_MG);
        ArrayResize (gda_MCPribul, cnt_MG);
        ArrayResize (gda_MCProfit, cnt_MG);
        ArrayResize (gia_MCHistoryOrdersTP, cnt_MG);
        ArrayResize (gda_K, cnt_MG);
        ArrayResize (gda_R, cnt_MG);
        ArrayInitialize (gda_K, fd_KBegin);
        //---- Заполняем и струкурируем массив просадок по каждому Магику
        if (gb_RealTrade)
        {fArrangeTwoArrays (gs_ICNameGV, gia_Magic, gda_ICMaxLOSS);}
        else
        {ArrayResize (gda_ICMaxLOSS, cnt_MG);}
    }
    else if (!gb_RealTrade)
    {
        for (int li_MG = 0; li_MG < MAX_TC; li_MG++)
        {Allowed_Magics = StringConcatenate (Allowed_Magics, gia_TC.Magic[li_MG], IIFs ((li_MG == MAX_TC - 1), "", ";"));}
    }
    Print ("Allowed_Magics = ", Allowed_Magics);
    if (gb_RealTrade) {gdt_LastBalanceTime = TimeCurrent();} else {gdt_LastBalanceTime = iTime (Symbol(), PERIOD_M1, 0);}
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                  Custor expert deinitialization function                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int deInit_ICManager (int fi_X = 5,                      // начальная координата X
                      int fi_Y = 15)                     // начальная координата Y
{
//----
    //---- Рисуем на графике комменты
    if (Draw_ICObject_ON)
    {
        //---- Подсчитываем итоги работы
        gd_MCPribul = f_CalculatePribul (Allowed_Pairs, gda_MCPribul, gia_MCHistoryOrders, gia_Magic);
        fInfoShow (fi_X, fi_Y);
    }
    if (!gb_RealTrade)
    {GlobalVariablesDeleteAll (gs_ICNameGV);}
    //---- Сохраняем в GV важные значения переменных
    else
    {fSet_GlobalVariable (gs_ICNameGV);}
    //---- Закрываем за собоой Semaphore
    /*if (!SemaphoreOn)
    {return (0);}
    else
    {SemaphoreDeinit ("TRADECONTEXT");}*/
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|               Custom include iteration function                                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int f_ICManager (int fi_X = 5,                      // начальная координата X
                 int fi_Y = 15,                     // начальная координата Y
                 bool fb_redraw = false,            // флаг перерисовки таблицы
                 double fd_K_Begin = 0.5)           // начальная величина коэффициента формирования "добавочного" лота
{
    int err = GetLastError();
//----
    //---- Запоминаем текущее время
    if (!gb_RealTrade) {gdt_curTime = iTime (Symbol(), 1, 0);} else {gdt_curTime = TimeCurrent();}
    //---- В тестовом режиме открываем ордера
    //fTestSendOrders (gia_Magic);
    //---- Проверяем изменение баланса
    gd_ChangeBalance = fGet_BalanceChange();
    if (gd_ChangeBalance > 0)
    {
        if (fCCV_D (gd_ChangeBalance, 0))
        {if (PrintCom) Print ("ChangeBalance = ", gd_ChangeBalance);}
    }
    //---- Если баланс изменился, то обнуляем количество скорректированных ведомых позиций
    if (gd_ChangeBalance != 0)
    {cnt_curSlaveOrders = 0;}
    //---- Готовим к работе в новом отчётном периоде массивы и расчитываем "добавочные" коэффициенты
    fPrepareForNewTP (30 * DAY, fd_K_Begin);
    //---- Кооректируем объёмы открытых позиций после проведения балансовых опреаций на счёте
    fVolumeCorrector();
    //---- Если баланс изменился, но все ведомые позиции скорректированы,
    //---- то установть новую точку отсчета отслеживания изменений баланса.
    if (gd_ChangeBalance != 0 && cnt_curSlaveOrders == 0)
    {gdt_LastBalanceTime = gdt_curTime;}
    //---- Следим за выделением средств для новых ведущих позиций
    fMManager();
    //---- Собираем статистику
    fGet_ICStatistic();
    //---- Выводим на график актуальную информацию
    fInfoShow (fi_X, fi_Y, fb_redraw, fd_K_Begin, FontIC_Table, BaseIC_color, TimeIC_color);
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "f_ICManager()");
//----
    return (0);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Корректируем размер ведомых позиций после балансовых опреаций             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fVolumeCorrector()
{
    double ld_Profit, ld_ChangeBalancePercent;
    int li_MasterMagic, li_NUM, err = GetLastError();
//----
    ds_SYM = "";
    ArrayInitialize (gda_ICProfit, 0);
    ArrayInitialize (gia_ICOrders, 0);
    gd_ICProfit = 0.0;
    gi_ICOrders = 0;
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {    
        //---- Перебираем ведомые позиции ...
        if (!OrderSelect (i, SELECT_BY_POS, MODE_TRADES))
        {return (0);}
        if (Magic == OrderMagicNumber())
        {
            gi_SlaveTicket = OrderTicket();
            gd_OldLot = OrderLots();
            gd_SlaveSL = OrderStopLoss();
            gd_SlaveTP = OrderTakeProfit();
            ld_Profit = OrderProfit() + OrderSwap() + OrderCommission();
            //---- Определяем Магик ведущего ордера
            li_MasterMagic = fGet_MasterMagicFromComment (Magic);
            //---- Определяем индекс ведущего Магка в массиве Магиков
            li_NUM = fCheckMyMagic (li_MasterMagic, gia_Magic);
            //---- Собираем статистику
            gda_ICProfit[li_NUM] += ld_Profit;
            gd_ICProfit += ld_Profit;
            gia_ICOrders[li_NUM]++;
            gi_ICOrders++;
            //---- Получаем торговую информацию по текущему символу
            fGet_MarketInfo();
            //---- ...и определяем для каждой ведущую
            gi_MasterTicket = fGet_MasterTicket();
            //---- Проверяем статус ведущей позиции
            if (OrderSelect (gi_MasterTicket, SELECT_BY_TICKET))
            {
                //---- Если ведущая закрыта, то закрываем ведомую
                if (OrderCloseTime() > 0)
                {fClose_SlaveOrder (gi_SlaveTicket);}
                else
                {
                    //---- Если ведущая открыта и баланс изменился, то корректируем ее объём
                    gd_NewLot = fGet_NewLot (Var_CalculateLots);
                    ld_ChangeBalancePercent = MathRound (100 * (gd_ChangeBalance / (gd_MICEquity - gd_ChangeBalance - gd_BaseBalance)));
                    //---- Баланс увеличился:
                    //---- Если gd_NewLot > gd_OldLot, то закрываем позицию и открываем новую на gd_NewLot
                    if (Increase_Allow)
                    {
                    	   if (gd_ChangeBalance > 0 && gd_NewLot > gd_OldLot)
                    	   {
                    		    if (PrintCom) {Print ("Balance increased on ", gd_ChangeBalance, " (", ld_ChangeBalancePercent, "%). ",
                    		    "Increase #", gi_MasterTicket, " from ", gd_OldLot, " to ", gd_NewLot);}
                        	 fClose_SlaveOrder (gi_SlaveTicket);
                        	 fOpen_SlaveOrder (gd_NewLot);
                            //---- Идём дальше
                            continue;
                    	   }
                    }
                    //---- Баланс уменьшился:
                    //---- Если gd_NewLot < gd_OldLot, то закрываем позицию и открываем новую на gd_NewLot
                    if (Decrease_Allow)
                    {
                    	   if (gd_ChangeBalance < 0 && gd_NewLot < gd_OldLot)
                    	   {
                    		    if (PrintCom) {Print ("Balance decreased on ", gd_ChangeBalance," (", ld_ChangeBalancePercent, "%). ",
                    		    "Decrease #", gi_MasterTicket," from ", gd_OldLot, " to ", gd_NewLot);}
                        	 fClose_SlaveOrder (gi_SlaveTicket);
                        	 fOpen_SlaveOrder (gd_NewLot);
                            //---- Идём дальше
                            continue;
                    	   }
					     }
					     //---- Если изменился уровень SL или TP у ведущей позиции, то меняем их у ведомой позиции 
					     if (gd_SlaveSL != OrderStopLoss() || gd_SlaveTP != OrderTakeProfit())
					     {OrderModify (gi_SlaveTicket, 0, OrderStopLoss(), OrderTakeProfit(), 0);}
                }
            }
            else
            {
                // Ошибка - нет ведущей позиции
                if (PrintCom) Print ("Error: can\'t find master procition for slave #", gi_SlaveTicket);
                fClose_SlaveOrder (gi_SlaveTicket);
            }
        }
    }
    //---- Собираем статистику по максимальной просадке
    for (i = 0; i < cnt_MG; i++)
    {gda_ICMaxLOSS[i] = MathMin (gda_ICMaxLOSS[i], gda_ICProfit[i]);}
    gd_ICMaxLOSS = MathMin (gd_ICMaxLOSS, gd_ICProfit);
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fVolumeCorrector()");
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         На каждый ведущий тикет открываем ведомый ордер                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fMManager()
{
    int    j = 0, li_NUM, li_Magic, err = GetLastError();
    double ld_Profit;
//----
    //---- Запоминаем все ведущие, у которых есть ведомые, в массив
    ArrayResize (gia_Tickets, MAX_ORDERS);
    ArrayInitialize (gia_Tickets, 0);
    ArrayInitialize (gda_MCProfit, 0);
    ArrayInitialize (gia_MCOrders, 0);
    gd_MCProfit = 0.0;
    gi_MCOrders = 0;
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {    
        //---- Перебираем ведомые позиции ...
        if (OrderSelect (i, SELECT_BY_POS, MODE_TRADES) && Magic == OrderMagicNumber())
        {
            gia_Tickets[j] = fGet_MasterTicket();
            j++;
        }
    }
    if (j > 0)
    {ArrayResize (gia_Tickets, j);}
    //---- Перебираем ведущие позиции и проверяем существование ведомой
    ds_SYM = "";
    for (i = OrdersTotal() - 1; i >= 0; i--)
    {    
        if (OrderSelect (i, SELECT_BY_POS, MODE_TRADES))
        {
            if (Magic != OrderMagicNumber())
            {
                if ((Allowed_Pairs == "" || StringFind (Allowed_Pairs, OrderSymbol()) != -1)
                && (Allowed_Magics == "" || StringFind (Allowed_Magics, DoubleToStr (OrderMagicNumber(), 0)) != -1))
                {   
                    gi_MasterTicket = OrderTicket();
                    li_Magic = OrderMagicNumber();
                    ld_Profit = OrderProfit() + OrderSwap() + OrderCommission();
                    //---- Определяем индекс ведущего Магка в массиве Магиков
                    li_NUM = fCheckMyMagic (li_Magic, gia_Magic);
                    //---- Собираем статистику
                    gda_MCProfit[li_NUM] += ld_Profit;
                    gd_MCProfit += ld_Profit;
                    gia_MCOrders[li_NUM]++;
                    gi_MCOrders++;
                    //---- Если ведомой нет, то открываем ее.
                    if (!fIs_SlaveTicket (gi_MasterTicket))
                    {
                        gd_NewLot = fGet_NewLot (Var_CalculateLots);
                        //---- Если прибыль позиции в минусе
                        //if (OrderProfit() + OrderSwap() + OrderCommission() < 0) // ???
                        //{
                            if (gd_NewLot > 0)
                            {
                   		        if (PrintCom) Print ("Found new master order #", gi_MasterTicket, ". Open slave order with size ", gd_NewLot);
                   		        fOpen_SlaveOrder (gd_NewLot);
                   		    }
                   		//}
                    }
                }
            }
        }
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fMManager()");
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Подготовка к работе в новом отчётном периоде                              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fPrepareForNewTP (int fi_TradePeriod, double fd_KBegin = 0.5)
{
    //fPrepareForNewTrade (30 * DAY);
    int err = GetLastError();
//----
    if (gdt_LastBegin_TP + fi_TradePeriod < gdt_curTime)
    {
        //---- Сохраняем данные о максимальных просадках по стратегиям
        for (int li_int = 0; li_int < ArraySize (gda_ICMaxLOSS); li_int++)
        {GlobalVariableSet (StringConcatenate (gs_ICNameGV, "_#MaxLOSS_", gia_Magic[li_int]), gda_ICMaxLOSS[li_int]);}
        //---- Расчитываем "добавочные" коэффициенты
        fGet_KoefResultTrade (gia_Magic, gda_K, gd_BaseBalance, gdt_LastBegin_TP, fd_KBegin);
        //---- Собираем обратно в массив данные о максимальных просадках по стратегиям
        fArrangeTwoArrays (gs_ICNameGV, gia_Magic, gda_ICMaxLOSS);
        //---- Задаём нужные размеры массивам со статистикой
        ArrayResize (gia_ICHistoryOrders, cnt_MG);
        ArrayResize (gda_ICPribul, cnt_MG);
        ArrayResize (gda_ICProfit, cnt_MG);
        ArrayResize (gia_ICOrders, cnt_MG);
        ArrayResize (gia_MCHistoryOrders, cnt_MG);
        ArrayResize (gda_MCPribul, cnt_MG);
        ArrayResize (gda_MCProfit, cnt_MG);
        ArrayResize (gia_MCOrders, cnt_MG);
        ArrayResize (gia_MCHistoryOrdersTP, cnt_MG);
        //---- Присваем текущее время началу новому торгового периода
        gdt_LastBegin_TP = gdt_curTime;
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fPrepareForNewTP()");
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Проверяем наличие управляемого тикета в базе тикетов                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fIs_SlaveTicket (int fi_Ticket) 
{
//----
    for (int i = ArraySize (gia_Tickets) - 1; i >= 0 ; i--)
    {
        if (gia_Tickets[i] == fi_Ticket)
        {return (true);}
    }
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Берём из коммента управляемого ордера тикет управляющего                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_MasterTicket()
{
    int li_Ticket = 0, end = 0;
//----
    if (Magic == OrderMagicNumber())
    {
        gs_Comment = OrderComment();
        end = StringFind (gs_Comment, ";", 0);
        if (end > 0)
        {li_Ticket = StrToInteger (StringTrimLeft (StringTrimRight (StringSubstr (gs_Comment, 0, end))));}
    }
//----
    return (li_Ticket);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Получаем размер лота для управляемого ордера                              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_NewLot (int fi_VarCalculate = 0)
{
    double ld_Lot = 0, ld_NewLot = 0;
	 int    li_NUM, err = GetLastError();
//----
	 //---- Удостоверяемся, что выделенный ордер является ведущим
	 if (OrderMagicNumber() != Magic)
	 {
		  ld_Lot = OrderLots();
		  switch (fi_VarCalculate)
		  {
		      case 0:
		          ld_NewLot = gd_MinLot * (gd_ICBalance / Balance_ForMinLot) * (ld_Lot / ((gd_BaseBalance / Balance_ForMinLot) * gd_MinLot));
	             ld_NewLot = MathMax (gd_MinLot, ld_NewLot);
		          break;
		      case 1:
                li_NUM = fCheckMyMagic (OrderMagicNumber(), gia_Magic);
                ld_NewLot = gd_ICBalance * ld_Lot / gd_BaseBalance * gda_K[li_NUM];
	             ld_NewLot = MathMax (ld_Lot, ld_NewLot);
		          break;
		  }
	 }
	 else {if (PrintCom) Print ("Error: getting new lot size from not base order");}
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fGet_NewLot()");
//----
    return (fLotsNormalize (ld_NewLot));
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       Производим нормализацию лота                                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fLotsNormalize (double fd_Lots)
{
    fd_Lots -= MarketInfo (Symbol(), MODE_MINLOT);
    fd_Lots /= MarketInfo (Symbol(), MODE_LOTSTEP);
    fd_Lots = MathRound (fd_Lots);
    fd_Lots *= MarketInfo (Symbol(), MODE_LOTSTEP);
    fd_Lots += MarketInfo (Symbol(), MODE_MINLOT);
    fd_Lots = NormalizeDouble (fd_Lots, gi_dig);
    fd_Lots = MathMax (fd_Lots, MarketInfo (Symbol(), MODE_MINLOT));
    fd_Lots = MathMin (fd_Lots, MarketInfo (Symbol(), MODE_MAXLOT));
//----
    return (fd_Lots);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Получаем торговую информацию по символу                                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_MarketInfo() 
{
//----
	 if (ds_SYM != OrderSymbol())
	 {
		  ds_SYM = OrderSymbol();
		  gi_Digits = MarketInfo (ds_SYM, MODE_DIGITS);
        gi_StopLevel = MarketInfo (ds_SYM, MODE_STOPLEVEL);
        spread = MarketInfo (ds_SYM, MODE_SPREAD);
        gd_Bid = MarketInfo (ds_SYM, MODE_BID); 
        gd_Ask = MarketInfo (ds_SYM, MODE_ASK);
        slip = spread;
	 }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Открываем управляемый ордер                                               |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fOpen_SlaveOrder (double lot)
{
    int li_NUM, li_MasterTicket = OrderTicket(), li_SlaveTicket = 0;
//----
    li_NUM = fCheckMyMagic (OrderMagicNumber(), gia_Magic);
    //---- Готовим комментарий для ведомого ордера
    gs_Comment = StringConcatenate (li_MasterTicket, "; ", OrderMagicNumber(), "#", OrderComment());
    ds_SYM = "";
    fGet_MarketInfo();
    gd_SL = OrderStopLoss();
    gd_TP = OrderTakeProfit();
    if (OrderType() == OP_BUY)
    {li_SlaveTicket = OrderSend (ds_SYM, OP_BUY, lot, NormalizeDouble (gd_Ask, gi_Digits), slip, 0, 0, gs_Comment, Magic);}
    else
    if (OrderType() == OP_SELL)
    {li_SlaveTicket = OrderSend (ds_SYM, OP_SELL, lot, NormalizeDouble (gd_Bid, gi_Digits), slip, 0, 0, gs_Comment, Magic);}
    if (li_SlaveTicket > 0)
    {
        if (gd_SL != 0 || gd_TP != 0)
        {
            if (OrderSelect (li_SlaveTicket, SELECT_BY_TICKET))
            {OrderModify (li_SlaveTicket, OrderOpenPrice(), gd_SL, gd_TP, 0);}
        }
        cnt_curSlaveOrders++;
        //---- Печатаем инфу об открытом ордере
        if (OrderSelect (li_SlaveTicket, SELECT_BY_TICKET))
        {OrderPrint();}
    }
    else {if (PrintCom) Print ("Error opening driven order: ", gs_Comment, " | ", ErrorDescription (GetLastError()));}
    //---- Выделяем ведущий ордер
    OrderSelect (li_MasterTicket, SELECT_BY_TICKET);
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Закрываем управляемый ордер                                               |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fClose_SlaveOrder (int fi_slaveTicket)
{
    int li_masterTicket = OrderTicket();
//----
    if (OrderSelect (fi_slaveTicket, SELECT_BY_TICKET) && OrderCloseTime() == 0)
    {
        ds_SYM = "";
    	  fGet_MarketInfo();
        if (OrderType() == OP_BUY)
        {OrderClose (fi_slaveTicket, OrderLots(), NormalizeDouble (gd_Bid, gi_Digits), slip);}
        if (OrderType() == OP_SELL)
        {OrderClose (fi_slaveTicket, OrderLots(), NormalizeDouble (gd_Ask, gi_Digits), slip);}
    }
    OrderSelect (li_masterTicket, SELECT_BY_TICKET);
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Проверяем факт балансовой операции                                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_BalanceChange()
{  
    double ld_newBalance = 0;
//----
    gd_MICEquity = AccountEquity();
    if (gb_RealTrade)
    {
        gd_ICBalance = AccountBalance() - gd_BaseBalance;
	     for (int i = OrdersHistoryTotal() - 1; i >= 0; i--)
        {
            OrderSelect (i, SELECT_BY_POS, MODE_HISTORY);
            if (OrderType() == 6)             // Ордер изменения баланса
            {
                if (OrderOpenTime() > gdt_LastBalanceTime)
                {ld_newBalance += OrderProfit();}
            }
        }
        if (Var_CalculateLots == 1 && Balance_ForMinLot > 0)
        {
            //---- Если абсолютная величина изменения баланса меньше Balance_ForMinLot - выходим
            if (MathAbs (ld_newBalance) < Balance_ForMinLot)
            {return (0);}
        }
    }
    else
    {
        static int li_newDay = 0;
        //---- Для тестирования искуственно 2 раза в неделю изменяем значения баланса
        if (TimeDayOfWeek (gdt_curTime) == 1 && li_newDay != TimeDayOfWeek (gdt_curTime))
        {
            gd_ICBalance = AccountBalance() - gd_BaseBalance - Test_BalanceChange;
            ld_newBalance = -Test_BalanceChange;
            li_newDay = TimeDayOfWeek (gdt_curTime);
        }
        if (TimeDayOfWeek (gdt_curTime) == 4 && li_newDay != TimeDayOfWeek (gdt_curTime))
        {
            gd_ICBalance = AccountBalance() - gd_BaseBalance + Test_BalanceChange;
            ld_newBalance = Test_BalanceChange;
            li_newDay = TimeDayOfWeek (gdt_curTime);
        }
    }
//----
    return (ld_newBalance);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Выводим на график актуальную информацию в левый верхний угол              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fInfoShow (int fi_X = 5,                      // начальная координата X
                int fi_Y = 15,                     // начальная координата Y
                bool fb_redraw = false,            // флаг перерисовки таблицы
                double fd_KBegin = 0.5,            // начальная величина коэффициента формирования "добавочного" лота
                string fs_Font_Table = "Calibri",  // шрифт таблицы
                color fc_Base_color = Lime,        // цвет основной таблицы
                color fc_Time_color = Aqua)        // цвет отображения времени
{
    double ld_Parameters;
    int    cnt = 0, li_row = 0, li_column = 70, li_HistoryOrders = 0, li_Weight_row = 15,
           li_X_time = 270,                        // координата X отображения времени
           li_deviation, li_znak, err = GetLastError(), li_size, li_Font, lia_deviation[] = {15,15,10,15,5,5,5,15,0};
    string ls_txt, ls_Parameters, ls_ND = "----",
           ls_row = "*", 
           lsa_report[] = {"Magic","Profit","Pribul","LOSS","MCProfit","MCPribul","PribulTP","R","K"};
    color  lc_color, lc_Profit = Blue, lc_Loss = Red;
//----
    li_size = ArraySize (lsa_report);
    //ArrayResize (lia_deviation, li_size);
    //---- 1-я строка
    if (!FindObject ("tbl_brow1_" + 0))
    {
        for (int li_int = 0; li_int < 147; li_int++)
        {SetLabel ("tbl_brow1_" + li_int, ls_row, fi_X + li_int * 4, fi_Y + li_row * li_Weight_row, 8, fs_Font_Table, 0, 0, fc_Base_color);}
    }
    //---- 2-я строка
    li_row++;
    li_znak = li_column / 10;
    for (li_int = 0; li_int < li_size; li_int++)
    {
        //li_deviation = (10 - StringLen (lsa_report[li_int])) / 2 * li_znak;
        SetLabel ("tbl_bres" + li_int, lsa_report[li_int], fi_X + li_int * li_column + lia_deviation[li_int], fi_Y + li_row * li_Weight_row, 10, fs_Font_Table, 0, 0, fc_Base_color);
    }
    //---- 3-я строка
    li_row++;
    if (!FindObject ("tbl_brow2_" + 0))
    {
        for (li_int = 0; li_int < 147; li_int++)
        {SetLabel ("tbl_brow2_" + li_int, ls_row, fi_X + li_int * 4, fi_Y + li_row * li_Weight_row, 8, fs_Font_Table, 0, 0, fc_Base_color);}
    }
    //---- 4-xx-я строка
    for (int li_MG = 0; li_MG <= cnt_MG; li_MG++)
    {
        li_row++;
        lc_color = fc_Base_color;
        for (li_int = 0; li_int < li_size; li_int++)
        {
            switch (li_int)
            {
                case 0: // Magic
                    if (li_MG == 0)
                    {
                        ls_Parameters = "SLAVE";
                        if (gd_ICPribul >= 0) {lc_color = lc_Profit;} else {lc_color = lc_Loss;}
                    }
                    else
                    {
                        ls_Parameters = DoubleToStr (gia_Magic[li_MG-1], 0);
                        if (gda_MCPribul[li_MG-1] >= 0) {lc_color = lc_Profit;} else {lc_color = lc_Loss;}
                    }
                    break;
                case 1: // IC Profit
                    if (li_MG == 0)
                    {
                        ld_Parameters = gd_ICProfit;
                        ls_Parameters = StringConcatenate (DoubleToStr (ld_Parameters, 0), "[", gi_ICOrders, "]");
                    }
                    else
                    {
                        ld_Parameters = gda_ICProfit[li_MG-1];
                        ls_Parameters = StringConcatenate (DoubleToStr (ld_Parameters, 1), "[", gia_ICOrders[li_MG-1], "]");
                    }
                    break;
                case 2: // IC Pribul
                    if (li_MG == 0)
                    {
                        ld_Parameters = gd_ICPribul;
                        ls_Parameters = StringConcatenate (DoubleToStr (ld_Parameters, 0), "[", gi_ICHistoryTotal, "]");
                    }
                    else
                    {ls_Parameters = ls_ND;}
                    break;
                case 3: // IC Loss
                    if (li_MG == 0)
                    {
                        ld_Parameters = gd_ICMaxLOSS;
                        ls_Parameters = DoubleToStr (ld_Parameters, 0);
                    }
                    else
                    {
                        ld_Parameters = gda_ICMaxLOSS[li_MG-1];
                        ls_Parameters = DoubleToStr (ld_Parameters, 1);
                    }
                    break;
                 case 4: // MC Profit
                    if (li_MG == 0)
                    {
                        ld_Parameters = gd_MCProfit;
                        ls_Parameters = StringConcatenate (DoubleToStr (ld_Parameters, 0), "[", gi_MCOrders, "]");
                    }
                    else
                    {
                        ld_Parameters = gda_MCProfit[li_MG-1];
                        ls_Parameters = StringConcatenate (DoubleToStr (ld_Parameters, 1), "[", gia_MCOrders[li_MG-1], "]");
                    }
                    break;
                case 5: // MC Pribul
                    if (li_MG == 0)
                    {
                        ld_Parameters = gd_MCPribul;
                        ls_Parameters = StringConcatenate (DoubleToStr (ld_Parameters, 0), "[", gi_MCHistoryTotal, "]");
                    }
                    else
                    {
                        ld_Parameters = gda_MCPribul[li_MG-1];
                        ls_Parameters = StringConcatenate (DoubleToStr (ld_Parameters, 1), "[", gia_MCHistoryOrders[li_MG-1], "]");
                    }
                    break;
                 case 6: // MC Profit TP
                    if (li_MG == 0)
                    {ls_Parameters = ls_ND;}
                    else
                    {
                        ld_Parameters = gda_MCProfitTP[li_MG-1];
                        ls_Parameters = StringConcatenate (DoubleToStr (ld_Parameters, 1), "[", gia_MCHistoryOrdersTP[li_MG-1], "]");
                    }
                    break;
               case 7: 
                    if (li_MG == 0)
                    {ls_Parameters = MinRating;}
                    else
                    {
                        ld_Parameters = gda_R[li_MG-1];
                        ls_Parameters = DoubleToStr (ld_Parameters, 0);
                        ls_Parameters = StringConcatenate (DoubleToStr (ld_Parameters, 0), "[", gia_TC.PeriodRating[li_MG-1], "]");
                    }
                    break;
               case 8: 
                    if (li_MG == 0)
                    {ls_Parameters = ls_ND;}
                    else
                    {
                        ld_Parameters = gda_K[li_MG-1];
                        ls_Parameters = DoubleToStr (ld_Parameters, 2);
                    }
                    break;
            }
            if (li_int != 0)
            {if (ld_Parameters >= 0) {lc_color = lc_Profit;} else {lc_color = lc_Loss;}}
            if (li_int == 8 && li_MG != 0)
            {if (ld_Parameters >= 0.5) {lc_color = lc_Profit;} else {lc_color = lc_Loss;}}
            if (li_int == 7)
            {
                if (li_MG != 0)
                {if (ld_Parameters >= MinRating) {lc_color = lc_Profit;} else {lc_color = lc_Loss;}}
                else {lc_color = lc_Profit;}
            }
            //---- Пустые строки подкрашиваем под цвет таблицы
            if (ls_Parameters == ls_ND)
            {lc_color = fc_Base_color;}
            if (li_MG == 0 && li_int != 0) {li_Font = 11;} else {li_Font = 10;}
            if (li_int == 8) {li_deviation = 15;} else {li_deviation = 0;}
            SetLabel ("tbl_bres_" + li_MG + "_" + li_int, ls_Parameters, fi_X + 10 + li_int * li_column - li_deviation, fi_Y + li_row * li_Weight_row, li_Font, fs_Font_Table, 0, 0, lc_color);
        }
    }
    //---- 5-я строка
    li_row++;
    if (!FindObject ("tbl_brow3_" + 0))
    {
        for (li_int = 0; li_int < 147; li_int++)
        {SetLabel ("tbl_brow3_" + li_int, ls_row, fi_X + li_int * 4, fi_Y + li_row * li_Weight_row, 8, fs_Font_Table, 0, 0, fc_Base_color);}
    }
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Собираем статистику                                                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_ICStatistic()
{
    int err = GetLastError();
//----
    //---- Собираем общую статистику о состоянии счёта
    fGet_ICStatisticPercent();
    if (!gb_ICVirtualTrade && Draw_ICObject_ON)
    {
        gd_MICMaxZalog = MathMax (gd_MICMaxZalog, AccountMargin());
        gd_MICMinEquity = MathMin (gd_MICMinEquity, AccountEquity());
        gd_MICMinEquity = MathMin (gd_MICMinEquity, AccountFreeMargin());
        gd_MICMaxZalogPercent = MathMax (gd_MICMaxZalogPercent, gd_MICZalogPercent);
        gd_MICMinEquityPercent = MathMin (gd_MICMinEquityPercent, gd_MICEquityPercent);
        gd_MICMinEquityPercent = MathMin (gd_MICMinEquityPercent, gd_MICMarginPercent);
        //---- Обновляем GV-переменные
        fUpdate_StatisticGV (gs_ICNameGV);
    }
    //---- Подсчитываем итоги работы
    if (fCCV_D (OrdersHistoryTotal(), 1))
    {
        gd_MCPribul = f_CalculatePribul (Allowed_Pairs, gda_MCPribul, gia_MCHistoryOrders, gia_Magic);
        gd_BaseBalance = BaseBalance + gd_MCPribul;
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fGet_Statistic()");
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Собираем статистику по состоянию счёта в процентах                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_ICStatisticPercent()
{
    if (AccountBalance() != 0.0)
    {
        gd_MICEquityPercent = 100 * AccountEquity() / AccountBalance();
        gd_MICMarginPercent = 100 * AccountFreeMargin() / AccountBalance();
        gd_MICZalogPercent = 100 * AccountMargin() / AccountBalance();
    }
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Общие функции                                                             |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Идентифицируем статус работы советника                                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGetIsStatusTrade (bool& fb_PrintCom,         // флаг разрешения вывода комментариев на печать
                        bool& fb_DrawObject,       // флаг разрешения вывода комментариев на график
                        bool& fb_InfoPrint,        // флаг разрешения вывода комментариев или на печать, или на график
                        bool& fb_SoundAlert,       // флаг разрешения вывода звукового сопровождения событий
                        bool& fb_RealTrade,        // идентификатор работы в тестере/не в тестере
                        bool& fb_VirtualTrade)     // статус работы советника в тестере
{
//----
    //---- Формируем префикс GV-переменных
    if (IsTesting())
    {gs_ICNameGV = gs_ICNameGV + "_t";}
    if (IsDemo())
    {gs_ICNameGV = gs_ICNameGV + "_d";}
    //---- Устанавливаем флаги для работы с информацией
    if (IsTesting() || IsOptimization())
    {
        fb_RealTrade = False;
        //---- Отключаем не используемые функции при тестировании и оптимизации
        if (IsOptimization())
        {fb_PrintCom = false;}
        if ((IsTesting() && !IsVisualMode()) || IsOptimization())
        {
            fb_DrawObject = false;
            fb_VirtualTrade = true;
        }
        fb_SoundAlert = false;
        //---- Подчищаем GV-переменные
        GlobalVariablesDeleteAll (gs_ICNameGV);
    }
    else
    {
        //---- Берём из глобальных переменных существенные для процесса значения
        fGet_GlobalVariable (gs_ICNameGV);
        //---- Удаляем старую таблицу
        fObjectsDeleteAll (0, OBJ_LABEL, "tbl_");
    }
    if (!IsOptimization())
    {
        //---- Организуем подготовку комментариев
        if (fb_PrintCom || fb_DrawObject)
        {fb_InfoPrint = true;}
    }
    //---- При первом запуске вносим в глобальную переменную начальные значения
    if (gd_MICMinEquity == 0.0)
    { 
        gd_MICMinEquity = AccountBalance();
        GlobalVariableSet (StringConcatenate (gs_ICNameGV, "_#min_EQUITY"), ND0 (gd_MICMinEquity));
        gd_MICMinEquityPercent = 100;
    }
    if (gd_MICMinEquity == 0.0)
    {
        gd_MICMinEquity = AccountBalance();
        GlobalVariableSet (StringConcatenate (gs_ICNameGV, "_#min_MARGIN"), ND0 (gd_MICMinEquity));
        gd_MICMinEquityPercent = 100;
    }
    if (gd_MICBeginBalance == 0.0)
    {
        gd_MICBeginBalance = AccountBalance();
        GlobalVariableSet (StringConcatenate (gs_ICNameGV, "_#BeginBalance"), ND0 (gd_MICBeginBalance));
    }
    if (fb_RealTrade)
    {
        int cnt = 0;
        gdt_LastBegin_TP = BeginTrade;
        while (TimeCurrent() - gdt_LastBegin_TP > gi_TP)
        {
            gdt_LastBegin_TP = BeginTrade + gi_TP * cnt;
            cnt++;
        }
        gdt_LastBegin_TP -= gi_TP;
        Print ("LastBegin_TP = ", TS_DM (gdt_LastBegin_TP));
    }
    else
    {gdt_LastBegin_TP = iTime (Symbol(), 1, 0);}
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор    : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|  Версия  : 27.10.2009                                                             |
//|  Описание: fControlChangeValue_S Фиксирует факт изменения проверяемого            |
//|  string параметра                                                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCCV_S (string param, int ix)
{
    static string cur_param[20];
    static bool   lb_first = true;
//---- 
    //---- При первом запуске инициализируем массив
    if (lb_first)
    {
        for (int l_int = 0; l_int < ArraySize (cur_param); l_int++)
        {cur_param[l_int] = "";}
        lb_first = false;
    }
    if (cur_param[ix] != param)
    {
        cur_param[ix] = param;
        return (true);
    }
//---- 
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор    : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|  Версия  : 27.10.2009                                                             |
//|  Описание: fControlChangeValue_D Фиксирует факт изменения проверяемого            |
//|  double параметра                                                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCCV_D (double param, int ix)
{
    static double cur_param[10];
    static bool   lb_first = true;
//---- 
    //---- При первом запуске инициализируем массив
    if (lb_first)
    {
        for (int l_int = 0; l_int < ArraySize (cur_param); l_int++)
        {cur_param[l_int] = 0;}
        lb_first = false;
    }
    if (cur_param[ix] != param)
    {
        cur_param[ix] = param;
        return (true);
    }
//---- 
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                          |
//+-----------------------------------------------------------------------------------+
//|  Версия   : 01.02.2008                                                            |
//|  Описание : Возвращает одно из двух значений взависимости от условия.             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string IIFs (bool condition, string ifTrue, string ifFalse)
{if (condition) {return (ifTrue);} else {return (ifFalse);}}
//+-----------------------------------------------------------------------------------+
double IIFd (bool condition, double ifTrue, double ifFalse)
{if (condition) {return (ifTrue);} else {return (ifFalse);}}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Функция, перевода времени в сек. в строку формата "yyyy.mm.dd hh:mi"       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string TS_DM (datetime v) {return (TimeToStr (v, TIME_DATE|TIME_MINUTES));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Функция, нормализации значения double до 0                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double ND0 (double v) {return (NormalizeDouble (v, 0));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Возвращает массив STRING из строки, разделённой sDelimiter                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fSplitStrToStr (string aString, string& aArray[], string aDelimiter = ",")
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
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       Считаем заработанную прибыль (если вообще заработали)                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double f_CalculatePribul (string fs_ListSymbols,     // лист контролируемых символов
                         double& ar_Pribul[],       // вовращаемый массив результатов работы по трём типам ордеров
                         int& ar_HistoryOrders[],   // возварщаемый массив закрытых ордеров по трём типам ордеров
                         int ar_Magic[],            // массив Magic
                         int op = -1,               // тип (BUY\SELL) учитываемых ордеров
                         datetime dt = 0)           // момент времени, с которого производим расчёт
{
    int li_int, li_ind_MG, err = GetLastError(), li_NUM, history_total = OrdersHistoryTotal();
    double ld_Pribul = 0, ld_ALLPribul = 0;
    string ls_Comment;
//----
    ArrayInitialize (ar_Pribul, 0.0);
    ArrayInitialize (ar_HistoryOrders, 0);
    gd_ICPribul = 0.0;
    gi_ICHistoryTotal = 0;
    gi_MCHistoryTotal = 0;
    for (li_int = history_total - 1; li_int >= 0; li_int--)
    {
        if (OrderSelect (li_int, SELECT_BY_POS, MODE_HISTORY))
        {
            if (fs_ListSymbols == "" || StringFind (fs_ListSymbols, OrderSymbol()) != -1)
            {
                if (OrderType() < 2 && (op < 0 || OrderType() == op))
                {
                    if (dt < OrderCloseTime())
                    {
                        li_ind_MG = fCheckMyMagic (OrderMagicNumber(), ar_Magic);
                        ld_Pribul = OrderProfit() + OrderSwap() + OrderCommission();
                        //---- Считаем итоги работы по ведущим ордерам
                        if (li_ind_MG >= 0)
                        {
                            ar_Pribul[li_ind_MG] += ld_Pribul;
                            ld_ALLPribul += ld_Pribul;
                            ar_HistoryOrders[li_ind_MG]++;
                            gi_MCHistoryTotal++;
                        }
                        //---- Считаем итоги работы по ведомым ордерам
                        else if (OrderMagicNumber() == Magic)
                        {
                            gd_ICPribul += ld_Pribul;
                            gi_ICHistoryTotal++;
                        }
                    }
                }
            }
        }
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "f_CalculatePribul()");
//----
    return (ld_ALLPribul);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Портфельный MM                                                            |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Рассчитываем коэффициент формирования "добавочного" лота                   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_KoefResultTrade (int& ar_MG[],            // возвращаемый массив рабочих стратегий\"портфелей"
                           double& ar_Koef[],       // возвращаемый массив коэффициентов формирования "добавочного" лота
                           double fd_KY,            // размер капитала управляющего
                           datetime fdt_Begin,      // начальная дата подсчёта результатов работы стратегий
                           double fd_K_Begin = 0.5) // начальная величина коэффициента формирования "добавочного" лота
{
    int      li_MG, li_size, err = GetLastError();
    double   ld_Profit, ld_K;
    string   ls_Name, lsa_NameGV[], ls_txt, ls_result;
    datetime ldt_CurTime;
//----
    //---- Формируем массив Магиков управляющих стратегий
    fGet_MasterMagics (ar_MG);
    ArrayResize (lsa_NameGV, cnt_MG);
    ArrayResize (gda_MCProfitTP, cnt_MG);
    ArrayResize (ar_Koef, cnt_MG);
    ArrayResize (gda_R, cnt_MG);
    ArrayInitialize (gda_MCProfitTP, 0);
    ArrayInitialize (gia_MCHistoryOrdersTP, 0);
    //---- За отчётный период подсчитываем результаты работы по каждому Magic
    for (int li_int = OrdersHistoryTotal() - 1; li_int >= 0; li_int--)
    {
        if (OrderSelect (li_int, SELECT_BY_POS, MODE_HISTORY))
        {
            for (li_MG = 0; li_MG < cnt_MG; li_MG++)
            {
                if (OrderMagicNumber() == ar_MG[li_MG])
                {
                    if (fdt_Begin < OrderCloseTime())
                    {
                        ld_Profit = OrderProfit() + OrderCommission() + OrderSwap();
                        gda_MCProfitTP[li_MG] += ld_Profit;
                        gia_MCHistoryOrdersTP[li_MG]++;
                    }
                }
            }
        }
    }
    ls_txt = StringConcatenate ("Result from ", TS_DM (fdt_Begin), " to ", TS_DM (gdt_curTime), ":\n");
    ArrayInitialize (ar_Koef, 0);
    //---- Формируем коэффициент "добавочного" лота
    for (li_MG = 0; li_MG < cnt_MG; li_MG++)
    {
        ld_K = gda_MCProfitTP[li_MG] / fd_KY;
        if (ld_K < 0)      // убыток
        {ld_K = 0;}        // инвест-капитал не задействуем
        else
        if (ld_K == 0)     // примем за начало работы
        {ld_K = fd_K_Begin;}
        else               // прибыль
        {ld_K += fd_K_Begin;}
        ar_Koef[li_MG] = ld_K;
        ls_result = StringConcatenate (ls_result, "Magic = ", ar_MG[li_MG], "; Profit = ", DoubleToStr (gda_MCProfitTP[li_MG], gi_dig), "; K = ", DoubleToStr (ld_K, gi_dig), ";", IIFs ((li_MG == cnt_MG - 1), " ", "\n"));
        //GlobalVariableSet (StringConcatenate (lsa_NameGV[li_MG], "#Koef"), ld_K);
    }
    if (PrintCom) Print (ls_txt, ls_result);
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fGet_KoefResultTrade()");
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|         Формируем массив Магиков управляющих стратегий                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_MasterMagics (int& iar_MG[])          // возвращаемый массив рабочих стратегий\"портфелей"
{  
    if (Allowed_Magics != "")
    {return;}
    int  li_MG, li_cnt_MG = 0, err = GetLastError();
    bool lb_result;
//----
	 ArrayResize (iar_MG, 50);
	 ArrayInitialize (iar_MG, -1.0);
	 for (int i = OrdersHistoryTotal() - 1; i >= 0; i--)
    {
        if (OrderSelect (i, SELECT_BY_POS, MODE_HISTORY))
        {
            lb_result = false;
            for (li_MG = 0; li_MG < cnt_MG; li_MG++)
            {
                if (OrderMagicNumber() == iar_MG[li_MG])
                {lb_result = true; break;}
            }
            if (!lb_result)
            {
                iar_MG[li_cnt_MG] = OrderMagicNumber();
                li_cnt_MG++;
            }
        }
    }
    cnt_MG = li_cnt_MG;
	 ArrayResize (iar_MG, li_cnt_MG);
	 ArraySort (iar_MG);
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fGet_MasterMagics()");
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       Получаем Magic Master-ордера из коммента ведомого                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_MasterMagicFromComment (int fi_MG)
{
    int pos1, pos2, li_Ticket = -1;
    string ls_Comment;
//----
    if (fi_MG == OrderMagicNumber())
    {
        ls_Comment = OrderComment();
        pos1 = StringFind (ls_Comment, ";", 0) + 1;
        pos2 = StringFind (ls_Comment, "#", pos1);
        if (pos1 > 0 && pos2 > 0)
        {li_Ticket = StrToInteger (StringTrimLeft (StringTrimRight (StringSubstr (ls_Comment, pos1, pos2 - pos1))));}
    }
//----
    return (li_Ticket);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       Формируем массив данных из GV и структурируем его согласно базовому массиву |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fArrangeTwoArrays (string fs_prefNameGV, // префикс имени GV-пременной
                        int ar_MG[],          // массив магиков управляющих ордеров
                        double& ar_Value[])   // упорядоченный массив с инфой о каждом из Магиков
{
    //fArrangeTwoArrays (gs_ICNameGV, gia_Magic, gda_ICMaxLOSS);
    string ls_Name;
    int    li_MG, li_pos, li_size = ArraySize (ar_MG);
//----
    ArrayResize (ar_Value, li_size);
    ArrayInitialize (ar_Value, 0.0);
    for (int li_GV = GlobalVariablesTotal() - 1; li_GV >= 0; li_GV--)
    {
        ls_Name = GlobalVariableName (li_GV);
        //---- Идентифицируем переменные советника по префиксу
        if (StringFind (ls_Name, fs_prefNameGV) == 0)
        {
            //---- Идентифицируем переменные советника по имени
            li_pos = StringFind (ls_Name, "_#MaxLOSS_");
            if (li_pos > 0)
            {
                for (li_MG = 0; li_MG < li_size; li_MG++)
                {
                    if (StringFind (ls_Name, DoubleToStr (ar_MG[li_MG], 0), li_pos) > 0)
                    {
                        ar_Value[li_MG] = GlobalVariableGet (ls_Name);
                        break;
                    }
                }
            }
        }
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fArrangeTwoArrays()");
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|       Проверяем Magic                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fCheckMyMagic (int fi_Magic, int ar_Magic[])
{
//----
    for (int li_int = 0; li_int < ArraySize (ar_Magic); li_int++)
    {
        if (fi_Magic == ar_Magic[li_int])
        {return (li_int);}
    }
//----
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Работа с графическими объектами                                           |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Описание : Установка объекта OBJ_LABEL                                           |
//+-----------------------------------------------------------------------------------+
//|  Параметры:                                                                       |
//|    name - наименование объекта                                                    |
//|    text - сам объект                                                              |
//|    X - координата X                                                               |
//|    Y - координата Y                                                               |
//|    size - размер объекта                                                          |
//|    Font - шрифт объекта                                                           |
//|    Corner - угол                                      (0  - по умолчанию)         |
//|    Angle - угол                                       (0  - по умолчанию)         |
//|    CL - цвет                                          (CLR_NONE - по умолчанию)   |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void SetLabel (string name, string text, int X, int Y, int size = 10, string Font = "Calibri",
               int Corner = 0, int Angle = 0, color CL = CLR_NONE)
{
    if (ObjectFind (name) == -1)
    {ObjectCreate (name, OBJ_LABEL, 0, 0, 0);}
    ObjectSet (name, OBJPROP_COLOR, CL);
    ObjectSet (name, OBJPROP_XDISTANCE, X);
    ObjectSet (name, OBJPROP_YDISTANCE, Y);
    ObjectSet (name, OBJPROP_CORNER, Corner);
    if (Angle > 0)
    {ObjectSet (name, OBJPROP_ANGLE, Angle);}
    if (text != "")
    {ObjectSetText (name, text, size, Font, CL);}
//----
    return;   
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                ПОИСК ГРАФИЧЕСКОГО ОБЪЕКТА                                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool FindObject (string fs_Name)
{
    string ls_Name;
    int err = GetLastError();
//----
    if (StringLen (fs_Name) == 0)
    {return (false);}
    if (ObjectFind (fs_Name) != -1)
    {return (true);}
    for (int li_OBJ = ObjectsTotal() - 1; li_OBJ >= 0; li_OBJ--)
    {
        ls_Name = ObjectName (li_OBJ);
        if (ls_Name == fs_Name)
        {return (true);}
    }
    //---- Контролируем возможные ошибки
	 fGetLastError (gs_ComError, StringConcatenate ("FindObject()[", fs_Name, "]"));
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|             УЛДАЛЕНИЕ ГРАФИЧЕСКИХ ОБЪЕКТОВ                                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fObjectsDeleteAll  (int fi_Window = -1,      // номер окна
                         int ti_Type = -1,        // тип объекта
                         string fs_Pref = "")     // префикс имени объекта
{
    string ls_Name; 
//----
	 for (int li_OBJ = ObjectsTotal() - 1; li_OBJ >= 0; li_OBJ--)
	 {
        ls_Name = ObjectName (li_OBJ);
        //---- Пропускаем "не своё" окно
        if (fi_Window >= 0)
        {
            if (ObjectFind (ls_Name) != fi_Window) 
            {continue;}
        }
        //---- Пропускаем "не наш" тип объекта
		  if (ti_Type >= 0)
		  {
		      if (ObjectType (ls_Name) != ti_Type) 
		      {continue;}
		  }
		  if (fs_Pref != "")
		  {
		     if (StringSubstr (ls_Name, 0, StringLen (fs_Pref)) != fs_Pref) 
		     {continue;}
		  }
		  ObjectDelete (ls_Name);
	 }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Работа с ошибками                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|     Функция ErrorDescription() возвращает не код ошибки а её словестное описание  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string ErrorDescription (int error_code)
{
    string error_string;
//---- 
    switch (error_code)
    {
         //---- codes returned from trade server
         case 0:      return ("НЕТ !!!");
         case 1:      error_string = "Попытка изменить уже установленные значения такими же значениями."; break;
         case 2:      error_string = "Общая ошибка. Прекратить все попытки торговых операций до выяснения обстоятельств."; break;
         case 3:      error_string = "В торговую функцию переданы неправильные параметры."; break;
         case 4:      error_string = "Торговый сервер занят."; break;
         case 5:      error_string = "Старая версия клиентского терминала."; break;
         case 6:      error_string = "Нет связи с торговым сервером."; break;
         case 7:      error_string = "Недостаточно прав."; break;
         case 8:      error_string = "Слишком частые запросы."; break;
         case 9:      error_string = "Недопустимая операция нарушающая функционирование сервера."; break;
         case 64:     error_string = "Счет заблокирован. Необходимо прекратить все попытки торговых операций."; break;
         case 65:     error_string = "Неправильный номер счета."; break;
         case 128:    error_string = "Истек срок ожидания совершения сделки."; break;
         case 129:    error_string = "Неправильная цена bid или ask, возможно, ненормализованная цена."; break;
         case 130:    error_string = "Слишком близкие стопы или неправильно рассчитанные или ненормализованные цены в стопах (или в цене открытия отложенного ордера)."; break;
         case 131:    error_string = "Неправильный объем, ошибка в грануляции объема."; break;
         case 132:    error_string = "Рынок закрыт."; break;
         case 133:    error_string = "Торговля запрещена."; break;
         case 134:    error_string = "Недостаточно денег для совершения операции."; break;
         case 135:    error_string = "Цена изменилась."; break;
         case 136:    error_string = "Нет цен."; break;
         case 137:    error_string = "Брокер занят."; break;
         case 138:    error_string = "Запрошенная цена устарела, либо перепутаны bid и ask."; break;
         case 139:    error_string = "Ордер заблокирован и уже обрабатывается."; break;
         case 140:    error_string = "Разрешена только покупка. Повторять операцию SELL нельзя."; break;
         case 141:    error_string = "Слишком много запросов."; break;
         case 142:    error_string = "Ордер поставлен в очередь."; break;
         case 143:    error_string = "Ордер принят дилером к исполнению."; break;
         case 144:    error_string = "Ордер аннулирован самим клиентом при ручном подтверждении сделки."; break;
         case 145:    error_string = "Модификация запрещена, так как ордер слишком близок к рынку и заблокирован из-за возможного скорого исполнения."; break;
         case 146:    error_string = "Подсистема торговли занята."; break;
         case 147:    error_string = "Использование даты истечения ордера запрещено брокером."; break;
         case 148:    error_string = "Количество открытых и отложенных ордеров достигло предела, установленного брокером."; break;
         case 149:    error_string = "Попытка открыть противоположную позицию к уже существующей в случае, если хеджирование запрещено."; break;
         case 4000:   return ("");
         case 4001:   error_string = "Неправильный указатель функции."; break;
         case 4002:   error_string = "Индекс массива - вне диапазона."; break;
         case 4003:   error_string = "Нет памяти для стека функций."; break;
         case 4004:   error_string = "Переполнение стека после рекурсивного вызова."; break;
         case 4005:   error_string = "На стеке нет памяти для передачи параметров."; break;
         case 4006:   error_string = "Нет памяти для строкового параметра."; break;
         case 4007:   error_string = "Нет памяти для временной строки."; break;
         case 4008:   error_string = "Неинициализированная строка."; break;
         case 4009:   error_string = "Неинициализированная строка в массиве."; break;
         case 4010:   error_string = "Нет памяти для строкового массива."; break;
         case 4011:   error_string = "Слишком длинная строка."; break;
         case 4012:   error_string = "Остаток от деления на ноль."; break;
         case 4013:   error_string = "Деление на ноль."; break;
         case 4014:   error_string = "Неизвестная команда."; break;
         case 4015:   error_string = "Неправильный переход."; break;
         case 4016:   error_string = "Неинициализированный массив."; break;
         case 4017:   error_string = "Вызовы DLL не разрешены."; break;
         case 4018:   error_string = "Невозможно загрузить библиотеку."; break;
         case 4019:   error_string = "Невозможно вызвать функцию."; break;
         case 4020:   error_string = "Вызовы внешних библиотечных функций не разрешены."; break;
         case 4021:   error_string = "Недостаточно памяти для строки, возвращаемой из функции."; break;
         case 4022:   error_string = "Система занята."; break;
         case 4050:   error_string = "Неправильное количество параметров функции."; break;
         case 4051:   error_string = "Недопустимое значение параметра функции."; break;
         case 4052:   error_string = "Внутренняя ошибка строковой функции."; break;
         case 4053:   error_string = "Ошибка массива."; break;
         case 4054:   error_string = "Неправильное использование массива-таймсерии."; break;
         case 4055:   error_string = "Ошибка пользовательского индикатора."; break;
         case 4056:   error_string = "Массивы несовместимы."; break;
         case 4057:   error_string = "Ошибка обработки глобальныех переменных."; break;
         case 4058:   error_string = "Глобальная переменная не обнаружена."; break;
         case 4059:   error_string = "Функция не разрешена в тестовом режиме."; break;
         case 4060:   error_string = "Функция не подтверждена."; break;
         case 4061:   error_string = "Ошибка отправки почты."; break;
         case 4062:   error_string = "Ожидается параметр типа string."; break;
         case 4063:   error_string = "Ожидается параметр типа integer."; break;
         case 4064:   error_string = "Ожидается параметр типа double."; break;
         case 4065:   error_string = "В качестве параметра ожидается массив."; break;
         case 4066:   error_string = "Запрошенные исторические данные в состоянии обновления."; break;
         case 4067:   error_string = "Ошибка при выполнении торговой операции."; break;
         case 4099:   error_string = "Конец файла."; break;
         case 4100:   error_string = "Ошибка при работе с файлом."; break;
         case 4101:   error_string = "Неправильное имя файла."; break;
         case 4102:   error_string = "Слишком много открытых файлов."; break;
         case 4103:   error_string = "Невозможно открыть файл."; break;
         case 4104:   error_string = "Несовместимый режим доступа к файлу."; break;
         case 4105:   error_string = "Ни один ордер не выбран."; break;
         case 4106:   error_string = "Неизвестный символ."; break;
         case 4107:   error_string = "Неправильный параметр цены для торговой функции."; break;
         case 4108:   error_string = "Неверный номер тикета."; break;
         case 4109:   error_string = "Торговля не разрешена. Необходимо включить опцию Разрешить советнику торговать в свойствах эксперта."; break;
         case 4110:   error_string = "Длинные позиции не разрешены. Необходимо проверить свойства эксперта."; break;
         case 4111:   error_string = "Короткие позиции не разрешены. Необходимо проверить свойства эксперта."; break;
         case 4200:   error_string = "Объект уже существует."; break;
         case 4201:   error_string = "Запрошено неизвестное свойство объекта."; break;
         case 4202:   error_string = "Объект не существует."; break;
         case 4203:   error_string = "Неизвестный тип объекта."; break;
         case 4204:   error_string = "Нет имени объекта."; break;
         case 4205:   error_string = "Ошибка координат объекта."; break;
         case 4206:   error_string = "Не найдено указанное подокно."; break;
         case 4207:   error_string = "Ошибка при работе с объектом."; break;
    }  
//---- 
    return (error_string);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|     Получаем номер и описание последней ошибки и выводим в массив комментов       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGetLastError (string& Comm_Error, string Com = "")
{
    int err = GetLastError();
    string ls_err;
//---- 
    if (err > 0 && err != 4202 && err != 4099)
    {
        ls_err = StringConcatenate (Com, ": Ошибка № ", err, ": ", ErrorDescription (err));
        Print (ls_err);
        Comm_Error = ls_err;
    }
//---- 
    return (err);
}
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Работа с GV-переменными                                                   |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|           Восстанавливаем из GV необходимые данные                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fGet_GlobalVariable (string fs_Name)
{
    int    err = GetLastError();
    string ls_Name, lsa_Name[] = {"_#LOSS","_#BeginBalance","_#min_EQUITY","_#min_EQUITY_Percent","_#min_MARGIN",
                                  "_#min_MARGIN_Percent","_#ZALOG","_#ZALOG_Percent","_#LastBegin_TP"};
//----
    for (int li_GV = 0; li_GV < ArraySize (lsa_Name); li_GV++)
    {
        ls_Name = StringConcatenate (fs_Name, lsa_Name[li_GV]);
        if (GlobalVariableCheck (ls_Name))
        {
            switch (li_GV)
            {
                case 0: gd_ICMaxLOSS = GlobalVariableGet (ls_Name); break;
                case 1: gd_MICBeginBalance = GlobalVariableGet (ls_Name); break;
                case 2: gd_MICMinEquity = GlobalVariableGet (ls_Name); break;
                case 3: gd_MICMinEquityPercent = GlobalVariableGet (ls_Name); break;
                case 4: gd_MICMinMargin = GlobalVariableGet (ls_Name); break;
                case 5: gd_MICMinMarginPercent = GlobalVariableGet (ls_Name); break;
                case 6: gd_MICMaxZalog = GlobalVariableGet (ls_Name); break;
                case 7: gd_MICMaxZalogPercent = GlobalVariableGet (ls_Name); break;
                case 8: gdt_LastBegin_TP = GlobalVariableGet (ls_Name); break;
            }
        }
    }
    for (int li_int = 0; li_int < ArraySize (gdta_CommTime); li_int++)
    {
        ls_Name = StringConcatenate (fs_Name, "_#CommTime", li_int);
        if (GlobalVariableCheck (ls_Name))
        {
            gdta_CommTime[li_int] = GlobalVariableGet (ls_Name);
            if (gdta_CommTime[li_int] == 0)
            {gdta_CommTime[li_int] = gdt_curTime;}
        }
    }
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fGet_GlobalVariable()");
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|           Сохраняем в GV необходимые данные                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fSet_GlobalVariable (string fs_Name)
{
    int li_SMB, err = GetLastError();
//----
    GlobalVariableSet (StringConcatenate (fs_Name, "_#LOSS"), ND0 (gd_ICMaxLOSS));
    GlobalVariableSet (StringConcatenate (fs_Name, "_#ZALOG"), ND0 (gd_MICMaxZalog));
    GlobalVariableSet (StringConcatenate (fs_Name, "_#ZALOG_Percent"), ND0 (gd_MICMaxZalogPercent));
    GlobalVariableSet (StringConcatenate (fs_Name, "_#min_EQUITY"), ND0 (gd_MICMinEquity));
    GlobalVariableSet (StringConcatenate (fs_Name, "_#min_EQUITY_Percent"), ND0 (gd_MICMinEquityPercent));
    GlobalVariableSet (StringConcatenate (fs_Name, "_#min_MARGIN"), ND0 (gd_MICMinMargin));
    GlobalVariableSet (StringConcatenate (fs_Name, "_#min_MARGIN_Percent"), ND0 (gd_MICMinMarginPercent));
    GlobalVariableSet (StringConcatenate (fs_Name, "_#LastBegin_TP"), gdt_LastBegin_TP);
    for (int li_int = 0; li_int < ArraySize (gdta_CommTime); li_int++)
    {GlobalVariableSet (StringConcatenate (fs_Name, "_#CommTime", li_int), gdta_CommTime[li_int]);}
    for (li_int = 0; li_int < ArraySize (gda_ICMaxLOSS); li_int++)
    {GlobalVariableSet (StringConcatenate (fs_Name, "_#MaxLOSS_", gia_Magic[li_int]), gda_ICMaxLOSS[li_int]);}
    //---- Контролируем возможные ошибки
    fGetLastError (gs_ComError, "fSet_GlobalVariable()");
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  SP:      Периодически обновляем статистику по счёту                              |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fUpdate_StatisticGV (string Name)
{
    static datetime dt_Time; 
//----
    if (dt_Time != iTime (Symbol(), 60, 0))
    {
        GlobalVariableSet (StringConcatenate (Name, "_#min_EQUITY"), ND0 (gd_MICMinEquity));
        GlobalVariableSet (StringConcatenate (Name, "_#min_EQUITY_Percent"), ND0 (gd_MICMinEquityPercent));
        GlobalVariableSet (StringConcatenate (Name, "_#ZALOG"), ND0 (gd_MICMaxZalog));
        GlobalVariableSet (StringConcatenate (Name, "_#ZALOG_Percent"), ND0 (gd_MICMaxZalogPercent));
        GlobalVariableSet (StringConcatenate (Name, "_#min_MARGIN"), ND0 (gd_MICMinMargin));
        GlobalVariableSet (StringConcatenate (Name, "_#min_MARGIN_Percent"), ND0 (gd_MICMinMarginPercent));
        dt_Time = iTime (Symbol(), 60, 0);
    }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Tester                                                                    |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|           Открываем ордера (для проверки работы кода)                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
/*void fTestSendOrders (int ar_MG[])
{
    static datetime ldt_NewBar;
    static int cnt_ord = 0, li_InTesterOrder = 1;
    int li_SL = 100, li_TP = 100, li_Decimal = 1,
        li_Ticket = 0;
    //---- Выставляем в тестовом режиме ордера
    if (gi_Digits == 3 || gi_Digits == 5)
    {li_Decimal = 10;}
    li_SL *= li_Decimal;
    li_TP *= li_Decimal;
    if (IsTesting())
    {
        if (ldt_NewBar != iTime (Symbol(), PERIOD_H4, 0) && OrdersTotal() < cnt_MG)
        {
            for (int li_MG = 0; li_MG < cnt_MG; li_MG++)
            {
                if (cnt_ord == li_MG)
                {
                    if (isLossLastPos ("", -1, ar_MG[li_MG]))
                    {li_InTesterOrder = IIFd ((li_InTesterOrder == 1), -1, 1);}
                    if (li_InTesterOrder == 1)
                    {li_Ticket = OrderSend (Symbol(), OP_BUY, gd_MinLot, Ask, 0, NormalizeDouble (Bid - li_SL * Point, Digits), NormalizeDouble (Bid + li_TP * Point, Digits), "tester", ar_MG[li_MG], 0, Blue);}
                    if (li_InTesterOrder == -1)
                    {li_Ticket = OrderSend (Symbol(), OP_SELL, gd_MinLot, Bid, 0, NormalizeDouble (Ask + li_SL * Point, Digits), NormalizeDouble (Ask - li_TP * Point, Digits), "tester", ar_MG[li_MG], 0, Red);}
                    if (OrderSelect (li_Ticket, SELECT_BY_TICKET))
                    {OrderPrint();}
                    cnt_ord++;
                    if (cnt_ord == cnt_MG)
                    {cnt_ord = 0;}
                    ldt_NewBar = iTime (Symbol(), PERIOD_H4, 0);
                    break;
                }
            }
            return;
        }
    }
}*/
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                          |
//+-----------------------------------------------------------------------------------+
//|  Версия   : 19.02.2008                                                            |
//|  Описание : Возвращает флаг убыточности последней позиции.                        |
//+-----------------------------------------------------------------------------------+
//|  Параметры:                                                                       |
//|    sy - наименование инструмента   (""   - любой символ,                          |
//|                                     NULL - текущий символ)                        |
//|    op - операция                   (-1   - любая позиция)                         |
//|    mn - MagicNumber                (-1   - любой магик)                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
/*bool isLossLastPos (string sy = "", int op = -1, int mn = -1)
{
    datetime t;
    int i, j = -1, k = OrdersHistoryTotal();
//----
    sy = IIFs ((sy == "0" || sy == ""), Symbol(), sy);
    for (i = k - 1; i >= 0; i--)
    {
        if (OrderSelect (i, SELECT_BY_POS, MODE_HISTORY))
        {
            if (OrderSymbol() == sy && (mn < 0 || OrderMagicNumber() == mn))
            {
                if (OrderType() == OP_BUY || OrderType() == OP_SELL)
                {
                    if (op < 0 || OrderType() == op)
                    {
                        if (t < OrderCloseTime())
                        {
                            t = OrderCloseTime();
                            j = i;
                        }
                    }
                }
            }
        }
    }
    if (OrderSelect (j, SELECT_BY_POS, MODE_HISTORY))
    {
        if (OrderProfit() < 0)
        {return (True);}
    }
//----
    return (False);
}*/
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

