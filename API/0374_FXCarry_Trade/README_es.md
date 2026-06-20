# Estrategia de Carry Trade en FX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de divisas clasifica un universo de instrumentos de divisas según el diferencial de tasas de interés entre la moneda base y la moneda cotizada. Al inicio de cada mes, toma posiciones largas en los `TopK` símbolos de mayor carry y posiciones cortas en los `TopK` de menor carry. Las ganancias buscan capturar el carry positivo en los largos mientras se paga el carry negativo en los cortos.

Los diferenciales de tasas de interés se obtienen de los datos de rendimiento de cada valor. Las posiciones se dimensionan de forma equitativa y se rebalancean mensualmente; cualquier instrumento que salga de los grupos superior o inferior se cierra y se reemplaza.

## Detalles

- **Criterios de entrada**:
  - En el primer día hábil del mes, calcular el diferencial de tasas de interés para cada divisa.
  - Tomar posiciones largas en las `TopK` divisas con mayor carry y cortas en las `TopK` con menor carry si los valores de las órdenes superan `MinTradeUsd`.
- **Largo/Corto**: Largo en carry alto, corto en carry bajo.
- **Criterios de salida**: Las posiciones se cierran cuando una divisa sale de los grupos seleccionados en el siguiente rebalanceo.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Universe` – lista de instrumentos de divisas.
  - `TopK` = 3.
  - `CandleType` = 1 día.
  - `MinTradeUsd` – valor mínimo de operación.
- **Filtros**:
  - Categoría: Carry.
  - Dirección: Largo y corto.
  - Marco temporal: Mensual.
  - Rebalanceo: Mensual.

