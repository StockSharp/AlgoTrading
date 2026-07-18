# Estrategia de Mínimo de 3 Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Mínimo de 3 Barras compra cuando el precio de cierre cae por debajo del cierre mínimo de las tres barras anteriores y sale cuando el precio cierra por encima del cierre máximo de las siete barras anteriores. Un filtro EMA opcional puede requerir que el precio se mantenga por encima de una media de largo plazo antes de permitir entradas.

## Detalles

- **Criterios de entrada**:
  - El precio de cierre está por debajo del cierre mínimo de las tres barras anteriores.
  - Opcional: el precio de cierre está por encima de la EMA cuando el filtro está activado.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - El precio de cierre está por encima del cierre máximo de las siete barras anteriores.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `MaPeriod` = 200
  - `LowestLength` = 3
  - `HighestLength` = 7
  - `UseEmaFilter` = false
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Long
  - Indicadores: EMA, Highest/Lowest
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
