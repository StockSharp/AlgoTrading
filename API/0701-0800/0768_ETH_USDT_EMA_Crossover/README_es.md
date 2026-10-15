# Estrategia de Cruce de EMA ETH/USDT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera ETH/USDT utilizando un cruce de EMA con filtros adicionales.

Se abre una posición larga cuando la EMA de 20 períodos cruza por encima de la EMA de 50 períodos mientras el precio está por encima de la EMA de 200 períodos, el RSI está por encima de 30, la volatilidad medida por ATR está por encima de su media móvil, y el volumen es mayor que su promedio. Se abre una posición corta en las condiciones opuestas.

Las posiciones se invierten cuando aparece la señal contraria. No se utiliza stop loss ni take profit explícito.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `EMA20 cruza por encima de EMA50` && `Close > EMA200` && `RSI > 30` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
  - **Corto**: `EMA20 cruza por debajo de EMA50` && `Close < EMA200` && `RSI < 70` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
- **Largo/Corto**: Ambos lados
- **Criterios de salida**:
  - Señal inversa
- **Stops**: No
- **Valores predeterminados**:
  - `EMA200 Length` = 200
  - `EMA20 Length` = 20
  - `EMA50 Length` = 50
  - `RSI Length` = 14
  - `ATR Length` = 14

- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, RSI, ATR
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
