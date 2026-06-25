# Estrategia Super Simple RSI Engulfing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto original SSEATwRSI de MetaTrader en StockSharp. Monitorea velas completadas y calcula un RSI de 7 períodos sobre el máximo de las velas. Una operación se activa solo cuando el RSI alcanza un extremo y las dos barras anteriores forman una reversión de engullimiento limpia.

Una configuración larga requiere que el RSI suba por encima del umbral de sobrecompra mientras una vela bajista es completamente engullida por la siguiente vela alcista. Una configuración corta espeja esta lógica usando una lectura de RSI sobrevendida y un patrón de engullimiento alcista-a-bajista. El tamaño de posición está fijado por el parámetro `Volume`, pero cualquier exposición opuesta es aplanada antes de abrir una nueva operación.

Una vez en el mercado, la estrategia sigue vigilando la ganancia y pérdida global. Si el PnL flotante alcanza el objetivo de ganancia configurado (en moneda de cuenta) o cae por debajo de la pérdida permitida, cierra toda la posición. No hay stops de seguimiento adicionales; las operaciones se gestionan únicamente por la reversión del patrón y los umbrales a nivel de cuenta.

## Detalles

- **Criterios de entrada**:
  - **Largo**: RSI en máximos > `OverboughtLevel` y la última vela engulle una barra bajista de hace dos barras mientras el precio cierra por encima de esa apertura más antigua.
  - **Corto**: RSI en máximos < `OversoldLevel` y la última vela engulle una barra alcista de hace dos barras mientras el precio cierra por debajo de esa apertura más antigua.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - PnL de cuenta ≥ `ProfitGoal` → aplanar.
  - PnL de cuenta ≤ `-MaxLoss` → aplanar.
  - La señal opuesta compensa automáticamente la posición anterior cuando se coloca una nueva orden.
- **Stops**: Verificaciones de take-profit y pérdida máxima basadas en moneda derivadas del PnL total de la estrategia.
- **Filtros**:
  - RSI calculado sobre el máximo de la vela para enfatizar los movimientos de agotamiento.
  - Confirmación mediante una reversión de engullimiento de dos barras.

## Parámetros

- `Volume` = 0.1 – Tamaño de orden en contratos. La exposición existente se compensa antes de abrir una nueva operación.
- `ProfitGoal` = 190 – Objetivo de ganancia en moneda que fuerza una posición plana una vez alcanzado.
- `MaxLoss` = 10 – Pérdida máxima permitida en moneda antes de que la estrategia cierre todas las posiciones. La verificación usa `-MaxLoss` internamente.
- `RsiPeriod` = 7 – Longitud de promedio del indicador RSI.
- `RsiPrice` = High – Fuente de precio utilizada para el cálculo del RSI.
- `OverboughtLevel` = 88 – Nivel de RSI que debe superarse antes de tomar una reversión larga.
- `OversoldLevel` = 37 – Nivel de RSI que debe ser superado a la baja antes de tomar una reversión corta.
- `CandleType` = velas de 1 hora por defecto; ajustar para que coincida con el marco temporal del gráfico original.
