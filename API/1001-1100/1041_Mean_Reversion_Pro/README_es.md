# Estrategia de Reversión a la Media Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Mean Reversion Pro es un sistema de reversión a la media diseñado para los principales índices. Utiliza dos medias móviles y niveles de rango intrabarra para detectar retrocesos. Se prefieren las posiciones largas ya que los índices tienden a moverse al alza.

## Detalles

- **Criterios de entrada**:
  - **Largo**: cierre por debajo de la SMA rápida, cierre por debajo del nivel del 20% del rango, cierre por encima de la SMA lenta, sin posición.
  - **Corto**: cierre por encima de la SMA rápida, cierre por encima del nivel del 80% del rango, cierre por debajo de la SMA lenta, sin posición.
- **Largo/Corto**: Ambos (se recomienda largo).
- **Criterios de salida**:
  - **Largo**: el cierre cruza por encima de la SMA rápida.
  - **Corto**: el cierre cruza por debajo de la SMA rápida.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Fast SMA` = 5
  - `Slow SMA` = 100
  - `Direction` = Solo largos
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Configurable
  - Indicadores: SMA
  - Stops: Ninguno
  - Complejidad: Simple
  - Marco temporal: Diario
