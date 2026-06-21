# Estrategia Markdown la Joya Oculta del Editor Pine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa Bandas de Bollinger construidas sobre una media móvil simple. Se abre una posición larga cuando el precio cierra por encima de la banda superior, y una posición corta cuando cierra por debajo de la banda inferior.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio de cierre cruza por encima de la banda superior.
  - **Corto**: El precio de cierre cruza por debajo de la banda inferior.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 50
  - `Multiplier` = 2
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger Bands
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
