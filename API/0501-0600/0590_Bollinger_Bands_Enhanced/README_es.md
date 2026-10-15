# Estrategia de Bollinger Bands Mejorada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Compra cuando el precio cae por debajo de la banda inferior de Bollinger mientras el mercado permanece por encima de una EMA de 200 períodos.  
Se coloca un stop loss en `entrada - ATR * stop`, y después de que el precio sube `ATR * trail` por encima de la entrada, la banda media se convierte en un objetivo trailing.

## Detalles

- **Criterios de entrada**: `Low > EMA` y `Low <= Banda inferior`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cierre por debajo de la banda media después de que se active el trailing o mínimo por debajo del stop.
- **Stops**: Stop loss basado en ATR.
- **Valores predeterminados**:
  - Período de Bollinger = 20
  - Período de EMA = 200
  - Período de ATR = 14
  - Stop ATR = 1.75
  - Trail ATR = 2.25

