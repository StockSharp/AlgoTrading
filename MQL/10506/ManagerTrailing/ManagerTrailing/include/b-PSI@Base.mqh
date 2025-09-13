//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                                    b-PSI@Base.mqh |
//|                                       Copyright © 2012, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//| 17.03.2012  Библиотека базовых функций.                                           |
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
extern int   ProfitMIN_Pips      = 3;              // Минимальный профит в пп.
//IIIIIIIIIIIIIIIIIII========Глобальные переменные модуля=======IIIIIIIIIIIIIIIIIIIIII+
double       bd_Price,                             // текущая цена по текущему инструменту
             bda_Price[2],                         // текущие цены (0 - Bid; 1 - Ask)
             bd_SL, bd_TP,                         // СТОПы
             bd_SymPoint,                          // Point текущего инструмента
             bd_Spread,                            // spread текущего инструмента
             bd_OpenPrice,                         // цена открытия текущего ордера
             bd_MINLOT, bd_LOTSTEP, bd_MAXLOT,
             bd_STOPLEVEL,                         // текущий уровень STOPLEVEL по инструменту
             bd_ProfitMIN,                         // размер БезУбытка
             bd_TrailingStop, bd_TrailStep, bd_BreakEven,
             bda_MMValue[5],                       // массив управляющих переменных библиотеки MM
             bd_ProfitCUR,                         // общий профит советника в валюте Депо
             bd_ProfitPercent,                     // общий профит советника в процентах
             bd_Pribul = 0.0,                      // результаты работы советника
             bd_MinEquity,                         // значение минимального эквити счёта в валюте Депо
             bd_MinEquityPercent,                  // значение минимального эквити счёта в процентах
             bd_curEquityPercent,                  // значение текущего эквити счёта в процентах
             bd_MinMargin,                         // минимальное значение свободных средств, разрешенных для открытия позиций
             bd_MinMarginPercent,                  // минимальное значение свободных средств, разрешенных для открытия позиций в процентах
             bd_curMarginPercent,                  // текущее значение свободных средств, разрешенных для открытия позиций в процентах
             bd_MaxZalog,                          // значение максимального залога в валюте Депо
             bd_MaxZalogPercent,                   // значение максимального залога в процентах
             bd_curZalogPercent,                   // значение текущего залога в процентах
             bd_BeginBalance = 0.0,                // начальное значение Баланса
             bd_MaxLOSS,                           // значение максимальной просадки по счёту
             bd_MaxLOSSPercent,                    // значение максимальной просадки по счёту в процентах
             bd_MAXBalance,                        // значение максимального баланса (для слежения за MAXOtkatDepoPercent)
             bd_curLOSSPercent,                    // текущее значение просадки по счёту в процентах
             bd_curMAXOtkatDepoPercent,            // текущий размер лосса от bd_MAXBalance
             //bd_LastLots = 0.0,                    // размер лота последнего убыточного ордера
             bd_Trail,                             // размер текущего трейлинга (м\у SL и ценой)
             bd_BaseBalance,                       // результаты работы советника по выделенному капиталу
             bd_LastBalance,                       // начальный баланс серии ордеров          
             bd_TP_Adv,                            // тэйк текущей прибыли советника в выбранных единицах (ValueInCurrency)
             bd_SL_Adv,                            // SL текущего убытка советника в выбранных единицах (ValueInCurrency)
             bd_Balance, bd_Equity, bd_FreeMargin, bd_Margin,
             bd_NewSL, bd_NewTP, bd_curSL, bd_curTP,
             bda_FIBO[] = {0.0,0.236,0.382,0.5,0.618,0.764};
int          bi_curOrdersInSeries = 0,             // текущее количество ордеров в убыточной серии
             bi_MyOrders = 0,                      // счётчик "своих" ордеров
             bi_cntTrades = 0,                     // дополнительные ордера при экстремальной торговле
             bi_Error = 0,                         // номер последней ошибки
             bi_curBarControlPeriod,               // текущий период контроля рабочего времени
             bi_SymDigits, bi_Decimal, bi_digit, bi_indERR, bi_Type, bi_ShiftLocalDC_sec,
             bi_curHOUR, bi_curDAY,
             bia_Periods[] = {1,5,15,30,60,240,1440,10080,43200},
             bia_PartClose_Levels[], bia_PartClose_Percents[];
bool         bb_RealTrade,                         // флаг работы on-line
             bb_VirtualTrade,                      // флаг оптимизации
             bb_OptimContinue,                     // флаг продолжения оптимизации
             bb_MMM = false,                       // MaxLot More MAXLOT
             bb_ClearGV = false,                   // флаг очистки отработанных GV-переменных
             bb_PrintCom,                          // флаг печать комментариев
             bb_ShowCom,                           // флаг показа комментариев на графике
             bb_CreatVStopsInChart,                // флаг создагия на чарте виртуальных СТОПов
             bb_PlaySound;                         // флаг звукового сопровождения событий
string       bsa_Comment[8],                       // массив с комментариями
             // bsa_Comment[0]- работа MM
             // bsa_Comment[1]- работа TimeControl
             // bsa_Comment[2]- информация по модификации ордеров и виртуальному трейлингу (Trail)
             // bsa_Comment[3]- информация по открытию ордеров (TradeLight)
             // bsa_Comment[4]- информация по закрытию ордеров (TradeLight)
             // bsa_Comment[5]- частичное закрытие ордеров (PartClose) и виртуальные СТОПы (VirtuaSTOPs)
             // bsa_Comment[6]- работа с общим профитом (ManagerPA)
             // bsa_Comment[7]- ошибки
      	    bs_libNAME,                           // имя работающей библиотеки
             bs_NameGV = "",                       // префикс GV-перменных терминала
             bs_ExpertName,                        // имя эксперта, выводимое на график
             bs_SymbolList = "",                   // лист управляемых советником символов
             bs_MagicList,                         // лист управляемых советником магиков
             bs_Delimiter,                         // Разделитель переменных в листе bs_MagicList
             bsa_prefGV[0],                        // массив с префиксами временных GV-перменных
             bs_ErrorTL = "",                      // переменная для сбора информации об ошибках
             bs_Symbol,                            // текущий инструмент
             bs_fName,                             // имя текущей функции
             bsa_sign[2],                          // комплект значков размерности параметров (валюта, %)
             bs_sign;                              // значёк валюты депозита
datetime     bdt_curTime,
             bdt_NewBar, bdt_BeginNewBar,
             bdt_LastTime,                         // последнее известное время
             bdt_BeginTrade, bdt_LastTrade = 0,
             bdt_LastBalanceTime, bdt_NewBarInPeriod, bdta_CommTime[6];
