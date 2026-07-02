# Manhã Noite Stochastic Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia transfere o consultor especialista MetaTrader 5 **Expert_AMS_ES_Stoch** (Morning/Evening Star com Stochastic confirmação) para StockSharp. The implementation keeps the original candlestick pattern recognition and stochastic confirmation rules while using the high-level candle subscription API so every decision is made on finished bars.

## Lógica da estratégia
- **Indicadores**
  - Oscilador Stochastic padrão com `%K`, `%D` configuráveis e períodos de desaceleração.
  - Média móvel simples do tamanho do corpo da vela (absoluta `open-close`) para classificar as velas como "longas" ou "pequenas", assim como a versão MQL.
- **Long Entry**
  - Padrão Morning Star nas últimas três velas concluídas:
    1. Duas barras atrás: corpo longo e baixista cujo tamanho excede a média corporal.
    2. Barra anterior: vela de corpo pequeno que fecha e abre abaixo da vela anterior.
    3. Barra atual: fechamento de alta acima do ponto médio da primeira vela.
  - A linha de sinal Stochastic (`%D`) está abaixo do limite de sobrevenda (padrão `30`).
  - A exposição curta existente é achatada antes de abrir a posição longa.
- **Short Entry**
  - Padrão Evening Star refletindo as regras acima.
  - Stochastic `%D` está acima do limite de sobrecompra (padrão `70`).
  - A exposição longa existente é fechada antes de abrir a negociação a descoberto.
- **Posição de saída**
  - As vendas curtas são fechadas quando `%D` ultrapassa o nível de recuperação rápida (`20`) ou o nível extremo (`80`).
  - As posições compradas são fechadas quando `%D` cruza abaixo de `80` ou `20`.
  - Esses cruzamentos reproduzem as "condições próximas" do módulo de sinal MQL.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Prazo (ou outro `DataType`) usado para detecção de padrões e todos os indicadores. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | `%K`, `%D` e períodos de desaceleração do oscilador estocástico. |
| `StochasticOverbought`, `StochasticOversold` | Limites de linha de sinal usados para confirmar entradas de Estrela Vespertina/Morning. |
| `PatternAveragePeriod` | Número de velas acabadas usadas para calcular a média do tamanho do corpo (`|abrir-fechar|`). |
| `ShortExitLevel`, `LongExitLevel` | `%D` níveis que forçam saídas curtas/longas quando atravessadas na direção oposta. |

## Notas de implementação
- As velas são processadas por meio de `SubscribeCandles().BindEx(...)`; o código só funciona com velas finalizadas e nunca chama `GetValue()` em indicadores.
- A média do tamanho do corpo depende de `SimpleMovingAverage` alimentado com corpos de velas absolutos para reproduzir o auxiliar `AvgBody()` da biblioteca MQL.
- As verificações de padrões são implementadas com métodos auxiliares dedicados para manter a lógica de decisão legível e espelhar as regras `CCandlePattern` originais.
- Before entering in the opposite direction the strategy closes any existing exposure to match the Expert Advisor's behaviour of operating one net position at a time.

## Diferenças do especialista MQL5
- As configurações de gerenciamento de dinheiro, trailing stop e lote fixo da estrutura MetaTrader não são reproduzidas; O volume do pedido StockSharp é controlado pela propriedade da estratégia `Volume`.
- O oscilador Stochastic usa a implementação do indicador StockSharp; os limites permanecem configuráveis ​​para que você possa ajustar o comportamento se o feed original do corretor produzir valores ligeiramente diferentes.
- O Logging fornece explicações detalhadas (em inglês) para cada entrada e saída para ajudar na depuração e no backtesting.
