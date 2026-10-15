# Gann Swing Multi Capa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza el análisis de swing Gann simplificado en múltiples capas.
Opera cuando tres direcciones de swing consecutivas se alinean.

El enfoque sigue la idea clásica de Gann sobre los cambios de dirección de swing.
Espera tres desplazamientos de swing consistentes antes de abrir una posición.

## Detalles

- **Criterios de entrada**: Tres direcciones de swing consecutivas en la misma orientación.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Dirección de swing opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Swing
  - Dirección: Ambos
  - Indicadores: Gann
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
