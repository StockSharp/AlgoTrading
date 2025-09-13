//+------------------------------------------------------------------+
//|                                             		AllMinutes.mq4 |
//|                                 Copyright © 2006-2015, komposter |
//|                                          http://www.komposter.me |
//+------------------------------------------------------------------+
#property copyright		"Copyright © 2006-2015, komposter"
#property link				"http://www.komposter.me"
#property version	 		"2.0"
#property strict

#include <WinUser32.mqh>

//---- Список графиков которые необходимо обрабатывать, разделённый запятой (",").
//---- Символ от периода отделяется пробелом (" ").
input string   ChartList="EURUSD 1,GBPUSD 1";      // * Charts list

//---- Разрешить/запретить рисовать бары в выходные
//---- Если == true, выходные останутся незаполнеными
//---- Если == false, выходные будут заполнены барами O=H=L=C
input bool      SkipWeekEnd=true;                     // * Skeep weekends

//---- Частота, с которой будут обновляться графики в милисекундах
//---- Чем больше значение, тем меньше ресурсов будет использовать скрипт.
input int      RefreshLuft=1000;

int      ChartsCount=0;
string   arrSymbols[100]; int arrPeriods[100],_PeriodSec[],_Bars[];
int      HistoryHandle[],hwnd[];
ulong      last_fpos[];
datetime   pre_time[],now_time[];
double   now_close[],now_open[],now_low[],now_high[];
double   pre_close[],pre_open[],pre_low[],pre_high[];
long      now_volume[],pre_volume[];
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   ChartsCount=0;
   int      _GetLastError=0,cnt_copy=0,cnt_add=0;
   int      pos=0,len=StringLen(ChartList);
   string   cur_symbol= "",cur_period = "",file_name = "";
   uchar      curchar = 0;
   int      i_unused[13];   ArrayInitialize(i_unused,0);

   bool      period_start=false;

//---- считаем количество графиков, которые необходимо обработать
   while(pos<=len)
     {
      curchar=(uchar)StringGetChar(ChartList,pos);
      if(CharToStr(curchar)==" ")
        {
         period_start=true;
         pos++;
         continue;
        }
      if(period_start && curchar>47 && curchar<58)
        { cur_period=cur_period+CharToStr(curchar); }
      else
        {
         if(curchar==',' || pos==len)
           {
            MarketInfo(cur_symbol,MODE_BID);
            if(GetLastError()==4106)
              {
               Alert("Неизвестный символ ",cur_symbol,"!!!");
               return(-1);
              }
            if(iClose(cur_symbol,StrToInteger(cur_period),0)<=0)
              {
               Alert("Неизвестный период ",cur_period,"!!!");
               return(-1);
              }

            arrSymbols[ChartsCount]=cur_symbol; arrPeriods[ChartsCount]=StrToInteger(cur_period);
            cur_symbol=""; cur_period=""; period_start=false;

            ChartsCount++;
           }
         else
           { cur_symbol=cur_symbol+CharToStr(curchar); }
        }
      pos++;
     }
   Print("< - - - Найдено ",ChartsCount," корректных графиков. - - - >");

   ArrayResize(arrSymbols,ChartsCount); ArrayResize(arrPeriods,ChartsCount);
   ArrayResize(HistoryHandle,ChartsCount); ArrayResize(hwnd,ChartsCount);
   ArrayResize(last_fpos,ChartsCount); ArrayResize(pre_time,ChartsCount);
   ArrayResize(now_time,ChartsCount); ArrayResize(now_close,ChartsCount);
   ArrayResize(now_open,ChartsCount); ArrayResize(now_low,ChartsCount);
   ArrayResize(now_high,ChartsCount); ArrayResize(now_volume,ChartsCount);
   ArrayResize(pre_close,ChartsCount); ArrayResize(pre_open,ChartsCount);
   ArrayResize(pre_low,ChartsCount); ArrayResize(pre_high,ChartsCount);
   ArrayResize(pre_volume,ChartsCount); ArrayResize(_PeriodSec,ChartsCount);
   ArrayResize(_Bars,ChartsCount);

