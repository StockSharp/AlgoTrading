# Estratégia de 1H Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de 1H Bollinger Bands** adapta o especialista de MetaTrader "1H Bolinger Bands" para a API de alto nível do StockSharp. A ideia é negociar rebotes das Bollinger Bands diárias enquanto a tendência horária está alinhada e o MACD mensal de longo prazo confirma a direção. A estratégia funciona no período H1 (padrão) e depende de fluxos de dados de períodos superiores adicionais para confirmação.

## Lógica de trading
- **Filtro de tendência:** Duas médias móveis ponderadas lineares (LWMA 250 e 500) no período base garantem que apenas operações alinhadas com a direção dominante sejam permitidas.
- **Padrão de gatilho:** No período superior (diário por padrão), a estratégia monitora uma vela cuja mínima perfura abaixo da Banda de Bollinger inferior e a próxima vela abre novamente acima dela (inverso para posições vendidas com a banda superior). Isso replica a condição de rebote original.
- **Confirmação de momentum:** O Momentum (período 14) é calculado no período superior. Pelo menos um dos três desvios de momentum mais recentes de 100 deve exceder o limiar configurado (padrão 0.3).
- **Filtro MACD:** Um MACD mensal (12/26/9) deve concordar com o sinal. Para operações compradas, a linha MACD deve estar acima da linha de sinal; para vendidas, deve estar abaixo.
- **Entrada:** Quando todos os filtros se alinham, a estratégia abre uma ordem de mercado. Se houver uma posição oposta aberta, o volume solicitado neutraliza a exposição existente e inverte a direção.

## Gestão de posições
O gerenciamento de risco é implementado diretamente na estratégia usando distâncias baseadas em pips convertidas através de `Security.PriceStep`:
- **Stop Loss:** Fecha a posição quando o preço se move contra a entrada pelo número configurado de pips.
- **Take Profit:** Assegura lucros quando o preço atinge o alvo de pips configurado.
- **Trailing Stop (opcional):** Quando habilitado e o movimento exceder a distância de trailing, um nível de trailing interno segue o preço. Uma barra que penetra esse nível fecha a operação.
- **Break-Even (opcional):** Após o preço avançar pela distância de ativação, o nível de stop é movido para o preço de entrada mais o offset configurado (menos para vendidas). Um recuo a esse nível encerra a posição.

O gerenciamento de lucros baseado em dinheiro do especialista original não é recriado; a versão StockSharp foca em controles baseados em preço para permanecer agnóstica à bolsa.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Período base para avaliação de sinais. | Velas de 1 hora |
| `HigherTimeFrame` | Período usado para Bollinger Bands e momentum. | Velas de 1 dia |
| `MacdTimeFrame` | Período para o MACD de confirmação. | Velas de 30 dias |
| `FastMaPeriod` / `SlowMaPeriod` | Comprimentos de LWMA rápida/lenta no período base. | 6 / 85 |
| `TrendFastPeriod` / `TrendSlowPeriod` | Filtros de tendência LWMA de longo prazo. | 250 / 500 |
| `MomentumPeriod` | Lookback de momentum no período superior. | 14 |
| `MomentumThreshold` | Desvio absoluto mínimo de 100 para momentum. | 0.3 |
| `BollingerPeriod` / `BollingerWidth` | Configurações das Bollinger Bands diárias. | 20 / 2.0 |
| `TradeVolume` | Volume base para cada nova posição. | 1 |
| `StopLossPips` / `TakeProfitPips` | Stop de proteção e alvo em pips. | 20 / 50 |
| `EnableTrailing` / `TrailingStopPips` | Toggle de trailing stop e distância. | true / 40 |
| `EnableBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Toggle de break-even, distância de ativação e offset. | true / 30 / 30 |

Todos os parâmetros numéricos são expostos através de `StrategyParam<T>` e podem ser otimizados no Designer/Runner.

## Notas de implementação
- A estratégia assina simultaneamente três fluxos de velas: período base, período superior para Bollinger/Momentum e período MACD.
- O Momentum usa o indicador `Momentum` padrão do StockSharp e armazena os três últimos desvios para imitar a lógica MQL.
- O volume de trading e as distâncias em pips assumem que `Security.PriceStep` está corretamente preenchido; caso contrário, a lógica de proteção não será acionada.
- O StockSharp mantém uma única posição líquida. O comportamento de escalonamento "Max_Trades" do script original é simplificado para uma única posição agregada neste port.
- As saídas baseadas em equity e as características de trailing de dinheiro da versão MQL são intencionalmente omitidas para manter a implementação neutra em relação à bolsa.

## Uso
1. Anexe a estratégia a um instrumento que forneça velas horárias, diárias e mensais (ou ajuste os parâmetros adequadamente).
2. Certifique-se de que o instrumento expõe `PriceStep` para que as distâncias em pips se traduzam em offsets de preço.
3. Configure o volume e os parâmetros de risco desejados na UI ou no código antes de iniciar a estratégia.
4. Inicie a estratégia; ela assinará automaticamente os dados necessários, avaliará sinais em velas fechadas e gerenciará a posição com as regras de proteção configuradas.

## Diferenças conhecidas do especialista MQL
- O trailing baseado em dinheiro e o stop total de equity não estão implementados; apenas controles baseados em preço são mantidos.
- Alertas, e-mails e notificações push do código MQL são omitidos.
- O empilhamento de ordens é substituído pelo modelo de posição líquida única do StockSharp.

Esses ajustes mantêm a estratégia idiomática para o StockSharp enquanto preservam a ideia central de trading do especialista original.
