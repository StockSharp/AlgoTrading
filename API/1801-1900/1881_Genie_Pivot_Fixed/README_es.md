# Estrategia Genie Pivot Fijo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el sistema de scalping de reversión en puntos pivote "Genie" originalmente escrito en MQL4. Escanea las últimas ocho velas para detectar reversiones bruscas en puntos pivote. Una operación larga se activa cuando siete mínimos consecutivos disminuyen y la vela actual forma un mínimo más alto mientras cierra por encima del máximo anterior. Una operación corta se activa cuando siete máximos consecutivos aumentan y la vela actual forma un máximo más bajo mientras cierra por debajo del mínimo anterior.

La estrategia usa un tamaño de posición fijo (Strategy.Volume) y aplica tanto un stop trailing como un take profit medidos en unidades de precio absolutas. Estos parámetros pueden optimizarse y permiten capturar reversiones rápidas mientras se protegen las ganancias abiertas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Low[7] > Low[6] > ... > Low[1]` && `Low[1] < Low[0]` && `High[1] < Close[0]`.
  - **Corto**: `High[7] < High[6] < ... < High[1]` && `High[1] > High[0]` && `Low[1] > Close[0]`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - El stop trailing o el take profit se alcanza.
- **Stops**:
  - Take-profit: distancia absoluta desde la entrada.
  - Stop trailing: distancia absoluta, que sigue a medida que la operación se mueve a favor.
- **Valores predeterminados**:
  - `TakeProfit` = 500.
  - `TrailingStop` = 200.
  - `CandleType` = 1 minuto.
- **Filtros**:
  - Categoría: Reversión.
  - Dirección: Ambos.
  - Indicadores: Ninguno.
  - Stops: Sí.
  - Complejidad: Simple.
  - Marco temporal: Corto plazo.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Medio.