//---
   for(int curChart=0; curChart<ChartsCount; curChart++)
     {
      _PeriodSec[curChart]=arrPeriods[curChart]*60;

      //---- открываем файл, в который будем записывать историю
      file_name=StringConcatenate("ALL",arrSymbols[curChart],arrPeriods[curChart],".hst");
      HistoryHandle[curChart]=FileOpenHistory(file_name,FILE_BIN|FILE_WRITE|FILE_SHARE_WRITE|FILE_SHARE_READ|FILE_ANSI);
      if(HistoryHandle[curChart]<0)
        {
         _GetLastError=GetLastError();
         Alert("FileOpenHistory( \"",file_name,"\", FILE_BIN | FILE_WRITE | FILE_SHARE_WRITE | FILE_SHARE_READ | FILE_ANSI )"," - Error #",_GetLastError);
         continue;
        }

      //---- Записываем заголовок файла
      FileWriteInteger(HistoryHandle[curChart],401,LONG_VALUE);
      FileWriteString(HistoryHandle[curChart],"Copyright © 2006-2015, komposter",64);
      FileWriteString(HistoryHandle[curChart],StringConcatenate("ALL",arrSymbols[curChart]),12);
      FileWriteInteger(HistoryHandle[curChart],arrPeriods[curChart],LONG_VALUE);
      FileWriteInteger(HistoryHandle[curChart],(int)MarketInfo(arrSymbols[curChart],MODE_DIGITS),LONG_VALUE);
      FileWriteInteger   ( HistoryHandle[curChart], 0, LONG_VALUE );       //timesign
      FileWriteInteger   ( HistoryHandle[curChart], 0, LONG_VALUE );       //last_sync
      FileWriteArray(HistoryHandle[curChart],i_unused,0,13);
      FileFlush(HistoryHandle[curChart]);

      //+------------------------------------------------------------------+
      //| Обрабатываем историю                                             |
      //+------------------------------------------------------------------+
      _Bars[curChart]=iBars(arrSymbols[curChart],arrPeriods[curChart]);
      pre_time[curChart]=iTime(arrSymbols[curChart],arrPeriods[curChart],_Bars[curChart]-1);
      for(int i=_Bars[curChart]-1; i>=1; i--)
        {
         //---- Запоминаем параметры бара
         now_open      [curChart] = iOpen   ( arrSymbols[curChart], arrPeriods[curChart], i );
         now_high      [curChart] = iHigh   ( arrSymbols[curChart], arrPeriods[curChart], i );
         now_low      [curChart]= iLow(arrSymbols[curChart],arrPeriods[curChart],i);
         now_close   [curChart] = iClose(arrSymbols[curChart],arrPeriods[curChart],i);
         now_volume   [curChart]= iVolume(arrSymbols[curChart],arrPeriods[curChart],i);
         now_time[curChart]=iTime(arrSymbols[curChart],arrPeriods[curChart],i)/_PeriodSec[curChart];
         now_time[curChart]*=_PeriodSec[curChart];

         //---- если есть пропущенные бары,
         while(now_time[curChart]>pre_time[curChart]+_PeriodSec[curChart])
           {
            pre_time[curChart] += _PeriodSec[curChart];
            pre_time[curChart] /= _PeriodSec[curChart];
            pre_time[curChart] *= _PeriodSec[curChart];

            //---- если это не выходные,
            if(SkipWeekEnd)
              {
               if(TimeDayOfWeek(pre_time[curChart])<=0 || 
                  TimeDayOfWeek(pre_time[curChart])>5) { continue; }
               if(TimeDayOfWeek(pre_time[curChart])==5)
                 {
                  if(TimeHour(pre_time[curChart])==23 || 
                     TimeHour(pre_time[curChart]+_PeriodSec[curChart])==23) { continue; }
                 }
              }

            //---- записываем пропущенный бар в файл
            WriteToFile(HistoryHandle[curChart],pre_time[curChart],pre_close[curChart],pre_close[curChart],pre_close[curChart],pre_close[curChart],1);
            FileFlush(HistoryHandle[curChart]);
            cnt_add++;
           }

         //---- записываем новый бар в файл
         WriteToFile(HistoryHandle[curChart],now_time[curChart],now_open[curChart],now_low[curChart],now_high[curChart],now_close[curChart],now_volume[curChart]);
         FileFlush(HistoryHandle[curChart]);
         cnt_copy++;

         //---- запоминаем значение времени и цену закрытия записанного бара
         pre_close[curChart]=now_close[curChart];
         pre_time[curChart]=now_time[curChart]/_PeriodSec[curChart];
         pre_time[curChart]*=_PeriodSec[curChart];
        }

      last_fpos[curChart]=FileTell(HistoryHandle[curChart]);

      //---- выводим статистику
      Print("< - - - ",arrSymbols[curChart],arrPeriods[curChart],": было ",cnt_copy," баров, добавлено ",cnt_add," баров - - - >");
      Print("< - - - Для просмотра результатов, откройте график \"ALL",arrSymbols[curChart],arrPeriods[curChart],"\" - - - >");
     }

