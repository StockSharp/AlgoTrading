# Estrategia de Reversión Uno-Dos-Tres
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Reversión Uno-Dos-Tres busca un patrón alcista 1-2-3 en la acción del precio. Se abre una posición larga cuando el mínimo de hoy está por debajo del de ayer, el mínimo de ayer está por debajo del mínimo de hace tres barras, el mínimo de hace dos barras está por debajo del mínimo de hace cuatro barras, y el máximo de hace dos barras está por debajo del máximo de hace tres barras. La operación se cierra después de un número definido de barras o cuando el precio cierra por encima de una media móvil.

## Detalles

- **Criterios de entrada:**
  - Mínimo actual < mínimo anterior.
  - Mínimo anterior < mínimo de hace tres barras.
  - Mínimo de hace dos barras < mínimo de hace cuatro barras.
  - Máximo de hace dos barras < máximo de hace tres barras.
- **Largo/Corto:** Solo largos.
- **Criterios de salida:**
  - Mantener durante `DaysToHold` barras o el cierre cruza por encima de la media móvil.
- **Stops:** Ninguno.
- **Valores predeterminados:**
  - `DaysToHold` = 7
  - `MaLength` = 200
- **Filtros:**
  - Categoría: Reversión
  - Dirección: Solo largos
  - Indicadores: Price action, SMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
