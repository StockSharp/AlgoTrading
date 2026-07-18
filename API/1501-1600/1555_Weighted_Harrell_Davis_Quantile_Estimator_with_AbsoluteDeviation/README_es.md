# Estrategia de Estimador de Cuantiles Harrell-Davis Ponderado con DesviaciónAbsoluta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza un estimador de cuantiles basado en la mediana con bandas de desviación absoluta para detectar valores atípicos en el precio.
Compra cuando el cierre cae por debajo de la banda inferior y vende cuando el cierre sube por encima de la banda superior.

## Detalles

- **Criterios de entrada**: cierre por debajo de la banda de desviación inferior o por encima de la banda superior
- **Largo/Corto**: Ambos
- **Criterios de salida**: cruce de la banda opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `Length` = 39
  - `DeviationMultiplier` = 1.213
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Median
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
