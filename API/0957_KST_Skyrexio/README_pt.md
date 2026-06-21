# Estratégia KST Skyrexio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra comprado quando o indicador Know Sure Thing (KST) cruza acima da sua linha de sinal enquanto o preço negocia acima de uma média móvel escolhida e da mandíbula do Alligator. Um filtro de índice de choppiness pode desativar entradas em mercados laterais. As posições são fechadas usando níveis de stop-loss e take-profit baseados em ATR.

- **Critérios de entrada**: KST cruza acima do sinal, preço acima da MA de filtro e da mandíbula do Alligator, choppiness abaixo do limiar.
- **Critérios de saída**: O preço atinge o stop-loss ATR ou o take-profit ATR.
- **Indicadores**: KST, ATR, Média Móvel, mandíbula do Alligator, Índice de Choppiness.

## Parâmetros
- `CandleType` – período do candle.
- `AtrStopLoss` – multiplicador ATR para stop-loss.
- `AtrTakeProfit` – multiplicador ATR para take-profit.
- `FilterMaType` – tipo da MA de filtro de tendência.
- `FilterMaLength` – comprimento da MA de filtro de tendência.
- `EnableChopFilter` – habilitar filtro de choppiness.
- `ChopThreshold` – limiar do índice de choppiness.
- `ChopLength` – período do índice de choppiness.
- `RocLen1..4` – comprimentos de ROC para KST.
- `SmaLen1..4` – comprimentos de SMA para KST.
- `SignalLength` – comprimento da SMA de sinal do KST.
