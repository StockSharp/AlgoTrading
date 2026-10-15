# Estrategia Martin - Sin Pérdida en Salida V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de promediado martingala añade a una posición larga cuando el precio cae un porcentaje configurado desde la primera entrada. Cada nueva orden aumenta el importe en efectivo por un multiplicador y ajusta el precio promedio. La posición se cierra cuando el máximo de la vela alcanza el precio promedio más el porcentaje de take profit, garantizando salidas solo con beneficio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Plano` → comprar por `Initial Cash`
  - **Añadir**: `Price <= EntryPrice * (1 - PriceStep% * orderCount)` && `orderCount < MaxOrders`
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - `High >= AvgPrice * (1 + TakeProfit%)`
- **Stops**: No
- **Valores predeterminados**:
  - `Initial Cash` = 100
  - `Max Orders` = 20
  - `Price Step %` = 1.5
  - `Take Profit %` = 1
  - `Increase Factor` = 1.05
- **Filtros**:
  - Categoría: Promediado a la baja
  - Dirección: Solo largos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
