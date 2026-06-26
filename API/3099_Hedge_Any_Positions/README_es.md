# Estrategia de Cobertura de Cualquier Posición
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Cobertura de Cualquier Posición** es una conversión directa del expert MQL5 original *Hedge any positions (barabashkakvn's edition)*. La versión de StockSharp mantiene la idea central intacta: monitorea cada pata abierta creada por la estrategia y, una vez que una pata pierde un número definido de pips, inmediatamente abre una posición opuesta con un tamaño de lote amplificado. La implementación se basa en la API de alto nivel de StockSharp, por lo que las órdenes de hedge se colocan mediante órdenes de mercado y el seguimiento de posiciones se maneja internamente sin código de enrutamiento de órdenes personalizado.

La estrategia puede opcionalmente colocar un trade inicial cuando comienza. Después simplemente reacciona a movimientos adversos de precio y construye una escalera de trades de cobertura, marcando cada pata como cubierta para que la misma posición no pueda desencadenar múltiples entradas opuestas.

## Flujo de trabajo de cobertura
1. **Feed de velas** – un `CandleType` configurable impulsa la estrategia. Solo se procesan velas terminadas.
2. **Cálculo de pérdidas** – en cada cierre de vela la estrategia verifica si el precio de cierre se movió contra cualquier pata abierta al menos `LosingPips` multiplicado por el tamaño de pip calculado.
3. **Ejecución del hedge** – si se encuentra una pata perdedora, se envía una orden de mercado en la dirección opuesta. El volumen de la orden equivale al volumen de la pata original multiplicado por `LotCoefficient`, redondeado al paso de volumen del instrumento y limitado al volumen mínimo/máximo permitido.
4. **Actualización de estado** – una vez que se despacha una orden opuesta, la pata original se marca como cubierta y el trade recién abierto se almacena como una nueva pata que a su vez puede ser cubierta más tarde si el precio se revierte nuevamente.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `CandleType` | Marco temporal usado para evaluar los movimientos de precio y desencadenar coberturas. | Velas de 1 minuto |
| `LosingPips` | Número de pips que el precio debe moverse contra una pata antes de abrir una cobertura. | 5 |
| `LotCoefficient` | Multiplicador aplicado al volumen original al enviar la orden de cobertura. | 2.0 |
| `AutoPlaceInitialTrade` | Cuando está habilitado la estrategia envía el primer trade automáticamente al iniciar. | Deshabilitado |
| `InitialVolume` | Tamaño de la orden usado por el trade inicial opcional. Redondeado al paso de volumen del instrumento. | 0.10 |
| `InitialDirection` | Lado (compra o venta) usado para el trade inicial opcional. | Compra |

> **Nota:** Establecer la propiedad `Strategy.Volume` al tamaño base de orden que se desea que use la estrategia. Los parámetros anteriores solo controlan el comportamiento específico de cobertura.

## Pautas de uso
1. Asignar un `Security`, `Portfolio` y `Volume` base deseado antes de iniciar la estrategia.
2. Ajustar `LosingPips` y `LotCoefficient` para reflejar la volatilidad y la tolerancia al riesgo del instrumento seleccionado.
3. Habilitar `AutoPlaceInitialTrade` si se desea que la versión de StockSharp cree la primera posición automáticamente; de lo contrario, abrir manualmente una pata inicial o dejar que otro componente lo haga.
4. Debido a que la API de alto nivel de StockSharp trabaja con posiciones netas, la lista de patas interna se usa para emular la estructura de hedge. Monitorear la exposición de la cuenta al ejecutar en cuentas de netting.
5. Revisar los reportes de ejecución: cada cobertura se coloca con una orden de mercado (`BuyMarket` o `SellMarket`).

## Diferencias del expert original
- La validación de margen, las comprobaciones de slippage y el registro detallado de resultados fueron eliminados; StockSharp ya reporta los problemas de ejecución a través de los eventos de estrategia.
- La conversión usa velas terminadas en lugar de datos tick a tick. Elegir un marco temporal suficientemente pequeño si se necesitan tiempos de reacción más rápidos.
- El redondeo de lotes ahora se basa en `Security.VolumeStep`, `Security.MinVolume` y `Security.MaxVolume` para mantenerse en conformidad con las reglas de trading del instrumento.
- Las alertas, notificaciones y el trade inicial aleatorio solo para el tester de la versión MQL fueron intencionalmente omitidos. El parámetro de entrada automática opcional reemplaza ese comportamiento.

## Mejoras recomendadas
- Combinar el módulo de cobertura con una estrategia de entrada separada que define cuándo debe crearse la primera posición.
- Agregar reglas de cierre basadas en capital o límites de profundidad máxima para prevenir cadenas de cobertura sin límite.
- Integrar monitoreo a nivel de portafolio para asegurar que los requerimientos de margen se mantengan dentro de límites aceptables.
