# Estratégia de Heikin Ashi Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o especialista do MetaTrader 4 "Heikin Ashi Trader" para o StockSharp. Mantém a lógica de confirmação multi-indicador do robô original e a implementa com a API de subscrição de velas de alto nível para que cada decisão seja baseada apenas em barras finalizadas.

## Detalhes
- **Indicadores**:
  - Velas Heikin-Ashi calculadas a partir do período de trabalho.
  - Duas médias móveis ponderadas lineares (LWMA) usando o preço típico da vela (`(high + low + close) / 3`).
  - Um oscilador estocástico (os períodos `%K/%D/Smooth` são configuráveis pelo usuário).
  - Momentum (distância do nível neutro 100).
  - Convergência/Divergência de Médias Móveis (MACD).
- **Critérios de entrada**:
  - **Comprado**: A última vela Heikin-Ashi deve ser altista, pelo menos um dos últimos três valores estocásticos deve estar acima do nível de sobrecompra, a LWMA rápida deve estar acima da LWMA lenta, a distância de momentum desde 100 deve exceder o limiar de compra, e a linha MACD deve estar acima do seu sinal.
  - **Vendido**: Condições espelhadas — vela Heikin-Ashi baixista, estocástico abaixo do nível de sobrevenda, LWMA rápida abaixo da LWMA lenta, distância de momentum acima do limiar de venda, e linha MACD abaixo do seu sinal.
  - Opcionalmente achatar a exposição oposta antes de entrar no novo trade (`CloseOppositePositions`).
- **Gestão de posições**:
  - Stop-loss e take-profit fixos em pips (derivados do passo de preço do ativo).
  - Trailing stop opcional que segue o fechamento assim que o trade avança `TrailingStopPips`.
  - Lógica de break-even que move o stop para `Entry ± BreakEvenOffsetPips` após o preço avançar `BreakEvenTriggerPips` a favor da posição.
  - Interruptor de saída manual (`ForceExit`) para achatar tudo na próxima vela.
- **Diferenças vs. versão MT4**:
  - O EA original avaliava momentum em um período superior. Este porto mantém os mesmos períodos de indicadores mas os lê do fluxo de velas primário para permanecer dentro da API de alto nível do StockSharp. Os parâmetros permitem ajustar os limiares para recriar a sensibilidade original.
  - As regras de stop baseadas em dinheiro do código MT4 não estão incluídas. O risco é gerenciado através de stops baseados em preço e o módulo de break-even.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período (ou qualquer outro tipo de vela) usado para todos os indicadores e decisões de trading. |
| `FastMaPeriod`, `SlowMaPeriod` | Períodos das médias móveis ponderadas lineares rápida e lenta (preço típico). |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Comprimentos `%K/%D` e fator de suavização do oscilador estocástico. |
| `StochasticOverbought`, `StochasticOversold` | Limiares estocásticos que devem ser cruzados durante os últimos três valores concluídos. |
| `MomentumPeriod` | Comprimento do indicador Momentum. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Distância absoluta mínima da linha 100 necessária para trades comprados/vendidos. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuração MACD. |
| `CloseOppositePositions` | Fechar o lado oposto antes de entrar em um novo trade. |
| `MaxPositions` | Exposição líquida máxima por direção (`0` = ilimitado). |
| `TradeVolume` | Volume de cada nova ordem; também atribuído ao `Volume` da estratégia. |
| `UseStopLoss`, `StopLossPips` | Habilitar e dimensionar o stop protetor em pips. |
| `UseTakeProfit`, `TakeProfitPips` | Habilitar e dimensionar o take-profit em pips. |
| `UseTrailingStop`, `TrailingStopPips` | Habilitar a lógica de trailing stop e definir sua distância. |
| `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Distância de ativação do break-even e o offset bloqueado. |
| `ForceExit` | Quando `true`, todas as posições são fechadas na próxima vela processada. |

## Notas de implementação
- A estratégia subscreve velas através de `SubscribeCandles().BindEx(...)` para que os indicadores recebam valores finalizados e o código nunca chame `GetValue()` diretamente.
- A conversão de pips usa o `PriceStep` do instrumento; se o mercado cotizar pips fracionários, configurar o passo do ativo adequadamente.
- Atualizações de trailing e break-even apenas movem o stop na direção favorável. A lógica de redefinição limpa os valores de stop/alvo em cache sempre que um trade é fechado, para que novas posições comecem com configurações de risco novas.
