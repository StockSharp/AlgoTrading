# Estrategia de Divergencia RSI en Oro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Divergencia RSI en Oro opera en scalping sobre el oro identificando divergencias alcistas y bajistas entre el precio y el Índice de Fuerza Relativa (RSI).
Cuando el precio marca un nuevo mínimo pero el RSI imprime un mínimo más alto, la estrategia busca comprar.
Por el contrario, cuando el precio marca un nuevo máximo pero el RSI imprime un máximo más bajo, la estrategia vende.
Ambas configuraciones se confirman solo si dos pivotes ocurren dentro de un rango de barras configurable.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Precio con mínimo más bajo, RSI con mínimo más alto, RSI < 40.
  - **Corto**: Precio con máximo más alto, RSI con máximo más bajo, RSI > 60.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Utiliza stop loss y take profit.
- **Stops**: Stop loss y take profit fijos en pips.
- **Valores predeterminados**:
  - `RsiLength` = 60
  - `StopLossPips` = 11
  - `TakeProfitPips` = 33
- **Filtros**:
  - Categoría: Divergencia
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
