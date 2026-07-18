# Ejemplo de estrategia de trailing stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

**SampleTrailingStopStrategy** es un puerto C# directo del MetaTrader asesor experto `SampleTrailingstop.mq4`. La estrategia no genera sus propias entradas; en cambio, vigila continuamente la posición actual y mantiene órdenes protectoras de limitación de pérdidas y toma de ganancias. La lógica refleja la EA original al respetar los niveles de parada y congelación impuestos por el corredor mientras se aplica un stop dinámico medido en puntos de precio.

Siempre que una posición larga se vuelve rentable y la mejor oferta se aleja lo suficiente del precio de entrada, la estrategia primero mueve el stop-loss justo por debajo de la oferta a la distancia mínima permitida. Las actualizaciones posteriores siguen la parada detrás de la oferta por el número configurado de puntos más los buffers del corredor. Las posiciones cortas se procesan simétricamente, con el stop por encima de la demanda. Los objetivos de obtención de beneficios opcionales se recalculan en cada evento posterior.

## Flujo de datos

* Suscríbase a las actualizaciones de Level1 para recibir las mejores cotizaciones de oferta y demanda.
* Realiza un seguimiento del precio de la posición actual a través de la base `Strategy` API.
* Vuelve a registrar órdenes de límite y parada de protección cuando se calculan nuevos precios.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `TrailingStopPoints` | `200` | Distancia entre el mercado y el trailing stop medida en puntos de precio. Este valor se agrega a los buffers del corredor durante los cálculos finales. |
| `TakeProfitPoints` | `1000` | Distancia de toma de ganancias opcional en puntos. Establezca en `0` para deshabilitar la gestión de obtención de beneficios. |
| `StopLevelPoints` | `0` | Restricción del nivel de parada del corredor expresada en puntos. Se agrega a la distancia de seguimiento para mantener válidas las órdenes de parada. |
| `FreezeLevelPoints` | `0` | Restricción del nivel de congelación del corredor expresada en puntos. El seguimiento espera hasta que el mercado supere este colchón del precio de entrada. |

Todas las distancias se traducen a valores de precio con el tamaño de tick del instrumento para emular el comportamiento de `_Point` de MetaTrader.

## Algoritmo de seguimiento

1. **Validación de posición**: la estrategia ignora el seguimiento hasta que existe una posición y se conoce la mejor oferta/demanda.
2. **Verificación de ganancias**: el seguimiento se activa solo cuando la posición es rentable (`bid > entry` para largos, `ask < entry` para cortos) y se ha borrado el búfer de congelación.
3. **Colocación del stop inicial**: si aún no hay un trailing stop activo, el stop se mueve a la distancia mínima permitida del mercado (oferta menos buffers para largos, pregunta más buffers para cortos) una vez que el precio se aleja al menos la distancia de seguimiento de la entrada.
4. **Actualizaciones de seguimiento**: si bien la posición sigue siendo rentable, el stop se profundiza más utilizando la distancia de seguimiento configurada más los buffers del corredor. Los niveles de obtención de beneficios se recalculan en cada actualización cuando está habilitado.
5. **Mantenimiento de órdenes**: las órdenes de protección se crean, actualizan o cancelan automáticamente a través de métodos auxiliares de alto nivel para que el corredor siempre vea los últimos valores de stop-loss y take-profit.

## Notas de uso

* Inicie la estrategia junto con otro componente que abra posiciones, o utilice órdenes manuales; este módulo sólo gestiona salidas.
* Asegúrese de que los metadatos del instrumento contengan los pasos de precio y volumen adecuados. La estrategia normaliza cada precio y cantidad generados para satisfacer las restricciones cambiarias.
* Cuando la dirección de la posición cambia, cualquier orden de protección heredada se cancela antes de que se reinicie el seguimiento para el nuevo lado.
