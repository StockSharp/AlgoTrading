# Estrategia MACD de Muestra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el clásico experto MACD Sample de MetaTrader.
Utiliza un cruce de MACD combinado con un filtro de tendencia EMA, niveles individuales de take-profit y stop-loss para operaciones largas y cortas, y un trailing stop opcional. El trading solo está permitido dentro de una ventana de tiempo configurable.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La línea MACD está por debajo de cero y cruza hacia arriba la línea de señal mientras la EMA sube.
  - **Corto**: La línea MACD está por encima de cero y cruza hacia abajo la línea de señal mientras la EMA cae.
- **Criterios de salida**:
  - Cruce MACD inverso.
  - Alcanzar los objetivos individuales de take-profit o stop-loss.
  - Activación del trailing stop.
- **Largo/Corto**: Ambos.
- **Valores predeterminados**:
  - `EMA Period` = 26
  - `MACD Open Level` = 3
  - `MACD Close Level` = 2
  - `Take Profit Long` = 50
  - `Take Profit Short` = 75
  - `Stop Loss Long` = 80
  - `Stop Loss Short` = 50
  - `Trailing Stop` = 30
  - Horario de trading: 4 a 19 UTC
- **Indicadores**: MACD, EMA
- **Marco temporal**: Velas de 1 hora por defecto
