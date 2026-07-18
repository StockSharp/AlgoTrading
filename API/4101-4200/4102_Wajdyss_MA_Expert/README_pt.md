# Estratégia especializada de Wajdyss MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Wajdyss MA Expert Strategy** é uma versão C# do MetaTrader 4 consultor especialista "wajdyss MA expert v3". Ele compara duas médias móveis configuradas com períodos independentes, modos de cálculo, mudanças e preços aplicados. Um cruzamento de alta da média rápida acima da média lenta abre uma exposição longa, enquanto um cruzamento de baixa abre uma exposição curta. A conversão reproduz as regras originais de gestão de dinheiro, fechamento automático opcional de negociações opostas e filtros de liquidação de final de dia/fim de semana.

## Lógica de negociação
1. Assine o `CandleType` selecionado (velas de 15 minutos por padrão) e calcule as médias móveis rápidas e lentas usando as configurações `MovingAverageMethod` e `PriceSource` escolhidas para cada perna.
2. Armazene os valores dos indicadores para velas finalizadas. Avalie um sinal de alta quando a média rápida (com sua mudança configurada) estiver acima da média lenta na última barra fechada, enquanto estava abaixo de duas barras atrás. Avalie um sinal de baixa com a condição inversa.
3. Imponha um resfriamento entre novas entradas na mesma direção. A estratégia deve esperar pelo menos uma vela completa do período subscrito após a última negociação desse lado, espelhando o guarda de tempo variável global da versão MT4.
4. Quando **AutoCloseOpposite** estiver ativado, cancele ordens de trabalho e reverta a exposição em uma única ordem de mercado: o novo volume de ordem inclui qualquer posição pendente na direção oposta para que a conta mude imediatamente.
5. Aplique filtros de fechamento diário e de sexta-feira. Após o `DailyCloseHour`/`DailyCloseMinute` ou `FridayCloseHour`/`FridayCloseMinute` configurado, todas as posições são achatadas e novas negociações são bloqueadas até a próxima sessão.

## Gestão de Risco e Dinheiro
- **TakeProfitPips**, **StopLossPips** e **TrailingStopPips** são interpretados em pips inteiros. A implementação os converte em etapas de preço usando os metadados de segurança e aciona o mecanismo `StartProtection` de StockSharp com saídas de mercado para paridade com a lógica final original.
- **UseMoneyManagement** emula o cálculo do lote MT4: `volume = (account_balance / BalanceReference) * InitialVolume`. Os limites de câmbio são respeitados por meio de verificações de etapas de volume, mínimo e máximo.
- Se o gerenciamento de dinheiro estiver desativado, os pedidos usarão **InitialVolume** diretamente.

## Parâmetros
| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|---------|-------------|
| `FastPeriod` | `int` | `10` | Período da média móvel rápida. |
| `FastShift` | `int` | `0` | Barras para alterar a média rápida antes de comparar os valores de cruzamento. |
| `FastMethod` | `MovingAverageMethod` | `Ema` | Modo de média móvel para a linha rápida (`Sma`, `Ema`, `Smma`, `Lwma`). |
| `FastPriceType` | `PriceSource` | `Close` | Preço da vela inserido na média móvel rápida (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `SlowPeriod` | `int` | `20` | Período da média móvel lenta. |
| `SlowShift` | `int` | `0` | Barras para mudar a média lenta antes da comparação. |
| `SlowMethod` | `MovingAverageMethod` | `Ema` | Modo de média móvel para a linha lenta. |
| `SlowPriceType` | `PriceSource` | `Close` | O preço da vela alimentou a média lenta. |
| `TakeProfitPips` | `decimal` | `100` | Distância até a meta de lucro em pips (definida como `0` para desativar). |
| `StopLossPips` | `decimal` | `50` | Distância até a parada de proteção em pips (definida como `0` para desativar). |
| `TrailingStopPips` | `decimal` | `0` | Distância do trailing stop em pips (definida como `0` para desativar). |
| `AutoCloseOpposite` | `bool` | `true` | Feche a exposição oposta antes de abrir uma nova negociação na outra direção. |
| `InitialVolume` | `decimal` | `0.1` | Baseie o volume de negociação antes de aplicar a gestão de dinheiro. |
| `UseMoneyManagement` | `bool` | `true` | Ative o dimensionamento de posição baseado em equilíbrio. |
| `BalanceReference` | `decimal` | `1000` | Divisor usado ao dimensionar o volume com o saldo da conta. |
| `DailyCloseHour` | `int` | `23` | Hora (0-23) após a qual as posições diárias são fechadas. |
| `DailyCloseMinute` | `int` | `45` | Componente minuto do filtro de fechamento diário. |
| `FridayCloseHour` | `int` | `22` | Hora (0-23) após a qual a negociação de sexta-feira termina. |
| `FridayCloseMinute` | `int` | `45` | Componente minuto do filtro de fechamento de sexta-feira. |
| `CandleType` | `DataType` | `15m` período de tempo | Série de velas usada para cálculos e tempo de resfriamento. |

## Notas
- A estratégia depende exclusivamente do StockSharp API de alto nível: velas são processadas por meio de `SubscribeCandles`, ligações de indicadores alimentam médias móveis e `StartProtection` gerencia ordens de stop/take-profit/trailing.
- O achatamento de posição usa ordens de mercado para espelhar os fechamentos imediatos de tickets opostos do especialista MT4.
- Nenhuma tradução do Python está incluída nesta pasta; apenas a implementação C# é fornecida.