//IIIIIIIIIIIIIIIIIII=========Подключение внешних модулей=======IIIIIIIIIIIIIIIIIIIIII+
#include     <stdlib.mqh>                          // Библиотека расшифровки ошибок
#include     <stderror.mqh>                        // Библиотека кодов ошибок
//#include     <b-PSI@MineGV.mqh>                    // Библиотека работы с базовыми GV-переменными
//#include     <b-PSI@GrafOBJ.mqh>                   // Библиотека рисования объектов на графике
//#include     <b-PSI@Comment.mqh>                   // Библиотека работы с комментариями
#import "b-PSI@GrafOBJ.ex4"
bool fCreate_FIBO (string fs_Name,                 // префикс имени объектов
                   int fi_Level,                   // количество рисуемых уровней
                   double ar_FiboLevel[],          // массив с FIBO-уровнями
                   datetime T1,                    // первая координата времени
                   double P1,                      // первая координата цены
                   datetime T2,                    // вторая координата времени
                   double P2);                     // вторая координата цены
                                     // Рисуем на графике FIBO-сетку
bool fCreat_OBJ (string fs_Name,                   // имя объекта
                 int fi_OBJ,                       // тип объекта
                 string fs_Description,            // описание объекта
                 int fi_FontSize,                  // размер шрифта текста
                 datetime fdt_Time1,               // 1-я координата времени
                 double fd_Price1,                 // 1-я координата цены
                 bool fb_Ray = true,               // свойство луч для OBJ_TREND
                 color fc_Color = Gold,            // цвет
                 datetime fdt_Time2 = 0,           // 2-я координата времени
                 double fd_Price2 = 0);            // 2-я координата цены
                                     // Рисуем OBJ_TREND, OBJ_HLINE, OBJ_VLINE, OBJ_TEXT
bool fSet_Arrow (string fs_Name,                   // имя объекта
                 int fi_ArrowCode,                 // номер значка для OBJ_ARROW
                 string fs_Description,            // описание объекта
                 int fi_Size = 0,                  // размер значка
                 color fc_Color = Gold,            // цвет
                 datetime fdt_Time = 0,            // координата времени
                 double fd_Price = 0);             // координата цены
                                     // Установка объекта OBJ_ARROW
bool fSet_Label (string fs_Name,                   // наименование объекта
                 string fs_Text,                   // сам объект
                 int fi_X,                         // координата X
                 int fi_Y,                         // координата Y
                 int fi_Size = 10,                 // размер шрифта объекта
                 string fs_Font = "Calibri",       // шрифт объекта
                 int fi_Corner = 0,                // угол привязки графика
                 int fi_Angle = 0,                 // угол по отношению к горизонту
                 color fc_CL = CLR_NONE);          // цвет
                                     // Установка объекта OBJ_LABEL
void fObjectsDeleteAll  (string fs_Pref = "",      // префикс имени объекта
                         int ti_Type = -1,         // тип объекта
                         int fi_Window = -1);      // номер окна
                                     // Удаляем графические объекты с графика
bool fObjectFind (string fs_Name);   // Ищем графический объект на чарте
bool fObjectDelete (string fs_Name); // Удаляем графический объект
#import
//IIIIIIIIIIIIIIIIIII===========Перечень функций модуля=========IIIIIIIIIIIIIIIIIIIIII+
//void fInit_Base (string fs_SymbolList,           // Лист рабочих валютных пар
                 //string fs_MagicList,            // Лист рабочих Магиковbool fb_ShowCom = true,   // Разрешение на показ комментариев на графике
                 //bool fb_ShowCom = true,         // Разрешение на показ комментариев на графике
                 //bool fb_PrintCom = true,        // Разрешение на печать комментариев
                 //bool fb_PlaySound = true)       // Разрешение на звуковое сопровождение событий
                 //bool fb_CreatVStopsInChart = false,// Разрешение на рисование виртуальных СТОПов на чарте
                 //string fs_Delimiter = ",")      // Разделитель переменных в листе fs_MagicList
                                     // Инициализация модуля
//|***********************************************************************************|
//| РАЗДЕЛ: Общие функции                                                             |
//|***********************************************************************************|
//double fGet_TradePrice (int fi_Price,            // Цена: 0 - Bid; 1 - Ask
                        //bool fb_RealTrade,       // реальная торговля или оптимизация\тестирование
                        //string fs_Symbol = "")   // валютная пара
                                     // Запускаем в цикл получение рыночной цены
//int fControl_NewBar (int fi_TF = 0)// Функция, контролирующая приход нового бара
//datetime fGet_TermsTrade (string fs_SymbolList,  // Лист управляемых валютных пар
                          //string fs_MagicList,   // Лист управляемых Магиков
                          //datetime& fdt_LastTrade,// когда был последний ордер
                          //string fs_Delimiter = ",")// Разделитель переменных в листе fs_MagicList
                                     // Находим дату начала торговли по дате первого открытого ордера
//string fSplitField (string fs_Value)// Разделяем разряды чисел пробелами
//double fGet_Point (string fs_Symbol = "")
                                     // Функция, гарантированного получения Point
//bool fCCV_D (double param, int ix) // Фиксирует факт изменения проверяемого double параметра
//string fGet_NameOP (int fi_Type)   // Функция возвращает наименование торговой операции
//string fGet_NameTF (int fi_TF)     // Возвращает наименование таймфрейма
//int NDPD (double v)                // Функция "нормализации" значения по Point в целое число
//double NDP (int v)                 // Функция, перевода int в double по Point
//double ND0 (double v)              // Функция нормализации значения double до целого
//double NDD (double v)              // Функция нормализации значения по Digits
//double NDDig (double v)            // Функция, нормализации значения double до минимальной разрядности лота
//string DS0 (double v)              // Функция, перевода значения из double в string c нормализацией по 0
//string DSD (double v)              // Функция, перевода значения из double в string c нормализацией по Digits
//string DSDig (double v)            // Функция, перевода значения из double в string c нормализацией по минимальной разрядности лота
//int LotDecimal()                   // Функция, определения минимальной разрядности лота
//bool fCheck_NewBarInPeriod (int fi_Period = 0,   // TF
                            //bool fb_Conditions = true)// условие на проверку
                                     // Контролируем факт прихода нового бара на NewBarInPeriod периоде (если NewBarInPeriod >= 0)
//string fGet_SignCurrency()         // Функция возвращает значёк валюты депозита
//string CheckBOOL (int M)           // Возвращает наименование состояния (ДА\НЕТ)
//double IIFd (bool condition, double ifTrue, double ifFalse)
                                     // Возвращает одно из двух значений DOUBLE в зависимости от условия
