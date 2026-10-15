# Estrategia de Grr Al Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Grr Al Breakout** es un port directo del asesor experto de MetaTrader `grr-al.mq5`. Observa el primer precio alcanzado al comienzo de cada vela y espera a que el mercado se mueva una distancia configurable desde ese nivel de anclaje. Cuando el movimiento supera el umbral, la estrategia ejecuta exactamente una operación para esa vela, opcionalmente invirtiendo la exposición existente.

La implementación en StockSharp mantiene el comportamiento del robot original basado en temporizador, pero lo traduce al modelo de suscripción de velas de alto nivel. Cada nueva instantánea de vela proporciona el precio de referencia inicial, mientras que las actualizaciones posteriores de la misma vela suministran el último cierre usado como precio de mercado en vivo. Este enfoque recrea la detección de ruptura tick a tick sin depender del procesamiento de eventos de bajo nivel.

## Lógica de trading
1. **Detección de anclaje** – cuando comienza una nueva vela, la estrategia almacena su precio de apertura (o el primer cierre disponible si la apertura no está aún disponible) y reinicia el disparador por vela.
2. **Verificación de ruptura** – mientras no se haya ejecutado ninguna operación durante la vela actual, el último cierre se compara con el anclaje. Si el precio sube más de `DeltaPoints` (convertido a precio por el tamaño del punto del instrumento), se abre una posición corta. Si el precio cae la misma distancia, se abre una posición larga.
3. **Ejecución única por vela** – una vez que se dispara una operación de ruptura, no se permiten órdenes adicionales hasta que comience la siguiente vela, imitando el flag `br` del EA original.
4. **Gestión de riesgo** – las distancias opcionales de stop-loss y take-profit se aplican inmediatamente después de abrir una posición. Si la orden solo reduce una exposición opuesta, se omiten los brackets de protección para evitar adjuntar stops a un portfolio plano.
5. **Dimensionamiento de posición** – la estrategia puede operar con un volumen fijo o limitar el tamaño de la orden a una fracción del volumen máximo reportado por el broker.

## Parámetros
- `Volume` – volumen base (en contratos) usado cuando `RiskFraction` es cero. Coincide con la constante `BASELOT` de la versión MQL.
- `RiskFraction` – valor entre 0 y 1. Si es mayor que cero, la estrategia limita el tamaño de la orden multiplicando el volumen máximo del broker por esta fracción y usa el valor menor entre ese límite y `Volume`.
- `DeltaPoints` – número de puntos del instrumento que el precio debe moverse desde la apertura de la vela para disparar una operación. Equivalente a la constante `DELTA`.
- `StopLossPoints` – distancia de stop protector en puntos. Cero deshabilita el stop, igual que la constante `SL` siendo cero en MQL.
- `TakeProfitPoints` – distancia de take-profit en puntos. Cero deshabilita el objetivo y replica el comportamiento de la constante `TP`.
- `CandleType` – descriptor de vela de StockSharp que define el marco temporal para el anclaje y el monitoreo de rupturas. Por defecto usa un marco temporal de cinco minutos pero puede cambiarse a cualquier período soportado.

## Notas y diferencias con la versión MQL
- El EA original usaba eventos de tick con un temporizador de un segundo. Este port aprovecha la API de suscripción de velas de StockSharp, que alimenta automáticamente el último estado de la vela; no se requiere gestión manual de temporizadores.
- La diferenciación bid/ask no está disponible en la interfaz de alto nivel, por lo que la estrategia usa el cierre de la vela como proxy del precio de operación. Los offsets de stop-loss y take-profit aún se aplican en puntos, coincidiendo con el comportamiento de la aritmética de puntos de MetaTrader.
- El cálculo de volumen basado en riesgo en MetaTrader dependía de la estimación del margen para una orden de un lote fijo. En este port el cálculo se simplifica a una fracción del volumen máximo para que permanezca agnóstico al broker.
- Dado que las estrategias de StockSharp son basadas en posición neta, enviar una orden en la dirección opuesta puede aplanar o invertir la exposición automáticamente, similar a la llamada `OrderSend` con modo netting en MetaTrader 5.

## Uso
1. Adjuntar la estrategia a un instrumento y portfolio en Designer, Runner o una aplicación host personalizada de StockSharp.
2. Configurar el marco temporal de vela deseado, la distancia de ruptura, stop-loss, take-profit y parámetros de volumen.
3. Iniciar la estrategia. Se suscribirá automáticamente a las velas elegidas, monitoreará cada nueva vela para un movimiento de ruptura y colocará órdenes de mercado cuando se cumplan las condiciones configuradas.

## Fuente original
- Asesor experto de MetaTrader 5: `MQL/244/grr-al.mq5`
