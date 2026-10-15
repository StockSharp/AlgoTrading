# Estrategia Syndicate Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una traducción a StockSharp del script original de MetaTrader **Syndicate_Trader_v_1_04.mq4** de la carpeta `MQL/12351`.

Opera basándose en un cruce entre medias móviles exponenciales rápida y lenta con confirmación de pico de volumen. Filtros de sesión opcionales restringen el trading a horas específicas. Niveles simples de take-profit y stop-loss gestionan el riesgo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La EMA rápida cruza por encima de la EMA lenta y el volumen supera la media móvil multiplicada por un factor configurable.
  - **Corto**: La EMA rápida cruza por debajo de la EMA lenta con la misma confirmación de volumen.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cruce opuesto.
  - Stop-loss o take-profit alcanzado.
  - Fuera de la ventana de sesión permitida.
- **Stops**: Stop-loss y take-profit fijos en puntos de precio.
- **Filtros**:
  - Filtro de pico de volumen.
  - Filtro de tiempo de sesión opcional.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `FastEmaLength` | Período de la EMA rápida. |
| `SlowEmaLength` | Período de la EMA lenta. |
| `VolumeMaLength` | Período para promediar el volumen. |
| `VolumeMultiplier` | Multiplicador aplicado al volumen promedio para definir un pico. |
| `TakeProfitPoints` | Take-profit en puntos de precio. |
| `StopLossPoints` | Stop-loss en puntos de precio. |
| `UseSessionFilter` | Activar o desactivar el filtro de sesión. |
| `SessionStartHour/SessionStartMinute` | Hora de inicio de la sesión de trading. |
| `SessionEndHour/SessionEndMinute` | Hora de fin de la sesión de trading. |
| `CandleType` | Tipo de vela y marco temporal. |
