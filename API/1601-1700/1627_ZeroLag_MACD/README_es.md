# Estrategia de Cruce ZeroLag MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en un cruce entre la línea MACD y su línea de señal. Fue convertida del asesor experto MetaTrader **ZeroLagEA-AIP v0.0.4**. La estrategia opera solo durante las horas de sesión configuradas y puede opcionalmente requerir que el cruce ocurra en la barra actual.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La línea MACD cruza por encima de la línea de señal.
  - **Corto**: La línea MACD cruza por debajo de la línea de señal.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cruce opuesto o salida forzada en el día y hora especificados.
- **Stops**: Ninguno.
- **Filtros**:
  - Horas de sesión definidas por `StartHour` y `EndHour`.
  - Requisito opcional de cruce reciente (`UseFreshSignal`).

## Parámetros

- `FastEmaLength` = 2
- `SlowEmaLength` = 34
- `SignalEmaLength` = 2
- `UseFreshSignal` = true
- `Volume` = 2
- `StartHour` = 9
- `EndHour` = 15
- `KillDay` = 5
- `KillHour` = 21
- `CandleType` = velas de 1 minuto
