# Estratégia Up3x1 Investor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Up3x1 Investor porta o clássico assessor especialista do MetaTrader que reage a velas de expansão forte. Ela observa a última barra completada no período configurado e abre uma nova posição na barra seguinte se o range anterior e o corpo foram suficientemente amplos na direção do fechamento.

A estratégia é projetada para mercados discricionários como os principais pares de forex no gráfico H1, mas os limites podem ser ajustados para outros instrumentos. Apenas uma posição é mantida por vez e cada ordem usa a propriedade `Volume` da estratégia como tamanho da operação.

## Lógica de Trading

- **Fonte de sinais** – velas de período completadas de `CandleType` (padrão: 1 hora).
- **Critérios de entrada**
  - Calcular o range máximo–mínimo e o corpo absoluto da vela anterior.
  - Entrar comprado se a vela fechou acima da abertura e tanto o range quanto o corpo excedem seus respectivos limites em pips.
  - Entrar vendido se a vela fechou abaixo da abertura e tanto o range quanto o corpo excedem os limites.
  - Ignorar novas entradas enquanto qualquer posição estiver aberta.
- **Gestão de posições**
  - Os níveis opcionais de stop-loss e take-profit são convertidos de pips para unidades de preço usando `Security.PriceStep`.
  - Um trailing stop é ativado assim que o preço avança `TrailingStopPips + TrailingStepPips` a partir da entrada.
  - O trailing stop só se move se o novo nível estiver pelo menos `TrailingStepPips` mais próximo do preço do que o nível de trailing anterior.
  - A estratégia sai de uma posição quando o preço toca os níveis de stop-loss, take-profit ou trailing stop.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Tipo de dados das velas usadas para sinais (padrão: período de 1 hora). |
| `RangeThresholdPips` | Distância mínima máximo–mínimo da vela anterior, expressa em pips. |
| `BodyThresholdPips` | Distância mínima abertura–fechamento da vela anterior, expressa em pips. |
| `StopLossPips` | Distância de stop-loss em pips. Definir como 0 para desativar. |
| `TakeProfitPips` | Distância de take-profit em pips. Definir como 0 para desativar. |
| `TrailingStopPips` | Distância mantida atrás do preço no trailing. Definir como 0 para desativar o trailing. |
| `TrailingStepPips` | Movimento adicional em pips necessário antes que o trailing stop seja ajustado. |

> **Nota:** Os limites em pips são multiplicados por `Security.PriceStep`. Certifique-se de que o instrumento tem um `PriceStep` válido para que as conversões de pips reflitam corretamente o seu instrumento.

## Notas de Uso

1. Atribua o `Security` alvo e o conector de trading antes de iniciar a estratégia.
2. Ajuste os limites em pips para refletir a volatilidade do seu mercado. Pares de forex com cotações de 5 dígitos normalmente usam 10 pips = 0.0010.
3. Defina o `Volume` da estratégia para o tamanho de ordem desejado. A lógica de dimensionamento de posição do EA original é intencionalmente simplificada para manter a versão StockSharp transparente.
4. Como os sinais são avaliados em velas fechadas, as entradas são enviadas imediatamente após a confirmação da vela de expansão.
