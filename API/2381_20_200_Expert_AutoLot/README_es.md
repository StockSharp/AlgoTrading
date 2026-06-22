# Estrategia Expert AutoLot 20/200
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre como máximo una posición por día en una hora definida por el usuario. Compara el precio de apertura de dos barras pasadas (T1 y T2). Si la barra anterior es más alta que la posterior en DeltaShort pips, abre una posición corta. Si la barra posterior es más alta en DeltaLong pips, abre una posición larga.

El volumen de la posición puede ser fijo o calcularse automáticamente a partir del balance de la cuenta. Cuando el balance disminuye respecto a la operación anterior, el lote se multiplica por BigLotSize.

Cada operación usa su propio take-profit y stop-loss en pips. Además, un tiempo máximo de retención (MaxOpenTime) cierra la operación después del número de horas especificado.

## Parámetros

- `CandleType` – marco temporal de las velas procesadas (por defecto 1 hora).
- `TradeHour` – hora del día en que se verifican las condiciones de entrada.
- `T1`, `T2` – desplazamientos de barras para comparar precios de apertura.
- `DeltaLong`, `DeltaShort` – diferencia mínima de precio de apertura en pips.
- `TakeProfitLong`, `StopLossLong` – protección para operaciones largas en pips.
- `TakeProfitShort`, `StopLossShort` – protección para operaciones cortas en pips.
- `Lot` – volumen de trading base.
- `AutoLot` – activar el cálculo automático de lote.
- `BigLotSize` – multiplicador aplicado tras una pérdida.
- `MaxOpenTime` – tiempo máximo en horas para mantener una posición.
