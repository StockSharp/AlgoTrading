# Estrategia del Modelo Fed
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este sistema de timing macroeconómico compara el rendimiento de ganancias del mercado de renta variable con el rendimiento de los bonos del Tesoro a 10 años. Cuando las acciones ofrecen mayor rendimiento, la estrategia mantiene un ETF de renta variable; cuando los bonos rinden más, pasa a efectivo. Una regresión mensual sobre la diferencia de rendimientos pronostica el valor del próximo mes para reducir los cambios ruidosos.

Al final de cada mes, el algoritmo pronostica el diferencial de rendimiento del mes siguiente usando el último año de datos. Si la previsión es positiva, compra renta variable; de lo contrario, mantiene el proxy de efectivo. Las posiciones cambian solo cuando el pronóstico cruza cero, minimizando la rotación.

## Detalles

- **Criterios de entrada**:
  - Al final del mes, realizar una regresión sobre las últimas `RegressionMonths` observaciones de `(EarningsYield - BondYield)` y pronosticar el siguiente valor.
  - Comprar el ETF de renta variable cuando el pronóstico esté por encima de cero y la orden cumpla con `MinTradeUsd`.
- **Largo/Corto**: Solo largos en renta variable o efectivo.
- **Criterios de salida**: Salir de la posición en renta variable cuando el diferencial de rendimiento pronosticado se vuelva negativo.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Universe` – [ETF de renta variable, ETF de efectivo opcional].
  - `BondYieldSym` – serie de rendimientos del Tesoro a 10 años.
  - `EarningsYieldSym` – rendimiento de ganancias del mercado de renta variable.
  - `RegressionMonths` = 12.
  - `CandleType` = 1 día.
  - `MinTradeUsd` – valor mínimo de operación.
- **Filtros**:
  - Categoría: Macro.
  - Dirección: Solo largos.
  - Marco temporal: Mensual.
  - Rebalanceo: Mensual.

