# BTCUSD Momentum After Abnormal Days

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Reglas de negociación

El rendimiento de una vela diaria terminada es `r=(Close-Open)/Open`. La media y desviación poblacional usan los `LookbackPeriod` rendimientos diarios anteriores, excluyendo el actual, y requieren ventana completa. Compra si `r>mean+k*sd`, vende si `r<mean-k*sd`, con `k=AbnormalThreshold`. La igualdad no entra.

Entra solo desde cero. Cantidad: `floor(CapitalPerTrade/Close/VolumeStep)*VolumeStep`; omite cantidades inferiores al mínimo y rechaza superiores al máximo. El capital se mide al cierre de señal y la ejecución puede variar. Cierra toda la posición ejecutada al cierre diario siguiente, o al primer cierre posterior disponible tras un hueco. La salida tiene prioridad sobre una nueva entrada en esa vela. Las órdenes pendientes bloquean duplicados; no hay stop de precio.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `CandleType` | `1d` |
| `LookbackPeriod` | `5` |
| `AbnormalThreshold` | `1.6` |
| `CapitalPerTrade` | `1000` |
