# Estrategia de Agotamiento ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un repentino aumento en el Average True Range indica una expansión de la volatilidad que puede desvanecerse rápidamente. Esta estrategia busca lecturas de ATR que superen una media móvil por un multiplicador configurable. Cuando se combina con una vela de reversión, tiene como objetivo capturar la posterior contracción.

Las pruebas indican una rentabilidad anual media de aproximadamente el 139%. Funciona mejor en el mercado de acciones.

Cada barra actualiza el ATR y su propio promedio. Si el ATR supera el promedio por el multiplicador y la vela cierra en dirección opuesta al movimiento anterior, se abre una operación. El stop-loss también utiliza un múltiplo del ATR, anclando el riesgo a los niveles actuales de volatilidad.

Las posiciones típicamente dependen del stop para la salida, buscando una retracción rápida después de que el pico de volatilidad se disipe.

## Detalles

- **Criterios de entrada**: Pico de ATR por encima del promedio con vela de reversión.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss.
- **Stops**: Sí, basado en ATR.
- **Valores predeterminados**:
  - `AtrPeriod` = 14
  - `AtrAvgPeriod` = 20
  - `AtrMultiplier` = 1.5
  - `MaPeriod` = 20
  - `StopLoss` = 2%
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: ATR, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

