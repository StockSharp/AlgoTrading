# Estrategia Fisher Org Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el indicador Fisher Transform con niveles superior e inferior predefinidos. Se abre una posición larga cuando el valor Fisher cruza al alza el nivel inferior. Se abre una posición corta cuando el valor Fisher cruza a la baja el nivel superior.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Fisher crosses above DownLevel`
  - **Corto**: `Fisher crosses below UpLevel`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - La señal opuesta activa la reversión de la posición
- **Stops**: No
- **Valores predeterminados**:
  - `Fisher Length` = 7
  - `UpLevel` = 1.5
  - `DownLevel` = -1.5
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Fisher Transform
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Medio plazo (H4)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
