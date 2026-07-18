# Estrategia de Spread Negativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Negative Spread aprovecha los momentos excepcionales en que el mejor precio de venta cae por debajo del mejor precio de compra, creando un spread negativo.
Cuando aparece este desequilibrio de precios, la estrategia vende al mercado e intenta capturar el spread anormal.
Después de abrir la posición corta, se cierra en la siguiente actualización del libro de órdenes cuando el mercado vuelve a un estado normal.

El sistema escucha únicamente eventos del libro de órdenes y no depende de velas ni indicadores.
Se proporcionan parámetros opcionales de stop-loss y take-profit como medidas de seguridad, calculados en pips usando el tamaño del tick del instrumento.

## Detalles
- **Criterios de entrada**: `BestAsk < BestBid` y sin posición activa.
- **Largo/Corto**: Solo corto.
- **Criterios de salida**: La posición se cierra inmediatamente después de abrirse.
- **Stops**: Stop-loss y take-profit opcionales en pips.
- **Valores predeterminados**:
  - `Volume` = 1
  - `TakeProfitPips` = 5000
  - `StopLossPips` = 5000
- **Filtros**:
  - Categoría: Arbitraje
  - Dirección: Corto
  - Indicadores: Ninguno
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Tick
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
