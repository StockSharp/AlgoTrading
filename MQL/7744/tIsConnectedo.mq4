//+------------------------------------------------------------------+
//|                                                (IsConnected).mq4 |
//|                                      Copyright © 2005, komposter |
//|                                      mailto:komposterius@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, komposter"
#property link      "mailto:komposterius@mail.ru"

bool 		first					= true;
bool 		Now_IsConnected	= false;
bool 		Pre_IsConnected	= true;
datetime Connect_Start = 0, Connect_Stop = 0;

int init() { start(); return(0); }

int start()
{
	int handle = FileOpen( "_IsConnected.txt", FILE_WRITE | FILE_READ, " " );
	FileSeek( handle, 0, SEEK_END );
	FileWrite( handle, "- - - - - - - - - - - Expert initialized  - - - - - - - - - -" );

	while ( !IsStopped() )
	{
		Pre_IsConnected = Now_IsConnected;
		Now_IsConnected = IsConnected();
		
		if ( first ) { Pre_IsConnected = !Now_IsConnected; }
		
		if ( Now_IsConnected != Pre_IsConnected )
		{
			if ( Now_IsConnected )
			{
				Connect_Start = LocalTime();
				if ( !first )
				{
					FileSeek( handle, -55, SEEK_CUR );
					FileWrite( handle, "- - - OffLine- - -       " + TimeToStr( Connect_Stop, TIME_DATE ) + "       " + TimeToStr( Connect_Stop, TIME_SECONDS ) + " - " + TimeToStr( Connect_Start, TIME_SECONDS ) );
				}
				if ( IsStopped() ) { break; }
				FileWrite( handle, "+ + + OnLine + + +       " + TimeToStr( Connect_Start, TIME_DATE ) + "       " + TimeToStr( Connect_Start, TIME_SECONDS ) + " - " );
			}
			else
			{
				Connect_Stop = LocalTime();
				if ( !first )
				{
					FileSeek( handle, -55, SEEK_CUR );
					FileWrite( handle, "+ + + OnLine + + +       " + TimeToStr( Connect_Start, TIME_DATE ) + "       " + TimeToStr( Connect_Start, TIME_SECONDS ) + " - " + TimeToStr( Connect_Stop, TIME_SECONDS ) );
				}
				if ( IsStopped() ) { break; }
				FileWrite( handle, "- - - OffLine- - -       " + TimeToStr( Connect_Stop, TIME_DATE ) + "       " + TimeToStr( Connect_Stop, TIME_SECONDS ) + " - " );
			}		
		}

		first = false;
		FileFlush( handle );
		Sleep(1000);
	}

	if ( Now_IsConnected )
	{
		FileSeek( handle, -55, SEEK_CUR );
		FileWrite( handle, "+ + + OnLine + + +       " + TimeToStr( Connect_Start, TIME_DATE ) + "       " + TimeToStr( Connect_Start, TIME_SECONDS ) + " - " + TimeToStr( LocalTime(), TIME_SECONDS ) );
	}
	else
	{
		FileSeek( handle, -55, SEEK_CUR );
		FileWrite( handle, "- - - OffLine- - -       " + TimeToStr( Connect_Stop, TIME_DATE ) + "       " + TimeToStr( Connect_Stop, TIME_SECONDS ) + " - " + TimeToStr( LocalTime(), TIME_SECONDS ) );
	}		
	FileWrite( handle, "- - - - - - - - - - - Expert was stoped - - - - - - - - - - -\n" );
	FileClose( handle );
return(0);
}