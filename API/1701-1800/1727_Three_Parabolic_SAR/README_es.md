# Estrategia de Tres Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Tres Parabolic SAR utiliza tres indicadores Parabolic SAR calculados en velas de 6 horas, 3 horas y 1 hora. Se abre una operación en el marco temporal de 1 hora cuando los dos marcos temporales superiores confirman la dirección y el SAR de 1 hora cambia de posición.

## Detalles

- **Criterios de entrada**:
  - El SAR en velas de 6h está por debajo del cierre y el de 3h también para largo; por encima para corto.
  - En velas de 1h el SAR cruza el precio: de arriba hacia abajo para largo, de abajo hacia arriba para corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: La posición se cierra cuando el SAR de 1h se mueve en contra de la posición o cuando alguno de los SAR de marcos temporales superiores se invierte.
- **Stops**: No.
- **Valores predeterminados**:
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `HigherTimeframe` = TimeSpan.FromHours(6)
  - `MiddleTimeframe` = TimeSpan.FromHours(3)
  - `TradingTimeframe` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Multi-marco temporal
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
