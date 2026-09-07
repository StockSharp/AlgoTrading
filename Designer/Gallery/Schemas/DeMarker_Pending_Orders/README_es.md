# Diagrama de órdenes pendientes con DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama convierte los cruces de niveles de DeMarker en órdenes limitadas de retroceso en vez de entrar de inmediato. Cada orden pendiente vive cuatro velas y la protección porcentual de beneficio y pérdida solo se conecta después de que la orden se ejecute realmente.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas finalizadas de quince minutos alimentan un oscilador DeMarker de longitud 14 y un flujo de precios de cierre.
- Los niveles inferior y superior son 0,3 y 0,7; los bloques de cruce detectan una caída por debajo del nivel inferior y una subida por encima del superior.
- Las entradas solo se aceptan entre las 07:00:00 y las 20:59:59 según la hora de la vela y únicamente con la posición plana.
- Una señal nueva reemplaza cualquier entrada pendiente anterior, y una orden sin ejecutar también vence tras cuatro velas finalizadas posteriores.
- El gráfico muestra las velas, el oscilador, ambos niveles y todas las ejecuciones de la estrategia.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando DeMarker cae por debajo de 0,3 dentro de la ventana de entrada y la posición está plana, se registra una compra limitada un porcentaje de separación por debajo del cierre actual.
- **Entrada en corto**: Cuando DeMarker sube por encima de 0,7 dentro de la ventana de entrada y la posición está plana, se registra una venta limitada un porcentaje de separación por encima del cierre actual.
- **Salida**: Una orden limitada sin ejecutar se cancela cuando otra señal la reemplaza o al terminar su temporizador de cuatro velas. Tras una ejecución, la protección cierra la posición en el objetivo del 1,2% o en el stop del 0,6%.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| DeMarker Length | 14 | Longitud de promediado del oscilador DeMarker. |
| Lower level | 0.3 | Un cruce descendente de este valor crea una señal larga. |
| Upper level | 0.7 | Un cruce ascendente de este valor crea una señal corta. |
| Pending indent, % | 0.1 | Distancia entre el precio pendiente y el cierre de la vela de señal. |
| Pending life, bars | 4 | Número de velas finalizadas posteriores antes de cancelar una orden sin ejecutar. |
| Entry window start | 07:00:00 | Primera hora de vela admitida por el filtro de entrada. |
| Entry window end | 20:59:59 | Última hora de vela admitida por el filtro de entrada. |
| Volume | 1 | Volumen de cada orden pendiente de entrada. |
| Take profit, % | 1.2 | Movimiento porcentual favorable desde el precio de entrada ejecutado. |
| Stop loss, % | 0.6 | Movimiento porcentual adverso desde el precio de entrada ejecutado. |
| Candles | 00:15:00 | Marco temporal de las velas de señal finalizadas. |

## Detalles del diagrama

- Para el cruce inferior, la constante 0,3 se conecta a Input Up de Crossing y DeMarker a Input Down; para el cruce superior, DeMarker ocupa Input Up y 0,7 Input Down.
- Dos bloques AND combinan el cruce correspondiente, el resultado del horario operativo y la comprobación de posición plana; las banderas por vela convierten cada señal aceptada en un único disparo.
- Las fórmulas calculan `close × (1 − indent / 100)` para Buy y `close × (1 + indent / 100)` para Sell, y los dos bloques de registro envían esos precios como órdenes limitadas.
- Cada señal inicia su propio contador N values. Su salida llega al bloque de cancelación correspondiente cuatro velas después, y antes de cada orden nueva ambos bloques reciben la instrucción de retirar entradas pendientes anteriores.
- Un bloque Trades for order vigila cada límite registrado. Solo su salida de ejecución llega a Position protection, por lo que una orden pendiente o cancelada no puede activar el stop ni el objetivo.
- Los bloques de registro de las versiones actuales de Designer también ofrecen una salida MyTrade; se conservan los bloques Trades for order independientes para mostrar claramente la cadena desde la orden hasta su ejecución.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
