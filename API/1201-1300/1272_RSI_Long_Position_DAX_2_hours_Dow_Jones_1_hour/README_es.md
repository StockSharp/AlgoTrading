# RSI Posición Larga DAX 2 Horas Dow Jones 1 Hora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

RSI Long Position compra cuando el RSI cruza por encima del nivel de sobreventa y cierra cuando el RSI supera el nivel de take profit o cae por debajo del nivel de stop.

## Detalles

- **Criterios de entrada**: RSI cruza por encima de `Oversold`
- **Largo/Corto**: Largo
- **Criterios de salida**: RSI mayor que `TakeProfit` o RSI cruza por debajo de `StopLoss`
- **Stops**: No
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `Oversold` = 35
  - `TakeProfit` = 55
  - `StopLoss` = 30
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Largo
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
