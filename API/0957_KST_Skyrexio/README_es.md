# Estrategia KST Skyrexio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra en largo cuando el indicador Know Sure Thing (KST) cruza por encima de su señal mientras el precio opera por encima de una media móvil elegida y la mandíbula del Alligator. Un filtro de índice de choppiness puede desactivar las entradas en mercados laterales. Las posiciones se cierran mediante niveles de stop-loss y take-profit basados en ATR.

- **Criterios de entrada**: KST cruza por encima de la señal, precio sobre la MA de filtro y la mandíbula del Alligator, choppiness por debajo del umbral.
- **Criterios de salida**: El precio alcanza el stop-loss ATR o el take-profit ATR.
- **Indicadores**: KST, ATR, Media Móvil, mandíbula del Alligator, Índice de Choppiness.

## Parámetros
- `CandleType` – marco temporal de las velas.
- `AtrStopLoss` – multiplicador ATR para el stop-loss.
- `AtrTakeProfit` – multiplicador ATR para el take-profit.
- `FilterMaType` – tipo de MA de filtro de tendencia.
- `FilterMaLength` – longitud de la MA de filtro de tendencia.
- `EnableChopFilter` – habilitar filtro de choppiness.
- `ChopThreshold` – umbral del índice de choppiness.
- `ChopLength` – período del índice de choppiness.
- `RocLen1..4` – longitudes de ROC para KST.
- `SmaLen1..4` – longitudes de SMA para KST.
- `SignalLength` – longitud de la SMA de señal de KST.
