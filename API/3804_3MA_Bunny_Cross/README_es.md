# Estrategia cruzada de conejitos 3MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
**ThreeMaBunnyCrossStrategy** es una conversión del MetaTrader 4 asesor experto "3MA Bunny Cross". Negocia cambios de tendencia basados ​​en el cruce entre dos promedios móviles ponderados lineales (LWMA) calculados sobre los precios de cierre del período de tiempo seleccionado. La versión StockSharp mantiene la idea original de revertir la posición inmediatamente después de un cruce y agrega comodidades API de alto nivel, como vinculación de indicadores y protección contra riesgos incorporada.

## Original MQL Descripción
El asesor experto fuente utiliza dos LWMA con períodos 5 y 20. Cuando la LWMA rápida cruza la LWMA lenta, el asesor cierra la posición opuesta, si existe, e inmediatamente abre una nueva operación en la dirección del cruce. Sólo se permite una posición en cualquier momento. El script original también verifica un número mínimo de barras y margen libre antes de operar.

## StockSharp Detalles de implementación
- La estrategia se suscribe a velas definidas por el parámetro `CandleType` (período de tiempo de 15 minutos por defecto) y las vincula a dos indicadores `LinearWeightedMovingAverage`.
- Los valores del indicador se proporcionan directamente al método de procesamiento a través de `Bind`, lo que elimina la necesidad de manejo manual del búfer.
- Los valores rápidos y lentos anteriores se almacenan en caché para detectar cruces usando la misma lógica que la versión MQL (`fast` cruzando por encima o por debajo de `slow`).
- Cuando se produce un cruce alcista y la posición actual es plana o corta, la estrategia envía una orden de compra de mercado del tamaño necesario para cerrar cualquier exposición corta y abrir una nueva larga (`Volume + |Position|`). El cruce bajista se comporta simétricamente para las ventas.
- `StartProtection()` se llama una vez al inicio para habilitar las rutinas de protección de posición integradas.
- La visualización del gráfico muestra las velas fuente junto con las dos medias móviles y las operaciones propias de la estrategia.

## Parámetros
- **CandleType**: tipo de datos que describe la serie de velas a la que suscribirse (el valor predeterminado es un período de tiempo de 15 minutos).
- **FastPeriod** – período de la LWMA rápida. Valor predeterminado: 5. Optimizable.
- **SlowPeriod** – período de la LWMA lenta. Predeterminado: 20. Optimizable.

## Indicadores
- `LinearWeightedMovingAverage` (rápido, período 5 por defecto).
- `LinearWeightedMovingAverage` (lento, período 20 por defecto).

## Reglas de trading
1. Espere a que termine una vela y verifique que la estrategia esté formada, en línea y permitida para operar.
2. Detecte un cruce alcista cuando la LWMA rápida estaba por debajo o igual a la LWMA lenta en la vela anterior y está por encima o igual a ella en la vela actual. En este caso, cierre cualquier posición corta existente y abra una larga.
3. Detecte un cruce bajista cuando la LWMA rápida estaba por encima o igual a la LWMA lenta en la vela anterior y está por debajo o igual a ella en la vela actual. En este caso, cierre cualquier posición larga existente y abra una corta.
4. Cada nuevo tamaño de orden se calcula como `Volume + |Position|` para revertir completamente cualquier exposición pendiente, garantizando que solo exista una posición direccional a la vez.
