# Stop Trailing Básico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Basic Trailing Stop combina filtros del Commodity Channel Index (CCI) y del Relative Strength Index (RSI) con un stop trailing simple. Cuando ambos indicadores señalan condiciones de sobreventa o sobrecompra, la estrategia abre una posición de mercado y coloca inmediatamente un stop trailing medido en pips. A medida que el precio se mueve favorablemente, el nivel del stop sigue la tendencia para asegurar ganancias.

Las pruebas indican un rendimiento anual medio de aproximadamente el 32%. Funciona mejor en el mercado de divisas.

Dado que el nivel del stop sigue continuamente al precio, el riesgo se ajusta automáticamente cuando se extiende la tendencia. Las salidas ocurren solo si se alcanza el stop trailing. El sistema mantiene una posición a la vez y puede operar en ambas direcciones.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `CCI` entre -150 y -100 y `RSI` entre 0 y 30.
  - **Corto**: `CCI` entre 100 y 250 y `RSI` entre 70 y 100.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop trailing alcanzado.
- **Stops**: Solo stop trailing.
- **Valores predeterminados**:
  - `StopLossPips` = 20
  - `CciPeriod` = 14
  - `RsiPeriod` = 14
  - `CandleType` = `TimeSpan.FromMinutes(1)`
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: CCI, RSI
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
