# Estrategia de dirección de Nevalyashka
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Nevalyashka es una adaptación de C# del asesor experto original MetaTrader 4 `Nevalyashka.mq4`. El EA invierte repetidamente su dirección de negociación: abre una única orden de mercado, espera hasta que la posición se cierra mediante una acción manual, de limitación de pérdidas o de obtención de beneficios, y vuelve a entrar instantáneamente en la dirección opuesta con el mismo volumen. La implementación StockSharp reproduce este comportamiento al tiempo que expone todas las configuraciones críticas como parámetros de estrategia.

## Lógica de trading
1. **Inicialización**
   - Cuando comienza la estrategia, calcula el tamaño del pip a partir del `PriceStep` del instrumento. Para símbolos Forex de 3 y 5 dígitos, el paso se multiplica por 10 para que coincida con la definición de punto MetaTrader.
   - `StartProtection` está configurado con distancias de stop-loss y take-profit convertidas de pips a puntos de precio. Se adjuntan órdenes de protección a cada puesto posterior.
   - Se envía una orden de mercado inicial en la dirección definida por `InitialDirection` (predeterminado: corto). El volumen solicitado se redondea al lote válido más cercano utilizando los valores `VolumeStep`, `MinVolume` y `MaxVolume` del valor.

2. **Seguimiento de posición**
   - `OnPositionChanged` captura cada cambio en la exposición neta. Cuando se abre una nueva posición, la estrategia almacena el volumen completado y recuerda el lado comercial.
   - Una vez que la posición vuelve completamente plana, la estrategia emite inmediatamente una nueva orden de mercado en la dirección opuesta, reutilizando el tamaño de lote previamente almacenado.

3. **Manejo de fallas**
   - Si el corredor rechaza el registro de una orden, el indicador de dirección pendiente se borra, lo que permite al operador de la plataforma volver a intentarlo manualmente o ajustar los parámetros sin un estado interno obsoleto.

El flujo de trabajo resultante refleja la idea "roly-poly" del guión original: el robot está siempre en el mercado, alternando entre posiciones largas y cortas con salidas fijas.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `StopLossPips` | Distancia del tope de protección en pips. | `50` | Convertido a puntos de precio mediante el cálculo del tamaño del pip; configúrelo en `0` para desactivar la parada. |
| `TakeProfitPips` | Distancia de la toma de ganancias protectora en pips. | `50` | Se convierte de la misma forma que el stop-loss; configúrelo en `0` para deshabilitar la obtención de ganancias. |
| `Volume` | Tamaño de lote utilizado para la primera operación. | `1` | Después del primer llenado, la estrategia reutiliza el volumen realmente ejecutado para todas las entradas futuras. |
| `InitialDirection` | Lado de la orden de mercado inicial. | `Sell` | Elija entre `Buy` y `Sell` para que coincida con el sesgo inicial deseado. |

## Notas de implementación
- No se requieren suscripciones a velas ni indicadores; la estrategia reacciona únicamente a eventos de posición y confirmaciones de pedidos.
- Se consulta `IsFormedAndOnlineAndAllowTrading()` antes de cada entrada para garantizar que el conector esté listo para operar.
- El redondeo de volumen utiliza `MidpointRounding.AwayFromZero` para que los lotes fraccionarios siempre alcancen un nivel negociable en lugar de cero.
- La lógica de conversión de pips se basa en metadatos del instrumento en lugar de suposiciones codificadas, lo que hace que el puerto funcione con símbolos FX, CFD o futuros con diferentes formatos de precios.

## Diferencias frente a la versión MQL
- La variante StockSharp expone la dirección inicial como parámetro en lugar de forzar el corto inicial desde el script MT4.
- Las órdenes de limitación de pérdidas y toma de ganancias se gestionan a través de `StartProtection`, que produce órdenes de protección nativas compatibles con cualquier conector StockSharp.
- Los rechazos de pedidos borran el estado pendiente interno para evitar el envío repetido de solicitudes no válidas.

Estos ajustes mantienen el espíritu del asesor original y al mismo tiempo se integran perfectamente con la API de alto nivel de StockSharp.
