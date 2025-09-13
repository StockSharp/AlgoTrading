//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|                                                                 b-PSI@Comment.mqh |
//|                                       Copyright © 2012, Igor Stepovoi aka TarasBY |
//|                                                                taras_bulba@tut.by |
//| 18.04.2012  Библиотека работы с комментариями                                     |
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
//IIIIIIIIIIIIIIIIIII=========Подключение внешних модулей=======IIIIIIIIIIIIIIIIIIIIII+
//IIIIIIIIIIIIIIIIIII========Глобальные переменные модуля=======IIIIIIIIIIIIIIIIIIIIII+
                    // bsa_Comment[0]- работа MM
                    // bsa_Comment[1]- работа TimeControl
                    // bsa_Comment[2]- информация по модификации ордеров и виртуальному трейлингу (Trail)
                    // bsa_Comment[3]- информация по открытию ордеров (TradeLight)
                    // bsa_Comment[4]- информация по закрытию ордеров (TradeLight)
                    // bsa_Comment[5]- частичное закрытие ордеров (PartClose) и виртуальные СТОПы (VirtuaSTOPs)
                    // bsa_Comment[6]- работа с общим профитом (ManagerPA)
                    // bsa_Comment[7]- ошибки
//IIIIIIIIIIIIIIIIIII===========Перечень функций модуля=========IIIIIIIIIIIIIIIIIIIIII+
/*void fSet_Comment (double fd_CheckParameter,            // проверяемый параметр (для конторля одноразовости вывода комментария)
                     int fi_Ticket,                       // Ticket
                     int fi_N,                            // номер комментария
                     string fs_MSG = "",                  // текст сообщения
                     bool fb_Right = true,                // успешность операции для которой готовим комментарий
                     double fd_Value1 = 0.0,              // передаваемый в комментарий параметр
                     double fd_Value2 = 0.0,              // передаваемый в комментарий параметр
                     double fd_Value3 = 0.0,              // передаваемый в комментарий параметр
                     double fd_Value4 = 0.0)*/            // передаваемый в комментарий параметр
                                     // Формируем комментарии для произошедших событий
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//+===================================================================================+
//|***********************************************************************************|
//| РАЗДЕЛ: Работа с комментариями                                                    |
//|***********************************************************************************|
//+===================================================================================+
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
//|  Автор    : TarasBY, taras_bulba@tut.by                                           |
//+-----------------------------------------------------------------------------------+
//|           Формируем комментарии для произошедших событий                          |
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+
void fSet_Comment (double fd_CheckParameter,  // проверяемый параметр (для конторля одноразовости вывода комментария)
                   int fi_Ticket,             // Ticket
                   int fi_N,                  // номер комментария
                   string fs_MSG = "",        // текст сообщения
                   bool fb_Right = true,      // успешность операции для которой готовим комментарий
                   double fd_Value1 = 0.0,    // передаваемый в комментарий параметр
                   double fd_Value2 = 0.0,    // передаваемый в комментарий параметр
                   double fd_Value3 = 0.0,    // передаваемый в комментарий параметр
                   double fd_Value4 = 0.0)    // передаваемый в комментарий параметр
{
	 if (bb_VirtualTrade) return;
	 int    err = GetLastError();
	 string ls_com = "", ls_NewRow = ": ", ls_order = "";
//----
    switch (fi_N)
    {
        //---- Комментарии ММ
        case 0: ls_com = StringConcatenate ("Начинаем серию заново: Lots = ", DSDig (fd_Value1)); break;
        case 1: break; // На доработке 
    }
    //---- Выводим комментарии на график, пишем в лог и в файл
    //fWrite_Log (fs_MSG, li_IND);
    //---- Озвучиваем события
    if (bb_PlaySound) {if (fb_Right) PlaySound ("ok.wav"); else PlaySound ("stops.wav");}
    //---- Контролируем возможные ошибки
	 fGet_LastErrorInArray (bsa_Comment, "fSet_Comment()", bi_indERR);
//----
	 return;
}
//IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII+

