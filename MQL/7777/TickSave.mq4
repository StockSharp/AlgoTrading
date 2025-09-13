//+------------------------------------------------------------------+
//|                                                     TickSave.mq4 |
//|                                      Copyright � 2006, komposter |
//|                                      mailto:komposterius@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, komposter"
#property link      "mailto:komposterius@mail.ru"

extern string	SymbolList		= "EURUSD,GBPUSD,AUDUSD,USDCAD,USDJPY,USDCHF";
extern bool		WriteWarnings	= false;

string SymbolsArray[32], strComment; double preBid[32];
int FileHandle[32], SymbolsCount = 0;

#define strEXPERT_WAS_STOPED  "--------------------------Expert was stoped"
#define strCONNECTION_LOST    "--------------------------Connection lost  "
#define strLEN						45

bool preIsConnected = true, nowIsConnected = true;

int init() { start(); return(0); }
int start()
{
	string ServerName = AccountServer();
	int CurYear = Year();
	int CurMonth = Month();

	// ��������� �� ������ SymbolList ������ ��������
	if ( !PrepareSymbolList() ) { return(-1); }

	// ��������� ��������� ����������
	if ( !ConnectionCheck() ) { return(-1); }

	// ��������� �����
	if ( !OpenFiles() ) { return(-1); }

	while ( !IsStopped() )
	{
		// ���� ��������� ������, ����� ���������� ������ - ��� ������ ������� ����� ������� ���� �����
		if ( ServerName != AccountServer()  ) { break; }

		// ���� ��������� �����, ����� ���������� ������ - ��� ������ ������ ����� ������ ���� ����
		if ( CurYear != Year() ) { break; }
		if ( CurMonth != Month() ) { break; }

		// ���� ������� �����,
		if ( !IsConnected() )
		{
			// ���������� �������������� �� ��� �����
			WriteConnectionLost();
			preIsConnected = false;
		}
		// ���� ����� ����,
		else
		{
			// ���������� ����������� ����
			WriteTick();
			preIsConnected = true;
		}
		Sleep(100);
	}

	// ��������� �����
	CloseFiles();

	Comment("");

	return(0);
}

//+------------------------------------------------------------------+
//| ��������� �� ������ SymbolList ������ ��������
//+------------------------------------------------------------------+
bool PrepareSymbolList()
{
	int		curchar = 0, len = StringLen( SymbolList ), curSymbol;
	string	cur_symbol = ""; SymbolsCount = 0;

	//---- ������������� ������������� ������ ������� ��������
	ArrayResize( SymbolsArray, 32 );

	//---- ���������� ��� ������� � ������ ��������
	for ( int pos = 0; pos <= len; pos ++ )
	{
		curchar = StringGetChar( SymbolList, pos );
		//---- ���� ������� ������ - �� ������� � �� ��������� ������ � ������,
		if ( curchar != ',' && pos != len )
		{
			//---- ��� - ���� �� �������� ����������� �������
			cur_symbol = cur_symbol + CharToStr( curchar );
			continue;
		}
		//---- ���� ������� ������ �������, ��� ��� - ��������� ������ � ������,
		else
		{ 
			//---- ������, � ���������� cur_symbol - ������ ��� �������. ��������� ���:
			MarketInfo( cur_symbol, MODE_BID );
			if ( GetLastError() == 4106 )
			{
				Alert( "����������� ������ ", cur_symbol, "!!!" );
				return(false);
			}

			//---- ���� ������ ������� ����������, ���������, ��� �� ������ ������ � ����� ������:
			bool Uniq = true;
			for ( curSymbol = 0; curSymbol < SymbolsCount; curSymbol ++ )
			{
				if ( cur_symbol == SymbolsArray[curSymbol] )
				{
					Uniq = false;
					break;
				}
			}

			//---- ���� ������ ������ � ����� ������ ���, ���������� ���, � ������� ����� ����������:
			if ( Uniq )
			{
				SymbolsArray[SymbolsCount] = cur_symbol;
				SymbolsCount ++;
				if ( SymbolsCount > 31 )
				{
					Alert( "������� ����� ��������! ������� ����� �������� 32 �����!" );
					return(false);
				}
			}

			//---- �������� �������� ����������
			cur_symbol = "";
		}
	}

	//---- ���� �� ���� ������ �� ��� ������, �������
	if ( SymbolsCount <= 0 )
	{
		Alert( "�� ���������� �� ������ �������!!!" );
		return(false);
	}
	
	//---- ������������� ������ ���� �������� ��� ���-�� ��������:
	ArrayResize		( SymbolsArray	, SymbolsCount );
	ArrayResize		( preBid			, SymbolsCount );
	ArrayInitialize( preBid			, -1 				);
	ArrayResize		( FileHandle	, SymbolsCount );
	ArrayInitialize( FileHandle	, -1 				);

	//---- ������� ����������:
	string uniq_symbols_list = SymbolsArray[0];
	for ( curSymbol = 1; curSymbol < SymbolsCount; curSymbol ++ )
	{
		if ( curSymbol == SymbolsCount - 1 )
		{ uniq_symbols_list = uniq_symbols_list + " � " + SymbolsArray[curSymbol]; }
		else
		{ uniq_symbols_list = uniq_symbols_list + ", " + SymbolsArray[curSymbol]; }
	}
	strComment = StringConcatenate( AccountServer(), ": �������������� ", SymbolsCount, " ������(-�,-��):\n", uniq_symbols_list, "\n" );
	Comment( strComment );

	return(true);
}

//+------------------------------------------------------------------+
//| ��������� ��������� ����������
//+------------------------------------------------------------------+
bool ConnectionCheck()
{
	while ( !IsConnected() )
	{
		Comment( AccountServer(), ": ��� ����� � ��������!!!" );
		if ( IsStopped() ) { return(false); }
		Sleep(100);
	}
	return(true);
}