//string IIFs (bool condition, string ifTrue, string ifFalse)
                                     // Возвращает одно из двух значений STRING в зависимости от условия
//color IIFc (bool condition, color ifTrue, color ifFalse)
                                     // Возвращает одно из двух значений COLOR в зависимости от условия
//|***********************************************************************************|
//| РАЗДЕЛ: Работы с массивами                                                        |
//|***********************************************************************************|
//int fGet_INDInArrayINT (int fi_Value, int ar_Array[])
//int fGet_INDInArraySTR (string fs_Value, string ar_Array[])
                                     // Получаем индекс искомого элемента в массиве
//int fGet_NumPeriods (int fi_Period)// Получаем номер периода графика
//InitializeArray_STR (string& PrepareArray[], string Value = "")
                                     // Инициализируем массив STRING
//void fCreat_ArrayGV (string& ar_Base[],          // рабочий массив
                     //string ar_Add[])            // добавляемы массив
                                     // Создаём массив имён временных GV-переменных
//int fSplitStrToStr (string fs_List,              // строка с данными
                    //string& ar_OUT[],            // возвращаемый массив
                    //string fs_Delimiter = ",")   // разделитель данных в строке
                                     // Возвращает массив STRING из строки, разделённой sDelimiter
//void fCreat_StrToInt (string ar_Value[],         // массив элементов string
                      //int& ar_OUT[],             // возвращаемый массив int
                      //int fi_IND,                // количество ячеек в массиве
                      //int fi_Factor = 1,         // множитель
                      //string fs_NameArray = "")  // имя возвращаемого массива
                                     // Возвращает массив INT из элементов массива STRING
//void fCreat_StrToDouble (string ar_Value[],      // массив элементов string
                         //double& ar_OUT[],       // возвращаемый массив double
                         //int fi_IND,             // количество ячеек в массиве
                         //double fd_Factor = 1.0, // множитель
                         //string fs_NameArray = "")// имя возвращаемого массива
                                     // Возвращает массив DOUBLE из элементов массива STRING
//string fCreat_StrAndArray (int fi_First,         // значение 1-го эелемента массива
                           //int& ar_OUT[],        // возвращаемый массив int
                           //int fi_cntIND,        // количество элементов в массиве
                           //string fs_Delimiter = ",")// разделитель элементов в возвращаемой строке
                                     // Возвращает строку из элементов массива INT и сам массив
//string fCreat_StrFromArray (string ar_Array[],   // массив со значениями
                            //string fs_Delimiter = ",")// разделитель элементов в возвращаемой строке
                                     // Возвращает строку из элементов массива, разделённых fs_Delimiter
//|***********************************************************************************|
//| РАЗДЕЛ: Работа с ошибками                                                         |
//|***********************************************************************************|
//int fGet_LastErrorInArray (string& Comm_Array[], // возвращаемый массив комментов
                           //string Com = "",      // дополнительная информация к сообщению об ошибке
                           //int index = -1)       // индекс ячейки в которую заносим сообщение об ошибке
                                     // Получаем номер и описание последней ошибки и выводим в массив комментов
//int fGet_LastError (string& Comm_Error, string Com = "")
                                     // Получаем номер и описание последней ошибки
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
//double fGet_ValueFromGV (string fs_Name,         // имя GV-переменной
                         //double fd_Value,        // если такой переменной нет, подставляемое значение
                         //bool fd_Condition = true)// условие на проверку наличия GV-переменной
                                     // Берём значение переменной или из GV, или (при её отстутствии) fd_Value
//bool fDraw_VirtSTOP (int fi_CMD,         // BUY - 1; SELL = -1
                     //int fi_OP,          // Тип СТОПа (0 - SL; 1 - TP)
                     //double fd_Level)    // Уровень СТОПа
                                     // Рисуем на графике виртуальный уровень
//void fWork_LineBU (double ar_Lots[])             // массив объёмов открытых рыночных ордеров
                               // Рисуем на чарте уровень бузубыточности
//void fDelete_MyObjectsInChart (string fs_Name,   // префикс имени объектов
                               //bool fb_Condition = true)// разрешение на удаление
                                     // Подчищаем за собой все отработанные объекты на графике
//void fClear_GV (int fi_cnt = 5000)               // кол-во очищаемых объектов (тикетов)
                                     // Подчищаем возможно оставшиеся GV-переменные
//bool fSTOPTRADE()                  // ПРЕДУПРЕЖДЕНИЕ
//|***********************************************************************************|
//| РАЗДЕЛ: MONEY MANAGEMENT                                                          |
//|***********************************************************************************|
//double fLotsNormalize (double fd_Lots)          // размер лота
                                     // Производим нормализацию лота
//double fGet_PipsValue()            // Определение стоимости пункта выделенного ордера
//bool fCheck_MinProfit (double fd_MinProfit,     // размер минимальной прибыли по ордеру
                       //double fd_NewSL,         // новый SL
                       //bool fb_Condition = true)// условие на проведение проверки
                                     // Проверяем условие ни минимальную прибль по ордеру
