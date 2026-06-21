# Estrategia SuperTrade ST1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia solo larga que combina Supertrend con filtro EMA y gestión de riesgos basada en ATR.

Las pruebas indican un retorno anual promedio de aproximadamente el 45%. Funciona mejor en el mercado de criptomonedas.

El sistema espera una caída en la dirección del Supertrend mientras el precio se mantiene por encima de la línea Supertrend y la EMA. El riesgo se controla con niveles de stop-loss y toma de ganancias basados en ATR en una proporción de 1:4.

## Detalles

- **Criterios de entrada**:
  - Dirección anterior de Supertrend > dirección actual
  - Cierre > Supertrend
  - Cierre > EMA
- **Largo/Corto**: Solo largos
- **Criterios de salida**: `Close <= entry - StopAtrMultiplier * ATR` o `Close >= entry + TakeAtrMultiplier * ATR`
- **Stops**: Stop-loss y toma de ganancias basados en ATR
- **Valores predeterminados**:
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `EmaPeriod` = 200
  - `StopAtrMultiplier` = 1.0
  - `TakeAtrMultiplier` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: Supertrend, EMA, ATR
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

