# Estrategia de cambio de media móvil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una StockSharp versión de alto nivel del clásico asesor experto **Promedio móvil** que se envía con MetaTrader 4. El sistema observa velas completadas y las compara con un promedio móvil simple desplazado (SMA) para detectar cambios de dirección. Las órdenes siempre se ejecutan en el mercado y la estrategia permanece en el mercado con como máximo una posición abierta en cualquier momento.

## Lógica de trading

1. Suscríbete a velas del período de tiempo configurable (predeterminado: 5 minutos) y calcula un SMA con el período solicitado.
2. Cambie SMA por el número especificado de velas completadas para emular el comportamiento de la función `iMA` original.
3. Evalúe la vela terminada anterior:
   - **Cruz alcista** (abre por debajo del SMA desplazado y cierra por encima) desencadena una entrada larga cuando no hay ninguna posición abierta.
   - **Cruz bajista** (abre por encima y cierra por debajo del SMA desplazado) activa una entrada corta cuando no hay ninguna posición abierta.
4. Gestione las salidas utilizando las mismas reglas cruzadas:
   - Una posición larga se cierra cuando la última vela cruza por debajo del SMA desplazado.
   - Una posición corta se cierra cuando la última vela cruza por encima del SMA desplazado.
5. Solo puede existir una posición en cualquier momento, que coincida con el comportamiento del EA original que alternaba entre órdenes de compra y venta.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Serie de velas utilizadas para los cálculos. Se puede seleccionar cualquier período de tiempo `DataType`. | marco de tiempo de 5 minutos |
| `MovingPeriod` | Número de velas para la longitud SMA. | 12 |
| `MovingShift` | Compensación del valor SMA en velas completadas. Emula el argumento `shift` de `iMA`. | 6 |
| `BaseVolume` | Volumen de pedido predeterminado para las entradas. Se utiliza el mismo volumen tanto para operaciones largas como cortas. | 1 |

## Manejo de indicadores

- Se crea un indicador `SimpleMovingAverage` en `OnStarted` y se vincula a la suscripción de vela a través del nivel alto `Bind` API.
- La salida sin procesar de SMA se almacena en una pequeña cola FIFO para obtener el valor de hace `MovingShift` velas. No se realiza ningún recálculo manual del indicador.
- La cola retiene solo valores `MovingShift + 1`, por lo que el uso de la memoria permanece constante incluso para turnos grandes.

## Gestión de pedidos y riesgos

- Los pedidos se realizan con `BuyMarket`/`SellMarket` y se dimensionan según el parámetro `BaseVolume`. Al cerrar, se utiliza el tamaño de la posición absoluta actual para garantizar una salida completa.
- La implementación original de MetaTrader ajustó dinámicamente el tamaño del lote en función del margen libre y las pérdidas recientes. El puerto StockSharp mantiene la lógica determinista y delega el tamaño de la posición al usuario a través del parámetro `BaseVolume`. Esto evita depender de métricas de cuentas específicas del corredor y al mismo tiempo preserva las reglas de entrada/salida.

## Notas de conversión

- Las señales se evalúan en la vela **anterior**, coincidiendo con la verificación `Volume[0] == 1` de MetaTrader que esperó una nueva barra antes de reaccionar.
- Solo se procesan velas completadas (`CandleStates.Finished`) para evitar operaciones prematuras.
- La estrategia utiliza los ayudantes de gráficos StockSharp para trazar velas, valores de indicadores y marcadores comerciales cuando hay un área de gráfico disponible.

## Uso

1. Compile la estrategia dentro de StockSharp Designer, Shell o Runner.
2. Seleccione el instrumento deseado y asigne un portafolio.
3. Configure los parámetros si se requieren diferentes plazos, duraciones o volúmenes.
4. Iniciar la estrategia; se suscribirá a la serie de velas elegida, monitoreará SMA cruces y operará en consecuencia.

## Más ideas

- Agregue paradas protectoras o niveles de toma de ganancias usando `StartProtection` si se requiere una gestión de riesgos más allá de la salida de reversión básica.
- Reemplace el SMA simple con otro indicador (EMA, LWMA, etc.) modificando la instancia del indicador manteniendo el flujo de trabajo de suscripción existente.
- Introduzca reglas de escala de posición ajustando el método `GetEntryVolume`.
