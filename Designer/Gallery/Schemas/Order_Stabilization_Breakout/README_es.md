# Diagrama de estrategia de ruptura tras estabilización con órdenes límite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama observa velas finalizadas de cinco minutos de BTCUSDT@BNBFT y busca una transición desde un cuerpo estabilizado por debajo de la mitad del ATR(14) hasta un cuerpo que se expande por encima de ese nivel. Coloca una orden límite en el cierre de la señal, le concede tres velas posteriores para finalizar y duplica la cantidad base de una reversión ejecutada.

![schema](schema.svg)

## Descripción de la estrategia

- Las velas finalizadas de cinco minutos proporcionan los precios Open y Close, mientras que los valores formados de ATR(14) miden la escala de volatilidad actual.
- Body se calcula como `abs(Close - Open)` y su límite de estabilización es `ATR * Stabilization Factor`. El factor predeterminado es `0.5`.
- El primer par Body/ATR formado inicializa los valores anteriores guardados sin crear una señal. Cada par formado posterior compara tanto el cuerpo anterior como el actual con su límite correspondiente.
- Una configuración exige `Previous Body < Previous ATR * 0.5` y `Current Body > Current ATR * 0.5`. La igualdad en cualquiera de los límites no cumple la condición.
- Un cierre compartido de orden pendiente permite solo una orden límite activa. Las puertas de vigencia separadas para compra y venta impiden que cada lado vuelva a usar su temporizador de tres velas antes de que este termine.

## Reglas de entrada y salida

- **Entrada larga**: Cuando una vela de expansión válida es alcista (`Close > Open`), el estado con signo es plano o corto, no hay una orden pendiente y la puerta de vigencia de compra está lista, se envía una compra límite al Close actual.
- **Entrada corta**: Cuando una vela de expansión válida es bajista (`Close < Open`), el estado con signo es plano o largo, no hay una orden pendiente y la puerta de vigencia de venta está lista, se envía una venta límite al Close actual.
- **Cantidad de la orden**: La cantidad es `Base Volume * (1 + abs(state))`. Una entrada desde plano usa una unidad base; una reversión aceptada desde `-1` o `1` usa dos unidades base.
- **Salida**: No hay protección basada en precio. La exposición cambia solo cuando se ejecuta un límite opuesto; un límite sin ejecutar recibe una solicitud de cancelación dirigida después de tres velas finalizadas estrictamente posteriores.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|---|---|---|
| Security | BTCUSDT@BNBFT | Instrumento usado por la suscripción de velas finalizadas de cinco minutos. Strategy Security debe tener el mismo valor porque las órdenes, cancelaciones y ejecuciones usan Strategy Security y Strategy Portfolio. |
| Candle Series | 00:05:00 | Velas finalizadas de cinco minutos usadas para ATR, cálculos del cuerpo, señales, conteo de vigencia y gráfico. |
| ATR Length | 14 | Longitud de promedio del indicador Average True Range. Las decisiones comienzan solo cuando ATR está formado. |
| Stabilization Factor | 0.5 | Multiplicador aplicado por separado a los valores anterior y actual de ATR para formar sus límites de cuerpo. |
| Lifetime N | 3 | Número de velas finalizadas estrictamente posteriores permitido antes de solicitar la cancelación de un límite sin ejecutar. |
| Base Volume | 1 | Cantidad de una entrada desde plano; la fórmula de acción la duplica para una reversión. |

## Detalles del diagrama

- La [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) Security configura únicamente la suscripción de [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) finalizadas. Cada vela completada suministra Open, Close y la vela completa enviada a ATR; los bloques de órdenes y operaciones usan Strategy Security y Strategy Portfolio, por lo que Strategy Security debe coincidir con Security.
- El bloque [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) emite solo valores formados de ATR(14). Los bloques de fórmula y cierre mantienen Body actual, límite actual, Body anterior y límite anterior alineados dentro de una decisión de vela.
- Un cierre de inicialización suprime la primera decisión formada y guarda su par. Las decisiones posteriores evalúan las dos relaciones estrictas con los límites antes de avanzar los cierres de valores anteriores.
- La vela actual llega a ambos contadores de vigencia antes de que se ejecute la rama de señal. Iniciar un contador después de esa entrada hace que se libere en la tercera vela finalizada posterior, por lo que la vela de señal nunca cuenta en su propia vigencia.
- Una configuración aceptada cierra la puerta del temporizador de su lado y activa el cierre pendiente compartido antes de disparar [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html). El cierre pendiente se limpia solo cuando esa orden informa un estado final; el temporizador del lado sigue sin estar disponible hasta su liberación de tres velas aunque la orden se ejecute antes.
- Cada bloque de registro guarda su referencia Order para una cancelación dirigida. La liberación de vigencia envía la referencia guardada correspondiente a la cancelación y vuelve a abrir únicamente la puerta del temporizador de ese lado.
- Los eventos MyTrade fijan el estado con signo desde ejecuciones reales: una venta ejecutada fija `-1`, una compra ejecutada fija `1` y el registro comienza en `0` para plano. El gráfico recibe velas, ATR, Body, el límite de estabilización, órdenes enviadas y ejecuciones de la estrategia.

## Uso

Importe el archivo `.json` en Designer, establezca Strategy Security en BTCUSDT@BNBFT, ejecútelo con historial de cinco minutos y revise las ejecuciones y cancelaciones de límites antes de ajustar el factor, la vigencia o la cantidad para otro entorno de negociación.
