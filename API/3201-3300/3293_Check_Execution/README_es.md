# Estrategia Check Execution
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Check Execution reproduce el comportamiento del experto MQL original que modifica repetidamente una orden del bróker para medir la calidad de ejecución. El algoritmo puede probar un buy stop pendiente o un sell stop de protección que cubre una posición larga abierta con una orden de mercado. Cada modificación registra tanto el spread observado como el tiempo necesario para que el centro de negociación acepte el cambio, lo que facilita evaluar condiciones sensibles a la latencia ofrecidas por un bróker.

## Lógica central
1. Suscribirse a actualizaciones de mejor bid/ask mediante la API de alto nivel `SubscribeLevel1`.
2. Colocar la orden inicial de prueba según el modo seleccionado:
   - **Pendiente** - enviar un buy stop por encima del precio ask actual.
   - **Mercado** - comprar a mercado y luego enviar un sell stop de protección por debajo del último ask.
3. En cada actualización de cotización:
   - Actualizar la media móvil del spread bid/ask usando `SimpleMovingAverage`.
   - Re-registrar la orden rastreada en el nuevo desplazamiento desde el precio ask cuando se requiere un cambio y no hay una solicitud previa esperando confirmación.
   - Medir la latencia de ejecución tan pronto como la orden vuelve al estado `Active` e introducirla en un segundo `SimpleMovingAverage` para obtener el retraso promedio en milisegundos.
4. Repetir el ciclo de modificación hasta alcanzar el número configurado de iteraciones. Después, la estrategia cancela cualquier orden pendiente/stop restante, cierra la posición larga abierta si es necesario e imprime las estadísticas agregadas de spread y latencia.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Volume` | Volumen de trading usado para cada orden. | `0.01` |
| `Iterations` | Número de intentos de modificación para promediar. Limitado a 1-500. | `30` |
| `Order Mode` | Selecciona el flujo: `Pending` o `Market`. | `Pending` |
| `Pending Offset` | Distancia en pasos de precio por encima del ask para el buy stop de prueba. | `100` |
| `Stop Offset` | Distancia en pasos de precio por debajo del ask para el sell stop de protección. | `100` |

## Notas de comportamiento
- Los valores de volumen se normalizan a las restricciones `VolumeStep`, `MinVolume` y `MaxVolume` del valor para evitar órdenes rechazadas.
- Los desplazamientos de precio se traducen a precios reales usando el `PriceStep` del instrumento. Se usa un paso predeterminado de `0.0001` si el valor no proporciona uno.
- La estrategia solo cuenta una modificación cuando el centro de negociación confirma la solicitud moviendo la orden al estado `Active` o `Done`. Cada confirmación actualiza tanto el temporizador de ejecución como el contador de modificaciones.
- Cuando se alcanza el número objetivo de iteraciones, la estrategia deja automáticamente de modificar órdenes, cancela la protección pendiente, cierra cualquier posición de prueba y registra un mensaje resumen con los promedios medidos.

## Diferencias con la versión MQL
- Los promedios de spread y ejecución se calculan con indicadores `SimpleMovingAverage` de StockSharp en lugar de arreglos manuales.
- La gestión de órdenes usa helpers de alto nivel como `BuyMarket`, `BuyStop`, `SellStop` y `ReRegisterOrder` para mantener coherencia con el framework de estrategias de StockSharp.
- La retroalimentación de interfaz se proporciona mediante el log de la estrategia en lugar de comentarios en el gráfico y objetos gráficos.
