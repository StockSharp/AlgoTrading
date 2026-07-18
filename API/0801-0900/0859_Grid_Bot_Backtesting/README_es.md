# Estrategia de Backtesting de Bot de Cuadrícula
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa un bot de trading en cuadrícula que acumula posiciones largas cuando el precio cae a los niveles de la cuadrícula y las cierra cuando el precio sube a la siguiente línea. Los límites pueden establecerse manualmente o calcularse a partir de datos recientes.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio cruza por debajo de una línea de cuadrícula sin orden activa
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - el precio cruza por encima de la siguiente línea de cuadrícula
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `AutoBounds` = true
  - `BoundSource` = "Hi & Low"
  - `BoundLookback` = 250
  - `BoundDeviation` = 0.10
  - `UpperBound` = 0.285
  - `LowerBound` = 0.225
  - `GridLines` = 30
- **Filtros**:
  - Categoría: Trading en rango
  - Dirección: Solo largos
  - Indicadores: Highest, Lowest, SimpleMovingAverage
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
