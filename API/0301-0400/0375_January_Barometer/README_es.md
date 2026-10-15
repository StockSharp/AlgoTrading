# Estrategia del Barómetro de Enero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Barómetro de Enero establece que el comportamiento del mercado en enero marca la pauta para el resto del año. Esta estrategia invierte en un ETF de renta variable durante el resto del año únicamente si enero cierra en positivo; de lo contrario, permanece en un proxy de efectivo. La decisión de asignación se toma una vez al año y se mantiene hasta final de año.

En el primer día hábil de febrero, el algoritmo mide el rendimiento total del ETF de renta variable durante enero. Si el rendimiento es positivo y el valor de la orden supera el umbral mínimo, compra el ETF de renta variable y lo mantiene hasta diciembre. Si enero fue negativo, mantiene el ETF de efectivo en su lugar. El proceso se repite cada año.

## Detalles

- **Criterios de entrada**:
  - En el primer día hábil de febrero, calcular el rendimiento total de enero de `EquityETF`.
  - Comprar `EquityETF` si el rendimiento es positivo y el tamaño de la orden >= `MinTradeUsd`; de lo contrario, mantener `CashETF`.
- **Largo/Corto**: Solo largos en renta variable o efectivo.
- **Criterios de salida**: Cerrar la posición en renta variable en el último día hábil del año.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `EquityETF` – ETF que representa el mercado de renta variable.
  - `CashETF` – ETF proxy de efectivo.
  - `CandleType` = 1 día.
  - `MinTradeUsd` – valor mínimo de operación.
- **Filtros**:
  - Categoría: Estacional.
  - Dirección: Solo largos.
  - Marco temporal: Largo plazo.
  - Rebalanceo: Anual.

