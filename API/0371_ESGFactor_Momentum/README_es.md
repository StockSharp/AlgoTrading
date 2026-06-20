# Estrategia de Momentum del Factor ESG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia rota entre un universo de valores puntuados por métricas ambientales, sociales y de gobernanza. Al inicio de cada mes clasifica todos los símbolos por su rendimiento acumulado y solo mantiene el de mayor desempeño. La premisa es que los activos que atraen capital ESG tienden a mantener el momentum. Para evitar una rotación excesiva, el algoritmo solo opera cuando el valor de la posición supera un umbral mínimo en dólares.

Durante el rebalanceo, el sistema cierra cualquier posición existente y reasigna capital al valor de mayor momentum. La cartera nunca usa apalancamiento ni posiciones cortas; está totalmente invertida en un único activo seleccionado por la fuerza del momentum.

## Detalles

- **Criterios de entrada**:
  - En el primer día hábil del mes, calcular el rendimiento total durante `LookbackDays` para cada valor.
  - Comprar el valor con mayor rendimiento si el tamaño de la orden es al menos `MinTradeUsd`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Todas las posiciones se cierran en cada rebalanceo mensual antes de abrir la nueva posición.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Universe` – lista de símbolos centrados en ESG.
  - `LookbackDays` = 252.
  - `CandleType` = 1 día.
  - `MinTradeUsd` – valor mínimo de operación.
- **Filtros**:
  - Categoría: Momentum.
  - Dirección: Solo largos.
  - Marco temporal: Medio plazo.
  - Rebalanceo: Mensual.

