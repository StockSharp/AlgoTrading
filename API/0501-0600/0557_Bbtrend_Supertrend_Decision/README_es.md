# BBTrend SuperTrend Decision

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Reglas de negociación

Se calculan dos bandas de Bollinger sobre cierres terminados: 20 y 50 barras, con dos desviaciones estándar poblacionales. Para bandas inferiores `Ls/Ll`, superiores `Us/Ul` y media corta `Ms`: `BBTrend = 100 * (abs(Ls-Ll) - abs(Us-Ul)) / Ms`.

SuperTrend se aplica al indicador. Los valores anterior y actual forman apertura/cierre sintéticos y su mínimo/máximo forma los extremos. El rango verdadero es el cambio absoluto; ATR comienza con diez rangos y sigue con suavizado Wilder. Las bandas parten del punto medio y `7 * ATR`, conservando sus límites anteriores hasta la ruptura. Se espera la formación de ambos Bollinger y ATR.

SuperTrend alcista busca `+Volume` y bajista `-Volume`. La orden por la diferencia revierte toda la posición. `TradeDirection = Buy/Sell` limita nuevas entradas, pero la señal contraria todavía cierra la posición existente; `null` permite ambas direcciones. Una orden pendiente bloquea otra.

`TpSlCondition` selecciona `None`, `TP`, `SL` o `Both` (Python 0/1/2/3), desactivado por defecto. Las distancias habilitadas son porcentajes definidos por `TakeProfitPerc` y `StopLossPerc`.

La protección local nativa usa velas y actualizaciones de precio suscritas del instrumento. Las distancias parten de los precios ejecutados. Las salidas son a mercado y dependen del mercado disponible y del paso de precio.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `ShortBbLength` | `20` |
| `LongBbLength` | `50` |
| `StdDev` | `2` |
| `SupertrendLength` | `10` |
| `SupertrendMultiplier` | `7` |
| `TradeDirection` | `null` |
| `TpSlCondition` | `None` |
| `TakeProfitPerc` | `30` |
| `StopLossPerc` | `20` |
| `CandleType` | `5m` |
