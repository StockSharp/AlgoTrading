# Estrategia de Reversión a la Media con VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia opera contra los movimientos que se alejan del precio promedio ponderado por volumen. El ATR se utiliza para medir cuánto debe desviarse el precio del VWAP antes de considerar una operación de reversión.

Las pruebas indican un retorno anual promedio de aproximadamente 58%. Funciona mejor en el mercado de acciones.

Se abre una posición larga cuando el precio cae por debajo del VWAP en más de `K` veces el ATR. Se toma una posición corta cuando el precio sube por encima del VWAP en la misma cantidad. Las operaciones se cierran en cuanto el precio regresa a la línea VWAP.

El enfoque está diseñado para traders intradía que esperan que los precios oscilen alrededor del VWAP en lugar de tendencias fuertes. Los stops dimensionados como múltiplo del ATR ayudan a mantener las pérdidas controladas si el movimiento continúa contra la operación.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Close < VWAP - K * ATR
  - **Corto**: Close > VWAP + K * ATR
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando close >= VWAP
  - **Corto**: Salir cuando close <= VWAP
- **Stops**: Sí, stop basado en ATR.
- **Valores predeterminados**:
  - `K` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AtrPeriod` = 14
- **Filtros**:
  - Categoría: Mean reversion
  - Dirección: Ambos
  - Indicadores: VWAP, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

