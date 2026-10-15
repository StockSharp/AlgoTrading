# Estrategia de diferencial de cruce de media móvil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una versión StockSharp del asesor experto MQL4 **"EA - Media móvil"** (archivo `EA - Moving Average.mq4`).
Opera con un solo instrumento reaccionando a los cruces de medias móviles que se detectan en la apertura de cada nueva vela.

## Idea central

- Utilice una media móvil exponencial rápida y lenta (EMA) calculada sobre la serie de velas seleccionada.
- Espere hasta que haya una nueva vela disponible y evalúe los valores EMA de las dos velas completadas más recientes, replicando las llamadas `iMA(..., shift=1/2)` del código original.
- Abra una **posición larga** cuando el EMA rápido haya cruzado por encima del EMA lento en la vela anterior mientras que la vela anterior todavía tenía el EMA rápido por debajo del EMA lento.
- Abra una **posición corta** cuando el EMA rápido haya cruzado por debajo del EMA lento en la vela anterior mientras que la vela anterior todavía tenía el EMA rápido por encima del EMA lento.
- Sólo se puede abrir una posición a la vez. La estrategia ignora las nuevas señales hasta que se cierran todas las órdenes.

## Gestión de órdenes

- Antes de realizar un pedido, se comprueba el diferencial actual. Si la mejor oferta y la mejor oferta están disponibles, el diferencial se convierte en puntos de instrumento y se compara con `MaxSpreadPoints`. Las señales que exceden el límite se omiten, al igual que la guardia `MarketInfo(..., MODE_SPREAD)` original.
- Después de enviar una orden de mercado, la estrategia refleja niveles de protección alrededor del precio de entrada:
  - El stop-loss se coloca en el valor EMA lenta de la vela anterior más/menos el `StopLossPoints` configurado.
  - La toma de ganancias se establece a la misma distancia del precio de entrada que el stop-loss, creando un objetivo simétrico como en la implementación MQL (`Ask + (Ask - StopLoss)` / `Bid - (StopLoss - Bid)`).
- Todas las distancias de precios expresadas en puntos se traducen a precios absolutos a través del instrumento `PriceStep`, por lo que el comportamiento coincide con la configuración basada en puntos de MetaTrader.

## Notas de conversión

- El experto original permite elegir diferentes modos de media móvil, pero su valor predeterminado usa EMA (`MAMode = 1`). La versión StockSharp se centra en EMA para mantener la implementación concisa; Si es necesario, se pueden agregar diferentes algoritmos de suavizado.
- El volumen comercial se proporciona a través del parámetro `TradeVolume` y se asigna a `Strategy.Volume` durante `OnStarted`.
- La estrategia se basa únicamente en los datos de velas proporcionados a través de `CandleType`. No hay colecciones de indicadores adicionales ni buffers históricos además del historial de dos valores EMA necesario para detectar cruces.

## Parámetros

- `CandleType`: tipo de datos de vela y período de tiempo al que suscribirse.
- `FastPeriod` – duración de la EMA rápida (el valor predeterminado es 21).
- `SlowPeriod`: duración del EMA lento (el valor predeterminado es 84).
- `StopLossPoints` – distancia de stop-loss en puntos del instrumento en relación con el EMA lenta.
- `MaxSpreadPoints`: margen máximo permitido en puntos antes de que se rechace un nuevo pedido.
- `TradeVolume`: tamaño de lote utilizado al enviar órdenes de mercado.

## Consejos de uso

1. Seleccione el símbolo y el período de tiempo de la vela antes de comenzar la estrategia para que los valores de EMA coincidan con el gráfico deseado en MetaTrader.
2. Proporcione datos de nivel 1 (mejor oferta/demanda) si desea que el filtro de diferencial funcione en tiempo real; de lo contrario, la estrategia supone que el diferencial es aceptable.
3. Asegúrese de que el valor tenga un `PriceStep` válido. Sin él, la estrategia no puede traducir distancias basadas en puntos en precios absolutos y se saltará la colocación de órdenes de protección.
