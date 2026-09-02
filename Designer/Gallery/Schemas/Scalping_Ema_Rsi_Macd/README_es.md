# Diagrama de la estrategia de scalping con cruce de EMA, RSI y MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un seguidor de tendencia de corto plazo que no se fía de un cruce sin más. Que la EMA rápida cruce a la lenta es solo el disparador; antes de enviar una orden el precio debe estar además del lado correcto de una EMA de tendencia mucho más lenta, el RSI debe encontrarse dentro de su banda de trabajo y no en un extremo, y la línea MACD debe seguir moviéndose en el sentido de la operación. Cada posición se entrega a un stop y un objetivo de protección, de modo que un scalp nunca queda abierto indefinidamente.

![schema](schema.svg)

## Resumen de la estrategia

- Tres medias móviles exponenciales cumplen funciones distintas: la pareja rápida y lenta produce la señal, y la larga dice qué lado del mercado está permitido siquiera.
- El bloque de cruce solo se dispara en el instante en que la media rápida cambia de lado, así que una misma tendencia no genera una cadena de entradas.
- El RSI se usa como filtro de extremos y no como señal: un cruce se acepta solo mientras el índice se mantiene entre el suelo y el techo, lo que aparta al diagrama de los movimientos agotados.
- La línea MACD se compara con su propio valor de una vela antes, de modo que el impulso debe coincidir con el cruce y no simplemente existir.
- El control de la posición hace que una entrada solo pueda abrir una operación, nunca agrandarla.

## Reglas de entrada y salida

- **Entrada en largo**: La EMA rápida cruza por encima de la lenta, la vela cierra por encima de la EMA de tendencia, el RSI está entre el suelo y el techo, la línea MACD está más alta que una vela antes y la posición está plana. La orden compra el volumen compartido a mercado.
- **Entrada en corto**: La EMA rápida cruza por debajo de la lenta, la vela cierra por debajo de la EMA de tendencia, el RSI está entre el suelo y el techo, la línea MACD está más baja que una vela antes y la posición está plana. La orden vende el volumen compartido a mercado.
- **Salida**: El bloque de protección de la posición cierra cada operación con un stop o un objetivo porcentual medidos desde el precio de ejecución. El original dimensiona ambos niveles con el rango verdadero medio, stop a dos ATR y objetivo al doble de ese riesgo, pero el bloque de protección solo admite un valor fijo, así que la distancia por ATR se sustituyó por un porcentaje del mismo orden de magnitud en este instrumento; recuperar la versión dinámica exigiría recalcular los niveles en el diagrama y enviar las órdenes a mano. Se han dejado fuera dos cosas más: la pausa de diez barras tras cada operación, que ningún bloque puede contar entre velas, y el giro con la señal contraria, ya que aquí la operación la terminan el stop y el objetivo. El original trabaja con velas de treinta minutos y este diagrama corre sobre las velas de cinco minutos del historial incluido.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Fast EMA Length | 12 | Periodo de la media móvil exponencial rápida que produce el cruce. |
| Slow EMA Length | 26 | Periodo de la media móvil exponencial lenta contra la que se cruza la rápida. |
| Trend EMA Length | 55 | Periodo de la media móvil exponencial de tendencia que decide qué lado está permitido. |
| RSI Length | 14 | Periodo de suavizado del índice de fuerza relativa. |
| RSI floor | 35 | Borde inferior de la banda del RSI; por debajo, el cruce se considera un movimiento ya recorrido. |
| RSI ceiling | 65 | Borde superior de la banda del RSI; por encima, el cruce se considera recalentado. |
| Take profit, % | 1 | Distancia del take profit desde el precio de ejecución, en porcentaje. |
| Stop loss, % | 0.5 | Distancia del stop loss desde el precio de ejecución, en porcentaje. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta los cinco indicadores y un conversor que lee el precio de cierre; el MACD se construye con los mismos periodos doce y veintiséis que la pareja de EMA.
- El bloque de cruce recibe la media rápida en su entrada superior y la lenta en la inferior, y un NO lógico convierte esa misma salida en el cruce a la baja del lado corto.
- La banda del RSI son dos comparaciones contra dos constantes que ambas entradas comparten; la prueba de impulso del MACD compara la línea con un bloque de valor anterior de una vela.
- Cada Y lógica reúne el cruce, el lado de la tendencia, ambos bordes del RSI, la prueba de impulso y la comprobación de posición plana, y luego dispara un bloque de entrada que toma su volumen de la constante compartida.
- Los dos bloques de entrada envían sus propias operaciones al bloque de protección de la posición, que es el que la cierra.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
