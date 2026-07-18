# Estrategia Turn Around Tuesday on Steroids
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia larga estacional que compra después de dos días consecutivos a la baja al inicio de la semana y sale en una ruptura por encima del máximo anterior. Un filtro de media móvil opcional confirma la dirección de la tendencia.

## Detalles

- **Criterios de entrada**: primer o segundo día de la semana con caída de dos días
- **Largo/Corto**: Largo
- **Criterios de salida**: cierre por encima del máximo anterior
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `StartingDay` = Sunday
  - `MaPeriod` = 200
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Solo largos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
