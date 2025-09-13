//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                                  VirtualTrend.mqh |
//|                                                Copyright © Evgeniy Trofimov, 2010 |
//|                                                   http://forum.mql4.com/ru/16793/ |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                        V I R T U A L       T R E N D                              |
//|                     Copyright: Evgeniy Trofimov, 2010                             |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

//IIIIIIIIIIIIIIIIIII==================CONSTANS=================IIIIIIIIIIIIIIIIIIIIII+
#define  VIRT_TRADES        1    // - торговые ордера и позиции
#define  VIRT_HISTORY       0    // - закрытые позиции и отмерённые отложенные ордера
#define  MAX_POS         3000    // - максимальное количество отслеживаемых позиций
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
// Эта библиотека содержит такие функции, как:
//Основные:
// + VirtualSend
// + VirtualSelect
// + VirtualClose - отменяет отложенные ордера и закрывает позиции
// + VirtualModify
//Вспомогательные:
// + VirtualCopyPosition - копирует позицию в другую область массива
// + VirtualHighTicket - поиск молодого тикета
// + VirtualFileLoad - загружает массив сделок из файла
// + VirtualFileSave - сохраняет массив сделок в файл
// + VirtualUpdate - обновляет данные с рынка
// + VirtualFilter - создаёт список индексов массива найденных позиций
// + VirtualProfitHistory - расчитывает значение скользящей средней баланса
// + VirtualProfit - расчитывает значение прибыли открытых позиций на данный момент времени
// + VirtualRating - расчёт рейтинга прибыльных ТС в процентах
// + VirtualExist  - функция используется для запрета открытия 2-ух сделок подряд по одному сигналу
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                   *****         Параметры модуля         *****                    |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
extern string   SETUP_VirtualTrade = "============= Общие настройки модуля ============";
extern bool     RatingON              = true;  // - выключатель рейтинга (если выключен, то сделки в файле должны совпадать с реальной торговлей)
extern bool     FastTest              = true;  // - не вести файл при тестировании
//IIIIIIIIIIIIIIIIIII========Глобальные переменные модуля========IIIIIIIIIIIIIIIIIIIIII+
int         Virt.Count,                // - количество позиций в массиве (от 0 до MAX_POS - 1)
            Virt.Index,                // - позиция выбранной сделки в базе функцией VirtualSelect()
            VirtBufferSize = 16,       // - количество элементов в одной строке базы (следующие строки)
            Virt.Ticket[MAX_POS],      // - Номер ордера
            Virt.Type[MAX_POS],        // - Тип сделки
            Virt.MagicNumber[MAX_POS], // - Магическое число
            Virt.Status[MAX_POS],      // - Статус ордера: 1 - открытая позиция/отложенный ордер; 0 - закрытая позиция/отменённый ордер
            Virt.Filter[MAX_POS],
            Virt.Filter.Count,
            DAY = 86400,               // - день в секундах
            Err.Number;                // - Номер ошибки
double      Virt.Lots[MAX_POS],        // - Объём
            Virt.OpenPrice[MAX_POS],   // - Цена открытия
            Virt.StopLoss[MAX_POS],
            Virt.TakeProfit[MAX_POS],
            Virt.ClosePrice[MAX_POS],  // - Цена закрытия
            Virt.Swap[MAX_POS],        // - Своп
            Virt.Profit[MAX_POS];      // - Прибыль в пипсолотах
datetime    Virt.CloseTime[MAX_POS],   // - Время закрытия
            Virt.OpenTime[MAX_POS],    // - Время отрытия
            Virt.Expiration[MAX_POS];  // - Дата отмены отложенного ордера
string      Virt.Comment[MAX_POS],     // - Комментарии
            Virt.Symbol[MAX_POS],      // - Символ
            Err.Description,           // - Описание последней ошибки
            bs_ComError = "";
