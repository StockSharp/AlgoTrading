//+------------------------------------------------------------------+
//|                                                 StringFormat.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "='", (A), "'")

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // formats
   Print("Format types");
   PRT(StringFormat("Plain string", 0));
   PRT(StringFormat("[Infinity Sign] Unicode: %c; ANSI: %C", '∞', '∞'));
   PRT(StringFormat("short (ok): %hi, short (overflow): %hi", SHORT_MAX, INT_MAX));
   PRT(StringFormat("int (ok): %i, int (overflow): %i", INT_MAX, LONG_MAX));
   PRT(StringFormat("long (ok): %lli, long (overflow): %i", LONG_MAX, LONG_MAX));
   PRT(StringFormat("ulong (ok): %llu, long signed (overflow): %lli", ULONG_MAX, ULONG_MAX));
   PRT(StringFormat("ulong (ok): %I64u", ULONG_MAX));
   PRT(StringFormat("ulong (HEX): %I64X, ulong (hex): %I64x", 1234567890123456, 1234567890123456));
   PRT(StringFormat("double PI: %f", M_PI));
   PRT(StringFormat("double PI: %e", M_PI));
   PRT(StringFormat("double PI: %g", M_PI));
   PRT(StringFormat("double PI: %a", M_PI));
   PRT(StringFormat("string: %s", "ABCDEFGHIJ"));
   PRT(StringFormat("string: %.7s", "ABCDEFGHIJ"));

   // width/precision/alignment
   Print("Right align (default)");
   PRT(StringFormat("space padding: %10i", SHORT_MAX));
   PRT(StringFormat("0-padding: %010i", SHORT_MAX));
   PRT(StringFormat("with sign: %+10i", SHORT_MAX));
   PRT(StringFormat("precision: %.10i", SHORT_MAX));
   
   Print("Sign placement");
   PRT(StringFormat("default: %i", SHORT_MAX));
   PRT(StringFormat("default: %i", SHORT_MIN));
   PRT(StringFormat("space  : % i", SHORT_MAX));
   PRT(StringFormat("space  : % i", SHORT_MIN));
   PRT(StringFormat("sign   : %+i", SHORT_MAX));
   PRT(StringFormat("sign   : %+i", SHORT_MIN));
   
   Print("Left align, space padding on the right");
   PRT(StringFormat("no sign (default): %-10i", SHORT_MAX));
   PRT(StringFormat("with sign: %+-10i", SHORT_MAX));

   Print("Fixed width & precision, space padding");
   PRT(StringFormat("double PI: %15.10f", M_PI));
   PRT(StringFormat("double PI: %15.10e", M_PI));
   PRT(StringFormat("double PI: %15.10g", M_PI));
   PRT(StringFormat("double PI: %15.10a", M_PI));
   
   Print("Fixed width (default precision = 6), space padding");
   PRT(StringFormat("double PI: %15f", M_PI));
   PRT(StringFormat("double PI: %15e", M_PI));
   PRT(StringFormat("double PI: %15g", M_PI));
   PRT(StringFormat("double PI: %15a", M_PI));
   
   Print("Fixed precision only, no padding");
   PRT(StringFormat("double PI: %.10f", M_PI));
   PRT(StringFormat("double PI: %.10e", M_PI));
   PRT(StringFormat("double PI: %.10g", M_PI));
   PRT(StringFormat("double PI: %.10a", M_PI));

   Print("Controlled width/precision with *");
   PRT(StringFormat("double PI: %*.*f", 12, 5, M_PI));
   PRT(StringFormat("string: %*s", 15, "ABCDEFGHIJ"));
   PRT(StringFormat("string: %-*s", 15, "ABCDEFGHIJ"));

   Print("Errors");
   // excessive specificators %d %f %s (no arguments for them)
   PRT(StringFormat("string: %s %d %f %s", "ABCDEFGHIJ"));
   // inconsistent types of specificator and argument
   PRT(StringFormat("string vs int: %d", "ABCDEFGHIJ"));
   PRT(StringFormat("double vs int: %d", M_PI));
   PRT(StringFormat("string vs double: %s", M_PI));
   
   /*
   
      output:
      
   Format types
   StringFormat(Plain string,0)='Plain string'
   StringFormat([Infinity Sign] Unicode: %c; ANSI: %C,'∞','∞')='[Infinity Sign] Unicode (ok): ∞; ANSI (overflow):  '
   StringFormat(short (ok): %hi, short (overflow): %hi,SHORT_MAX,INT_MAX)='short (ok): 32767, short (overflow): -1'
   StringFormat(int (ok): %i, int (overflow): %i,INT_MAX,LONG_MAX)='int (ok): 2147483647, int (overflow): -1'
   StringFormat(long (ok): %lli, long (overflow): %i,LONG_MAX,LONG_MAX)='long (ok): 9223372036854775807, long (overflow): -1'
   StringFormat(ulong (ok): %llu, long signed (overflow): %lli,ULONG_MAX,ULONG_MAX)='ulong (ok): 18446744073709551615, long signed (overflow): -1'
   StringFormat(ulong (ok): %I64u,ULONG_MAX)='ulong (ok): 18446744073709551615'
   StringFormat(ulong (HEX): %I64X, ulong (hex): %I64x,1234567890123456,1234567890123456)='ulong (HEX): 462D53C8ABAC0, ulong (hex): 462d53c8abac0'
   StringFormat(double PI: %f,M_PI)='double PI: 3.141593'
   StringFormat(double PI: %e,M_PI)='double PI: 3.141593e+00'
   StringFormat(double PI: %g,M_PI)='double PI: 3.14159'
   StringFormat(double PI: %a,M_PI)='double PI: 0x1.921fb54442d18p+1'
   StringFormat(string: %s,ABCDEFGHIJ)='string: ABCDEFGHIJ'
   StringFormat(string: %.7s,ABCDEFGHIJ)='string: ABCDEFG'
   Right align (default)
   StringFormat(space padding: %10i,SHORT_MAX)='space padding:      32767'
   StringFormat(0-padding: %010i,SHORT_MAX)='0-padding: 0000032767'
   StringFormat(with sign: %+10i,SHORT_MAX)='with sign:     +32767'
   StringFormat(precision: %.10i,SHORT_MAX)='precision: 0000032767'
   Sign placement
   StringFormat(default: %i,SHORT_MAX)='default: 32767'
   StringFormat(default: %i,SHORT_MIN)='default: -32768'
   StringFormat(space  : % i,SHORT_MAX)='space  :  32767'
   StringFormat(space  : % i,SHORT_MIN)='space  : -32768'
   StringFormat(sign   : %+i,SHORT_MAX)='sign   : +32767'
   StringFormat(sign   : %+i,SHORT_MIN)='sign   : -32768'
   Left align, space padding on the right
   StringFormat(no sign (default): %-10i,SHORT_MAX)='no sign (default): 32767     '
   StringFormat(with sign: %+-10i,SHORT_MAX)='with sign: +32767    '
   Fixed width & precision, space padding
   StringFormat(double PI: %15.10f,M_PI)='double PI:    3.1415926536'
   StringFormat(double PI: %15.10e,M_PI)='double PI: 3.1415926536e+00'
   StringFormat(double PI: %15.10g,M_PI)='double PI:     3.141592654'
   StringFormat(double PI: %15.10a,M_PI)='double PI: 0x1.921fb54443p+1'
   Fixed width (default precision = 6), space padding
   StringFormat(double PI: %15f,M_PI)='double PI:        3.141593'
   StringFormat(double PI: %15e,M_PI)='double PI:    3.141593e+00'
   StringFormat(double PI: %15g,M_PI)='double PI:         3.14159'
   StringFormat(double PI: %15a,M_PI)='double PI: 0x1.921fb54442d18p+1'
   Fixed precision only, no padding
   StringFormat(double PI: %.10f,M_PI)='double PI: 3.1415926536'
   StringFormat(double PI: %.10e,M_PI)='double PI: 3.1415926536e+00'
   StringFormat(double PI: %.10g,M_PI)='double PI: 3.141592654'
   StringFormat(double PI: %.10a,M_PI)='double PI: 0x1.921fb54443p+1'
   Controlled width/precision with *
   StringFormat(double PI: %*.*f,12,5,M_PI)='double PI:      3.14159'
   StringFormat(string: %*s,15,ABCDEFGHIJ)='string:      ABCDEFGHIJ'
   StringFormat(string: %-*s,15,ABCDEFGHIJ)='string: ABCDEFGHIJ     '
   Errors
   StringFormat(string: %s %d %f %s,ABCDEFGHIJ)='string: ABCDEFGHIJ 0 0.000000 (missed string parameter)'
   StringFormat(string vs int: %d,ABCDEFGHIJ)='string vs int: 0'
   StringFormat(double vs int: %d,M_PI)='double vs int: 1413754136'
   StringFormat(string vs double: %s,M_PI)='string vs double: (non-string passed)'
   
   */
   
}
//+------------------------------------------------------------------+