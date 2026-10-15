# Estrategia Figurelli Series
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia convierte el experto MetaTrader5 "Exp_FigurelliSeries" a StockSharp. Utiliza un indicador personalizado Figurelli Series que mide la diferencia entre el número de medias móviles por encima y por debajo del precio actual. Las operaciones ocurren una vez al día a una hora de inicio definida por el usuario y todas las posiciones se cierran a una hora de parada.

## Indicador
El indicador Figurelli Series crea una cadena de medias móviles exponenciales comenzando desde *Start Period* e incrementando en *Step* para *Total* medias. En cada barra cuenta cuántas medias están por encima y por debajo del precio de cierre. El valor del indicador es `bids - asks` donde `bids` es el recuento de medias por debajo del precio y `asks` es el recuento de medias por encima del precio.

## Reglas de trading
- A las `Start Hour:Start Minute`:
  - Comprar si el valor del indicador es positivo y no hay posición larga.
  - Vender si el valor del indicador es negativo y no hay posición corta.
- A partir de `Stop Hour:Stop Minute`, cualquier posición abierta se cierra.
- Solo se usan velas terminadas del `Candle Type` seleccionado.

## Parámetros
- `StartPeriod` – período inicial de la media móvil.
- `Step` – incremento de período entre medias.
- `Total` – número de medias móviles.
- `StartHour` / `StartMinute` – hora en que pueden producirse entradas.
- `StopHour` / `StopMinute` – hora para cerrar todas las posiciones.
- `CandleType` – tipo de vela para los cálculos.
