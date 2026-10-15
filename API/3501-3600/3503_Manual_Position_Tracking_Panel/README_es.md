# Estrategia del panel de seguimiento de posición manual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

El asesor experto original MQL5 proporcionó un panel de control visual que permitía al operador gestionar hasta cinco posiciones largas y cinco posiciones cortas manualmente. Los botones dentro del panel eliminaron los niveles de toma de ganancias existentes, recalcularon nuevos precios de toma de ganancias de la entrada o los movieron al punto de equilibrio para los boletos seleccionados. El puerto StockSharp automatiza estas acciones protectoras sin la interfaz visual. La estrategia monitorea la posición agregada del símbolo configurado y mantiene dinámicamente una orden protectora de toma de ganancias que refleja el flujo de trabajo del panel.

Pasos clave de automatización:

- Coloque una toma de ganancias al precio de entrada más/menos una distancia de pip configurable de MetaTrader cuando aparezca una posición.
- Opcionalmente, impulse la toma de ganancias al precio de entrada promedio una vez que el mercado se mueva en la dirección favorable en la cantidad solicitada de pips, bloqueando efectivamente una salida de equilibrio.
- Respete las distancias de congelación/detención de los corredores cuando se publiquen a través de datos de Nivel 1, o aproximarlas utilizando el diferencial actual y un multiplicador controlado por el usuario.
- Cancelar la orden de protección siempre que se deshabilite la gestión o se cierre la posición, manteniendo el comportamiento consistente con el botón "Eliminar TP" del panel.

La clase se basa exclusivamente en métodos StockSharp API de alto nivel (`SubscribeLevel1`, `SellLimit`, `BuyLimit`, `ReRegisterOrder`, etc.) y utiliza la normalización automática de volumen/precio para que pueda conectarse a cualquier instrumento compatible con el conector.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **Distancia de obtención de beneficios (pips)** | MetaTrader distancia de pips agregada al precio de entrada al crear la toma de ganancias protectora. |
| **Habilitar toma de ganancias basada en entradas** | Permite la colocación automática del take-profit derivado del precio de entrada. Cuando está deshabilitada, la estrategia solo reacciona a las solicitudes de equilibrio. |
| **Habilitar el punto de equilibrio** | Mueve la toma de ganancias nuevamente al precio de entrada promedio una vez que se satisface el umbral de rentabilidad. |
| **Gatillo de equilibrio (pips)** | Se requiere un movimiento favorable mínimo (en MetaTrader pips) antes de que se aplique el punto de equilibrio. Un valor de `0` lo aplica inmediatamente. |
| **Administrar posiciones largas** | Cuando se procesa `true` el lado largo de la posición agregada. |
| **Administrar posiciones cortas** | Cuando se procesa `true` el lado corto de la posición agregada. |
| **Eliminar toma de ganancias cuando esté deshabilitado** | Cancela la orden de protección si no se cumplen las condiciones de gestión (similar al botón Eliminar TP original). |
| **Acciones de gestión de registros** | Habilita el registro informativo para cada acción de creación, modificación o cancelación realizada por el algoritmo. |
| **Congelar multiplicador de distancia** | Multiplicador utilizado para aproximar las distancias de congelación/detención del diferencial actual cuando el intercambio no publica niveles explícitos. |

## Señales y reglas de ejecución.

1. Al iniciarse, la estrategia se suscribe a las actualizaciones de Nivel 1 para realizar un seguimiento de los mejores precios de oferta y demanda, además de los niveles opcionales de congelación y parada expuestos por la puerta de enlace.
2. Cada vez que aparece una nueva operación, la posición general cambia o llegan nuevos datos de nivel 1, la estrategia reevalúa la lógica de protección.
3. Si no hay ninguna posición abierta, se cancela cualquier orden de obtención de beneficios existente.
4. Si una posición está activa y el lado correspondiente está habilitado:
   - El objetivo base es el precio de entrada desplazado por la distancia de obtención de beneficios configurada (si está habilitada).
   - Cuando se habilita el punto de equilibrio y el precio de mercado actual se ha movido lo suficiente, el objetivo se fija en el precio de entrada promedio.
   - El objetivo se ajusta para respetar las distancias de congelación/parada comparándolo con la cotización actual del mercado.
   - El precio y el volumen se normalizan a través de `PriceStep`/`VolumeStep`, luego se registra o se vuelve a registrar una orden limitada en el lado opuesto.
5. Si la configuración deshabilita la administración para el lado detectado, la toma de ganancias existente se elimina cuando **Eliminar toma de ganancias cuando está deshabilitada** es `true`.

## Notas de gestión de riesgos

- El algoritmo sólo gestiona órdenes de toma de beneficios. Los niveles de stop-loss, la lógica de seguimiento o las salidas parciales están fuera de su alcance.
- Debido a que el panel original trabajaba con MetaTrader "pips" (puntos), la estrategia calcula el tamaño del pip automáticamente a partir de `PriceStep` y la precisión del instrumento para seguir siendo compatible con los símbolos de Forex.
- Las distancias de congelación/parada de nivel 1 se respetan cuando están disponibles. Si el corredor no los envía, el parámetro multiplicador permite al usuario crear un búfer de seguridad a partir del spread en vivo, evitando modificaciones rechazadas.
- La estrategia no crea nuevas entradas al mercado; está diseñado para conectarse a sistemas de negociación externos o discrecionales que ya gestionan la ejecución de órdenes.

## Consejos de uso

1. Adjunte la estrategia al instrumento que desea supervisar y asegúrese de que el conector proporcione información de Nivel 1.
2. Configure la distancia del pip para que coincida con el objetivo protector que utilizó anteriormente dentro de MetaTrader.
3. Habilite el módulo de equilibrio cuando desee que la protección bloquee las ganancias una vez que una posición se vuelva favorable. Deje el gatillo en cero para lograr un punto de equilibrio inmediato.
4. Desactive la gestión de un lado (largo o corto) si desea mantener un control discrecional sobre esa dirección.
5. Supervise la salida del registro cuando **Acciones de administración de registros** esté activo para verificar que los pedidos se creen o ajusten como se esperaba.
