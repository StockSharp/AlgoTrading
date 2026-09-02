# Diagrama de la estrategia RSI-2 de Larry Connors
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El RSI-2 de Larry Connors compra el pánico y vende la euforia, pero solo en el sentido que permite la media lenta: un RSI de dos periodos marca el extremo, una SMA de 50 decide la dirección y una SMA de 5 marca el momento de salir. El original opera velas de cuatro horas; este diagrama trabaja con velas de cinco minutos para ajustarse al histórico intradía incluido.

![schema](schema.svg)

## Resumen de la estrategia

- El RSI de longitud dos reacciona a una sola vela, así que una lectura por debajo de 6 o por encima de 95 señala un arrebato breve de ventas o de compras, no un estado duradero.
- La SMA lenta actúa como filtro de dirección: los largos solo se toman por encima y los cortos solo por debajo, de modo que el diagrama se mantiene del lado del movimiento mayor.
- La posición se abre únicamente desde plano y la SMA rápida la cierra en cuanto el precio vuelve a cruzar esa media, por lo que las operaciones suelen durar una o dos velas.
- El bloque de protección añade un stop y un objetivo porcentuales en lugar de los niveles en pips del original, que no pueden calcularse desde el paso de precio dentro de un diagrama.

## Reglas de entrada y salida

- **Entrada en largo**: El RSI(2) está por debajo del nivel de entrada larga, el cierre está por encima de la SMA lenta y la posición es plana. La orden compra el volumen compartido a mercado y abre el largo.
- **Entrada en corto**: El RSI(2) está por encima del nivel de entrada corta, el cierre está por debajo de la SMA lenta y la posición es plana. La orden vende el volumen compartido a mercado y abre el corto.
- **Salida**: El largo se cierra cuando el cierre vuelve por encima de la SMA rápida y el corto cuando cae por debajo; el stop del 1% o el objetivo del 2% cierran antes la posición si el precio llega primero a uno de ellos.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| RSI Length | 2 | Periodo de suavizado del índice de fuerza relativa; dos velas por diseño. |
| Fast SMA Length | 5 | Periodo de la SMA rápida que marca la salida. |
| Slow SMA Length | 50 | Periodo de la SMA lenta que decide qué lado puede operarse. |
| RSI Long Entry | 6 | Nivel de RSI por debajo del cual se permite un largo. |
| RSI Short Entry | 95 | Nivel de RSI por encima del cual se permite un corto. |
| Take Profit, % | 2 | Distancia del objetivo respecto al precio de entrada, en porcentaje. |
| Stop Loss, % | 1 | Distancia del stop respecto al precio de entrada, en porcentaje. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el RSI, ambas medias móviles y un conversor que lee el precio de cierre de cada vela terminada.
- Seis bloques de comparación contienen las reglas: dos enfrentan el RSI a sus niveles de entrada, dos el cierre a la SMA lenta y dos el cierre a la SMA rápida.
- Las dos Y de entrada incluyen también la comprobación de posición plana, y los bloques de entrada están configurados para abrir posición, así que una señal nunca amplía una operación en curso.
- Los bloques de salida están configurados para cerrar posición, por lo que actúan solo si existe una posición del lado contrario; todas las operaciones propias llegan al bloque de protección para que su stop y su objetivo sigan a la posición real.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
