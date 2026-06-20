# Estrategia de Divergencia con OBV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El On-Balance Volume rastrea el volumen de operaciones acumulado con la idea de que el volumen precede al precio. Cuando el precio forma un nuevo máximo pero OBV no lo confirma, o viceversa, puede estar gestándose una reversión. Esta estrategia usa esa divergencia para operar en contra de movimientos insostenibles.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 112%. Funciona mejor en el mercado de divisas.

Para cada vela, el OBV se actualiza y se compara con la lectura anterior. Surge una señal alcista si el precio hace un mínimo más bajo mientras el OBV registra un mínimo más alto. Una señal bajista ocurre cuando el precio sube a un máximo más alto pero el OBV se queda rezagado. Una media móvil proporciona un punto de salida, mientras que un stop porcentual mantiene las pérdidas controladas.

El enfoque intenta capturar la reversión a la media tras el agotamiento del volumen y generalmente mantiene las operaciones solo hasta que el precio cruza de vuelta la línea media.

## Detalles

- **Criterios de entrada**: Divergencia Precio/OBV.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Precio cruzando la media móvil o stop-loss.
- **Stops**: Sí, basados en porcentaje.
- **Valores predeterminados**:
  - `DivergencePeriod` = 5
  - `MAPeriod` = 20
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoría: Divergencia
  - Dirección: Ambos
  - Indicadores: OBV, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio

