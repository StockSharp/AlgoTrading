# Estrategia de cierre de ganancias de fin de semana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de fin de semana Close Profit** automatiza el script MetaTrader *Closeprofitendofweek.mq5*. La estrategia supervisa el instrumento asignado y, los viernes, después de una hora límite configurable, sale de todas las posiciones rentables. El objetivo es asegurar ganancias flotantes antes de que aparezca el riesgo de brecha del fin de semana.

## Comportamiento original de MQL
El Asesor Experto fuente sondeaba continuamente las posiciones a través del controlador del temporizador. Siempre que la hora del servidor coincidía con la hora del viernes y la hora de finalización configurada, recorría todas las posiciones abiertas en el símbolo negociado. Cada posición con beneficio positivo se cerró mediante una orden de mercado. Los símbolos criptográficos se excluyeron explícitamente porque se negocian sin descansos de fin de semana.

## StockSharp Implementación
El puerto C# mantiene la misma lógica de protección mientras usa el nivel alto API de StockSharp:
- Suscríbete a una serie de velas configurables solo para recibir actualizaciones periódicas.
- Comprueba cada vela terminada y verifica que represente un viernes cuyo horario de cierre sea posterior al corte definido por el usuario.
- Accede a la cartera conectada para evaluar la posición neta del símbolo de la estrategia.
- Emite una orden de mercado en dirección opuesta por cada exposición rentable que aún esté abierta.
- Se salta la rutina por completo cuando el instrumento se clasifica como un activo criptográfico.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `StartTradeTime` | Inicio de la ventana de monitoreo (mantenido por paridad con las entradas MQL). | `00:00` |
| `EndTradeTime` | Hora del viernes a partir de la cual se deben cerrar las posiciones rentables. | `20:00` |
| `CloseTradesAtEndTime` | Habilita o deshabilita la rutina de cierre automático. | `true` |
| `CandleType` | Serie de datos utilizada para realizar un seguimiento del tiempo (el valor predeterminado es velas de 1 minuto). | `TimeFrameCandle(1m)` |

## Flujo de ejecución
1. Cuando se inicia la estrategia, se verifica si el valor seleccionado pertenece a la clase de criptoactivo. Los instrumentos criptográficos se ignoran para reflejar la cláusula de protección MetaTrader.
2. Se crea una suscripción de vela para recibir devoluciones de llamada periódicas una vez que se termina cada vela.
3. Cada vela terminada activa las comprobaciones programadas. Sólo los viernes que cierran después de la hora límite dan lugar a un procesamiento adicional.
4. La estrategia escanea la cartera conectada, filtra la posición que corresponde al valor configurado y lee su beneficio flotante.
5. Si el beneficio flotante es mayor que cero, se envía una orden de mercado en la dirección opuesta para cerrar completamente la exposición.
6. Se evitan órdenes de salida duplicadas inspeccionando las órdenes activas antes de enviar otras nuevas.

## Notas de uso
- Adjunte la estrategia a un instrumento no criptográfico junto con la misma cartera que posee las posiciones abiertas que desea supervisar.
- La estrategia no abre nuevas operaciones; sólo gestiona puestos existentes.
- El parámetro `StartTradeTime` existe para la paridad de configuración y extensiones futuras, pero la lógica actual no hace referencia a él.
- Para carteras de múltiples símbolos, ejecute una instancia por instrumento para replicar el alcance de un solo símbolo del script MetaTrader.

## Limitaciones
- La detección de ganancias se basa en que la cartera de corredores informe PnL flotante. Si la cartera no se actualiza en tiempo real el comando de cierre puede retrasarse.
- Sólo se cierran las posiciones para el símbolo de estrategia configurado. Las exposiciones mantenidas sobre otros símbolos permanecen intactas.
- La verificación se ejecuta en los eventos de cierre de velas. Seleccione un período de tiempo adecuadamente corto si necesita un cronograma más ajustado.
