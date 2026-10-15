# Diagrama de estrategia de barrido del día anterior con alerta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera rupturas fallidas del rango del día UTC anterior. A medianoche fija el máximo y el mínimo de ese día, espera que una vela de quince minutos barra un límite y cierre de nuevo dentro, entra contra el barrido y escribe un mensaje de registro por cada vela de señal aceptada.

![schema](schema.svg)

## Descripción de la estrategia

- Las velas finalizadas de quince minutos proporcionan el máximo, mínimo y cierre utilizados en cada decisión.
- Highest(96) y Lowest(96) cubren un día completo. Los bloques Previous value con desplazamiento 1 excluyen la nueva vela de medianoche antes de capturar el rango.
- El máximo y mínimo capturados permanecen fijos desde las 00:00 UTC hasta el final de ese día natural. La evaluación de entradas comienza a las 00:15 UTC.
- Una ruptura fallida por encima del máximo retenido genera una configuración corta; una ruptura fallida por debajo del mínimo retenido genera una configuración larga.
- Solo se permiten entradas con posición plana y se usan órdenes de mercado de una unidad. Si ambas condiciones aparecen en una vela, el barrido del máximo tiene prioridad.
- Cada entrada ejecutada recibe un take-profit fijo del 1% y un stop-loss del 1%, activados como salidas de mercado a partir de cierres de velas finalizadas.

## Reglas de entrada y salida

- **Entrada larga**: De 00:15 a 23:59:59 UTC, el mínimo de la vela debe estar por debajo del mínimo retenido del día anterior, su cierre por encima de ese nivel, no debe existir un barrido simultáneo del máximo y Position debe ser cero. Se compra una unidad a mercado.
- **Entrada corta**: En la misma ventana, el máximo de la vela debe estar por encima del máximo retenido del día anterior, su cierre por debajo de ese nivel y Position debe ser cero. Se vende una unidad a mercado.
- **Alerta**: Cada señal larga o corta aceptada pasa una vez por un Flag por vela. String Formatter incluye el cierre de la vela de señal en un mensaje que Notification envía al registro de la estrategia.
- **Salida**: Position protection cierra la entrada ejecutada tras un movimiento favorable del 1% o uno adverso del 1%. Ambas salidas protectoras son órdenes de mercado.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|---|---|---|
| Velas | 00:15:00 | Marco temporal de las velas finalizadas para construir el rango y decidir. |
| Longitud de Highest | 96 | Número de máximos de quince minutos en un rango diario completo. |
| Fuente de Highest | Sin definir | No se ha seleccionado un campo de entrada alternativo para el indicador. |
| Longitud de Lowest | 96 | Número de mínimos de quince minutos en un rango diario completo. |
| Fuente de Lowest | Sin definir | No se ha seleccionado un campo de entrada alternativo para el indicador. |
| Volumen | 1 | Tamaño de cada orden de entrada a mercado. |
| Take-profit | 1% | Movimiento favorable desde la ejecución que activa la protección. |
| Stop-loss | 1% | Movimiento adverso desde la ejecución que activa la protección. |
| Stop-loss dinámico | false | Mantiene fijo el límite del stop. |
| Usar órdenes de mercado | true | Envía las salidas protectoras activadas como órdenes de mercado. |

## Detalles del diagrama

- Los convertidores HighPrice y LowPrice alimentan los dos indicadores móviles de 96 valores; ClosePrice proporciona las pruebas de retorno al rango, el texto de alerta y las comprobaciones de protección.
- Cada resultado móvil pasa por un bloque Previous value finalizado con desplazamiento 1. Entre 00:00 y 00:14:59 UTC, las Variables de captura toman el valor anterior y las Variables de retención lo publican en cada vela.
- Cuatro comparaciones detectan una perforación del máximo con cierre bajo el máximo retenido o una perforación del mínimo con cierre sobre el mínimo retenido. Working time limita ambas combinaciones a 00:15-23:59:59 UTC.
- El valor de la posición se muestrea en cada vela y se compara con cero. La puerta corta combina el barrido del máximo con la comprobación de posición plana; la puerta larga también exige la señal invertida del barrido del máximo para conservar la prioridad.
- Las dos puertas aceptadas activan Buy y Sell en bloques Modify position y se combinan para informar. Un Flag reiniciado por cada vela evita mensajes duplicados dentro de un evento de señal sin suprimir señales posteriores del mismo día.
- Las ejecuciones de entrada arman el bloque Position protection compartido. Su precio de referencia se actualiza con el cierre finalizado, por lo que no se observan toques intravela que retroceden antes del cierre.
- El primer rango utilizable necesita 96 velas anteriores de quince minutos. Los niveles solo se actualizan en la captura de medianoche y permanecen sin cambios hasta el siguiente día UTC.
- El gráfico muestra velas, límites diarios móviles y retenidos, ejecuciones de compra y venta, y todas las ejecuciones de la estrategia, incluidas las salidas protectoras.

## Uso

Importe el archivo `.json` en Designer, ejecútelo en el backtester con suficiente historial para formar el primer rango diario y ajuste el marco temporal, las longitudes del rango, las distancias de protección y el volumen al instrumento antes de operar en vivo.
