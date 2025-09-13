//+------------------------------------------------------------------+
//|                                              EqualVolumeBars.mq4 |
//|                                 Copyright © 2008-2015, komposter |
//|                                          http://www.komposter.me |
//+------------------------------------------------------------------+
#property copyright		"Copyright © 2008-2015, komposter"
#property link				"http://www.komposter.me"
#property version	 		"4.0"
#property strict

#include <WinUser32.mqh>

enum mode
{
	EqualVolumeBars = 0,
	RangeBars = 1
};

//---- Количество тиков в одном баре
input mode		WorkMode		= EqualVolumeBars;	// Chart mode
input int		TicksInBar	= 100;					// TicksInBar / PointsInBar
input	bool		FromM1		= true;					// Build chart from M1 data
input int		StartYear	= 2015;					// StartYear of ticks file (if M1 mode doesn't selected)
input int		StartMonth	= 03;						// StartMonth of ticks file (if M1 mode doesn't selected)

int		HistoryHandle	= -1, hwnd = 0, TicksFilePos = 0;
ulong		HistoryFilePos = 0;
datetime	time, now_time;
double	now_close, now_open, now_low, now_high;
long		now_volume;

int pre_time, last_fpos = 0;
double pre_close;

string	prefix = "";

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
{
	int    _GetLastError = 0, cnt_bars = 0;

	if ( WorkMode == EqualVolumeBars )
		prefix = "!Eqv";
	else
		prefix = "!Rng";

	// обнуляем хэндл окна off-line графика
   hwnd = 0;

	//---- Prepare history header
	int      file_version	= 401;
//	string   c_copyright		= "(C)opyright 2003, MetaQuotes Software Corp.";
	string   c_copyright		= "Copyright © 2008-2015, komposter";
	string   c_symbol			= StringConcatenate( prefix, _Symbol );
	int      i_period			= TicksInBar;
	int      i_digits			= Digits;
	int      i_unused[13];	ArrayInitialize(i_unused,0);

	//--- Open file
	HistoryHandle = FileOpenHistory( c_symbol + (string)i_period + ".hst", FILE_BIN | FILE_WRITE | FILE_SHARE_WRITE | FILE_SHARE_READ | FILE_ANSI );
	if ( HistoryHandle < 0 )
	{
		_GetLastError = GetLastError();
		Alert( "FileOpenHistory( \"", c_symbol + (string)i_period + ".hst", "\" )", " - Error #", _GetLastError );
		return(INIT_FAILED);
	}

	//--- Write history file header
	FileWriteInteger	( HistoryHandle, file_version,LONG_VALUE);
	FileWriteString	( HistoryHandle, c_copyright,64);
	FileWriteString	( HistoryHandle, c_symbol,12);
	FileWriteInteger	( HistoryHandle, i_period,LONG_VALUE);
	FileWriteInteger	( HistoryHandle, i_digits,LONG_VALUE);
	FileWriteInteger	( HistoryHandle, 0,LONG_VALUE);
	FileWriteInteger	( HistoryHandle, 0,LONG_VALUE);
	FileWriteArray		( HistoryHandle, i_unused,0,13);
	
	FileFlush			( HistoryHandle);

	//+------------------------------------------------------------------+
	//| Обрабатываем историю
	//+------------------------------------------------------------------+
	int year = StartYear, month = StartMonth;
	int cur_date = year*100+month, end_date = Year()*100+Month();

	double bid; string tmp_str1, tmp_str2;
	now_time = 60; now_close = 0; now_open = 0; now_low = 0; now_high = 0; now_volume = 0;

	if ( !FromM1 )
	{
		while ( cur_date <= end_date )
		{
			string ticks_file_name = StringConcatenate( "[Ticks]\\", AccountServer(), "\\", Symbol(), "_", year, ".", strMonth( month ), ".csv" );
			int ticks_file_handle = FileOpen( ticks_file_name, FILE_READ | FILE_CSV | FILE_ANSI | FILE_SHARE_READ );
	
			//---- Если возникла ошибка
			if ( ticks_file_handle <= 0 )
			{
				Alert( "Ошибка при открытии файла \"", ticks_file_name, "\" #", GetLastError(), "!" );
			}
			else
			{
				FileSeek( ticks_file_handle, 0, SEEK_SET );

				while ( !FileIsEnding( ticks_file_handle ) )
				{
					if ( GetLastError() == 4099 ) break;
	
					tmp_str1 = FileReadString( ticks_file_handle );
					if ( StringLen( tmp_str1 ) < 19 ) continue;
	
					tmp_str2 = FileReadString( ticks_file_handle );
	
					time = StrToTime  ( tmp_str1 );
					bid  = StrToDouble( tmp_str2 );

					if ( time < 1 || bid < _Point/2.0 ) continue;

					// сформировался бар или первый бар
					if ( IsNewBar() || now_volume < 1 )
					{
						if ( IsNewBar() )
						{
	               	WriteToFile( HistoryHandle, now_time, now_open, now_low, now_high, now_close, now_volume );
							cnt_bars ++;
						}
	
						// Нормализуем время до целой минуты
						time			= time / 60;
						time			*= 60;
	
						// Проверяем, чтоб не получилось 2 бара с одним временем
						if ( time <= now_time ) time = now_time + 60;
	
						now_time		= time;
						now_open		= bid;
						now_low		= bid;
						now_high		= bid;
						now_close	= bid;
						now_volume	= 1;
					}
					else
					{
						if ( bid < now_low  ) now_low  = bid;
						if ( bid > now_high ) now_high = bid;
						now_close = bid;
						now_volume ++;
					}
				}
	
				//---- Закрываем файл
				FileClose( ticks_file_handle );
				GetLastError();
			}
	
			month ++;
			if ( month > 12 )
			{
				month = 1;
				year ++;
			}
			cur_date = year*100+month;
		}
	}
	// FromM1
	else
	{
		for ( int i = iBars( _Symbol, PERIOD_M1 )-1; i >= 0; i -- )
		{
			// сформировался бар или первый бар
			if ( IsNewBar() || now_volume < 1 )
			{
				if ( IsNewBar() )
				{
            	WriteToFile( HistoryHandle, now_time, now_open, now_low, now_high, now_close, now_volume );
					cnt_bars ++;
				}

				// Нормализуем время до целой минуты
				time			= iTime( _Symbol, PERIOD_M1, i ) / 60;
				time			*= 60;

				// Проверяем, чтоб не получилось 2 бара с одним временем
				if ( time <= now_time ) time = now_time + 60;

				now_time		= time;
				now_open		= iOpen	( _Symbol, PERIOD_M1, i );
				now_low		= iLow	( _Symbol, PERIOD_M1, i );
				now_high		= iHigh	( _Symbol, PERIOD_M1, i );
				now_close	= iClose	( _Symbol, PERIOD_M1, i );
				now_volume	= iVolume( _Symbol, PERIOD_M1, i );
			}
			else
			{
				if ( iLow	( _Symbol, PERIOD_M1, i ) < now_low  ) now_low  = iLow	( _Symbol, PERIOD_M1, i );
				if ( iHigh	( _Symbol, PERIOD_M1, i ) > now_high ) now_high = iHigh	( _Symbol, PERIOD_M1, i );
				now_close = iClose( _Symbol, PERIOD_M1, i );
				now_volume += iVolume( _Symbol, PERIOD_M1, i );
			}
		}
	}

	// запоминаем место в файле, перед записью 0-го бара
	HistoryFilePos = FileTell( HistoryHandle);

	// записываем 0-й бар для отобажения на графике
	WriteToFile( HistoryHandle, now_time, now_open, now_low, now_high, now_close, now_volume );
	FileFlush( HistoryHandle );

	// выводим статистику
	Print( "< - - - ", cnt_bars, " bars writed - - - >" );
	Print( "< - - - Open \"", StringConcatenate( prefix, _Symbol ), ", M", TicksInBar, "\" chart to view results- - - >" );

	// обновляем график
	RefreshWindow();

	return(INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
	if ( HistoryHandle > 0 )
	{
		//---- закрываем файл
		FileClose( HistoryHandle );
		HistoryHandle = -1;
	}
}

void OnTick()
{
	if ( HistoryHandle < 0 ) return;

	//+------------------------------------------------------------------+
	//| Обрабатываем поступающие тики
	//+------------------------------------------------------------------+
	//---- ставим "курсор" перед последним баром
	FileSeek( HistoryHandle, HistoryFilePos, SEEK_SET );

	now_volume ++;

	// бар продолжается
	if ( IsOldBar() )
	{
		if ( Bid < now_low  ) now_low  = Bid;
		if ( Bid > now_high ) now_high = Bid;
		now_close = Bid;

		// записываем 0-й бар для отобажения на графике
   	WriteToFile( HistoryHandle, now_time, now_open, now_low, now_high, now_close, now_volume );
		FileFlush( HistoryHandle );
	}
	// пришел тик нового бара
	else
	{
		// записываем 1-й бар
   	WriteToFile( HistoryHandle, now_time, now_open, now_low, now_high, now_close, now_volume-1 );

		// Нормализуем время до целой минуты
		time			= iTime( _Symbol, PERIOD_M1, 0 ) / 60;
		time			*= 60;

		// Проверяем, чтоб не получилось 2 бара с одним временем
		if ( time <= now_time ) time = now_time + 60;

		now_time		= time;
		now_open		= Bid;
		now_low		= Bid;
		now_high		= Bid;
		now_close	= Bid;
		now_volume	= 1;

		// запоминаем место в файле, перед записью 0-го бара
		HistoryFilePos = FileTell( HistoryHandle);

		// записываем 0-й бар
   	WriteToFile( HistoryHandle, now_time, now_open, now_low, now_high, now_close, now_volume );
		FileFlush( HistoryHandle );
	}

	// обновляем график
	RefreshWindow();
}

void RefreshWindow()
{
	//---- находим окно, в которое будем "отправлять" свежие котировки
	if ( hwnd == 0 )
	{
		hwnd = WindowHandle( StringConcatenate( prefix, _Symbol ), TicksInBar );
		if ( hwnd != 0 ) { Print( "< - - - \"", StringConcatenate( prefix, _Symbol ), ", M", TicksInBar, "\" detected! - - - >" ); }
	}
	//---- и, если нашли, обновляем его
	if ( hwnd != 0 ) { PostMessageA( hwnd, WM_COMMAND, 33324, 0 ); }
}

string strMonth( int m )
{
	if ( m < 10 ) return( StringConcatenate( "0", m ) );
	return( (string)m );
}

void WriteToFile( int handle, datetime t, double o, double l, double h, double c, long v )
{
	MqlRates rate;
	
	rate.time = t;
	rate.open = o;
	rate.low  = l;
	rate.high = h;
	rate.close = c;
	rate.tick_volume = v;
   rate.spread = 0;
   rate.real_volume = 0;

	FileWriteStruct( handle, rate );
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsNewBar()
{
	if ( WorkMode == EqualVolumeBars )
	{
		if ( now_volume >= TicksInBar ) return(true);
	}
	else
	{
		if ( (now_high-now_low)/_Point >= TicksInBar ) return(true);
	}

	return(false);
}

bool IsOldBar()
{
	if ( WorkMode == EqualVolumeBars )
	{
		if ( now_volume <= TicksInBar ) return(true);
	}
	else
	{
		if ( (now_high-now_low)/_Point <= TicksInBar ) return(true);
	}

	return(false);
}

