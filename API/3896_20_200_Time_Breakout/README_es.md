# Fuga del tiempo Twenty200
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una versión StockSharp del asesor experto MetaTrader **20/200 expert v4.2 (AntS)**. Espera una hora específica del día de negociación y luego compara dos precios históricos de apertura por hora (6 y 2 barras en la configuración predeterminada). Si la apertura distante es más alta que la apertura más cercana en más de `Short Delta` pips, la estrategia vende, mientras que la brecha inversa que excede `Long Delta` pips abre una posición larga.

## Lógica comercial

- La estrategia se suscribe a velas horarias (configurables a través de `Candle Type`).
- Sólo se permite una operación por día. Las órdenes se realizan cuando se activa una vela con una hora igual a `Trade Hour`.
- Las señales utilizan el precio de apertura `LookbackFar` y las barras `LookbackNear` de la vela actual.
  - **Configuración breve:** `Open[t1] - Open[t2] > Short Delta × pip`.
  - **Configuración larga:** `Open[t2] - Open[t1] > Long Delta × pip`.
- Se envía una orden de mercado con el volumen calculado. Las distancias de stop-loss y take-profit se toman de la versión MetaTrader y se expresan en pips, y se convierten automáticamente a precios a través de `Security.PriceStep`.
- Sólo puede existir una posición a la vez. La negociación diaria se reanuda el siguiente día calendario.

## Gestión de posiciones

- El stop-loss y la take-profit se evalúan en cada actualización de la vela utilizando los extremos alto/bajo de la vela.
- `Max Open Hours` fuerza una salida del mercado cuando la vida útil de la posición excede el número de horas configurado (504 horas de forma predeterminada). Establezca el parámetro en cero para desactivar el temporizador de seguridad.

## gestión del dinero

- `Fixed Volume` define el tamaño del contrato de reserva utilizado cuando `Use Auto Lot` está deshabilitado o la información del saldo no está disponible.
- Cuando `Use Auto Lot` está habilitado, el tamaño del lote sigue la enorme tabla de pasos del asesor experto. En StockSharp, la tabla se aproxima por `volume = round(balance × Auto Lot Factor, 2)` con el factor predeterminado `0.000038`, reproduciendo los valores MT4 dentro de un pip de volumen en todo el rango documentado (300 USD a 270 000 USD+).
- Si el valor actual de la cartera cae por debajo del último saldo registrado, la siguiente operación se multiplica por `Big Lot Multiplier`, imitando la operación de recuperación "Big Lot" en el código original.
- Los volúmenes se alinean con `Security.VolumeStep` y se sujetan entre `MinVolume`/`MaxVolume` cuando están disponibles.

## Diferencias vs. el MetaTrader EA

- El script MT4 almacenó más de mil filas de umbrales manuales. La versión StockSharp utiliza un coeficiente lineal (`Auto Lot Factor`) que se ajusta a la misma escalera. Ajuste el factor si necesita una réplica exacta para un corredor diferente.
- Las órdenes de stop-loss/take-profit se simulan mediante salidas del mercado en los extremos de las velas. Esto mantiene el comportamiento consistente en las pruebas retrospectivas y en las operaciones en vivo sin depender del soporte de la orden de parada del lado de la bolsa.
- Las variables globales (`globalBalans`, `globalPosic`) se reemplazan con el estado en memoria. No se requiere ningún sistema de archivos ni estado de terminal.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| Take Profit largo/corto | Distancia en pips para objetivos de ganancias. |
| Stop Loss largo/corto | Distancia en pips para detener las pérdidas. |
| Hora comercial | Hora de la sesión (0–23) en la que las señales pueden activarse. |
| Vista atrás lejana/cerca | ¿Cuántas barras hay que inspeccionar para determinar los dos precios de apertura? |
| Delta largo/corto | Espacio de pips requerido para abrir una posición. |
| Horario máximo de apertura | Vida útil máxima de la posición en horas (0 desactiva la guardia). |
| Volumen fijo | Volumen de contrato de referencia cuando el tamaño automático está deshabilitado. |
| Usar lote automático | Habilite el tamaño del lote a partir del valor de la cuenta. |
| Factor de lote automático | Multiplicador aplicado al valor de la cartera para emular la tabla de pasos MT4. |
| Multiplicador de lote grande | Multiplicador de volumen aplicado después de una caída del capital. |
| Tipo de vela | Marco de tiempo utilizado para las velas de señal. |
