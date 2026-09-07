# Diagrama de estrategia con señal entre instrumentos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama sincroniza velas finalizadas de cuatro horas de TONUSDT@BNBFT y BTCUSDT@BNBFT. TONUSDT aporta una señal de tasa de cambio de 20 periodos, mientras que BTCUSDT aporta su propio filtro de media móvil simple de 20 periodos. El diagrama está concebido para ejecutarse con Strategy Security configurado como BTCUSDT@BNBFT; un pestillo numérico `0/1` para plano/largo, un estado compartido de permiso de entrada y la protección dirigida por ejecuciones administran esa exposición exclusivamente larga.

![schema](schema.svg)

## Resumen de la estrategia

- Dos variables de instrumento independientes configuran únicamente las suscripciones de velas de cuatro horas de TONUSDT y BTCUSDT. Sus velas finalizadas se alinean antes de decidir, por lo que el impulso TON y el filtro de tendencia BTC siempre pertenecen al mismo intervalo sincronizado.
- TON ROC(20) es alcista por encima de cero y se convierte en condición de salida cuando es igual o inferior a cero. BTCUSDT permite la entrada cuando su Close es igual o superior a SMA(20), y un Close inferior a SMA(20) es condición de salida.
- La entrada larga exige cuatro condiciones simultáneas: `TON ROC(20) > 0`, `BTC Close >= BTC SMA(20)`, el pestillo interno indica estado plano y el estado compartido `Cooldown is ready` permite entrar. El AND ensamblado externamente activa entonces una compra a mercado NoCondition con volumen 1.
- La acción de entrada retira el permiso compartido e inicia Entry Cooldown N, mientras que la venta discrecional por señal retira el mismo permiso e inicia Signal-exit Cooldown N. El temporizador correspondiente restaura el permiso después de ocho pares de velas sincronizados; una comprobación de generación suprime la finalización de un temporizador anterior tras un reinicio más reciente. Las salidas no esperan este estado y las salidas de protección no lo reinician.
- La ejecución de compra a mercado de BTCUSDT cambia el pestillo a largo y activa una protección con take-profit del 2% y stop-loss fijo, no móvil, del 2.5%. Una ejecución del cierre discrecional o la activación y ejecución de Take/Stop devuelve el pestillo a plano. La estrategia nunca abre una posición corta.

## Reglas de entrada y salida

