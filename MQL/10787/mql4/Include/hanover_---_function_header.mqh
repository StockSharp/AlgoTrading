//+------------------------------------------------------------------+
//|                                  hanover --- function header.mqh |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| defines                                                          |
//+------------------------------------------------------------------+
// #define MacrosHello   "Hello, world!"
// #define MacrosYear    2005

//+------------------------------------------------------------------+
//| DLL imports                                                      |
//+------------------------------------------------------------------+
// #import "user32.dll"
//   int      SendMessageA(int hWnd,int Msg,int wParam,int lParam);

// #import "my_expert.dll"
//   int      ExpertRecalculate(int wParam,int lParam);
// #import

//+------------------------------------------------------------------+
//| EX4 imports                                                      |
//+------------------------------------------------------------------+
// #import "stdlib.ex4"
//   string ErrorDescription(int error_code);
// #import
//+------------------------------------------------------------------+

#include <WinUser32.mqh>

#import "kernel32.dll"
// "kernel32.dll" is required by log() function
// requires use of Microsoft DebugView app (debugging alternative for MQL4)
// see http://www.forexfactory.com/showthread.php?t=245303 thread for more info
  void OutputDebugStringA(string msg);
#import

#import "shell32.dll"
// call another executable from within MQL4
  int ShellExecuteA(int hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);
#import

#import "wininet.dll"
// "wininet.dll" is required by ReadWebPage() function
#define INTERNET_FLAG_PRAGMA_NOCACHE    0x00000100 // Forces the request to be resolved by the origin server, even if a cached copy exists on the proxy.
#define INTERNET_FLAG_NO_CACHE_WRITE    0x04000000 // Does not add the returned entity to the cache. 
#define INTERNET_FLAG_RELOAD            0x80000000 // Forces a download of the requested file, object, or directory listing from the origin server, not from the cache.
  int InternetAttemptConnect (int x);
  int InternetOpenA(string sAgent, int lAccessType, string sProxyName = "", string sProxyBypass = "", int lFlags = 0);
  int InternetOpenUrlA(int hInternetSession, string sUrl, string sHeaders = "", int lHeadersLength = 0, int lFlags = 0, int lContext = 0);
  int InternetReadFile(int hFile, int& sBuffer[], int lNumBytesToRead, int& lNumberOfBytesRead[]);
  int InternetCloseHandle(int hInet);
#import

//===========================================================================
//                            FUNCTIONS LIBRARY
//===========================================================================

