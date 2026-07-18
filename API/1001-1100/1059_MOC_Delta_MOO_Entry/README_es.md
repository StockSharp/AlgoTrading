# Estrategia MOC Delta MOO Entry
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula el delta de volumen de compra y venta durante la sesión de 14:50–14:55 y opera a las 08:30 si el porcentaje del delta supera un umbral relativo al volumen del día. Utiliza filtros SMA sobre el precio de apertura y aplica take profit y stop loss basados en ticks.

## Detalles

- **Criterios de entrada:**
  - **Largo:** 08:30, delta MOC % por encima del umbral, apertura por encima de SMA15 y SMA30.
  - **Corto:** 08:30, delta MOC % por debajo del umbral negativo, apertura por debajo de SMA15 y SMA30.
- **Criterios de salida:**
  - **Stops:** Take profit y stop loss en ticks.
  - **Por tiempo:** Cierre de todas las posiciones a las 14:50.
- **Valores predeterminados:**
  - `DeltaThreshold` = 2
  - `TakeProfitTicks` = 20
  - `StopLossTicks` = 10
