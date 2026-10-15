# Estrategia de Portafolio Forex Fraus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera un único instrumento basándose en el indicador **Williams %R** con un período largo. Cuando el indicador sale de zonas extremas, la estrategia abre posiciones en la dirección del rompimiento.

## Cómo funciona

1. Se calcula Williams %R durante `WprPeriod` velas.
2. Cuando el indicador cae por debajo de `BuyThreshold`, se prepara una oportunidad larga. Una vez que sube por encima del umbral, se coloca una orden de compra de mercado.
3. Cuando el indicador sube por encima de `SellThreshold`, se prepara una oportunidad corta. Una vez que cae por debajo del umbral, se coloca una orden de venta de mercado.
4. Las posiciones solo se permiten durante la ventana de tiempo entre `StartHour` y `StopHour`.
5. Se pueden habilitar stop loss, take profit y trailing stop opcionales a través de parámetros.

## Parámetros

- `WprPeriod` – período de Williams %R.
- `BuyThreshold` – valor para habilitar una señal larga.
- `SellThreshold` – valor para habilitar una señal corta.
- `StartHour` / `StopHour` – límites de la sesión de trading.
- `SlPoints` – stop loss en puntos. Desactivado si es 0.
- `TpPoints` – take profit en puntos. Desactivado si es 0.
- `UseTrailing` – habilitar la lógica de trailing stop.
- `TrailingStop` – distancia de trailing en puntos.
- `TrailingStep` – paso para actualizaciones del trailing.
- `CandleType` – tipo de vela a suscribir.

## Notas

La versión MQL4 original operaba múltiples pares de divisas y gestionaba órdenes para cada uno. Este puerto en C# se enfoca en un único instrumento y demuestra la idea central usando la API de alto nivel de StockSharp.
