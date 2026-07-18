# Estratégia GlamTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia GlamTrader** é uma conversão da API de alto nível do StockSharp do assessor especialista MetaTrader `GlamTrader.mq5`. O robô original combina uma média móvel deslocada com o oscilador Laguerre RSI e o Awesome Oscillator para filtrar o momentum antes de abrir uma única posição a mercado. O porte preserva a árvore de decisão exata e as regras de gerenciamento de capital enquanto adapta a execução de ordens, o charting e os controles de risco às convenções do StockSharp.

## Como a estratégia funciona

1. Subscrever a série de velas definida por `CandleType` (M15 por padrão). O período de tempo selecionado alimenta cada indicador.
2. Construir uma média móvel configurável na fonte `AppliedPrice` selecionada e deslocá-la `MaShift` barras para reproduzir o buffer deslocado usado no MetaTrader.
3. Recriar o filtro Laguerre RSI internamente usando o filtro recursivo de quatro estágios (`LaguerreGamma` controla o fator de suavização). O valor permanece no intervalo `[0;1]` como o indicador personalizado original.
4. Calcular o Awesome Oscillator com médias simples padrão de 5/34 do preço mediano e armazenar as leituras atuais e anteriores para detecção de inclinação.
5. Apenas quando nenhuma posição está aberta:
   - **Entrada comprada** – média móvel acima do fechamento atual, Laguerre RSI acima de `0.15`, e Awesome Oscillator subindo em relação à barra anterior.
   - **Entrada vendida** – média móvel abaixo do fechamento atual, Laguerre RSI abaixo de `0.75`, e Awesome Oscillator caindo em relação à barra anterior.
6. Na entrada, a estratégia converte distâncias de stop-loss/take-profit de pips para deslocamentos de preço usando o tamanho de tick do instrumento. As distâncias são ajustadas para cotações de 3 ou 5 dígitos exatamente como `Point * 10` em MQL.
7. Enquanto uma posição está ativa, o algoritmo espelha a rotina de trailing original: uma vez que o preço avança mais de `TrailingStopPips + TrailingStepPips`, o stop é arrastado para `TrailingStopPips` atrás (ou acima) do mercado. As saídas são executadas quando o intervalo da vela toca o preço do trailing stop ou do take-profit.

## Lógica de entrada e saída

- Manter no máximo uma posição a todo momento. Sinais opostos são ignorados até que a operação atual seja fechada.
- Operações compradas requerem uma média móvel deslocada baixista (preço cruzando acima da linha), Laguerre RSI saindo da zona de sobrevendido (`> 0.15`), e momentum crescente do Awesome Oscillator.
- Operações vendidas requerem uma média móvel deslocada altista (preço cruzando abaixo da linha), Laguerre RSI caindo da zona de sobrecomprado (`< 0.75`), e momentum decrescente do Awesome Oscillator.
- Stops e alvos são aplicados com comparações de preço contra máximas/mínimas de vela para que toques intrabarra sejam respeitados mesmo que a lógica seja executada em velas terminadas.
- O trailing segue a regra do MetaTrader: o stop só se move após o preço avançar pela distância do stop mais o passo de trailing, e nunca retrocede.

## Parâmetros

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(15).TimeFrame()` | Período usado para cálculos de indicadores e tomada de decisão. |
| `TradeVolume` | `decimal` | `1` | Volume usado para ordens a mercado. |
| `StopLossBuyPips` | `decimal` | `50` | Distância de stop-loss em pips para entradas compradas. |
| `TakeProfitBuyPips` | `decimal` | `50` | Distância de take-profit em pips para entradas compradas. |
| `StopLossSellPips` | `decimal` | `50` | Distância de stop-loss em pips para entradas vendidas. |
| `TakeProfitSellPips` | `decimal` | `50` | Distância de take-profit em pips para entradas vendidas. |
| `TrailingStopPips` | `decimal` | `5` | Distância do trailing stop em pips. Defina como zero para desabilitar o trailing. |
| `TrailingStepPips` | `decimal` | `15` | Lucro adicional (em pips) necessário antes que o trailing stop possa se mover. |
| `MaPeriod` | `int` | `14` | Comprimento de lookback da média móvel. |
| `MaShift` | `int` | `1` | Deslocamento positivo aplicado à média móvel. |
| `MaMethod` | `MaMethod` | `LinearWeighted` | Tipo de média móvel (simples, exponencial, suavizado ou ponderado linearmente). |
| `AppliedPrice` | `AppliedPrice` | `Weighted` | Fonte de preço usada para a média móvel e o filtro Laguerre. |
| `LaguerreGamma` | `decimal` | `0.7` | Coeficiente de suavização Laguerre (intervalo 0–1). |

## Dicas de uso

1. Anexe a estratégia ao ativo desejado, certifique-se de que o modelo de broker forneça informações de tamanho/passo de tick, e defina `CandleType` para corresponder ao período que deseja operar.
2. Ajuste os parâmetros de risco baseados em pips à volatilidade do instrumento. A conversão normaliza automaticamente as distâncias usando `PriceStep`; símbolos FX de cinco dígitos recebem o multiplicador 10× esperado.
3. Auxiliares de gráfico opcionais desenham a média móvel na área de preço e traçam o Awesome Oscillator em um painel separado junto com suas próprias operações.
4. Inicie a estratégia. Ela gerenciará stops e trailing internamente, espelhando as rotinas `OpenBuy`, `OpenSell` e de trailing do código MQL original.

## Notas

- A implementação do Laguerre RSI espelha o indicador `laguerre.mq5`, incluindo a normalização `CU/(CU+CD)`.
- Os valores do Awesome Oscillator vêm do indicador embutido do StockSharp, portanto não é necessária cópia manual de buffer.
- Como a lógica é avaliada em velas completadas, os backtests e o trading ao vivo permanecem determinísticos e livres de repintura no nível de tick.
