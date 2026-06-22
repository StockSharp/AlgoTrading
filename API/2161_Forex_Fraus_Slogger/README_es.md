# Estrategia Forex Fraus Slogger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el sistema de reversión por envoltura de MetaTrader.

## Lógica

- Calcula una SMA de 1 período como precio base.
- Las envolturas superior e inferior se establecen a `EnvelopePercent` por ciento desde la base.
- Cuando el precio cierra por encima de la banda superior y luego regresa por debajo, se entra en posición corta.
- Cuando el precio cierra por debajo de la banda inferior y luego regresa por encima, se entra en posición larga.
- Las posiciones están protegidas por un stop trailing.

## Parámetros

- `EnvelopePercent` – desplazamiento porcentual para las envolturas (predeterminado 0.1).
- `TrailingStop` – distancia del stop trailing en unidades de precio (predeterminado 0.001).
- `TrailingStep` – movimiento mínimo de precio requerido para avanzar el stop trailing (predeterminado 0.0001).
- `ProfitTrailing` – habilitar el trailing solo cuando la posición sea rentable.
- `UseTimeFilter` – operar solo durante las horas especificadas.
- `StartHour` – inicio de la ventana de operaciones.
- `StopHour` – fin de la ventana de operaciones.
- `CandleType` – marco temporal de velas utilizado para los cálculos.

## Notas

- La estrategia utiliza órdenes de mercado mediante `BuyMarket` y `SellMarket`.
- El stop trailing cierra la posición cuando el precio cruza el nivel de stop.