- **Entrada en largo**: En un par sincronizado de velas finalizadas de cuatro horas, el AND externo de entrada activa una compra NoCondition a mercado con volumen 1 cuando TON ROC(20) está por encima de cero, BTC Close es igual o superior a BTC SMA(20), el pestillo indica plano y el estado compartido `Cooldown is ready` permite entrar. La acción negocia el Strategy Security seleccionado, que debe ser BTCUSDT@BNBFT para coincidir con el parámetro de velas Traded Security.
- **Entrada en corto**: No hay entrada corta. La venta discrecional usa ReduceOnly, MarketOrder y volumen 1, por lo que solo puede reducir la exposición del Strategy Security seleccionado; la protección Take y Stop también cierra la exposición larga.
- **Salida**: Cuando el pestillo indica largo, `TON ROC(20) <= 0` o `BTC Close < BTC SMA(20)` activa la venta ReduceOnly a mercado sin esperar el estado compartido de enfriamiento. El take-profit del 2% o el stop-loss fijo del 2.5% también pueden cerrar la exposición mediante una orden a mercado; el stop no es móvil.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Traded Security | BTCUSDT@BNBFT | Instrumento usado únicamente por la suscripción de velas del activo negociado. Configure el Strategy Security seleccionado con el mismo valor BTCUSDT@BNBFT, pues las acciones y las operaciones de estrategia usan Strategy Security. |
| Signal Security | TONUSDT@BNBFT | Instrumento usado únicamente por la suscripción de velas de señal; su impulso contribuye a la señal y ninguna acción se dirige a esta variable. |
| BTC Candles Series | 04:00:00 | Serie de velas finalizadas de cuatro horas de BTCUSDT usada para Close, SMA(20), decisiones de estado y gráfico. |
| TON Candles Series | 04:00:00 | Serie de velas finalizadas de cuatro horas de TONUSDT usada para ROC(20) y decisiones sincronizadas. |
| BTC SMA Length | 20 | Periodo de SimpleMovingAverage calculada con velas finalizadas de BTCUSDT. |
| TON ROC Length | 20 | Periodo de RateOfChange calculado con velas finalizadas de TONUSDT. |
| ROC Threshold | 0 | Nivel cero que separa el estado de impulso positivo para entrar del estado no positivo para salir. |
| Entry Cooldown N | 8 | Número de pares de velas sincronizados que cuenta el temporizador de la acción de entrada antes de poder restaurar el permiso compartido. |
| Signal-exit Cooldown N | 8 | Número de pares de velas sincronizados que cuenta el temporizador de la salida discrecional por señal antes de poder restaurar el permiso compartido. |
| Order Volume | 1 | Cantidad fija usada por la compra NoCondition a mercado y la venta ReduceOnly a mercado del Strategy Security seleccionado. |
| Take Profit | 2% | Ganancia porcentual desde el precio de ejecución de entrada que activa el take-profit. |
| Stop Loss | 2.5% | Pérdida porcentual desde el precio de ejecución de entrada que activa el stop-loss. |
| Trailing Stop Loss | false | Desactivado, por lo que el stop-loss del 2.5% permanece fijo y no sigue un movimiento favorable del precio. |
| Use Market Orders | true | Activado, por lo que el take-profit y el stop-loss cierran la posición mediante órdenes a mercado. |

## Detalles del diagrama

- Bloques separados de [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) de tipo instrumento alimentan únicamente las suscripciones independientes de velas TONUSDT@BNBFT y BTCUSDT@BNBFT. Las acciones de orden y las Operaciones de estrategia usan Strategy Security; seleccione también allí BTCUSDT@BNBFT para que coincida con el parámetro de velas Traded Security.
- Dos bloques de [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emiten solo velas finalizadas de cuatro horas. Un bloque [Sincronización](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/sync.html) empareja ambos flujos con intervalo `04:00:00` antes de que cualquiera entre en la cadena de decisión.
- Un bloque de [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) calcula RateOfChange 20 para TONUSDT y otro calcula SimpleMovingAverage 20 para BTCUSDT. Los bloques de comparación expresan los estados positivo y no positivo de ROC y las relaciones de BTC Close por encima o por debajo de su media.
- Una [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) Unit numérica actúa como pestillo de estado: `0` significa plano y `1` significa largo. Buy MyTrade escribe `1`; el MyTrade de la venta discrecional y los eventos de activación y MyTrade de Take/Stop escriben `0`. Los bloques lógicos combinan ese estado con las señales sincronizadas.
- Dos temporizadores N valores específicos de cada acción mantienen Entry Cooldown N y Signal-exit Cooldown N en 8. Cualquiera de las acciones retira un único permiso compartido de entrada; su temporizador puede restaurar `Cooldown is ready` después de ocho pares sincronizados, mientras una comprobación de generación rechaza la finalización obsoleta de un temporizador anterior. El AND externo de entrada plana tiene una sola entrada de enfriamiento. Activa una compra [Modificar posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) configurada como NoCondition, MarketOrder y volumen 1; la salida por señal activa una venta separada ReduceOnly, MarketOrder y volumen 1 sin esa entrada.
- La [Protección de posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) recibe las ejecuciones de compra y cierre discrecional: la compra activa Take Profit `2%` y Stop Loss fijo `2.5%`, mientras que el cierre limpia la protección obsoleta. Trailing Stop Loss es `false` y Use Market Orders es `true`. El gráfico recibe ambos flujos de velas sincronizados, BTC SMA(20), TON ROC(20), los flujos de órdenes Take y Stop y todas las ejecuciones BTC de Operaciones de estrategia.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
