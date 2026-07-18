# Estrategia de Reversión por F-Score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina los fundamentos del Piotroski F-Score con la reversión de precio a corto plazo. Cada mes compra la acción de peor desempeño entre aquellas con F-Score alto y, opcionalmente, vende en corto la de mejor desempeño con F-Score bajo. La premisa es que las empresas fundamentalmente sólidas se recuperan tras caídas temporales, mientras que las empresas débiles revierten tras sus repuntes.

En el primer día hábil del mes, el algoritmo clasifica el universo por rendimiento de un mes. Va largo en el valor con menor rendimiento con `FScore >= FHi` y, si está disponible, va corto en el valor con mayor rendimiento con `FScore <= FLo`. Las posiciones se mantienen durante un mes.

## Detalles

- **Criterios de entrada**:
  - Largo: entre los valores con `FScore >= FHi`, comprar el de menor rendimiento `Lookback` si el tamaño de la operación >= `MinTradeUsd`.
  - Corto (opcional): entre los valores con `FScore <= FLo`, vender en corto el de mayor rendimiento `Lookback`.
- **Largo/Corto**: Largo y corto.
- **Criterios de salida**: Cerrar todas las posiciones en el siguiente rebalanceo mensual.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Universe` – valores a evaluar.
  - `Lookback` = 21 días.
  - `FHi` = 7.
  - `FLo` = 3.
  - `CandleType` = 1 día.
  - `MinTradeUsd` – valor mínimo de operación.
- **Filtros**:
  - Categoría: Reversión a la media.
  - Dirección: Largo y corto.
  - Marco temporal: Corto plazo.
  - Rebalanceo: Mensual.