//---
   if(!EventSetMillisecondTimer(RefreshLuft))
     {
      Alert("Can't set timer!");
      return(INIT_FAILED);
     }

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Обрабатываем поступающие тики                                    |
//+------------------------------------------------------------------+
void OnTimer()
  {
   RefreshRates();
   for(int curChart=0; curChart<ChartsCount; curChart++)
     {
      //---- ставим "курсор" перед последним баром
      //---- (это необходимо на всех запусках, кроме первого)
      FileSeek(HistoryHandle[curChart],last_fpos[curChart],SEEK_SET);

      //---- Запоминаем параметры бара
      now_open      [curChart] = iOpen   ( arrSymbols[curChart], arrPeriods[curChart], 0 );
      now_high      [curChart] = iHigh   ( arrSymbols[curChart], arrPeriods[curChart], 0 );
      now_low      [curChart]= iLow(arrSymbols[curChart],arrPeriods[curChart],0);
      now_close   [curChart] = iClose(arrSymbols[curChart],arrPeriods[curChart],0);
      now_volume   [curChart]= iVolume(arrSymbols[curChart],arrPeriods[curChart],0);
      now_time[curChart]=iTime(arrSymbols[curChart],arrPeriods[curChart],0)/_PeriodSec[curChart];
      now_time[curChart]*=_PeriodSec[curChart];

      //---- если бар сформировался, 
      if(now_time[curChart]>=pre_time[curChart]+_PeriodSec[curChart])
        {
         //---- записываем сформировавшийся бар
         WriteToFile(HistoryHandle[curChart],pre_time[curChart],pre_open[curChart],pre_low[curChart],pre_high[curChart],pre_close[curChart],pre_volume[curChart]);
         FileFlush(HistoryHandle[curChart]);

         //---- запоминаем место в файле, перед записью 0-го бара
         last_fpos[curChart]=FileTell(HistoryHandle[curChart]);
        }

      //---- если появились пропущенные бары,
      while(now_time[curChart]>pre_time[curChart]+_PeriodSec[curChart])
        {
         pre_time[curChart] += _PeriodSec[curChart];
         pre_time[curChart] /= _PeriodSec[curChart];
         pre_time[curChart] *= _PeriodSec[curChart];

         //---- если это не выходные,
         if(SkipWeekEnd)
           {
            if(TimeDayOfWeek(pre_time[curChart])<=0 || 
               TimeDayOfWeek(pre_time[curChart])>5) { continue; }
            if(TimeDayOfWeek(pre_time[curChart])==5)
              {
               if(TimeHour(pre_time[curChart])==23 || 
                  TimeHour(pre_time[curChart]+_PeriodSec[curChart])==23) { continue; }
              }
           }

         //---- записываем пропущенный бар в файл
         WriteToFile(HistoryHandle[curChart],pre_time[curChart],pre_close[curChart],pre_close[curChart],pre_close[curChart],pre_close[curChart],1);
         FileFlush(HistoryHandle[curChart]);

         //---- запоминаем место в файле, перед записью 0-го бара
         last_fpos[curChart]=FileTell(HistoryHandle[curChart]);
        }

      //---- записываем текущий бар
      WriteToFile(HistoryHandle[curChart],now_time[curChart],now_open[curChart],now_low[curChart],now_high[curChart],now_close[curChart],now_volume[curChart]);
      FileFlush(HistoryHandle[curChart]);

      //---- запоминаем параметры записанного бара
      pre_open[curChart]      = now_open[curChart];
      pre_high[curChart]      = now_high[curChart];
      pre_low[curChart]=now_low[curChart];
      pre_close[curChart]=now_close[curChart];
      pre_volume[curChart]=now_volume[curChart];
      pre_time[curChart]=now_time[curChart]/_PeriodSec[curChart];
      pre_time[curChart]*=_PeriodSec[curChart];

      //---- находим окно, в которое будем "отправлять" свежие котировки
      if(hwnd[curChart]==0)
        {
         hwnd[curChart]=WindowHandle(StringConcatenate("ALL",arrSymbols[curChart]),arrPeriods[curChart]);
         if(hwnd[curChart]!=0) { Print("< - - - График ","ALL"+arrSymbols[curChart],arrPeriods[curChart]," найден! - - - >"); }
        }
      //---- и, если нашли, обновляем его
      if(hwnd[curChart]!=0) { PostMessageA(hwnd[curChart],WM_COMMAND,33324,0); }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   for(int curChart=0; curChart<ChartsCount; curChart++)
     {
      if(HistoryHandle[curChart]>=0)
        {
         //---- закрываем файл
         FileClose(HistoryHandle[curChart]);
         HistoryHandle[curChart]=-1;
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void WriteToFile(int handle,datetime t,double o,double l,double h,double c,long v)
  {
   MqlRates rate;

   rate.time = t;
   rate.open = o;
   rate.low  = l;
   rate.high = h;
   rate.close= c;
   rate.tick_volume=v;
   rate.spread=0;
   rate.real_volume=0;

   FileWriteStruct(handle,rate);
  }
//+------------------------------------------------------------------+