//+------------------------------------------------------------------+
//|  Функция возвращает рейтинг положительных сделок, созданных      |
//|  той или иной торговой системой на определённом инструменте      |
//|  (100% - самая прибыльная ТС, 0% - ни рыба ни мясо)              |
//+------------------------------------------------------------------+
double VirtualRating (int fMagic,                      // магическое число, по которому рейтинг будет отличать одну торговую систему от другой
                      string fSymbol,                  // символ, по которому рассматривается рейтинг
                      int period,                      // количество последних сделок
                      int applied_price,               // Используемая цена: 0 - в валюте депозита, 1 - в пунктах;
                      string filename = "virtual.csv") // файл сделок
{
    if (!RatingON) return (100);
//----
    int MagicNum[];
    double Profit[];
    //double Rating[];
    int i, j, err = GetLastError();
    bool MagicExist;
//----
    VirtualFileLoad (filename);
    if (VirtualFilter (VIRT_HISTORY, -1, -1, fSymbol) < 1) return (0);
    for (i = 0; i < Virt.Filter.Count; i++)
    {
        MagicExist = false;
        for (j = 0; j < ArraySize (MagicNum); j++)
        {
            if (MagicNum[j] == Virt.MagicNumber[Virt.Filter[i]])
            {
                MagicExist = true;
                break;
            }
        }// Next j
        if (!MagicExist)
        {
            ArrayResize (MagicNum, ArraySize (MagicNum) + 1);
            MagicNum[ArraySize (MagicNum) - 1] = Virt.MagicNumber[Virt.Filter[i]];
        }
    }// Next i
    ArrayResize (Profit, ArraySize (MagicNum));
    for (i = 0; i < ArraySize (MagicNum); i++)
    {
        Profit[i] = VirtualProfitHistory (applied_price, period, 0, -1, -1, fSymbol, MagicNum[i], true)
                  + VirtualProfit (applied_price, -1, -1, fSymbol, MagicNum[i], true);
        if (Profit[i] < 0) Profit[i] = 0;
    }// Next i
    j = ArrayMaximum (Profit); // 100%
    if (Profit[j] == 0) return (0);
    //ArrayResize (Rating, ArraySize (MagicNum));
    for (i = 0; i < ArraySize (MagicNum); i++)
    {
        if (fMagic == MagicNum[i])
        { return (100 * Profit[i] / Profit[j]); }
    }// Next i
    //---- Контролируем возможные ошибки
    fGetLastError (bs_ComError, "VirtualRating()");
//----
    return (0);
}// VirtualRating()
//+------------------------------------------------------------------+
//|  Функция возвращает значение прибыли открытых сделок             |
//|  Примечание: файл с позициями должен быть загружен до вызова     |
//|  этой функции.                                                   |
//+------------------------------------------------------------------+
double VirtualProfit (int applied_price = 1,    // Используемая цена: 0 - в валюте депозита, 1 - в пунктах;
                      int fTicket = -1,
                      int fType = -1,
                      string fSymbol = "",
                      int fMagic = -1,
                      bool dh = false,          // dh = true - Делить на количество пройденных дней, для вычисления относительной прибыли
                      string fComment = "")
{
    double Profit, plus, deltaDay, ld_Point;
    int err = GetLastError();
    datetime OldDay = TimeCurrent();
//----
    if (VirtualFilter (VIRT_TRADES, fTicket, fType, fSymbol, fMagic, fComment) > 0)
    {
        for (int i = 0; i < Virt.Filter.Count; i++)
        {
            if (Virt.Type[Virt.Filter[i]] < 2)
            {
                if (Virt.OpenTime[Virt.Filter[i]] < OldDay) OldDay = Virt.OpenTime[Virt.Filter[i]];
                if (applied_price == 0)              // В валюте депозита
                {plus = Virt.Profit[Virt.Filter[i]];}
                else                                 // В пунктах
                {
                    ld_Point = MarketInfo (Virt.Symbol[Virt.Filter[i]], MODE_POINT);
                    if (Virt.Type[Virt.Filter[i]] == OP_BUY)
                    {plus = (Virt.ClosePrice[Virt.Filter[i]] - Virt.OpenPrice[Virt.Filter[i]]) / ld_Point;}
                    else
                    {plus = (Virt.OpenPrice[Virt.Filter[i]] - Virt.ClosePrice[Virt.Filter[i]]) / ld_Point;}
                }
                Profit = Profit + plus;
            }
        }// Next i
    }
    if (dh)
    {
        deltaDay = (TimeCurrent() - OldDay) / DAY;
        if (deltaDay < 1)
        {deltaDay = 1;}
        Profit = Profit / deltaDay;
    }
    //---- Контролируем возможные ошибки
    fGetLastError (bs_ComError, "VirtualProfit()");
//----
    return (Profit);
}// VirtualProfit()
//+------------------------------------------------------------------+
//|  Функция возвращает значение прибыли закрытых сделок             |
//|  Примечание: файл с позициями должен быть загружен до вызова     |
//|  этой функции.                                                   |
//+------------------------------------------------------------------+
double VirtualProfitHistory (int applied_price = 1,    // Используемая цена: 0 - в валюте депозита, 1 - в пунктах;
                             int period = 0,           // Период усреднения для вычисления прибыли. Если равен 0, то для всех сделок;
                             int shift = 0,            // Индекс получаемого значения из массива сделок (сдвиг относительно последней закрытой сделки на указанное количество сделок назад).
                             int fTicket = -1,
                             int fType = -1,
                             string fSymbol = "", 
                             int fMagic = -1, 
                             bool dh = false,          // dh = true - Делить на количество пройденных дней, для вычисления относительной прибыли
                             string fComment = "")
{
    double Profit, plus, deltaDay, ld_Point;
    datetime beginDay = TimeCurrent(), endDay;
    int j, k, err = GetLastError();
//----
    if (VirtualFilter (VIRT_HISTORY, fTicket, fType, fSymbol, fMagic, fComment) > 0)
    {
        for (int i = Virt.Filter.Count - 1; i >= 0; i--)
        {
            if (Virt.Type[Virt.Filter[i]] < 2)
            {
                k++;
                if (k > shift)
                {
                    j++;
                    if (j > period && period > 0) break;
                    if (Virt.OpenTime[Virt.Filter[i]] < beginDay) beginDay = Virt.OpenTime[Virt.Filter[i]];
                    if (Virt.CloseTime[Virt.Filter[i]] > endDay) endDay = Virt.CloseTime[Virt.Filter[i]];
                    if (applied_price == 0)          // В валюте депозита
                    {plus = Virt.Profit[Virt.Filter[i]];}
                    else                             // В пунктах
                    {
                        ld_Point = MarketInfo (Virt.Symbol[Virt.Filter[i]], MODE_POINT);
                        if (Virt.Type[Virt.Filter[i]] == OP_BUY)
                        {plus = (Virt.ClosePrice[Virt.Filter[i]] - Virt.OpenPrice[Virt.Filter[i]]) / ld_Point;}
                        else
                        {plus = (Virt.OpenPrice[Virt.Filter[i]] - Virt.ClosePrice[Virt.Filter[i]]) / ld_Point;}
                    }
                    Profit = Profit + plus;
                }
            }
        }// Next i
    }
    if (dh)
    {
        deltaDay = (endDay - beginDay) / DAY;
        //Print (TimeToStr (endDay) + " - " + TimeToStr (beginDay) + " = " + DoubleToStr (deltaDay, 2) + " дней");
        if (deltaDay < 1)
        {deltaDay = 1;}
        Profit = Profit / deltaDay;
    }
    //---- Контролируем возможные ошибки
    fGetLastError (bs_ComError, "VirtualProfitHistory()");
//----
    return (Profit);
}// VirtualProfitHistory()
//+------------------------------------------------------------------+
//|  Функция выбирает ордер для дальнейшей работы с ним.             |
//|  Возвращает TRUE при успешном завершении функции.                |
//|  Примечание: файл с позициями должен быть загружен до вызова     |
//|  этой функции.                                                   |
//+------------------------------------------------------------------+
bool VirtualSelect (int index,              // Позиция ордера или номер ордера в зависимости от второго параметра. 
                    int select,             // Флаг способа выбора. Mожет быть одним из следующих величин: SELECT_BY_POS или SELECT_BY_TICKET
                    int pool = VIRT_TRADES) // Источник данных для выбора. Используется, когда параметр select равен SELECT_BY_POS: VIRT_TRADES или VIRT_HISTORY
{
//----
    if (select == SELECT_BY_POS)
    {
        if (VirtualFilter (pool) > 0)
        {
            if (index < Virt.Filter.Count)
            {
                Virt.Index = Virt.Filter[index];
                return (true);
            }
        }
    }
    else         // select == SELECT_BY_TICKET
    {
        if (VirtualFilter (-1, index) > 0)
        {
            Virt.Index = Virt.Filter[0];
            return (true);
        }
    }
//----
    return (false);
}// VirtualSelect()
//+------------------------------------------------------------------+
//|  Изменяет параметры ранее открытых позиций или отлож. ордеров.   |
//|  Возвращает TRUE при успешном завершении функции.                |
//|  Внимание!!! Нет никаких проверок на минимально допустимый       |
//|  уровень выставления price, SL и TP!!!                           |
//+------------------------------------------------------------------+
bool VirtualModify (int ticket,
                    double price,
                    double stoploss,
                    double takeprofit,
                    datetime expiration,
                    string filename = "virtual.csv")
{
    int  i, err = GetLastError();
    bool lb_result = false;
//----
    VirtualFileLoad (filename);
    if (VirtualFilter (VIRT_TRADES, ticket) > 0)
    {
        i = Virt.Filter[0];
        if (Virt.Type[i] < 2)
        {      
            if (stoploss > 0 && Virt.StopLoss[i] != stoploss)
            {Virt.StopLoss[i] = stoploss; lb_result = true;}
            if (takeprofit > 0 && Virt.TakeProfit[i] != takeprofit)
            {Virt.TakeProfit[i] = takeprofit; lb_result = true;}
        }
        else
        {
            if (price > 0) Virt.OpenPrice[i] = price;
            if (stoploss > 0) Virt.StopLoss[i] = stoploss;
            if (takeprofit > 0) Virt.TakeProfit[i] = takeprofit;
            if (expiration > 0) Virt.Expiration[i] = expiration;
        }
        VirtualFileSave (filename);
        //---- Контролируем возможные ошибки
        fGetLastError (bs_ComError, "VirtualModify()");
        return (lb_result);
    }
    else
    {
        Err.Number = 102;
        Err.Description = "При попытке изменить позицию номер его найти не удалось";
        return (false);
    }   
//----
}// VirtualModify()
//+------------------------------------------------------------------+
//|  Закрытие позиции.                                               |
//|  Возвращает TRUE при успешном завершении функции.                |
//|  Информация об ошибках хранится в переменных Err.*               |
//+------------------------------------------------------------------+
bool VirtualClose (int ticket, string filename = "virtual.csv")
{
    double ld_Point, ld_TickValue;
    int    i, j, err = GetLastError();
//----
    VirtualFileLoad (filename);
    if (VirtualFilter (VIRT_TRADES, ticket) > 0)
    {
        i = Virt.Filter[0];
        ld_Point = MarketInfo (Virt.Symbol[i], MODE_POINT);
        ld_TickValue = MarketInfo (Virt.Symbol[i], MODE_TICKVALUE);
        if (Virt.Type[i] == OP_BUY)
        {
            Virt.CloseTime[i] = TimeCurrent();                           // Время закрытия
            Virt.ClosePrice[i] = MarketInfo (Virt.Symbol[i], MODE_BID);  // Цена закрытия
            Virt.Swap[i] = MathRound ((Virt.CloseTime[i] - Virt.OpenTime[i]) / DAY) * MarketInfo (Virt.Symbol[i], MODE_SWAPLONG) * Virt.Lots[i];
            Virt.Profit[i] = (Virt.ClosePrice[i] - Virt.OpenPrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
        }
        else if (Virt.Type[i] == OP_SELL)
        {
            Virt.CloseTime[i] = TimeCurrent();                           // Время закрытия
            Virt.ClosePrice[i] = MarketInfo (Virt.Symbol[i], MODE_ASK);  // Цена закрытия
            Virt.Swap[i]= MathRound ((Virt.CloseTime[i] - Virt.OpenTime[i]) / DAY) * MarketInfo (Virt.Symbol[i], MODE_SWAPSHORT) * Virt.Lots[i];
            Virt.Profit[i] = (Virt.OpenPrice[i] - Virt.ClosePrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
        }
        else if (Virt.Type[i] > 1 && Virt.Type[i] < 6)
        {
            Virt.CloseTime[i] = TimeCurrent();                           // Время отмены
            Virt.Comment[i] = Virt.Comment[i] + "[canceled]";
        }
        for (j = i - 1; j >= 0; j--)
        {if (Virt.Status[j] == 0) break;}// Next j
        Virt.Status[i] = 0;                  
        if (j < i - 1)
        {
            j++;
            VirtualCopyPosition (j, MAX_POS - 1);
            VirtualCopyPosition (i, j);
            VirtualCopyPosition (MAX_POS - 1, i);
        }
        VirtualFileSave (filename);
        //---- Контролируем возможные ошибки
        fGetLastError (bs_ComError, "VirtualClose()");
        return (true);
    }
    else
    {
        Err.Number = 101;
        Err.Description = "При попытке закрыть позицию номер его найти не удалось";
        return (false);
   }
//----
}// VirtualClose()
//+------------------------------------------------------------------+
//|  Функция создаёт список индексов массива найденных позиций,      |
//|  соответствующих существующим параметрам фильтра                 |
//|  и возвращает размер списка.                                     |
//|  Примечание: файл с позициями должен быть загружен до вызова     |
//|  этой функции.                                                   |
//+------------------------------------------------------------------+
int VirtualFilter (int fStatus = -1,
                   int fTicket = -1, 
                   int fType = -1, 
                   string fSymbol = "", 
                   int fMagic = -1, 
                   string fComment = "")
{
    int err = GetLastError();
//----
    Virt.Filter.Count = 0;
    for (int i = 0; i < Virt.Count; i++)
    {
        if (fTicket == -1 || Virt.Ticket[i] == fTicket)
        {
            if (fType == -1 || Virt.Type[i] == fType)
            {
                if (fSymbol == "" || Virt.Symbol[i] == fSymbol)
                {
                    if (fComment == "" || StringFind (Virt.Comment[i], fComment) > -1)
                    {
                        if (fMagic == -1 || Virt.MagicNumber[i] == fMagic)
                        {
                            if (fStatus == -1 || Virt.Status[i] == fStatus)
                            {
                                Virt.Filter[Virt.Filter.Count] = i;
                                Virt.Filter.Count++;
                            }
                        }
                    }
                }
            }
        }
    }// Next i
    //---- Контролируем возможные ошибки
    fGetLastError (bs_ComError, "VirtualFilter()");
//----
    return (Virt.Filter.Count);
}// VirtualFilter()
//+------------------------------------------------------------------+
//|  Процедура обновления файла сделок в соответствии с              |
//|  произошедшими изменениями на рынке. Здесь происходят:           |
//|  + начисление свопов;                                            |
//|  + виртуальные закрытия сделок по выставленым заранее SL и TP;   |
//|  + обновления цен закрытия открытых позиций, расчёт прибылей;    |
//|  + открытие отложенных ордеров;                                  |
//|  + экспирация не сработавших отложенных ордеров;                 |
//+------------------------------------------------------------------+
void VirtualUpdate (string filename = "virtual.csv")
{
    double ld_Point, ld_TickValue;
    bool   is_changed, is_closed;
    int    i, j, err = GetLastError();
//----
    VirtualFileLoad (filename);
    for (i = Virt.Count - 1; i >= 0; i--)
    {
        is_closed = false;
        if (Virt.Status[i] == 1)
        {
            is_changed = true;
            ld_Point = MarketInfo (Virt.Symbol[i], MODE_POINT);
            ld_TickValue = MarketInfo (Virt.Symbol[i], MODE_TICKVALUE);
            switch (Virt.Type[i])
            {
                case OP_BUY:
                    Virt.CloseTime[i] = TimeCurrent();                             // Время закрытия
                    Virt.ClosePrice[i] = MarketInfo (Virt.Symbol[i], MODE_BID);    // Цена закрытия
                    Virt.Swap[i] = MathRound ((Virt.CloseTime[i] - Virt.OpenTime[i]) / DAY) * MarketInfo (Virt.Symbol[i], MODE_SWAPLONG) * Virt.Lots[i];
                    Virt.Profit[i] = (Virt.ClosePrice[i] - Virt.OpenPrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                    if (Virt.TakeProfit[i] > 0)
                    {
                        if (MarketInfo (Virt.Symbol[i], MODE_BID) >= Virt.TakeProfit[i])
                        {
                            Virt.ClosePrice[i] = Virt.TakeProfit[i];               // Цена закрытия
                            Virt.Profit[i] = (Virt.ClosePrice[i] - Virt.OpenPrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                            Virt.Comment[i] = Virt.Comment[i] + "[tp]";
                            is_closed = true;
                        }
                    }// End TakeProfit
                    if (Virt.StopLoss[i] > 0)
                    {
                        if (MarketInfo (Virt.Symbol[i], MODE_BID) <= Virt.StopLoss[i])
                        {
                            Virt.ClosePrice[i] = Virt.StopLoss[i];                 // Цена закрытия
                            Virt.Profit[i] = (Virt.ClosePrice[i] - Virt.OpenPrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                            Virt.Comment[i] = Virt.Comment[i] + "[sl]";
                            is_closed = true;
                        }
                    }// End StopLoss
                    break;
                case OP_SELL:
                    Virt.CloseTime[i] = TimeCurrent();                             // Время закрытия
                    Virt.ClosePrice[i] = MarketInfo (Virt.Symbol[i], MODE_ASK);    // Цена закрытия
                    Virt.Swap[i] = MathRound ((Virt.CloseTime[i] - Virt.OpenTime[i]) / DAY) * MarketInfo (Virt.Symbol[i], MODE_SWAPSHORT) * Virt.Lots[i];
                    Virt.Profit[i] = (Virt.OpenPrice[i] - Virt.ClosePrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                    if (Virt.TakeProfit[i] > 0)
                    {
                        if (MarketInfo (Virt.Symbol[i], MODE_ASK) <= Virt.TakeProfit[i])
                        {
                            Virt.ClosePrice[i] = Virt.TakeProfit[i];               // Цена закрытия
                            Virt.Profit[i] = (Virt.OpenPrice[i] - Virt.ClosePrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                            Virt.Comment[i] = Virt.Comment[i] + "[tp]";
                            is_closed = true;
                        }
                    }// End TakeProfit
                    if (Virt.StopLoss[i] > 0)
                    {
                        if (MarketInfo (Virt.Symbol[i], MODE_ASK) >= Virt.StopLoss[i])
                        {
                            Virt.ClosePrice[i] = Virt.StopLoss[i];                 // Цена закрытия
                            Virt.Profit[i] = (Virt.OpenPrice[i] - Virt.ClosePrice[i]) * Virt.Lots[i] * ld_TickValue / ld_Point + Virt.Swap[i];
                            Virt.Comment[i] = Virt.Comment[i] + "[sl]";
                            is_closed=true;
                        }
                    }// End StopLoss
                    break;
                case OP_BUYLIMIT:
                    if (MarketInfo (Virt.Symbol[i], MODE_ASK) <= Virt.OpenPrice[i])
                    {Virt.Type[i] = OP_BUY;}
                    break;
                case OP_SELLLIMIT:
                    if (MarketInfo (Virt.Symbol[i], MODE_BID) >= Virt.OpenPrice[i])
                    {Virt.Type[i] = OP_SELL;}
                    break;
                case OP_BUYSTOP:
                    if (MarketInfo (Virt.Symbol[i], MODE_ASK) >= Virt.OpenPrice[i])
                    {Virt.Type[i] = OP_BUY;}
                    break;
                case OP_SELLSTOP:
                    if (MarketInfo (Virt.Symbol[i], MODE_BID) <= Virt.OpenPrice[i])
                    {Virt.Type[i] = OP_SELL;}
                    break;
            }// End switch
            if (Virt.Type[i] > 1 && Virt.Type[i] < 6)
            {
                if (Virt.Expiration[i] > 0)
                {
                    if (TimeCurrent() > Virt.Expiration[i])
                    {
                        Virt.Comment[i] = Virt.Comment[i] + "[expiration]";
                        is_closed = true;
                    }
                }
            }
            if (is_closed)
            {
                for (j = i; j >= 0; j--)
                {if (Virt.Status[j] == 0) break;}// Next j
                Virt.Status[i] = 0;                  
                if (j < i - 1)
                {
                    j++;
                    VirtualCopyPosition (j, MAX_POS - 1);
                    VirtualCopyPosition (i, j);
                    VirtualCopyPosition (MAX_POS - 1, i);
                }
            }// End if (is_closed)
        }
        else {break;}// End if (Virt.Status[i]==1)
    }// Next i
    if (is_changed) VirtualFileSave (filename);
    //---- Контролируем возможные ошибки
    fGetLastError (bs_ComError, "VirtualUpdate()");
//----
}// VirtualUpdate()
//+------------------------------------------------------------------+
//|  Процедура копрования позиции в другую ячейку массива            |
//+------------------------------------------------------------------+
void VirtualCopyPosition (int FirstPosition, int SecondPosition)
{
    Virt.Ticket[SecondPosition] = Virt.Ticket[FirstPosition];           // - Номер ордера
    Virt.OpenTime[SecondPosition] = Virt.OpenTime[FirstPosition];       // - Время отрытия
    Virt.Type[SecondPosition] = Virt.Type[FirstPosition];               // - Тип сделки
    Virt.Lots[SecondPosition] = Virt.Lots[FirstPosition];               // - Объём
    Virt.Symbol[SecondPosition] = Virt.Symbol[FirstPosition];           // - Символ
    Virt.OpenPrice[SecondPosition] = Virt.OpenPrice[FirstPosition];     // - Цена открытия
    Virt.StopLoss[SecondPosition] = Virt.StopLoss[FirstPosition];
    Virt.TakeProfit[SecondPosition] = Virt.TakeProfit[FirstPosition];
    Virt.CloseTime[SecondPosition] = Virt.CloseTime[FirstPosition];     // - Время закрытия
    Virt.ClosePrice[SecondPosition] = Virt.ClosePrice[FirstPosition];   // - Цена закрытия
    Virt.Swap[SecondPosition] = Virt.Swap[FirstPosition];               // - Своп
    Virt.Profit[SecondPosition] = Virt.Profit[FirstPosition];           // - Прибыль в пипсолотах
    Virt.Comment[SecondPosition] = Virt.Comment[FirstPosition];         // - Комментарии
    Virt.MagicNumber[SecondPosition] = Virt.MagicNumber[FirstPosition]; // - Магическое число
    Virt.Expiration[SecondPosition] = Virt.Expiration[FirstPosition];   // - Дата отмены отложенного ордера
    Virt.Status[SecondPosition] = Virt.Status[FirstPosition];           // - Статус ордера: 1 - открытая позиция/отложенный ордер; 0 - закрытая позиция/отменённый ордер
}// VirtualCopyPosition()
//+------------------------------------------------------------------+
//|  Основная функция, используемая для открытия позиции или         |
//|  установки отложенного ордера.                                   |
//|  Возвращает номер тикета, который назначен ордеру торговым       |
//|  сервером или -1 в случае неудачи.                               |
//+------------------------------------------------------------------+
int VirtualSend (string symbol,                    // Наименование финансового инструмента, с которым проводится торговая операция. 
                 int cmd,                          // Торговая операция. Может быть любым из значений торговых операций. 
                 double volume,                    // Количество лотов.
                 double price,                     // Цена открытия. 
                 int slippage,                     // Максимально допустимое отклонение цены для рыночных ордеров в пунктах (ордеров на покупку или продажу).
                 double stoploss,                  // Цена закрытия позиции при достижении уровня убыточности (0 в случае отсутствия уровня убыточности).
                 double takeprofit,                // Цена закрытия позиции при достижении уровня прибыльности (0 в случае отсутствия уровня прибыльности).
                 string comment = "",              // Текст комментария ордера. Последняя часть комментария может быть изменена торговым сервером.
                 int magic = 0,                    // Магическое число ордера. Может использоваться как определяемый пользователем идентификатор.
                 datetime expiration = 0,          // Срок истечения отложенного ордера.
                 string filename = "virtual.csv")  // Имя файла виртуальных сделок из каталога TerminalPath()+"\experts\files"
{
    double ld_Point = MarketInfo (symbol, MODE_POINT),
           ld_StopLevel = MarketInfo (symbol, MODE_STOPLEVEL),
           lda_Price[2];
    int err = GetLastError();
//----
    lda_Price[0] = MarketInfo (symbol, MODE_ASK);
    lda_Price[1] = MarketInfo (symbol, MODE_BID);
    //---- Блок проверок:
    if (cmd == OP_BUY)
    {
        //---- Цена открытия покупки должна быть около Ask +- sleeppage
        if ((price > lda_Price[cmd] + slippage * ld_Point)
        || (price < lda_Price[cmd] - slippage * ld_Point))
        {
            Err.Number = 1;
            Err.Description = "Цена открытия позиции далеко от рынка";
            return (-1);
        }
    }
    else if (cmd == OP_SELL)
    {
        if ((price > lda_Price[cmd] + slippage * ld_Point)
        || (price < lda_Price[cmd] - slippage * ld_Point))
        {
            Err.Number = 1;
            Err.Description = "Цена открытия позиции далеко от рынка";
            return (-1);
        }
    }
    else if (cmd == OP_BUYSTOP)
    {
        if (price <= lda_Price[0] + ld_StopLevel * ld_Point)
        {
            Err.Number = 2;
            Err.Description = "Цена открытия отложенного ордера слишком близка к рынку";
            return (-1);
        }
    }
    else if (cmd == OP_SELLSTOP)
    {
        if (price >= lda_Price[1] - ld_StopLevel * ld_Point)
        {
            Err.Number = 2;
            Err.Description = "Цена открытия отложенного ордера слишком близка к рынку";
            return (-1);
        }
    }
    else if (cmd == OP_BUYLIMIT)
    {
        if (price >= lda_Price[0] - ld_StopLevel * ld_Point)
        {
            Err.Number = 2;
            Err.Description = "Цена открытия отложенного ордера слишком близка к рынку";
            return (-1);
        }
    }
    else if (cmd == OP_SELLLIMIT)
    {
        if (price <= lda_Price[1] + ld_StopLevel * ld_Point)
        {
            Err.Number = 2;
            Err.Description = "Цена открытия отложенного ордера слишком близка к рынку";
            return (-1);
        }
    }
    if (stoploss != 0.0)
    {
        if ((cmd == OP_BUY) || (cmd == OP_BUYSTOP) || (cmd == OP_BUYLIMIT))         // Покупка
        {
            if (price - stoploss <= ld_StopLevel * ld_Point)
            { 
                Err.Number = 3;
                Err.Description = "Уровень StopLoss или TakeProfit слишком близок к цене";
                return (-1);
            }
        }
        else if ((cmd == OP_SELL) || (cmd == OP_SELLSTOP) || (cmd == OP_SELLLIMIT)) // Продажа
        {
            if (stoploss - price <= ld_StopLevel * ld_Point)
            { 
                Err.Number = 3;
                Err.Description = "Уровень StopLoss или TakeProfit слишком близок к цене";
                return (-1);
            }
        }
    }
    if (takeprofit != 0.0)
    {
        if ((cmd == OP_BUY) || (cmd == OP_BUYSTOP) || (cmd == OP_BUYLIMIT))         // Покупка
        {
            if (takeprofit - price <= ld_StopLevel * ld_Point)
            {
                Err.Number = 3;
                Err.Description = "Уровень StopLoss или TakeProfit слишком близок к цене";
                return (-1);
            }
        }
        else if ((cmd == OP_SELL) || (cmd == OP_SELLSTOP) || (cmd == OP_SELLLIMIT)) // Продажа
        {
            if (price - takeprofit <= ld_StopLevel * ld_Point)
            {
                Err.Number = 3;
                Err.Description = "Уровень StopLoss или TakeProfit слишком близок к цене";
                return (-1);
            }
        }      
    }
    if ((volume < MarketInfo (symbol, MODE_MINLOT))
    || (volume > MarketInfo (symbol, MODE_MAXLOT)))
    {
        Err.Number = 4;
        Err.Description = "Неправильный лот";
        return (-1);
    }
    if (expiration <= TimeCurrent() && expiration != 0)
    {
        Err.Number = 5;
        Err.Description = "Неправильная дата экспирации";
        return (-1);
    }
    //---- Конец блока проверок
    //---- Защита от переполнения массива
    int i, j, k;
    VirtualFileLoad (filename);
   
    if (Virt.Count > MAX_POS - 2)
    {
        //---- Удаляем все отменённые отложенные ордера
        for (i = 0; i < Virt.Count; i++)
        {
            if ((Virt.Type[i] > 1) && (Virt.Status[i] == 0))
            {
                //---- Все сделки, расположенные ниже этого отложенного ордера сдвигаются на уровень вверх:
                for (j = i; j < Virt.Count - 1; j++)
                {
                    Virt.Ticket[j] = Virt.Ticket[j+1];
                    Virt.OpenTime[j] = Virt.OpenTime[j+1];
                    Virt.Type[j] = Virt.Type[j+1];
                    Virt.Lots[j] = Virt.Lots[j+1];
                    Virt.Symbol[j] = Virt.Symbol[j+1];
                    Virt.OpenPrice[j] = Virt.OpenPrice[j+1];
                    Virt.StopLoss[j] = Virt.StopLoss[j+1];
                    Virt.TakeProfit[j] = Virt.TakeProfit[j+1];
                    Virt.CloseTime[j] = Virt.CloseTime[j+1];
                    Virt.ClosePrice[j] = Virt.ClosePrice[j+1];
                    Virt.Swap[j] = Virt.Swap[j+1];
                    Virt.Profit[j] = Virt.Profit[j+1];
                    Virt.Comment[j] = Virt.Comment[j+1];
                    Virt.MagicNumber[j] = Virt.MagicNumber[j+1];
                    Virt.Expiration[j] = Virt.Expiration[j+1];
                    Virt.Status[j] = Virt.Status[j+1];
                }// Next j
                Virt.Count--;
            }
        }// Next i
        //---- Поиск первой закрытой сделки
        for (i = 0; i < Virt.Count; i++)
        {
            if ((Virt.Type[i] < 2) && (Virt.Status[i] == 0))
            {break;}
        }// Next i
        if (i == Virt.Count)
        {
            Err.Number = 402;
            Err.Description = "Количество открытых сделок привысило максимальнодопустимый уроваень! Необходимо переделать торговую систему.";
            return (-1);
        }
        else
        {
            //---- Поиск второй закрытой сделки
            for (j = i + 1; j < Virt.Count; j++)
            {if (Virt.Status[j] == 0) {break;}}// Next j
            if (j == Virt.Count)
            {
                Err.Number = 402;
                Err.Description = "Количество открытых сделок привысило максимальнодопустимый уроваень! Необходимо переделать торговую систему.";
                return (-1);
            }
            else
            {
                //---- В первую закрытую сделку вносим данные о совокупности 2 сделок (первой и второй):
                Virt.Ticket[i] = Virt.Ticket[j];
                Virt.OpenTime[i] = Virt.OpenTime[j];
                Virt.Type[i] = -1;
                Virt.Lots[i] = Virt.Lots[i] + Virt.Lots[j];
                Virt.Symbol[i] = "";
                Virt.OpenPrice[i] = 0;
                Virt.StopLoss[i] = 0;
                Virt.TakeProfit[i] = 0;
                Virt.CloseTime[i] = Virt.CloseTime[j];
                Virt.ClosePrice[i] = 0;
                Virt.Swap[i] = Virt.Swap[i] + Virt.Swap[j];
                Virt.Profit[i] = Virt.Profit[i] + Virt.Profit[j];
                Virt.Comment[i] = "Archive";
                Virt.MagicNumber[i] = Virt.MagicNumber[j];
                Virt.Expiration[i] = Virt.Expiration[j];
                //---- Все сделки, расположенные ниже второй закрытой сделки сдвигаются на уровень вверх:
                for (k = j; k < Virt.Count - 1; k++)
                {
                    Virt.Ticket[k] = Virt.Ticket[k+1];
                    Virt.OpenTime[k] = Virt.OpenTime[k+1];
                    Virt.Type[k] = Virt.Type[k+1];
                    Virt.Lots[k] = Virt.Lots[k+1];
                    Virt.Symbol[k] = Virt.Symbol[k+1];
                    Virt.OpenPrice[k] = Virt.OpenPrice[k+1];
                    Virt.StopLoss[k] = Virt.StopLoss[k+1];
                    Virt.TakeProfit[k] = Virt.TakeProfit[k+1];
                    Virt.CloseTime[k] = Virt.CloseTime[k+1];
                    Virt.ClosePrice[k] = Virt.ClosePrice[k+1];
                    Virt.Swap[k] = Virt.Swap[k+1];
                    Virt.Profit[k] = Virt.Profit[k+1];
                    Virt.Comment[k] = Virt.Comment[k+1];
                    Virt.MagicNumber[k] = Virt.MagicNumber[k+1];
                    Virt.Expiration[k] = Virt.Expiration[k+1];
                    Virt.Status[k] = Virt.Status[k+1];
                }// Next k
                Virt.Count--;
            }
        }
    }
    //---- Конец защиты от переполнения массива
    //---- Добавление новой позиции в массив
    Virt.Count++;
    Virt.Ticket[Virt.Count-1] = VirtualHighTicket() + 1;
    Virt.OpenTime[Virt.Count-1] = TimeCurrent();
    Virt.Type[Virt.Count-1] = cmd;
    Virt.Lots[Virt.Count-1] = volume;
    Virt.Symbol[Virt.Count-1] = symbol;
    Virt.OpenPrice[Virt.Count-1] = price;
    Virt.StopLoss[Virt.Count-1] = stoploss;
    Virt.TakeProfit[Virt.Count-1] = takeprofit;
    Virt.Comment[Virt.Count-1] = comment;
    Virt.MagicNumber[Virt.Count-1] = magic;
    Virt.Expiration[Virt.Count-1] = expiration;
    Virt.Status[Virt.Count-1] = 1;
    //---- Сохранение изменений
    VirtualFileSave (filename);
    //---- Контролируем возможные ошибки
    fGetLastError (bs_ComError, "VirtualSend()");
//----
    return (Virt.Ticket[Virt.Count-1]);
}// VirtualSend()
//+------------------------------------------------------------------+
//|  Поиск молодого тикета                                           |
//+------------------------------------------------------------------+
int VirtualHighTicket()
{
    int i, j;
//----
    for (i = 0; i < Virt.Count; i++)
    {
        if (Virt.Ticket[i] > j)
        {j = Virt.Ticket[i];}
    }// Next i
//----
    return (j);
}// VirtualHighTicket()
//+------------------------------------------------------------------+
//|  Функция выполняет загрузку из файла сделок в массив сделок      |
//|  и возвращает количество загруженных строк.                      |
//+------------------------------------------------------------------+
int VirtualFileLoad (string file)
{
    if (FastTest)
    {if (IsTesting()) return (0);}
//----
    int k, Count, err = GetLastError();
    string buffer[];
    ArrayResize (buffer, VirtBufferSize * MAX_POS);
    int handle = FileOpen (file, FILE_CSV|FILE_READ, ';');
//----
    if (handle < 1)
    {
        Err.Number = 401;
        Err.Description = "Файл сделок не обнаружен";
        return (-1);
    }
    else
    {
        Count = 0;
        //---- Считываем файл
        while (!FileIsEnding (handle))
        {
            buffer[Count] = FileReadString (handle);
            Count++;
        }
        Count--;
        FileClose (handle);
        //---- Заполнение массива
        Virt.Count = 0;
        k = VirtBufferSize;
        //if (fCCV_D (Count / VirtBufferSize - 1, 2))
        //Print ("Загружено ", Count / VirtBufferSize - 1, " строк");
        while (Virt.Count < Count / VirtBufferSize - 1)
        {
            Virt.Ticket[Virt.Count] = StrToInteger (buffer[k]);         // Номер ордера
            Virt.OpenTime[Virt.Count] = StrToTime (buffer[k+1]);        // Время отрытия
            Virt.Type[Virt.Count] = StrToInteger (buffer[k+2]);         // Тип сделки
            Virt.Lots[Virt.Count] = StrToDouble (buffer[k+3]);          // Объём
            Virt.Symbol[Virt.Count] = buffer[k+4];                      // Символ
            Virt.OpenPrice[Virt.Count] = StrToDouble (buffer[k+5]);     // Цена открытия
            Virt.StopLoss[Virt.Count] = StrToDouble (buffer[k+6]);
            Virt.TakeProfit[Virt.Count] = StrToDouble (buffer[k+7]);
            Virt.CloseTime[Virt.Count] = StrToTime (buffer[k+8]);       // Время закрытия
            Virt.ClosePrice[Virt.Count] = StrToDouble (buffer[k+9]);    // Цена закрытия
            Virt.Swap[Virt.Count] = StrToDouble (buffer[k+10]);         // Своп
            Virt.Profit[Virt.Count] = StrToDouble (buffer[k+11]);       // Прибыль в пипсолотах
            Virt.Comment[Virt.Count] = buffer[k+12];                    // Комментарии
            Virt.MagicNumber[Virt.Count] = StrToInteger (buffer[k+13]); // Магическое число
            Virt.Expiration[Virt.Count] = StrToTime (buffer[k+14]);     // Дата отмены отложенного ордера
            Virt.Status[Virt.Count] = StrToInteger (buffer[k+15]);      // Статус ордера: 1 - открыт; 0 - закрыт
            k += VirtBufferSize;
            Virt.Count++;
        }
        //---- Контролируем возможные ошибки
        fGetLastError (bs_ComError, "VirtualFileLoad()");
        return (Virt.Count);
    }
//----
}// VirtualFileLoad()
//+------------------------------------------------------------------+
//|  Процедура сохранения массива сделок в указанный файл.           |
//+------------------------------------------------------------------+
void VirtualFileSave (string file)
{
    if (FastTest)
    {if (IsTesting()) return (0);}
//----
    int Count = 0, err = GetLastError();
    int handle = FileOpen (file, FILE_CSV|FILE_WRITE, ';');
//----
    FileWrite (handle, "Номер ордера", "Время отрытия", "Тип", "Объём", "Символ", "Цена открытия", "S/L", "T/P",
    "Время закрытия", "Цена закрытия", "Своп", "Прибыль", "Комментарий", "Магическое число", "Экспирация", "Статус");
    //---- Сохранение в файл
    while (Count < Virt.Count)
    {
        //----Возможно придётся применить преобразование данных
        FileWrite (handle,
        Virt.Ticket[Count],
        TimeToStr (Virt.OpenTime[Count]),
        Virt.Type[Count],
        Virt.Lots[Count],
        Virt.Symbol[Count],
        Virt.OpenPrice[Count],
        Virt.StopLoss[Count],
        Virt.TakeProfit[Count],
        TimeToStr (Virt.CloseTime[Count]),
        Virt.ClosePrice[Count],
        Virt.Swap[Count],
        Virt.Profit[Count],
        Virt.Comment[Count],
        Virt.MagicNumber[Count],
        TimeToStr (Virt.Expiration[Count]),
        Virt.Status[Count]);
        Count++;
    }
    FileClose (handle);   
    //if (fCCV_D (Count, 1))
    //Print ("Записано ", Count, " строк");   
    //---- Контролируем возможные ошибки
    fGetLastError (bs_ComError, "VirtualFileSave()");
//----
}// VirtualFileSave()
//+------------------------------------------------------------------+
bool VirtualExist (datetime TimeOpenCandle, int fMagic = 0)
{
//----
    VirtualFilter (VIRT_TRADES, -1, -1, Symbol(), fMagic);
    for (int i = 0; i < Virt.Filter.Count; i++)
    {
        if (Virt.OpenTime[Virt.Filter[i]] >= TimeOpenCandle)
        {return (true);}
    }// Next i
//----
   return (false);
}// VirtualExist()
//+------------------------------------------------------------------+