/*
AppendIfMissing()             appends a character to a string if it's not already the rightmost character
AppendIfNotNull()             appends a character to a string if the string is not null
BarConvert()                  converts a bar# (candle#) to the equivalent bar# on another timeframe
BaseToNumber()                performs multibase arithmetic: converts a non-base 10 number (string) to a base 10 integer
BoolToStr()                   converts a boolean value to "true" or "false"
DateToStr()                   formats a MT4 date/time value to a string, using a very sophisticated format mask
DatesToStr()                  performs multiple DateToStr() operations in a single function
DebugDoubleArray()            unloads a set of values from a double array, prefixed by the element number, to a single string
DebugIntegerArray()           unloads a set of values from an int array, prefixed by the element number, to a single string
DebugStringArray()            unloads a set of values from a string array, prefixed by the element number, to a single string
DivZero()                     returns 0 instead of 'divide by zero' error, if denominator evaluates to 0
DoubleArrayToStr()            unloads a set of values from a double array to a single string, inserting a specified delimiter between the values
EasterDay()                   returns the MT4time value of Easter Sunday for the given year
ExpandCcy()                   expands a curency symbol name, e.g. "EJ" to "EURJPY"
ExtractAlpha()                returns alphabetic (or other) characters in a string
ExtractUnique()               returns 1 occurrence only of each char in a string; result may be optionally sorted asc/desc
FileSort()                    shell sorts an ASCII text file, rewriting the file with its records in alphanumeric order
GetHash()                     calculate and return checksum of a string
GetVisibility()               returns suitable OBJPROP_TIMEFRAMES value from a timeframes string (e.g. "M1,M5,M15")
IntegerArrayToStr()           unloads a set of values from an int array to a single string, inserting a specified delimiter between the values
ListGlobals()                 lists all GlobalVariables and their values, to a string
ListOrders()                  lists all orders of your chosen stati (open/pending/closed/deleted) to a string
LookupDoubleArray()           looks up a numeric value in a double array, returning the element number (if found)
LookupIntegerArray()          looks up a numeric value in an int array, returning the element number (if found)
LookupStringArray()           looks up a string value in a string array, returning the element number (if found)
MathFactorial()               calculates a factorial n(n-1)(n-2)...1 using a recursive technique
MathFix()                     returns the value of N, rounded to D decimal places (fixes precision bug in MQL4 MathRound) 
MathInt()                     returns the value of N, rounded DOWN to D decimal places (fixes precision bug in MQL4 MathFloor)
MathSign()                    returns the sign (-1,0,+1) of a number
NumberToBase()                performs multibase arithmetic: converts a base 10 integer to a non-base 10 number (string)
NumberToStr()                 formats a numeric (int/double) value to a string, using a very sophisticated format mask
NumbersToStr()                performs multiple NumberToStr() operations in a single function
OrderStatus()                 given a ticket number, returns the order status (O=open, P=pending, C=closed, D=deleted, U=unknown)
ReadWebPage()                 reads a page from a specified URL into a string
ReduceCcy()                   reduces a currency symbol name, e.g. "EURJPY" to "EJ"
ReturnDay()                   returns the MT4time value of the (e.g.) 3rd Sunday after 14 Feb 2011
ShellsortDoubleArray()        shell sorts an array of double values into ascending or descending sequence
ShellsortIntegerArray()       shell sorts an array of int values into ascending or descending sequence
ShellsortString()             shell sorts the characters in a string into ascending or descending ASCII sequence
ShellsortStringArray()        shell sorts an array of string values into ascending or descending ASCII sequence
StrToBool()                   converts a suitable string (T(rue)/t(rue)/F(alse)/f(alse)/1) to a boolean value
StrToChar()                   returns the decimal ASCII value of a 1 byte string (inverse of MQL4's CharToStr())
StrToColor()                  converts a string (color name, RGB values, etc to a MQL4 color
StrToDate()                   converts a number of different string patterns to a MT4 date/time value
StrToDoubleArray()            loads a double array from a delimiter-separated set of string values (e.g. "1,2,3"); returns the number of array elements loaded
StrToIntegerArray()           loads an int array from a delimiter-separated set of string values (e.g. "1,2,3"); returns the number of array elements loaded
StrToNumber()                 strips all non-numeric characters from a string, returning a numeric (int/double) value
StrToStr()                    left/right/center aligns, or truncates, a string, using a very sophisticated format mask
StrToStringArray()            loads a string array from a delimiter-separated set of string values (e.g. "1,2,3"); returns the number of array elements loaded
StrToTF()                     converts a timeframe string to a number (e.g. "M15" to 15)
StringArrayToStr()            unloads a set of values from an string array to a single string, inserting a specified delimiter between the values
StringDecrypt()               unencrypts a string that was previously encrypted using StringEncrypt()
StringEncrypt()               encrypts a string
StringFindCount()             returns the number of occurrences of a certain substring in a string
StringInsert()                inserts characters into a given position in a string
StringLeft()                  returns the leftmost characters, or all but the N rightmost characters, of a string
StringLeftExtract()           extracts N characters from a string, counting from the left
StringLeftPad()               inserts specified padding characters at the beginning of a string
StringLeftTrim()              removes all leading spaces from a string
StringLower()                 converts all alphabetic characters in a string to lowercase
StringOverwrite()             overwrites characters in a given position of a string
StringRepeat()                returns a given string, repeated N times
StringReplace()               replaces substring in a string with another substring
StringReverse()               reverses a string, e.g. "ABCDE" becomes "EDCBA"
StringRight()                 returns the rightmost characters, or all but the N leftmost characters, of a string
StringRightExtract()          extracts N characters from a string, counting from the right
StringRightPad()              appends specified padding characters to the end of a string
StringRightTrim()             removes all trailing spaces from a string
StringTranslate()             translates characters in a string, given a full translation table
StringTrim()                  removes all (leading, trailing and embedded) spaces from a string
StringUpper()                 converts all alphabetic characters in a string to uppercase
StrsToStr()                   performs multiple StrToStr() operations in a single function
TFToStr()                     converts a number to a timeframe string (e.g.  1440 to "D1")
YMDtoDate()                   converts 3 integers (year, month and day) to a MT4 date/time value
d()                           outputs up to 8 values to the file /EXPERTS/FILES/DEBUG.TXT, appending data to the end of the file
dd()                          outputs up to 8 values to the file /EXPERTS/FILES/DEBUG.TXT, creating a new file
err_msg()                     returns a full description of an error, given its error code number
log()                         outputs up to 8 values for viewing using Microsoft's DebugView facility
*/