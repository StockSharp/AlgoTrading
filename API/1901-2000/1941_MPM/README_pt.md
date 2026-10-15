# Estratégia MPM Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão simplificada do expert MQL original `mpm-1_8.mq4`.
Ela aguarda uma sequência de velas progressivas e então abre uma posição na
mesma direção. O Average True Range é usado para avaliar o tamanho das velas e para
o trailing dos stops.

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `ProgressiveCandles` | Número de velas consecutivas necessárias para acionar uma operação. |
| `ProgressiveSize` | Tamanho mínimo do corpo da vela em relação ao ATR para contar como progressiva. |
| `StopRatio` | Proporção do ATR usada para o trailing do nível de stop. |
| `AtrPeriod` | Período do indicador Average True Range. |
| `CandleType` | Tipo de velas usado pela estratégia. |
| `ProfitPerLot` | Meta de lucro por lote. |
| `BreakEvenPerLot` | Lucro necessário para sair no breakeven. |
| `LossPerLot` | Perda máxima tolerada por lote. |

## Lógica

1. A cada vela concluída, o tamanho do corpo é comparado com o ATR.
2. Um contador de alta ou de baixa é incrementado quando o corpo excede o
   limiar `ProgressiveSize`.
3. Após `ProgressiveCandles` observadas em uma direção, uma ordem a mercado é enviada.
4. O nível de stop é arrastado por `StopRatio` do ATR.
5. As posições são fechadas quando o stop é atingido ou quando as metas de lucro/perda
   são alcançadas.
