# Estrategia MA Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia MA Parabolic SAR intenta capturar tendencias sostenidas utilizando una media móvil para determinar la dirección prevaleciente y los puntos Parabolic SAR para el momento de entrada y la colocación del stop. Cuando ambos indicadores se alinean, el sistema asume que el momentum es suficientemente fuerte para seguirlo.

Las pruebas indican un rendimiento anual promedio de aproximadamente 76%. Funciona mejor en el mercado de divisas.

Se abre una posición larga cuando el precio de cierre está por encima de la media móvil y los puntos Parabolic SAR se invierten por debajo del mercado. Se toma una posición corta cuando el precio está por debajo de la media y los puntos SAR se invierten por encima del precio, señalando presión bajista. La estrategia sale una vez que el precio cruza el SAR en dirección opuesta, asegurando ganancias o limitando pérdidas.

Este enfoque es más adecuado para traders que prefieren el seguimiento sistemático de tendencias con stops claros y mecánicos. El Parabolic SAR se ajusta continuamente a medida que cambia la volatilidad, manteniendo la exposición en línea con las condiciones del mercado mientras que la media móvil evita las operaciones contra la tendencia más amplia.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Price > MA && Price > Parabolic SAR
  - **Corto**: Price < MA && Price < Parabolic SAR
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando el precio cae por debajo del Parabolic SAR
  - **Corto**: Salir cuando el precio sube por encima del Parabolic SAR
- **Stops**: Sí, dinámico vía Parabolic SAR y stop fijo opcional.
- **Valores predeterminados**:
  - `MaPeriod` = 20
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `TakeValue` = new Unit(0, UnitTypes.Absolute)
  - `StopValue` = new Unit(2, UnitTypes.Percent)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA, Parabolic SAR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

