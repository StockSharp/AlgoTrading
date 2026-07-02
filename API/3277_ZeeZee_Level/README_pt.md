# Estratégia ZeeZee Level
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia ZeeZee Level replica o comportamento do expert advisor original do MetaTrader "ZeeZee Level" usando a API de alto nível do StockSharp. A estratégia analisa swings ZigZag no período selecionado e negocia na direção do extremo mais recente. Distâncias de stop loss, take profit e trailing stop de proteção são expressas em pips, e o tamanho da posição segue uma progressão estilo martingale após operações perdedoras.

## Lógica de negociação

1. Candles são assinados usando o período definido por `CandleType`.
2. Um `ZigZagIndicator` com parâmetros configuráveis de profundidade, desvio e backstep rastreia máximas e mínimas de swing.
3. Quando nenhuma posição está aberta, a estratégia compara a recência da última máxima e mínima ZigZag confirmadas dentro da janela `ZigZagIdInterval`:
   - Se a última máxima de swing for mais recente que a última mínima de swing, uma posição vendida é aberta.
   - Se a última mínima de swing for mais recente que a última máxima de swing, uma posição comprada é aberta.
4. Apenas uma posição é mantida por vez. O volume de entrada é arredondado para o passo de volume do instrumento.
5. Depois que a posição é aberta, níveis de stop loss, take profit e trailing stop opcional são anexados usando as distâncias em pips configuradas. O trailing stop segue o preço extremo conforme a operação se move a favor.
6. Posições são fechadas assim que o nível de stop loss ou take profit é tocado. Quando ambos os níveis são atingidos no mesmo candle, o nível mais próximo do preço de entrada vence o desempate.
7. Após cada saída, o volume é redefinido para o valor inicial em operações lucrativas ou multiplicado pelo fator martingale em operações perdedoras.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `ZigZagDepth` | Número de candles considerados ao buscar novos pivôs ZigZag. |
| `ZigZagDeviation` | Movimento mínimo de preço (em passos de preço) necessário para confirmar um novo pivô. |
| `ZigZagBackstep` | Número mínimo de barras antes que o indicador possa mudar de direção. |
| `ZigZagIdInterval` | Número máximo de barras usado para olhar para trás em busca das últimas máximas e mínimas ZigZag. |
| `StopLossPips` | Distância de stop loss em pips. Defina como zero para desabilitar. |
| `TakeProfitPips` | Distância de take profit em pips. Defina como zero para desabilitar. |
| `TrailingStopPips` | Distância de trailing stop em pips. Defina como zero para desabilitar. |
| `InitialVolume` | Volume base de operação usado no início de um ciclo martingale. |
| `MartingaleMultiplier` | Fator aplicado ao próximo volume de operação após uma posição perdedora. |
| `CandleType` | Tipo de candle e período usado para a análise. |

## Gestão de dinheiro

- Os volumes são alinhados ao passo de volume do instrumento e limitados entre os limites mínimo e máximo da bolsa.
- Operações vencedoras redefinem o volume para `InitialVolume`, enquanto operações perdedoras o multiplicam por `MartingaleMultiplier`.

## Gestão de risco

- Distâncias de stop loss, take profit e trailing stop são avaliadas em cada candle concluído.
- O trailing stop se move apenas na direção da operação e nunca recua.
- A negociação é ignorada enquanto a estratégia já mantém uma posição ou enquanto os swings ZigZag não estão disponíveis dentro do intervalo configurado.

## Observações

- A estratégia usa apenas candles fechados para corresponder ao comportamento do expert advisor original.
- Conversões de pips dependem do `PriceStep` do instrumento. Garanta que os metadados do instrumento estejam carregados antes de iniciar a estratégia.
