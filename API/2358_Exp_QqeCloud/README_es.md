# Estrategia Exp QqeCloud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un enfoque de seguimiento de tendencia que aplica el indicador QQE (Quantitative Qualitative Estimation) a un RSI suavizado.
La estrategia abre posiciones solo en un horario de inicio de sesión predefinido y las cierra cuando ocurre la señal opuesta
o finaliza la sesión de trading.

## Detalles

- **Criterios de entrada**:
  - **Largo**: A las `StartHour`:`StartMinute`, la tendencia QQE gira hacia arriba.
  - **Corto**: A las `StartHour`:`StartMinute`, la tendencia QQE gira hacia abajo.
- **Criterios de salida**:
  - Señal de tendencia QQE opuesta.
  - El tiempo supera `StopHour`:`StopMinute`.
- **Indicadores**:
  - RSI (período `RsiPeriod`, suavizado por `RsiSmoothing`).
  - Bandas QQE usando multiplicador `QqeFactor`.
- **Stops**: Ninguno por defecto.
- **Valores predeterminados**:
  - `CandleType` = velas de 1 minuto
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.236
  - `StartHour` = 0, `StartMinute` = 0
  - `StopHour` = 23, `StopMinute` = 59
- **Filtros**:
  - Ventana de tiempo para entradas y salidas
  - Seguimiento de tendencia, marco temporal único
