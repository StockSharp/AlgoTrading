# Estrategia de Horas Pico del Nasdaq 100
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera el Nasdaq 100 únicamente durante las dos primeras horas y la última hora de la sesión. Utiliza confirmación de tendencia EMA, filtros RSI, ATR y VWAP con trailing stops y stops de break-even basados en ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Precio por encima de la EMA corta, EMA corta por encima de la EMA larga, ambas EMAs en ascenso, RSI por encima de 50 y precio por encima del VWAP durante las horas pico de sesión.
  - **Corto**: Condiciones opuestas.
- **Largo/Corto**: Largo y corto.
- **Criterios de salida**:
  - Trailing stop basado en ATR o stop de break-even.
  - Salida temporal tras un número configurable de barras o reversión de tendencia EMA.
- **Stops**: Trailing ATR con break-even.
- **Valores predeterminados**:
  - `Long EMA` = 21
  - `Short EMA` = 9
  - `RSI` = 14
  - `ATR` = 14
  - `Trail ATR Mult` = 1.5
  - `Initial SL Mult` = 0.5
  - `Break-even ATR Mult` = 1.5
  - `Time Exit Bars` = 20
- **Filtros**:
  - Categoría: Intradía
  - Dirección: Ambos
  - Indicadores: EMA, RSI, ATR, VWAP
  - Stops: Trailing
  - Complejidad: Avanzado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
