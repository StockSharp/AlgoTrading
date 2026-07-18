# Bot para Mercado Spot - Estrategia de Cuadrícula Personalizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Bot para Mercado Spot - Cuadrícula Personalizada compra una posición inicial y agrega nuevas órdenes cuando el precio cae un porcentaje especificado por debajo del último punto de entrada. Cierra todas las posiciones cuando el precio sube por encima del precio de entrada promedio en el objetivo de ganancia.

## Detalles

- **Criterios de entrada**:
  - Comprar en el momento de inicio.
  - Comprar cantidad adicional cuando el precio cae `NextEntryPercent`% por debajo del último punto de entrada.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Cerrar todas las posiciones cuando el precio supera el precio de entrada promedio en `ProfitPercent`% y la posición abierta es rentable.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `OrderValue` = 10
  - `MinAmountMovement` = 0.00001
  - `Rounding` = 5
  - `NextEntryPercent` = 0.5
  - `ProfitPercent` = 2
- **Filtros**:
  - Categoría: Grid trading
  - Dirección: Largo
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
