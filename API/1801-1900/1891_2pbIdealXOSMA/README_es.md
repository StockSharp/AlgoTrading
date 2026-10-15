# Estrategia 2pbIdeal XOSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una traducción a C# del asesor experto MQL5 **Exp_2pbIdealXOSMA**. Analiza la pendiente del histograma MACD para determinar el impulso del mercado. Cuando el histograma sube durante dos barras consecutivas, el sistema entra en una posición larga y cierra cualquier corto abierto. Cuando el histograma baja durante dos barras consecutivas, la estrategia entra en una posición corta y cierra cualquier largo abierto.

Por defecto, el algoritmo opera en velas de 4 horas, pero el marco temporal es configurable. Todas las operaciones se ejecutan a precio de mercado y la posición se revierte cuando aparece la señal contraria. No se aplica stop-loss ni take-profit dentro del ejemplo; el control de riesgo puede añadirse externamente si se desea.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El histograma en la barra `t-1` está por debajo de `t-2` y el histograma actual supera a `t-1`.
  - **Corto**: El histograma en la barra `t-1` está por encima de `t-2` y el histograma actual está por debajo de `t-1`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: La señal opuesta cierra la posición actual.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `FastPeriod` = 10
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `SignalBar` = 1
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único (MACD)
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: 4 horas (configurable)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
