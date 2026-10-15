# Estrategia EA Trix
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia EA Trix replica la lógica del asesor experto de MetaTrader 5 que combina el indicador *TRIX ARROWS* con
herramientas básicas de gestión de riesgo. El sistema espera que la triple media móvil exponencial (TRIX) y su línea de señal
se crucen antes de entrar en nuevas posiciones. Puede reaccionar inmediatamente en la vela de señal o retrasar la ejecución
hasta la siguiente barra, emulando el comportamiento original de "operar al cierre de barra".

## Lógica de Trading

1. Construir dos medias móviles exponenciales triple-suavizadas:
   - TRIX se calcula aplicando tres EMAs con la longitud **TRIX EMA** al cierre de la vela y tomando la tasa de cambio de un
     bar del tercer suavizado.
   - La línea de señal se calcula de la misma manera pero usa la longitud **Signal EMA**.
2. Detectar cambios de dirección a través de cruces:
   - Cuando la línea de señal cruza **por encima** del TRIX, la estrategia prepara una entrada larga.
   - Cuando la línea de señal cruza **por debajo** del TRIX, prepara una entrada corta.
3. Dependiendo del ajuste **Trade On Close**, la estrategia:
   - Ejecuta inmediatamente al precio de cierre de la barra de señal; o
   - Pone en cola la orden y la ejecuta en la apertura de la siguiente barra (coincidiendo con la opción del EA de MT5 para
     operar en barras cerradas).
4. Antes de abrir una nueva posición, el algoritmo revierte automáticamente cualquier exposición contraria para que solo exista
   una posición neta a la vez.

## Gestión de Posiciones

- **Stop loss** – distancia fija opcional desde el precio de llenado. Se deshabilita cuando se establece en cero.
- **Take profit** – objetivo de beneficio opcional. Se deshabilita cuando se establece en cero.
- **Break-even** – una vez que el precio avanza a favor de la operación por la distancia seleccionada, el stop se mueve al
  precio de entrada.
- **Trailing stop** – después de que el precio se mueve por la distancia de trailing, el stop sigue al precio con el incremento
  mínimo de **Trailing Step** seleccionado.
- Las salidas protectoras se evalúan en cada vela completada usando los valores high/low de la vela. Cuando se activa una salida
  protectora, la posición se cierra con una orden de mercado.

## Parámetros

| Nombre | Descripción |
| ------ | ----------- |
| `CandleType` | Tipo de datos (marco temporal) de las velas procesadas por la estrategia. |
| `Volume` | Tamaño de posición usado para nuevas entradas. Las posiciones existentes se revierten automáticamente cuando es necesario. |
| `EmaPeriod` | Longitud de las medias móviles exponenciales usadas para calcular la curva TRIX. |
| `SignalPeriod` | Longitud de las medias móviles exponenciales usadas para calcular la curva de señal. |
| `TradeOnCloseBar` | Si `true`, las entradas se ponen en cola y se ejecutan en la apertura de la siguiente barra. Si `false`, la ejecución ocurre inmediatamente al cierre de la barra de señal. |
| `StopLoss` | Distancia desde el precio de entrada hasta el stop protector. Establecer en `0` para deshabilitar. |
| `TakeProfit` | Distancia al objetivo de beneficio. Establecer en `0` para deshabilitar. |
| `TrailingStop` | Distancia para que se active el trailing stop. Establecer en `0` para deshabilitar. |
| `TrailingStep` | Incremento mínimo aplicado al actualizar el trailing stop. |
| `BreakEven` | Distancia requerida para mover el stop al precio de entrada. Establecer en `0` para deshabilitar. |

## Notas de Uso

- La estrategia se suscribe a un único feed de velas y se apoya exclusivamente en velas completadas según lo requerido por las
  directrices del API de alto nivel de StockSharp.
- Las distancias predeterminadas de gestión de riesgo se expresan en unidades de precio. Ajustarlas de acuerdo con el tamaño
  del tick del instrumento operado.
- Dado que las órdenes se envían via comandos de mercado, se asume que el precio de llenado es el cierre de la vela (o apertura
  para señales en cola) en backtests.

## Notas de Conversión

- El experto MQL5 original usa el indicador externo *TRIX ARROWS* (código 19056). La conversión reconstruye los mismos cálculos
  usando instancias de `ExponentialMovingAverage` de StockSharp y lógica de tasa de cambio sin depender de buffers personalizados.
- La gestión de riesgo de MT5 dependía de órdenes stop y límite del lado del broker. En StockSharp, las salidas protectoras se
  replican monitoreando los extremos de velas y emitiendo órdenes de mercado.
- Las alertas, notificaciones de sonido y parámetros específicos del broker se omitieron porque no son parte de la lógica de
  trading central.
