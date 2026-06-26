# Estratégia JS Signal Baes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port do StockSharp do expert advisor do MetaTrader "JS Signal Baes". Ela avalia seis períodos diferentes simultaneamente (M1, M5, M15, M30, H1, H4 por padrão) e aguarda até que todos os indicadores monitorados concordem na mesma direção de mercado antes de abrir uma posição. Os sinais podem ser invertidos através do parâmetro **Reverse** para usuários que querem negociar contra a tendência detectada.

## Indicadores e confirmações
Os seguintes indicadores são calculados em cada um dos seis períodos:

- **Duas Médias Móveis** usando o método de suavização selecionado (simples, exponencial, suavizado ou ponderado linearmente).
- **MACD (Moving Average Convergence Divergence)** usando comprimentos configuráveis de rápido, lento e sinal.
- **RSI (Relative Strength Index)** com um parâmetro de período dedicado.
- **CCI (Commodity Channel Index)** com seu próprio comprimento de lookback.
- **Oscilador Stochastic** definido por períodos K, D e suavização.

Um período é considerado **altista** quando:

1. MA Rápida > MA Lenta.
2. Linha principal MACD > Linha de sinal MACD.
3. RSI > 50.
4. CCI > 0.
5. Stochastic %K > 40.

Um período é considerado **baixista** quando:

1. MA Rápida < MA Lenta.
2. Linha principal MACD < Linha de sinal MACD.
3. RSI < 50.
4. CCI < 0.
5. Stochastic %K < 60.

## Regras de negociação
Uma nova posição líquida é aberta apenas quando o período primário (padrão M1) fecha e **todos os seis períodos** são simultaneamente altistas ou baixistas:

- **Entrada longa:** todo período é altista. Se *Reverse* estiver habilitado, o sinal se torna uma entrada curta.
- **Entrada curta:** todo período é baixista. Se *Reverse* estiver habilitado, o sinal se torna uma entrada longa.

As posições não são piramidadas. A estratégia aguarda até que a posição existente seja fechada externamente antes de agir em um novo sinal. Não há saídas automáticas além da lógica de sinal oposto do expert advisor original.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| CciPeriod | 13 | Comprimento de lookback para o Commodity Channel Index. |
| FastMaPeriod | 5 | Comprimento da média móvel rápida. |
| SlowMaPeriod | 9 | Comprimento da média móvel lenta. |
| MaMethod | LinearWeighted | Tipo de suavização de média móvel aplicado a ambas as médias. |
| MacdFastPeriod | 8 | Comprimento EMA rápido usado pelo MACD. |
| MacdSlowPeriod | 17 | Comprimento EMA lento usado pelo MACD. |
| MacdSignalPeriod | 9 | Comprimento da linha de sinal usado pelo MACD. |
| StochasticKPeriod | 5 | Período K para o oscilador stochastic. |
| StochasticDPeriod | 3 | Período D para o oscilador stochastic. |
| StochasticSmoothing | 3 | Fator de suavização para o oscilador stochastic. |
| RsiPeriod | 9 | Comprimento de lookback do RSI. |
| ReverseSignals | false | Inverter a direção de cada sinal de negociação. |
| TimeFrame1..6 | M1, M5, M15, M30, H1, H4 | Séries de velas atribuídas a cada período. |

## Notas
- Os parâmetros padrão replicam a configuração embutida na versão do MetaTrader.
- Gestão monetária, stop-loss, take-profit e lógica de trailing do código original não são reproduzidos; use controles de risco no nível do portfólio se necessário.
- Certifique-se de que dados históricos estejam disponíveis para cada período selecionado para que os indicadores possam se aquecer antes de negociar.
