# Estrategia Hull Suite by MRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que compara la media móvil de tipo Hull seleccionada con su valor de dos barras atrás. Las posiciones largas se abren cuando la media sube por encima de su valor de hace dos barras, y las posiciones cortas cuando cae por debajo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `MA > MA[2]`.
  - **Corto**: `MA < MA[2]`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Reversión ante señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 55
  - `Mode` = Hma
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Hull MA
  - Stops: Ninguno
  - Complejidad: Simple
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
