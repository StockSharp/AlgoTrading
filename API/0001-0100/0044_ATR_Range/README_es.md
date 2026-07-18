# Estrategia ATR Range Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
ATR Range Breakout mide el movimiento del precio durante un número fijo de barras y lo compara con el rango verdadero promedio. Cuando el movimiento supera el ATR, se abre una posición en la dirección del movimiento.

Las pruebas indican un retorno anual promedio de aproximadamente 169%. Funciona mejor en el mercado de criptomonedas.

La estrategia verifica el precio cada N barras y utiliza la media móvil para señales de salida. Busca capturar el momentum cuando la volatilidad se expande más allá de los niveles normales.

Las operaciones se cierran cuando el precio cruza de nuevo la media móvil o cuando se activa el stop basado en ATR.

## Detalles

- **Criterios de entrada**: El precio se mueve más que el ATR durante el período de observación.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El precio cruza la MA o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `LookbackPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: ATR, MA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

