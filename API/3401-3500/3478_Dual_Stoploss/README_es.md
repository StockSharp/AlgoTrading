# Estrategia doble de stoploss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el comportamiento del experto MetaTrader **Dual StopLoss.mq4**. Actúa como una capa de gestión de riesgos: monitorea las órdenes protectoras de stop-loss adjuntas a las posiciones abiertas y cierra esas posiciones unos pocos puntos antes de que se active el stop. La salida anticipada está diseñada para evitar un deslizamiento negativo en movimientos altamente volátiles y al mismo tiempo respetar la colocación del stop inicial del operador.

## como funciona

1. La estrategia se suscribe a los datos de Level1 para realizar un seguimiento de la mejor oferta/demanda actual y la distancia `StopLevel` (o equivalente) publicada por el corredor.
2. Cada vez que llegan nuevos precios o cambian órdenes/negocios, busca la orden de parada activa más cercana que pertenece al valor administrado.
3. La distancia entre el precio de mercado y ese stop de protección se compara con un umbral configurable:
   - Umbral = `WhenToClosePoints × pointValue + stopLevelDistance`.
   - `pointValue` coincide con el `Point` de MetaTrader (0,0001 para la mayoría de los pares de divisas, detectado automáticamente desde la configuración de seguridad).
   - `stopLevelDistance` proviene de los campos de Nivel 1 (`StopLevel`, `MinStopPrice`, `StopPrice` o `StopDistance`) cuando estén disponibles; de lo contrario, cero.
4. Cuando la distancia restante es menor o igual que el umbral, la posición se cierra inmediatamente mediante una orden de mercado.

La lógica cubre tanto posiciones largas como cortas. Para posiciones largas, la mejor oferta se compara con el precio de venta; para posiciones cortas, la mejor demanda se compara con el precio stop de compra. Sólo se consideran las órdenes stop y stop-limit en estado activo.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **CuandoCerrarPuntos** | Distancia (en MetaTrader puntos) desde el nivel de parada que debería activar la salida anticipada. Valor predeterminado: 10. Establezca en cero para confiar únicamente en la distancia mínima del nivel de parada del corredor. |

## Notas y limitaciones

- La estrategia **no** abre posiciones por sí sola; solo gestiona posiciones que ya existen y tienen órdenes de parada de protección.
- Asegúrese de que el conector/corredor subyacente proporcione valores de nivel de parada a través de datos de Nivel1 si desea tener en cuenta las distancias mínimas impuestas por el corredor. Si falta esa información, la estrategia aún funciona utilizando solo la distancia del punto configurada.
- La llamada `StartProtection()` habilita las guardas de seguridad integradas en StockSharp para que las salidas de emergencia permanezcan activas una vez iniciada la estrategia.
- Las paradas se detectan a partir de la colección `Orders` de la estrategia. Asegúrese de que las paradas de protección estén registradas a través de la misma instancia de estrategia para que aparezcan en esta lista.
- Cuando existen varias órdenes stop para la misma dirección, se utiliza la más cercana al mercado.

## Consejos de uso

1. Adjunte la estrategia a una cartera/valor donde las posiciones se abren manualmente o mediante otro sistema, pero las paradas de protección se colocan bajo el mismo contexto de estrategia.
2. Configure `WhenToClosePoints` para que coincida con la cantidad de protección que necesita antes de la parada. Este valor se interpreta exactamente igual que en MetaTrader (puntos, no unidades de precio).
3. Inicie la estrategia y supervise el registro. Cuando el precio de mercado se acerca al tope, la estrategia emitirá una orden de mercado para cerrar la posición de forma proactiva.
4. Combine este módulo con otras estrategias de entrada o de tamaño de posición para crear un flujo de trabajo comercial completo.
