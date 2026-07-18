# Estratégia RsiBoosterStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`RsiBoosterStrategy` é uma versão para StockSharp do expert advisor do MetaTrader *RSI booster*. A estratégia compara o valor rápido do RSI calculado no candle atual com um RSI atrasado que usa o candle anterior. Quando a diferença ultrapassa uma razão definida pelo usuário, a estratégia abre uma posição a mercado e depois gerencia a operação com stops fixos, alvos de take-profit, um trailing stop opcional e uma cadeia de ordens reversas para recuperação de perdas.

A estratégia é construída sobre a API de alto nível do StockSharp. Ela assina uma única série de candles, usa os indicadores incorporados `RelativeStrengthIndex` e emprega o sistema de parâmetros de estratégia para que todas as entradas fiquem disponíveis para otimização dentro do Designer.

## Lógica de negociação

1. Dois indicadores RSI são calculados em cada candle concluído.
   * O RSI rápido usa `FirstRsiPeriod` e `FirstRsiPrice`, e lê o candle mais recente.
   * O RSI atrasado usa `SecondRsiPeriod` e `SecondRsiPrice`, mas a estratégia mantém o valor anterior para que ele funcione como um atraso de uma barra.
2. Quando `fast RSI - delayed RSI` é maior que `Ratio`, a estratégia compra se não houver posição comprada aberta. Quando a diferença fica abaixo de `-Ratio`, ela vende se não houver posição vendida aberta.
3. `OnlyOnePositionPerBar` garante que ocorra no máximo uma entrada por direção para o mesmo carimbo de tempo do candle.
4. Depois de cada candle, a estratégia avalia as regras de stop-loss, take-profit e trailing. Se uma das condições for acionada, a posição é fechada imediatamente.
5. Quando uma posição é fechada com PnL realizado negativo, a lógica opcional de recuperação pode entrar em uma posição reversa (direção oposta) com o mesmo volume. O número de operações de recuperação encadeadas é limitado por `ReturnOrdersMax`.

## Gestão de risco

* **Stop-loss** - expresso em pontos do instrumento por meio de `StopLossPips`. A posição é fechada quando o preço cruza o nível do stop.
* **Take-profit** - expresso em pontos do instrumento por meio de `TakeProfitPips`.
* **Trailing stop** - se habilitado por `TrailingStopPips`, o stop começa a seguir o preço quando o lucro ultrapassa a distância configurada. `TrailingStepPips` define a melhora mínima antes de mover o nível trailing.
* **Ordem de retorno** - ativada quando `ReturnOrderEnabled` é `true`. Depois de uma operação perdedora, a estratégia abre instantaneamente uma ordem a mercado na direção oposta enquanto contabiliza quantas ordens de recuperação foram emitidas.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `Volume` | Volume de negociação usado para cada ordem a mercado (lotes ou contratos). |
| `Ratio` | Diferença mínima de RSI necessária para abrir uma posição. |
| `StopLossPips` | Distância do stop-loss em pontos do instrumento. |
| `TakeProfitPips` | Distância do take-profit em pontos do instrumento. |
| `TrailingStopPips` | Distância do trailing stop em pontos do instrumento. |
| `TrailingStepPips` | Melhora mínima antes de mover o trailing stop. |
| `OnlyOnePositionPerBar` | Impede múltiplas entradas durante o mesmo candle. |
| `ReturnOrderEnabled` | Habilita a lógica de recuperação com ordem reversa. |
| `ReturnOrdersMax` | Número máximo de ordens de recuperação consecutivas. |
| `FirstRsiPeriod` | Período do RSI rápido. |
| `FirstRsiPrice` | Fonte de preço para o RSI rápido (corresponde aos modos de preço aplicado do MetaTrader). |
| `SecondRsiPeriod` | Período do RSI atrasado. |
| `SecondRsiPrice` | Fonte de preço para o RSI atrasado (corresponde aos modos de preço aplicado do MetaTrader). |
| `CandleType` | Série de candles usada para a análise. |

## Observações

* A conversão do passo de preço respeita o `PriceStep` do instrumento sempre que disponível. Se o instrumento não fornecer um passo de preço, é usado um fallback de `0.0001`.
* O contador da cadeia de recuperação é reiniciado sempre que ocorre uma operação lucrativa ou quando o número máximo configurado de ordens de recuperação é atingido.
* A estratégia desenha ambos os indicadores RSI na área do gráfico para uma inspeção visual rápida junto com as operações executadas.
