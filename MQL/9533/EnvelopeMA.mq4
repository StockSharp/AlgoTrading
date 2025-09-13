//+------------------------------------------------------------------+
//|                                                   EnvelopeMA.mq4 |
//|                André Duarte de Novais, Cassio Jandir Pagnoncelli |
//|                                            www.inf.ufpr.br/cjp07 |
//+------------------------------------------------------------------+
#property copyright "André Duarte de Novais, Cassio Jandir Pagnoncelli"
#property link      "www.inf.ufpr.br/cjp07"

/*
   - Este é um SHORTER
   
   - Otimizações: TimeCurrent() % (Period()*60) == 0
   
   - Otimizações: Usar Fibonacci Levels (com segurança) para encerramento 
*/

//---- Parâmetros: início
extern string    _mo_start    = "";
extern string    _mo          = "Small Risk Management Module (developed by the authors)";
extern string    _mo_1        = ">> Closes all positions whenever equity/balance ratio is lower than";
extern double    LiquidaPercentual = 0.58;
extern string    _mo_2        = ">> Lot size (choose one method: 1, 2 or 3)";
extern string    _mo_2_1      = "1) Fixed [0=not fixed,x=size] {default method}";
extern double    LoteFixo     = 0.5;
extern string    _mo_2_2      = "2) lot size relative to % of free margin (Requirement: LoteFixo=0)";
extern double    MargemLivre  = 0;
//extern string    _mo_2_3      = "3) Counter-pips (min 1:100=100, 1:200=50, 1:400=25, 1:50=200)";
/*extern*/ int       ContraPips   = 1000;
extern string    _mo_end      = "";
//---- Parâmetros: fim

//---- Globais: início
double   MargemInicial = -1;
//---- Globais: fim


//---- Parâmetros: início
extern bool      AguardaCompletarCandle = false;
extern int       TP           = 25;
extern int       SL           = 25;
extern int       PerEnv       = 280;
extern double    sdEnv        = 0.08;
extern int       PerMM        = 6;
extern int       PerMMl       = 18;

#define MAGICNUM      100301

//---- Globais
int      HorarioUltimoCandle  = 0;
bool     Habilitado           = true; // fora de uso

//---- Situações do mercado
#define TENDENCIA_ALTA           1
#define TENDENCIA_ALTA_ENTRADA   2
#define TENDENCIA_ALTA_SAIDA     3
#define TENDENCIA_BAIXA         -1
#define TENDENCIA_BAIXA_ENTRADA -2
#define TENDENCIA_BAIXA_SAIDA   -3
#define CONGESTAO               97
#define SEM_MUDANCA_TENDENCIA   56


// Início
int init()
{
   return(0);
}

// Fim
int deinit()
{
   return(0);
}

// Loopback start
int start()
{
   // Aguarda completar o candle para tomar decisões
   if ( AguardaCompletarCandle )
      if ( TimeCurrent() - HorarioUltimoCandle < 60 * Period() )
         return (0);
      else
         HorarioUltimoCandle = TimeCurrent();
   
   if ( AccountEquity()/AccountBalance() < LiquidaPercentual )
      EncerraTodasAsPosicoes(OP_SELL);
   
   // Mostra acompanhamento da conta na tela
   ContaEmTela();
   
   // Administra S/L e T/P das posições abertas
   int i;
   for ( i=0; i<OrdersTotal(); i++ )
      if ( OrderSelect(i, SELECT_BY_POS, MODE_TRADES) && OrderCloseTime() == 0 )
         if ( OrderType() == OP_BUY || OrderType() == OP_SELL )
            AdministraEncerramentos(OrderTicket(), OrderType());
   
   // Estratégia de envelopes para short
   EnvelopeMMshort();
   
   return(0);
}

/*
   Estratégia Envelope com fechamentos em MM
*/
void EnvelopeMMshort()
{
   // Parâmetros
   double   Env_lo, Env_hi, MM, MM_l;
   Env_lo = iEnvelopes(Symbol(), PERIOD_M15, PerEnv, MODE_EMA, 0, PRICE_HIGH, sdEnv, MODE_LOWER, 0);
   Env_hi = iEnvelopes(Symbol(), PERIOD_M15, PerEnv, MODE_EMA, 0, PRICE_HIGH, sdEnv, MODE_UPPER, 0);
   MM     = iMA(Symbol(), PERIOD_M15, PerMM,  0, MODE_EMA, PRICE_LOW, 0);
   MM_l   = iMA(Symbol(), PERIOD_M15, PerMMl, 1, MODE_EMA, PRICE_LOW, 0);
   
   // abre venda à descoberto pendente
   if ( !PosicaoAberta(OP_SELL) && !PosicaoAberta(OP_SELLSTOP) ) 
      if (( MM > Env_lo && MM_l > Env_lo && Bid > Env_lo && MM <= Env_hi && MM_l <= Env_hi && Bid <= Env_hi ))
         OrderSend(Symbol(), OP_SELLSTOP, NormalizeDouble(Lote(), 2), NormalizeDouble(Env_lo, 4), 1, 
         SL_TP(OP_SELLSTOP, Env_lo, -SL), SL_TP(OP_SELLSTOP, Env_lo,  TP), 
         NULL, 0, TimeCurrent() + 60*(5*Period()-1));
}

