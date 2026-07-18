# Estrategia de Reversión por Brecha Descendente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Reversión por Brecha Descendente busca reversiones alcistas tras una apertura con brecha a la baja.
Cuando una nueva sesión abre por debajo del mínimo anterior pero cierra por encima de su apertura, suele atrapar a los vendedores y señala un rebote.

La estrategia entra en largo cuando aparece este patrón y sale cuando el precio cierra por encima del máximo anterior.

## Detalles

- **Criterios de entrada**: patrón de reversión por brecha descendente
- **Largo/Corto**: Solo largos
- **Criterios de salida**: cierre por encima del máximo anterior
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = 1 day
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Solo largos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
