# Estrategia mínima de Frank Ud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este ejemplo traslada el asesor experto clásico **Frank Ud** MetaTrader a StockSharp utilizando la estrategia de alto nivel API. El script original MQL ejecuta una cuadrícula de martingala cubierta que sigue agregando posiciones cada vez que el precio se mueve con respecto a la última entrada. Las ganancias se bloquean una vez que la orden más reciente (y por lo tanto la más grande) gana una cantidad fija de pips, después de lo cual *todas* las operaciones de ese lado se cierran simultáneamente.

## Lógica central

1. **Cobertura simétrica.** La estrategia mantiene dos escaleras independientes de posiciones de mercado: una escalera larga y una escalera corta. Por lo tanto, es posible mantener posiciones largas y cortas al mismo tiempo, como en el modo de cobertura de MetaTrader.
2. **Martingale progresión.** El primer pedido en cualquier lado usa `InitialVolume` (por defecto, 0,1 lotes). Cada entrada posterior en el mismo lado duplica el mayor volumen abierto actualmente. Los ajustes de volumen respetan las restricciones `MinVolume`, `MaxVolume` y `VolumeStep` del instrumento.
3. **Espaciado de entrada.** Se agrega una nueva posición solo cuando el precio se ha movido al menos `ReEntryPips` (predeterminado 41 pips) más allá del mejor precio de entrada de la escalera existente. La escalera larga espera a que los precios de venta caigan por debajo de `lowest_buy - ReEntryPips`, mientras que la escalera corta espera a que los precios de oferta suban por encima de `highest_sell + ReEntryPips`.
4. **Recolección de ganancias.** Para cada escalera, la operación con el mayor volumen actúa como la orden "desencadenante". Cuando su beneficio excede `TakeProfitPips` (65 pips predeterminado), o cuando el precio toca el nivel de obtención de beneficios implícito `(TakeProfitPips + 25)` utilizado por la versión MQL, cada posición en ese lado se aplana con una única orden de mercado.
5. **Protección de margen.** Antes de enviar cualquier entrada nueva, la estrategia verifica que el margen libre informado por la cartera (`CurrentValue - BlockedValue`) se mantenga por encima de `Balance × MinimumFreeMarginRatio` (predeterminado 0,5). Si el corredor no informa las estadísticas de la cartera, la verificación vuelve al comportamiento de volumen fijo del experto original.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPips` | Umbral de beneficio de pip medido en el pedido más grande y más reciente. Una vez superado, se cierran todas las posiciones de ese lado. |
| `ReEntryPips` | Distancia mínima de pips entre la mejor entrada existente y la oferta/demanda actual antes de que se agregue una nueva orden de martingala. |
| `InitialVolume` | Tamaño de lote base para el primer pedido de cada escalera. Los pedidos posteriores duplican el mayor volumen activo. |
| `MinimumFreeMarginRatio` | Relación requerida entre margen libre y saldo antes de que se permitan nuevas entradas. Establezca en 0 para desactivar la verificación. |

## Notas de implementación

- La estrategia se basa únicamente en cotizaciones de nivel 1: las actualizaciones de ofertas impulsan la lógica de escalera corta y las actualizaciones de solicitudes impulsan la lógica de escalera larga.
- Las intenciones de los pedidos se rastrean en un diccionario interno para que `OnNewMyTrade` sepa si un llenado abrió o cerró una escalera. Esto imita la contabilidad de boletos explícita en la fuente MQL.
- La contabilidad de posiciones almacena cada llenado (precio y volumen) en listas en lugar de consultar estadísticas acumulativas, preservando el comportamiento de las matrices MQL que se utilizaron para localizar el lote más grande y su precio de entrada.
- El buffer adicional de 25 pips que el experto original colocó en cada orden de toma de ganancias se retiene como condición de salida adicional.

> **Nota:** El puerto Python se omite intencionalmente por ahora, según lo solicitado. La carpeta contiene sólo la implementación de C# y la documentación multilingüe.
