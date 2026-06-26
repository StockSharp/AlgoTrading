# Estratégia Fibonacci Retracement Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Fibonacci Retracement Momentum** é uma conversão do expert advisor original do MetaTrader "FIBONACCI.mq4" para a API de alto nível do StockSharp. A estratégia combina níveis de retração de Fibonacci multi-períodos com filtros de momentum e MACD para temporizar entradas de pullback na direção da tendência prevalecente. A lógica de negociação principal é executada no período base, enquanto os dados de confirmação são derivados de períodos de agregação superiores.

O algoritmo foi reescrito do zero usando expressões idiomáticas do StockSharp: assinaturas de candles, vínculos de indicadores e os helpers de gerenciamento de ordens integrados. A lógica de trailing da EA fonte foi simplificada para focar no comportamento central de ruptura de retração, enquanto preserva a estrutura de sinal original (toque de Fibonacci + impulso de momentum + filtro de tendência).

## Como funciona
1. **Período primário** — a estratégia assina os candles base selecionados (15 minutos por padrão) e calcula duas médias móveis ponderadas (rápida e lenta) para avaliar a direção local.
2. **Período de ancoragem Fibonacci** — o período superior (padrão: 1 hora) fornece o candle concluído mais recente. Seu máximo/mínimo é usado para construir a grade de retração de Fibonacci de 0%–100%. O mesmo fluxo de candles alimenta um indicador de momentum (retrospectiva 14) e o desvio absoluto do nível neutro 100 é armazenado para as últimas três barras.
3. **Período do filtro MACD** — um MACD de longo prazo (padrão: 12/26/9) é calculado em candles mensais (aproximação de 30 dias) e atua como filtro de confirmação de tendência.
4. Em cada candle base finalizado, o algoritmo verifica se o preço retrocedeu a qualquer nível de Fibonacci enquanto os fechamentos anteriores permaneceram no lado oposto desse nível. Combinado com alinhamento de médias móveis, impulso de momentum e confirmação MACD, uma operação é aberta.
5. As saídas protetoras dependem de distâncias de stop-loss e take-profit expressas em passos de preço. Se o preço se mover contra a posição ou alcançar o alvo, a posição é liquidada.

## Regras de entrada
### Configuração comprada
- O último candle do período superior define os níveis de Fibonacci; a mínima do candle base atual toca ou penetra qualquer nível enquanto pelo menos um dos três fechamentos anteriores permaneceu acima dele.
- A média móvel ponderada rápida está acima da média móvel ponderada lenta no período base.
- O desvio de momentum `|Momentum - 100|` no período superior excede o limiar configurado para qualquer um dos últimos três valores.
- A linha principal do MACD está acima da linha de sinal no período MACD.
- Verificação estrutural: a máxima do candle base anterior está acima da mínima de duas barras atrás (reflete `Low[2] < High[1]` da EA).

### Configuração vendida
- A máxima do candle base atual toca qualquer nível de Fibonacci enquanto pelo menos um dos últimos três fechamentos permaneceu abaixo dele.
- A média móvel ponderada rápida está abaixo da média móvel ponderada lenta.
- O desvio de momentum supera o limiar para qualquer uma das últimas três leituras.
- A linha principal do MACD está abaixo da linha de sinal no período MACD.
- Verificação estrutural: a máxima do candle anterior está acima da mínima da barra imediatamente anterior (análogo a `Low[1] < High[2]`).

### Gerenciamento de posição
- Se um sinal oposto aparecer enquanto há uma posição aberta, a estratégia primeiro fecha a posição existente e aguarda a próxima barra para iniciar a reversão. Isso reflete o tratamento conservador de ordens do código MQL original.

## Gestão de risco
- **Stop loss / Take profit** — configurado em múltiplos do passo de preço do instrumento. Zero desabilita a saída correspondente.
- **Rastreamento do preço de entrada** — o preço de preenchimento é aproximado pelo fechamento do candle de sinal e é usado para calcular as distâncias protetoras.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `FastMaLength` | 6 | Comprimento da média móvel ponderada rápida no período base. |
| `SlowMaLength` | 85 | Comprimento da média móvel ponderada lenta. |
| `MomentumLength` | 14 | Retrospectiva do momentum no período Fibonacci. |
| `MomentumThreshold` | 0.3 | Desvio absoluto mínimo de 100 necessário para validar o momentum. |
| `StopLossSteps` | 20 | Distância de stop-loss em passos de preço (0 desabilita). |
| `TakeProfitSteps` | 50 | Distância de take-profit em passos de preço (0 desabilita). |
| `MacdFastLength` | 12 | Comprimento da EMA rápida usada dentro do MACD. |
| `MacdSlowLength` | 26 | Comprimento da EMA lenta usada dentro do MACD. |
| `MacdSignalLength` | 9 | Comprimento da EMA de sinal usada dentro do MACD. |
| `CandleType` | Candles de 15 minutos | Período de execução primário. |
| `FibonacciCandleType` | Candles de 1 hora | Período que fornece âncoras de Fibonacci e momentum. |
| `MacdCandleType` | Candles de 30 dias | Período que fornece o filtro de tendência MACD. |

## Notas de uso
- Ajuste os parâmetros de período para corresponder ao mapeamento original da EA (ex., M5 → M30, M15 → H1). O StockSharp permite qualquer tipo de candle, incluindo barras de range ou tick.
- Como a estratégia usa `ClosePosition()` para liquidar, a propriedade `Volume` deve corresponder ao tamanho de operação desejado (padrão: equivalente a 1 lote).
- A conversão foca na lógica orientada por indicadores; os extras de gestão monetária da versão MQL (equity stop, trailing por saldo de conta, etc.) foram omitidos intencionalmente para maior clareza. Você pode estender a classe com proteção adicional conectando `ManageRisk`.
- Execute a estratégia dentro do StockSharp Designer, Shell ou Runner com os adaptadores de dados de mercado necessários configurados.