//double fGet_BreakEven (string fs_Symbol,        // Symbol
                       //double ar_Lots[],        // массив объёмов открытых рыночных ордеров
                       //double fd_Profit)        // текущий профит по открытым ордера
                                     // Определение Уровня БезУбытка по символу
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Инициализация модуля                                                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fInit_Base (string fs_SymbolList,               // Лист рабочих валютных пар
                 string fs_MagicList,                // Лист рабочих Магиков
                 bool fb_ShowCom = true,             // Разрешение на показ комментариев на графике
                 bool fb_PrintCom = true,            // Разрешение на печать комментариев
                 bool fb_PlaySound = true,           // Разрешение на звуковое сопровождение событий
                 bool fb_CreatVStopsInChart = false, // Разрешение на рисование виртуальных СТОПов на чарте
                 string fs_Delimiter = ",")          // Разделитель переменных в строке рабочих Магиков
{
//----
    //---- Фиксируем разрядность котировок
    if (Digits % 2 == 1) {bi_Decimal = 10;} else {bi_Decimal = 1;}
    //---- Формируем префикс GV-переменных
    //if (IsTesting()) {bs_NameGV = bs_NameGV + "_t";}
    //if (IsDemo()) {bs_NameGV = bs_NameGV + "_d";}
    //---- Инициализация библиотеки функций торговых операций
    fInit_Trade();
    bs_SymbolList = fs_SymbolList;
    bs_MagicList = fs_MagicList;
    bs_Delimiter = fs_Delimiter;
    bb_RealTrade = (!IsTesting() && !IsOptimization());
    bb_VirtualTrade = (IsOptimization() || (IsTesting() && !IsVisualMode()));
    if (bb_VirtualTrade) fb_CreatVStopsInChart = false;
    bb_CreatVStopsInChart = fb_CreatVStopsInChart;
    bs_ExpertName = StringConcatenate (WindowExpertName(), ":  ", fGet_NameTF (Period()), "_", Symbol());
    bb_PrintCom = fb_PrintCom;
    bb_ShowCom = fb_ShowCom;
    bb_PlaySound = fb_PlaySound;
    bdt_curTime = TimeCurrent();
    bd_MAXLOT = MarketInfo (Symbol(), MODE_MAXLOT);
    bd_MINLOT = MarketInfo (Symbol(), MODE_MINLOT);
    bd_LOTSTEP = MarketInfo (Symbol(), MODE_LOTSTEP);
    ProfitMIN_Pips *= bi_Decimal;
    fGet_MarketInfo (Symbol(), -1);
    if (bd_SymPoint == 0.0) {bd_SymPoint = fGet_Point();}
    bi_digit = LotDecimal();
    bs_sign = StringConcatenate (fGet_SignCurrency(), " ");
    InitializeArray_STR (bsa_Comment);
    //---- Определяем индекс "ошибок" в массиве комментариев (gsa_Comment)
    bi_indERR = ArraySize (bsa_Comment) - 1;
    //---- Счётчик дополнительных ордеров при экстремальной торговле
    string ls_Name = StringConcatenate (bs_NameGV, "_#cntTrades");
    bi_cntTrades = fGet_ValueFromGV (ls_Name, 0.0);
    //---- На всякий случай подчищаем временные GV-переменные
    if (!bb_RealTrade) fClear_GV();
    fRight_CompilTL();
    bb_OptimContinue = false;
    //---- Подчищаем старые комментарии
    Comment ("");
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fInit_Base()", bi_indERR);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Общие функции                                                             |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Запускаем в цикл получение рыночной цены.                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_TradePrice (int fi_Price,           // Цена: 0 - Bid; 1 - Ask
                        bool fb_RealTrade,      // реальная торговля или оптимизация\тестирование
                        string fs_Symbol = "")  // валютная пара
{
    double ld_Price = 0.0;
//----
    if (fs_Symbol == "") {fs_Symbol = Symbol();}
    //RefreshRates();
    switch (fi_Price)
    {
        case 0:
            if (fb_RealTrade)
            {
                while (ld_Price == 0.0)
                {
                    if (fs_Symbol == Symbol()) {ld_Price = Bid;} else {ld_Price = MarketInfo (fs_Symbol, MODE_BID);}
                    if (!IsExpertEnabled() || IsStopped()) {break;}
                    Sleep (50); RefreshRates();
                }
            }
            else {if (fs_Symbol == Symbol()) {return (Bid);} else {return (MarketInfo (fs_Symbol, MODE_BID));}}
            break;
        case 1:
            if (fb_RealTrade)
            {
                while (ld_Price == 0.0)
                {
                    if (fs_Symbol == Symbol()) {ld_Price = Ask;} else {ld_Price = MarketInfo (fs_Symbol, MODE_ASK);}
                    if (!IsExpertEnabled() || IsStopped()) {break;}
                    Sleep (50); RefreshRates();
                }
            }
            else {if (fs_Symbol == Symbol()) {return (Ask);} else {return (MarketInfo (fs_Symbol, MODE_ASK));}}
            break;
    }
//----
    return (ld_Price);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция, контролирующая приход нового бара                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fControl_NewBar (int fi_TF = 0)
{bdt_NewBar = iTime (Symbol(), fi_TF, 0); return (fi_TF);}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|         Находим дату начала торговли по дате первого открытого ордера             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
datetime fGet_TermsTrade (string fs_SymbolList,      // Лист управляемых валютных пар
                          string fs_MagicList,       // Лист управляемых Магиков
                          datetime& fdt_LastTrade,   // когда был последний ордер
                          string fs_Delimiter = ",") // Разделитель переменных в листе fs_MagicList
{
    int      history_total = OrdersHistoryTotal();
    datetime ldt_Time = TimeCurrent(), ldt_OpenTime, ldt_CloseTime;
    string   ls_Symbol;
//----
    //fGet_TermsTrade (Symbol(), MG, bdt_LastTrade);
    bi_Error = GetLastError();
    for (int li_int = history_total - 1; li_int >= 0; li_int--)
    {
        if (!OrderSelect (li_int, SELECT_BY_POS, MODE_HISTORY)) continue;
        ls_Symbol = OrderSymbol();
        if (StringFind (fs_SymbolList, ls_Symbol) < 0 && StringLen (fs_SymbolList) > 0) continue;
        if (!fCheck_MyMagic (fs_MagicList, fs_Delimiter)) continue;
        if (OrderType() > 1) continue;
        ldt_OpenTime = OrderOpenTime();
        if (ldt_Time > ldt_OpenTime) {ldt_Time = ldt_OpenTime; continue;}
        ldt_CloseTime = OrderCloseTime();
        if (ldt_CloseTime > fdt_LastTrade) {fdt_LastTrade = ldt_CloseTime;}
    }
    //---- Если ордеров нет - заполняем текущим временем
    if (fdt_LastTrade == 0) fdt_LastTrade = ldt_Time;
    //---- Контролируем возможные ошибки
    fGet_LastErrorInArray (bsa_Comment, "fCalculate_Pribul()", bi_indERR);
//----
    return (ldt_Time);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Разделяем разряды чисел пробелами                                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fSplitField (string fs_Value)
{
    string ls_Begin = fs_Value, ls_End, ls_tmp;
    int    li_N1 = StringFind (ls_Begin, "."), li_Len, li_plus = 0;
    bool   lb_minus = (StringFind (ls_Begin, "-") == 0);
//----
    if (lb_minus) li_plus = 1;
    //---- Отрезаем дробную часть и первых три разряда (до тысяч)
    if (li_N1 > 0)
    {
        li_N1 = MathMax (0, li_N1 - 3);
        ls_End = StringSubstr (ls_Begin, li_N1);
        if (li_N1 > 0) {ls_Begin = StringSubstr (ls_Begin, 0, li_N1);}
        else {return (fs_Value);}
    }
    li_Len = StringLen (ls_Begin);
    if (li_Len <= li_plus) {return (fs_Value);}
    while (li_Len > 3 + li_plus)
    {
        ls_tmp = StringSubstr (ls_Begin, li_Len - 3);
        ls_End = StringConcatenate (ls_tmp, " ", ls_End);
        ls_Begin = StringSubstr (ls_Begin, 0, li_Len - 3);
        li_Len = StringLen (ls_Begin);
    }
//----
    return (StringTrimLeft (StringConcatenate (ls_Begin, " ", ls_End)));
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция, гарантированного получения Point                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_Point (string fs_Symbol = "")
{
    double ld_Point = 0.0;
//----
    if (fs_Symbol == "") {fs_Symbol = Symbol();}
    ld_Point = MarketInfo (fs_Symbol, MODE_POINT);
    //---- Если результата нет
    if (ld_Point == 0.0)
    {
        int li_Digits = MarketInfo (fs_Symbol, MODE_DIGITS);
        if (li_Digits > 0) {ld_Point = 1.0 / MathPow (10, li_Digits);}
    }
    else {return (ld_Point);}
//----
    return (ld_Point);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор    : TarasBY                                                               |
//+-----------------------------------------------------------------------------------+
//|  Версия   : 27.10.2009                                                            |
//|  Описание : fControlChangeValue_D Фиксирует факт изменения проверяемого           |
//|  double параметра                                                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCCV_D (double param, int ix)
{
    static double cur_param[20];
    static bool   lb_first = true;
//---- 
    //---- При первом запуске инициализируем массив
    if (lb_first) {ArrayInitialize (cur_param, 0.0); lb_first = false;}
    if (cur_param[ix] != param) {cur_param[ix] = param; return (true);}
//---- 
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция возвращает наименование торговой операции                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fGet_NameOP (int fi_Type) 
{
//----
    switch (fi_Type) 
    {
        case -2          : return ("trading");
        case -1          : return ("ALL");
        case OP_BUY      : return ("BUY");
        case OP_SELL     : return ("SELL");
        case OP_BUYLIMIT : return ("BUYLIMIT");
        case OP_SELLLIMIT: return ("SELLLIMIT");
        case OP_BUYSTOP  : return ("BUYSTOP");
        case OP_SELLSTOP : return ("SELLSTOP");
        case 6           : if (OrderType() == 6) return ("balance"); else return ("UNI");
        case 7           : return ("pending");
    }
    return (StringConcatenate ("None (", fi_Type, ")"));
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Возвращает наименование таймфрейма                                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fGet_NameTF (int fi_TF)
{
//----
    if (fi_TF == 0) fi_TF = Period();
    switch (fi_TF)
    {
        case PERIOD_M1:  return ("M1");
		  case PERIOD_M5:  return ("M5");
		  case PERIOD_M15: return ("M15");
		  case PERIOD_M30: return ("M30");
		  case PERIOD_H1:  return ("H1");
		  case PERIOD_H4:  return ("H4");
		  case PERIOD_D1:  return ("D1");
		  case PERIOD_W1:  return ("W1");
		  case PERIOD_MN1: return ("MN1");
	 }
//----
	 return ("UnknownPeriod");
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция "нормализации" значения по Point в целое число                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int NDPD (double v) {return (v / bd_SymPoint);}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция, перевода int в double по Point                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double NDP (int v) {return (v * bd_SymPoint);}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция нормализации значения double до целого                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double ND0 (double v) {return (NormalizeDouble (v, 0));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция нормализации значения double по Digits                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double NDD (double v) {return (NormalizeDouble (v, bi_SymDigits));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция, нормализации значения double до минимальной разрядности лота      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double NDDig (double v) {return (NormalizeDouble (v, bi_digit));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция, перевода значения из double в string c нормализацией по 0         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DS0 (double v) {return (DoubleToStr (v, 0));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция, перевода значения из double в string c нормализацией по Digits    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DSD (double v) {return (DoubleToStr (v, bi_SymDigits));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция, перевода значения из double в string c нормализацией по           |
//| минимальной разрядности лота                                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string DSDig (double v) {return (DoubleToStr (v, bi_digit));} 
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        Функция, определения минимальной разрядности лота                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int LotDecimal()
{return (MathCeil (MathAbs (MathLog (bd_LOTSTEP) / MathLog (10))));}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Контролируем факт прихода нового бара на NewBarInPeriod периоде            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_NewBarInPeriod (int fi_Period = 0,            // TF
                            bool fb_Conditions = true)    // условие на проверку
{
//----
    if (fi_Period >= 0)
    {
        if (fb_Conditions)
        {
            datetime ldt_BeginBarInPeriod = iTime (Symbol(), fi_Period, 0);
            if (bdt_NewBarInPeriod == ldt_BeginBarInPeriod) {return (false);}
            bdt_NewBarInPeriod = ldt_BeginBarInPeriod;
        }
    }
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Функция возвращает значёк валюты депозита                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fGet_SignCurrency()
{
//---- 
    if (AccountCurrency() == "USD") return ("$");
    if (AccountCurrency() == "EUR") return ("€");
//---- 
    return ("RUB");
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Возвращает наименование состояния (ДА\НЕТ)                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string CheckBOOL (int M)
{
//---- 
    switch (M)
    {
        case 0: return ("OFF");
        case 1: return ("ON");
    }
//---- 
    return ("Don`t know...");
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                          |
//+-----------------------------------------------------------------------------------+
//|  Версия   : 01.02.2008                                                            |
//|  Описание : Возвращает одно из двух значений взависимости от условия.             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double IIFd (bool condition, double ifTrue, double ifFalse)
{if (condition) {return (ifTrue);} else return (ifFalse);}
//+-----------------------------------------------------------------------------------+
string IIFs (bool condition, string ifTrue, string ifFalse)
{if (condition) {return (ifTrue);} else return (ifFalse);}
//+-----------------------------------------------------------------------------------+
color IIFc (bool condition, color ifTrue, color ifFalse)
{if (condition) {return (ifTrue);} else return (ifFalse);}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Работы с массивами                                                        |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|  UNI:  Получаем индекс искомого элемента в массиве                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_INDInArrayINT (int fi_Value, int ar_Array[])
{
//---- 
    for (int li_IND = 0; li_IND < ArraySize (ar_Array); li_IND++)
    {if (ar_Array[li_IND] == fi_Value) return (li_IND);}
//---- 
    return (-1);
}
//+-----------------------------------------------------------------------------------+
int fGet_INDInArraySTR (string fs_Value, string ar_Array[])
{
//---- 
    for (int li_IND = 0; li_IND < ArraySize (ar_Array); li_IND++)
    {if (ar_Array[li_IND] == fs_Value) return (li_IND);}
//---- 
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|  UNI:  Получаем номер периода графика                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_NumPeriods (int fi_Period)
{
    static int lia_Periods[] = {1,5,15,30,60,240,1440,10080,43200};
//---- 
    for (int l_int = 0; l_int < ArraySize (lia_Periods); l_int++)
    {if (lia_Periods[l_int] == fi_Period) return (l_int);}
//---- 
    return (-1);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|  UNI:  Инициализируем Value массив STRING                                         |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int InitializeArray_STR (string& PrepareArray[], string Value = "")
{
    int l_int, size = ArraySize (PrepareArray);
//----
    for (l_int = 0; l_int < size; l_int++)
    {PrepareArray[l_int] = Value;}
//----
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Создаём массив имён временных GV-переменных                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCreat_ArrayGV (string& ar_Base[],  // рабочий массив
                     string ar_Add[])    // добавляемый массив
{
    int li_int, li_sizeB = ArraySize (ar_Base), li_sizeA = ArraySize (ar_Add);
    bool lb_duble;
//----
    for (int li_IND = 0; li_IND < li_sizeA; li_IND++)
    {
        lb_duble = false;
        //---- Осуществляем проверку на дубликаты
        for (li_int = 0; li_int < li_sizeB; li_int++)
        {
            if (ar_Add[li_IND] == ar_Base[li_int])
            {lb_duble = true; break;}
        }
        //---- Если дубликат - идём дальше
        if (lb_duble) continue;
        //---- Увеличиваем счётчик
        li_sizeB++;
        //---- Увеличиваем базовый массив
        ArrayResize (ar_Base, li_sizeB);
        //---- Вносим в последнюю ячейку новое значение
        ar_Base[li_sizeB - 1] = ar_Add[li_IND];
    }
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Возвращает массив STRING из строки, разделённой sDelimiter                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fSplitStrToStr (string fs_List,             // строка с данными
                    string& ar_OUT[],           // возвращаемый массив
                    string fs_Delimiter = ",")  // разделитель данных в строке
{
    string tmp_str = "", tmp_char = "";
//----
    ArrayResize (ar_OUT, 0);
    for (int i = 0; i < StringLen (fs_List); i++)
    {
        tmp_char = StringSubstr (fs_List, i, 1);
        if (tmp_char == fs_Delimiter)
        {
            if (StringTrimLeft (StringTrimRight (tmp_str)) != "")
            {
                ArrayResize (ar_OUT, ArraySize (ar_OUT) + 1);
                ar_OUT[ArraySize (ar_OUT) - 1] = tmp_str;
            }
            tmp_str = "";
        }
        else {if (tmp_char != " ") tmp_str = tmp_str + tmp_char;}
    }
    if (StringTrimLeft (StringTrimRight (tmp_str)) != "")
    {
        ArrayResize (ar_OUT, ArraySize (ar_OUT) + 1);
        ar_OUT[ArraySize (ar_OUT) - 1] = tmp_str;
    }
//----
    return (ArraySize (ar_OUT));
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Возвращает массив INT из элементов массива STRING                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCreat_StrToInt (string ar_Value[],        // массив элементов string
                      int& ar_OUT[],            // возвращаемый массив int
                      int fi_IND,               // количество ячеек в массиве
                      int fi_Factor = 1,        // множитель
                      string fs_NameArray = "") // имя возвращаемого массива
{
    int    li_size = ArraySize (ar_Value);
    string ls_row = "";
//----
    ArrayResize (ar_OUT, fi_IND);
    for (int li_int = 0; li_int < fi_IND; li_int++)
    {
        if (li_int < li_size) {ar_OUT[li_int] = StrToInteger (ar_Value[li_int]) * fi_Factor;}
        else {ar_OUT[li_int] = StrToDouble (ar_Value[li_size - 1]) * fi_Factor;}
        ls_row = StringConcatenate (ls_row, fs_NameArray, "[", li_int, "] = ", ar_OUT[li_int], "; ");
    }
    if (fs_NameArray != "") Print (ls_row);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Возвращает массив DOUBLE из элементов массива STRING                       |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fCreat_StrToDouble (string ar_Value[],        // массив элементов string
                         double& ar_OUT[],         // возвращаемый массив double
                         int fi_IND,               // количество ячеек в массиве
                         double fd_Factor = 1.0,   // множитель
                         string fs_NameArray = "") // имя возвращаемого массива
{
    int    li_size = ArraySize (ar_Value);
    string ls_row = "";
//----
    ArrayResize (ar_OUT, fi_IND);
    for (int li_int = 0; li_int < fi_IND; li_int++)
    {
        if (li_int < li_size) {ar_OUT[li_int] = StrToDouble (ar_Value[li_int]) * fd_Factor;}
        else {ar_OUT[li_int] = StrToDouble (ar_Value[li_size - 1]) * fd_Factor;}
        ls_row = StringConcatenate (ls_row, fs_NameArray, "[", li_int, "] = ", ar_OUT[li_int], "; ");
    }
    if (fs_NameArray != "") Print (ls_row);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Возвращает строку из элементов массива INT и сам массив                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fCreat_StrAndArray (int fi_First,              // значение 1-го эелемента массива
                           int& ar_OUT[],             // возвращаемый массив int
                           int fi_cntIND,             // количество элементов в массиве
                           string fs_Delimiter = ",") // разделитель элементов в возвращаемой строке
{
    string ls_row = "";
//----
    ArrayResize (ar_OUT, fi_cntIND);
    for (int li_int = 0; li_int < fi_cntIND; li_int++)
    {
        if (li_int == fi_cntIND - 1) fs_Delimiter = "";
        ar_OUT[li_int] = fi_First + li_int;
        ls_row = StringConcatenate (ls_row, ar_OUT[li_int], fs_Delimiter);
    }
//----
    return (ls_row);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Возвращает строку из элементов массива, разделённых fs_Delimiter           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
string fCreat_StrFromArray (string ar_Array[],           // массив со значениями
                            string fs_Delimiter = ",")   // разделитель элементов в возвращаемой строке
{
    string ls_row = "";
    int    li_size = ArraySize (ar_Array);
//----
    for (int li_int = 0; li_int < li_size; li_int++)
    {
        if (li_int == li_size - 1) fs_Delimiter = "";
        ls_row = StringConcatenate (ls_row, ar_Array[li_int], fs_Delimiter);
    }
//----
    return (ls_row);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Работа с ошибками                                                         |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Получаем номер и описание последней ошибки и выводим в массив комментов    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_LastErrorInArray (string& Comm_Array[],  // возвращаемый массив комментов
                           string Com = "",       // дополнительная информация к сообщению об ошибке
                           int index = -1)        // индекс ячейки в которую заносим сообщение об ошибке
{
    if (bb_VirtualTrade) {return (0);}
    int err = GetLastError();
//---- 
    if (err > 0)
    {
        string ls_err = StringConcatenate (Com, ": Ошибка № ", err, " :: ", ErrorDescription (err));
        Print (ls_err);
        if (index >= 0) {Comm_Array[index] = ls_err;}
    }
//---- 
    return (err);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Получаем номер и описание последней ошибки                                 |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
int fGet_LastError (string& Comm_Error, string Com = "")
{
    if (bb_VirtualTrade) {return (0);}
    int err = GetLastError();
//---- 
    if (err > 0)
    {
        Comm_Error = StringConcatenate (Com, ": Ошибка № ", err, " :: ", ErrorDescription (err));
        Print (Comm_Error);
    }
//---- 
    return (err);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Сервисных функций                                                         |
//|***********************************************************************************|
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Пишем Log-файл                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fWrite_Log (string fs_Txt, int fi_IND = -1)
{
    if (!bb_RealTrade)
    {
        if (IsVisualMode()) {fPrintAndShowComment (fs_Txt, bb_ShowCom, bb_PrintCom, bsa_Comment, fi_IND);}
        return;
    }
    static datetime ldt_NewDay = 0;
    static string   ls_FileName = "";
    datetime ldt_BarD1 = iTime (Symbol(), NULL, PERIOD_D1);
    //---- Имя лог файла определяем один раз в сутки
    if (ldt_NewDay != ldt_BarD1)
    {
        ls_FileName = StringConcatenate (WindowExpertName(), "_", Symbol(), "_", Period(), "-", Month(), "-", Day(), ".log");
        ldt_NewDay = ldt_BarD1;
    }
    int handle = FileOpen (ls_FileName, FILE_READ|FILE_WRITE|FILE_CSV, "/t");
//----
    FileSeek (handle, 0, SEEK_END);      
    FileWrite (handle, StringConcatenate (TimeToStr (TimeCurrent(), TIME_DATE|TIME_SECONDS), ": ", fs_Txt));
    FileClose (handle);
	 fPrintAndShowComment (fs_Txt, bb_ShowCom, bb_PrintCom, bsa_Comment, fi_IND);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Выводим на печать и на график комментарии                                  |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fPrintAndShowComment (string& fs_Text,          // Возвращаемая пустая строка
                           bool fb_ShowConditions,   // Разрешение на показ комментов на графике
                           bool fb_PrintConditions,  // Разрешение на печать комментов
                           string& ar_Show[],        // Возвращаемый массив комментов
                           int fi_IND = -1)          // индекс ячейки массива, куда внесли изменения
{
    if (fb_ShowConditions || fb_PrintConditions)
    {
        if (StringLen (fs_Text) > 0)
        {
            if (fb_ShowConditions) {if (fi_IND >= 0) ar_Show[fi_IND] = fs_Text;}
            if (fb_PrintConditions)
            {
                if (bs_libNAME != "") fs_Text = StringConcatenate (bs_libNAME, ":     ", fs_Text);
                Print (fs_Text);
            }
        }
    }
    //---- Очищаем переменную
    fs_Text = "";
//---- 
    return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Берём значение переменной или из GV, или (при её отстутствии) fd_Value     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_ValueFromGV (string fs_Name,            // имя GV-переменной
                         double fd_Value,           // если такой переменной нет, подставляемое значение
                         bool fd_Condition = true)  // условие на проверку наличия GV-переменной
{
//----
    if (!fd_Condition) {return (fd_Value);}
    if (GlobalVariableCheck (fs_Name)) {return (GlobalVariableGet (fs_Name));}
    else {return (fd_Value);}
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Рисуем на графике виртуальный уровень                                      |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fDraw_VirtSTOP (int fi_CMD,         // BUY - 1; SELL = -1
                     int fi_OP,          // Тип СТОПа (0 - SL; 1 - TP)
                     double fd_Level)    // Уровень СТОПа
{
    if (OrderSymbol() != Symbol()) return (false);
//----
    bool   lb_result = false;
    int    li_pip, li_cmd = 1;
    color  lc_color;
    string ls_Name, lsa_NameOP[] = {"SL","TP"};
//----
    if (fi_OP == 1) li_cmd = -1;
    li_pip = li_cmd * fi_CMD * NDPD (OrderOpenPrice() - fd_Level);
    if (li_pip > 0) {lc_color = Aqua;} else {lc_color = Magenta;}
    ls_Name = StringConcatenate (bs_NameGV, "_", OrderTicket(), "_Virt", lsa_NameOP[fi_OP]);
    lb_result = fCreat_OBJ (ls_Name, OBJ_TREND, StringConcatenate (lsa_NameOP[fi_OP], "(", li_pip, ")"), 10, OrderOpenTime(), fd_Level, False, lc_color, OrderOpenTime() + 10 * Period() * 60, fd_Level);
    ObjectSet (ls_Name, OBJPROP_WIDTH, 2);
//----
    return (lb_result);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Рисуем на чарте уровень бузубыточности                                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fWork_LineBU (double ar_Lots[])     // массив объёмов открытых рыночных ордеров
{
    if (bb_VirtualTrade) return;
    if (bi_MyOrders < 2)
    {
        string ls_Name = StringConcatenate (bs_NameGV, "_BU");
        if (ObjectFind (ls_Name) == 0) ObjectDelete (ls_Name);
    }
    else if (bi_MyOrders > 1)
    {
        //---- Рисуем на чарте уровень бузубыточности
        double ld_BU = fGet_BreakEven (bs_Symbol, ar_Lots, bd_ProfitCUR);
        if (ld_BU > 0.0)
        {
            int li_cmd = -1;
            ls_Name = StringConcatenate (bs_NameGV, "_BU");
            if (ld_BU > Bid && ar_Lots[0] > ar_Lots[1]) li_cmd = 1;
            if (ld_BU < Ask && ar_Lots[0] < ar_Lots[1]) li_cmd = 1;
            fCreat_OBJ (ls_Name, OBJ_HLINE, StringConcatenate ("BU (", NDPD (ld_BU - Bid), ")"), 10, Time[0], ld_BU, False, Gold, Time[0], ld_BU);
            ObjectSet (ls_Name, OBJPROP_STYLE, 1);
        }
    }
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Подчищаем возможно оставшиеся GV-переменные                                |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fClear_GV (int fi_cnt = 5000)    // кол-во очищаемых объектов (тикетов)
{
    string ls_Name, lsa_Name[] = {"_#Delta_SL","_#LastLossLevel","_#VirtSL",
           "_#VirtTP","_#BeginSL","_#SL","_#TP","_#Num","_#Lots","_#BU","_#STOP","_#OP"};
    int    li_size = ArraySize (lsa_Name), li_IND;
//---- 
    for (int li_CNT = 0; li_CNT < fi_cnt; li_CNT++)
    {
        for (li_IND = 0; li_IND < li_size; li_IND++)
        {
            ls_Name = StringConcatenate (li_CNT, lsa_Name[li_IND]);
            if (GlobalVariableCheck (ls_Name)) GlobalVariableDel (ls_Name); 
        }
    }
//---- 
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        ПРЕДУПРЕЖДЕНИЕ                                                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fSTOPTRADE()
{
//----
    if (bb_OptimContinue) if (!bb_VirtualTrade)
    {fSet_Label ("STOP", "STOP TRADE !!! Смотри ЛОГ. Смени настройки.", 200, 200, 20, "Calibri", 0, 0, Red); return (true);}
//----
    return (false);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Подчищаем за собой все отработанные объекты на графике                     |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
/*void fDelete_MyObjectsInChart (string fs_Name,             // префикс имени объектов
                               bool fb_Condition = true)   // разрешение на удаление
{
    if (!fb_Condition) return;
    string ls_Name;
//----
    //---- Подчищаем объекты на графике
    for (int li_OBJ = ObjectsTotal() - 1; li_OBJ >= 0; li_OBJ--)
    {
        ls_Name = ObjectName (li_OBJ);
        if (StringFind (ls_Name, fs_Name) == 0) {ObjectDelete (ls_Name);}
    }
//----
}*/
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|        "Нейтрализуем" не задействованные в коде функции                           |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fRight_CompilTL()
{
//----
    return;
    IIFd (true, 1.0, 0.0);
    IIFs (true, "", "");
    IIFc (true, Blue, Red);
    fGet_NameTF (0);
    ND0 (1.0);
    DS0 (1.0);
    NDPD (1.);
    CheckBOOL (1);
    fGet_NumPeriods (0);
    int li_tmp, lia_tmp[1];
    fGet_TermsTrade ("", 0, li_tmp);
    fCreat_StrAndArray (0, lia_tmp, 0);
    string lsa_tmp[1], ls_tmp, lsa_tmp2[1];
    fCreat_ArrayGV (lsa_tmp, lsa_tmp2);
    fCreat_StrFromArray (lsa_tmp);
    fGet_LastError (ls_tmp); 
    fGet_INDInArraySTR ("", lsa_tmp);
    fSplitStrToStr ("", lsa_tmp);
    InitializeArray_STR (lsa_tmp);
    fSplitField ("");
    double lda_tmp[1];
    fGet_INDInArrayINT (1, lia_tmp);
    fCreat_StrToInt (lsa_tmp, lia_tmp, 1);
    fCreat_StrToDouble (lsa_tmp, lda_tmp, 1);
    fGet_BreakEven (Symbol(), lda_tmp, 0);
    fControl_MAXLife (0);
    fControl_NewBar (0);
    fCCV_D (1, 0);
    fDraw_VirtSTOP (li_tmp, 0, 0);
    fWork_LineBU (lda_tmp);
    fCheck_MinProfit (0, 0);
    fCheck_NewBarInPeriod (0);
//----
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: MONEY MANAGEMENT                                                          |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Производим нормализацию лота                                               |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fLotsNormalize (double fd_Lots)     // размер лота
{
    double ld_Lot = fd_Lots;
//----
    fd_Lots -= bd_MINLOT;
    fd_Lots /= bd_LOTSTEP;
    fd_Lots = MathRound (fd_Lots);
    fd_Lots *= bd_LOTSTEP;
    fd_Lots += bd_MINLOT;
    if (fd_Lots < bd_MINLOT)
    {
        fSet_Comment (bdt_curTime, 0, 6, "", True, ld_Lot, fd_Lots);
        fd_Lots = bd_MINLOT;
    }
    //---- Обходим ограничение потолка максимального лота
    /*if (!bb_MMM)
    {
        if (fd_Lots > bd_MAXLOT)
        {
            fSet_Comment (bdt_curTime, 0, 7, "", True, fd_Lots);
            fd_Lots = bd_MAXLOT;
        }
    }*/
//----
    return (NDDig (fd_Lots));
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Определение стоимости пункта выделенного ордера                            |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_PipsValue()
{
    double ld_Price, ld_TickValue, ld_pips;
//----
    //---- Если ордер закрыт
    if (OrderCloseTime() > 0) {ld_Price = OrderClosePrice();}
    else {ld_Price = fGet_TradePrice (OrderType(), bb_RealTrade, OrderSymbol());}
    ld_pips = NDPD (OrderOpenPrice() - ld_Price);
    if (ld_pips == 0.0) {return (1);}
    ld_TickValue = MathAbs ((OrderProfit() + OrderSwap() + OrderCommission()) / ld_pips);
    //---- Расчёт стоимости пункта на MINLOT
    //ld_TickValue = ld_TickValue / OrderLots() * MarketInfo (gs_Symbol, MODE_MINLOT);
    if (ld_TickValue == 0.0) {return (1);}
//----
    return (ld_TickValue);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//         Проверяем условие ни минимальную прибыль по ордеру.                        |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
bool fCheck_MinProfit (double fd_MinProfit,      // размер минимальной прибыли по ордеру
                       double fd_NewSL,          // новый SL
                       bool fb_Condition = true) // условие на проведение проверки
{
    if (fb_Condition)
    {
        if (OrderType() == OP_BUY)
        {if (NDD ((fd_NewSL - OrderOpenPrice()) - fd_MinProfit) < 0.0) return (false);}
        else {if (NDD ((OrderOpenPrice() - fd_NewSL) - fd_MinProfit) < 0.0) return (false);}
    }
//----
    return (true);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор : TarasBY, taras_bulba@tut.by                                              |
//+-----------------------------------------------------------------------------------+
//|        Определение Уровня БезУбытка по символу                                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
double fGet_BreakEven (string fs_Symbol,     // Symbol
                       double ar_Lots[],     // массив объёмов открытых рыночных ордеров
                       double fd_Profit)     // текущий профит по открытым ордера
{
     double ld_BU = 0.0, ld_Lots = NDDig (ar_Lots[0] - ar_Lots[1]),  // разность объемов ордеров Buy и Sell
            ld_tickvalue = MarketInfo (fs_Symbol, MODE_TICKVALUE);   // цена одного пункта
//----
     if (ld_Lots != 0.0)
     {
         //---- Уровень общего безубытка для открытых ордеров
         if (ld_Lots > 0) ld_BU = fGet_TradePrice (0, bb_RealTrade, fs_Symbol) - NDP (fd_Profit / (ld_tickvalue * ld_Lots));
         else if (ld_Lots < 0) ld_BU = fGet_TradePrice (1, bb_RealTrade, fs_Symbol) - NDP (fd_Profit / (ld_tickvalue * ld_Lots));
     }
//----
    return (ld_BU);
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

