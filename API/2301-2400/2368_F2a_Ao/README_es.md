# Estrategia F2a AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto original de MetaTrader "F2a_AO". Filtra el Awesome Oscillator con una SMA corta y abre operaciones solo en la dirección de una vela de referencia en un marco temporal superior.

El oscilador se calcula en su propio marco temporal. Cuando la vela de referencia cierra por encima de su apertura, un AO filtrado positivo desencadena una entrada larga y cierra cualquier posición corta. Cuando la vela de referencia cierra por debajo de su apertura, un AO filtrado negativo desencadena una entrada corta y cierra cualquier posición larga.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La vela de referencia es alcista y el AO filtrado > 0.
  - **Corto**: La vela de referencia es bajista y el AO filtrado < 0.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - AO filtrado < 0 cierra posiciones largas.
  - AO filtrado > 0 cierra posiciones cortas.
- **Stops**: Sin stop-loss o take-profit explícito, el módulo de protección está habilitado.
- **Valores predeterminados**:
  - `IndicatorTimeFrame` = 12 horas.
  - `TrendTimeFrame` = 1 día.
  - `FastPeriod` = 13.
  - `SlowPeriod` = 144.
  - `FilterLength` = 3.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Awesome Oscillator, SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
