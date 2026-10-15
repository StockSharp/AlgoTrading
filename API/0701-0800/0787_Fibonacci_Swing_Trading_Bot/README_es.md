# Bot de Swing Trading Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza niveles de retroceso Fibonacci para operar movimientos de swing.

Este bot calcula los niveles de retroceso 0.618 y 0.786 del rango de las últimas 50 barras y abre posiciones cuando las velas rompen por encima o por debajo de estos niveles. La gestión de riesgo se realiza mediante parámetros configurables de stop loss y riesgo/beneficio.

## Detalles

- **Criterios de entrada**: Acción del precio con niveles Fibonacci.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss o take profit.
- **Stops**: Sí, basado en porcentaje.
- **Valores predeterminados**:
  - `FiboLevel1` = 0.618
  - `FiboLevel2` = 0.786
  - `RiskRewardRatio` = 2
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Swing
  - Dirección: Ambos
  - Indicadores: Fibonacci, Donchian
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: 4h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