/*
   Administra os pontos de T/P e S/L das ordens abertas com o cruzamento das MM
*/
void AdministraEncerramentos(int Ticket, int TipoOrdem)
{
   // Parâmetros
   double   Env_lo, Env_hi, MM, MM_l;
   Env_lo = iEnvelopes(Symbol(), PERIOD_M15, PerEnv, MODE_EMA, 0, PRICE_HIGH, sdEnv, MODE_LOWER, 0);
   Env_hi = iEnvelopes(Symbol(), PERIOD_M15, PerEnv, MODE_EMA, 0, PRICE_HIGH, sdEnv, MODE_UPPER, 0);
   MM     = iMA(Symbol(), PERIOD_M15, PerMM,  0, MODE_EMA, PRICE_LOW, 0);
   MM_l   = iMA(Symbol(), PERIOD_M15, PerMMl, 1, MODE_EMA, PRICE_LOW, 0);
   
   double   SAR_amarelo, SAR_verde, SAR_azul, Abertura;
   SAR_amarelo = iSAR(Symbol(), PERIOD_M15, 0.03 , 0.5, 0);
   SAR_verde   = iSAR(Symbol(), PERIOD_M15, 0.015, 0.6, 0);
   SAR_azul    = iSAR(Symbol(), PERIOD_M15, 0.02 , 0.2, 0);
   
   int candlesDist   = MathCeil((TimeCurrent() - OrderOpenTime())/(Period()*60));
   double minHigh;
   
   if ( !OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES) ) return;
   switch (TipoOrdem)
   {
      case OP_SELL:
         // fechamento com P-SAR e MM
         Abertura = OrderOpenPrice();
         if ((MM_l < Abertura && MM < Abertura && Ask < Abertura) &&
             (SAR_amarelo < Ask && SAR_verde < Ask && SAR_azul < Ask && MM > MM_l))
            EncerraTodasAsPosicoes(OP_SELL);
         
         // arruma o S/L
         minHigh = iLowest(Symbol(), PERIOD_M15, MODE_HIGH, candlesDist, 0);
         if ( candlesDist >= 4 && minHigh < Abertura - 3*Point && Bid < Env_lo
               &&  OrderStopLoss() != Env_lo )
            OrderModify(Ticket, Abertura, Env_lo, OrderTakeProfit(), 0);
      break;
   }
}

/*
   Devolve o tamanho do lote a ser negociado
*/
double Lote() {
   // calcula a margem inicial a partir da primeira ordem
   if ( MargemInicial == -1 )
      MargemInicial = AccountFreeMargin();
   
   // para lotes fixos
   if ( LoteFixo != 0 ) 
      return (LoteFixo);
   
   // para uso percentual da margem livre
   if ( MargemLivre != 0 )
      return (NormalizeDouble(AccountFreeMargin()*AccountLeverage()*MargemLivre/100000, 2));
   
   // uso máximo da margem de forma que um movimento de `ContraPips' pips tornem a conta sem liquidez (100% drawdown) 
   // respeitando um mínimo associado com a alavancagem da conta (1:100=100pips, 1:200=50pips, 1:400=25pips)
   if ( ContraPips != 0 ) {
      double margem = MathPow(2, -(MathLog(ContraPips/(100*100/AccountLeverage()))/MathLog(2)));
      double tam_lote = NormalizeDouble(AccountFreeMargin()*margem*AccountLeverage()/100000, 2);
      if ( tam_lote >= 0.01 )
         return (tam_lote);
   }
   
   return (0.01);
}

/*
   Calcula o preço de fechamento p/ imediato (preço de mercado) para ordem short/long
*/
double PrecoDeFechamentoAMercado(int TipoOrdem)
{
   switch (TipoOrdem)
   {
      case OP_BUY:
         return (NormalizeDouble(Bid, MarketInfo(Symbol(), MODE_DIGITS)));
      break;
      case OP_SELL:
         return (NormalizeDouble(Ask, MarketInfo(Symbol(), MODE_DIGITS)));
      break;
      default:
         return (0);
      break;
   }
}

/*
   Verifica as posições abertas
*/
bool PosicaoAberta(int Op)
{
   int i;
   for ( i=0; i<OrdersTotal(); i++ )
      if ( OrderSelect(i, SELECT_BY_POS, MODE_TRADES) && OrderCloseTime() == 0 )
         if ( OrderType() == Op )
            return (true);

   return (false);
}

/*
   Encerra todas as posições abertas
*/
void EncerraTodasAsPosicoes(int Op)
{
   int i;
   for ( i=0; i<OrdersTotal(); i++ )
      if ( OrderSelect(0, SELECT_BY_POS, MODE_TRADES) && OrderCloseTime() == 0 && Op == OrderType() )
         OrderClose(OrderTicket(), OrderLots(), PrecoDeFechamentoAMercado(OrderType()), 0);
}

/*
   Calcula T/P (S/L) a ser colocado na ordem
   Obs.: Para S/L use SL_TP(OP, -StopLossEmPips) e para T/P, SL_TP(OP, TakeProfitEmPips)
*/
double SL_TP(int TipoOrdem, double Preco, int Pips) 
{
   if ( Pips == 0 )
      return (0);
   
   switch (TipoOrdem)
   {
      case OP_BUYLIMIT:
      case OP_BUYSTOP:
      case OP_BUY:
         if ( Preco == 0 ) 
            Preco = Ask;
         return ( Preco + Pips*Point );
      break;
      case OP_SELLLIMIT:
      case OP_SELLSTOP:
      case OP_SELL:
         if ( Preco == 0 )
            Preco = Bid;
         return ( Preco - Pips*Point );
      break;
   }
}

/*
   Imprime dados sobre a conta em tela
*/
void ContaEmTela()
{
   Comment( "Lucro: ", AccountProfit(), 
            ", P/F: ", AccountEquity()/MargemInicial, 
            ", Liquidez: ", AccountEquity(),
            ", Liquidez/Balanço: ", AccountEquity()/AccountBalance());
}