//+------------------------------------------------------------------+
//| ��������� �����, � ������� ����� ���������� ����
//+------------------------------------------------------------------+
bool OpenFiles()
{
	int _GetLastError;
	for ( int curSymbol = 0; curSymbol < SymbolsCount; curSymbol ++ )
	{
		string FileName = StringConcatenate( "[Ticks]\\", AccountServer(), "\\", SymbolsArray[curSymbol], "_", Year(), ".", strMonth(), ".csv" );
		FileHandle[curSymbol] = FileOpen( FileName, FILE_READ | FILE_WRITE );

		if ( FileHandle[curSymbol] < 0 )
		{
			_GetLastError = GetLastError();
			Alert( "FileOpen( " + FileName + ", FILE_READ | FILE_WRITE ) - Error #", _GetLastError );
			return(false);
		}

		if ( !FileSeek( FileHandle[curSymbol], 0, SEEK_END ) )
		{
			_GetLastError = GetLastError();
			Alert( "FileSeek( " + FileHandle[curSymbol] + ", 0, SEEK_END ) - Error #", _GetLastError );
			return(false);
		}

		if ( WriteWarnings )
		{
			if ( FileWrite( FileHandle[curSymbol], strEXPERT_WAS_STOPED ) < 0 )
			{
				_GetLastError = GetLastError();
				Alert( "Ticks(" + Symbol() + ") - FileWrite() Error #", _GetLastError );
				return(false);
			}
			FileFlush( FileHandle[curSymbol] );
		}

		preBid[curSymbol] = MarketInfo( SymbolsArray[curSymbol], MODE_BID );
	}
	return(true);
}

//+------------------------------------------------------------------+
//| ���� ������� �����, ���������� �������������� �� ��� �����
//+------------------------------------------------------------------+
void WriteConnectionLost()
{
	int _GetLastError;

	if ( !preIsConnected ) { return(0); }
	
	Comment( strComment, "��� ����� � ��������!!!" );

	if ( !WriteWarnings ) { return(0); }

	for ( int curSymbol = 0; curSymbol < SymbolsCount; curSymbol ++ )
	{
		if ( FileHandle[curSymbol] < 0 ) { continue; }

		if ( !FileSeek( FileHandle[curSymbol], -strLEN, SEEK_END ) )
		{
			_GetLastError = GetLastError();
			Alert( "FileSeek( " + FileHandle[curSymbol] + ", -strLEN, SEEK_END ) - Error #", _GetLastError );
			continue;
		}

		if ( FileWrite( FileHandle[curSymbol], strCONNECTION_LOST ) < 0 )
		{
			_GetLastError = GetLastError();
			Alert( "FileWrite() Error #", _GetLastError );
		}

		if ( FileWrite( FileHandle[curSymbol], strEXPERT_WAS_STOPED ) < 0 )
		{
			_GetLastError = GetLastError();
			Alert( "FileWrite() Error #", _GetLastError );
		}

		FileFlush( FileHandle[curSymbol] );
	}
}

//+------------------------------------------------------------------+
//| ���������� ����������� ����
//+------------------------------------------------------------------+
void WriteTick()
{
	int _GetLastError; double curBid; int curDigits;
	Comment( strComment );
	for ( int curSymbol = 0; curSymbol < SymbolsCount; curSymbol ++ )
	{
		if ( FileHandle[curSymbol] < 0 ) { continue; }

		curBid = MarketInfo( SymbolsArray[curSymbol], MODE_BID );
		curDigits = MarketInfo( SymbolsArray[curSymbol], MODE_DIGITS );

		if ( 	NormalizeDouble( curBid - preBid[curSymbol], curDigits ) < 0.00000001 && 
				NormalizeDouble( preBid[curSymbol] - curBid, curDigits ) < 0.00000001 ) { continue; }

		preBid[curSymbol] = curBid;

		if ( WriteWarnings )
		{
			if ( !FileSeek( FileHandle[curSymbol], -strLEN, SEEK_END ) )
			{
				_GetLastError = GetLastError();
				Alert( "FileSeek( " + FileHandle[curSymbol] + ", -strLEN, SEEK_END ) - Error #", _GetLastError );
				continue;
			}
		}
		else
		{
			if ( !FileSeek( FileHandle[curSymbol], 0, SEEK_END ) )
			{
				_GetLastError = GetLastError();
				Alert( "FileSeek( " + FileHandle[curSymbol] + ", 0, SEEK_END ) - Error #", _GetLastError );
				continue;
			}
		}

		if ( FileWrite( FileHandle[curSymbol], TimeToStr( TimeCurrent(), TIME_DATE | TIME_SECONDS ), DoubleToStr( curBid, curDigits ) ) < 0 )
		{
			_GetLastError = GetLastError();
			Alert( "FileWrite() Error #", _GetLastError );
		}

		if ( WriteWarnings )
		{
			if ( FileWrite ( FileHandle[curSymbol], strEXPERT_WAS_STOPED ) < 0 )
			{
				_GetLastError = GetLastError();
				Alert( "FileWrite() Error #", _GetLastError );
			}
		}

		FileFlush( FileHandle[curSymbol] );
	}
}

//+------------------------------------------------------------------+
//| ��������� ��� �����
//+------------------------------------------------------------------+
void CloseFiles()
{
	for ( int curSymbol = 0; curSymbol < SymbolsCount; curSymbol ++ )
	{
		if ( FileHandle[curSymbol] > 0 )
		{
			FileClose( FileHandle[curSymbol] );
			FileHandle[curSymbol] = -1;
		}
	}
}

string strMonth()
{
	if ( Month() < 10 ) return( StringConcatenate( "0", Month() ) );
	return(Month());
}