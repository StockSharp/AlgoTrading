# Estrategia Alli Heik
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Alli Heik es una conversión del asesor experto MetaTrader 5 "AlliHeik". Opera el **Heiken Ashi Smoothed Oscillator** (HASO) publicado originalmente por mladen. El indicador construye una vela Heiken Ashi personalizada suavizando los precios brutos de apertura, máximo, mínimo y cierre con una media móvil seleccionable, aplica un paso de suavizado adicional al punto medio de Heiken Ashi, y luego mide la diferencia barra a barra de ese valor suavizado. Una media móvil de la diferencia forma la línea de señal.

Las decisiones de trading se toman en el cruce del oscilador y la línea de señal evaluada en velas completamente cerradas. La estrategia ofrece un modo inverso opcional, la capacidad de cerrar automáticamente las posiciones opuestas, manejo estático de stop-loss/take-profit, y un trailing stop que imita la lógica de pasos de la versión original de MetaTrader.

## Reglas de trading

1. **Preparación del indicador**
   - Pre-suavizar datos OHLC con una de SMA, EMA, SMMA o LWMA.
   - Construir velas Heiken Ashi a partir de los datos suavizados y promediar apertura/cierre para obtener un punto medio.
   - Post-suavizar el punto medio y calcular el oscilador como la diferencia entre valores suavizados consecutivos.
   - Suavizar el oscilador con una media móvil configurable para crear la línea de señal.
2. **Condiciones de entrada**
   - *Modo normal*: abrir un **largo** cuando el oscilador cruza **por debajo** de la línea de señal, abrir un **corto** cuando cruza **por encima** de la línea de señal (reproduciendo exactamente la lógica MQL).
   - *Modo inverso*: intercambiar las condiciones de largo y corto.
   - Las señales se evalúan solo en velas terminadas. Las posiciones existentes pueden opcionalmente cerrarse antes de entrar en una nueva operación en la dirección opuesta.
3. **Gestión de salidas**
   - Las distancias de stop-loss y take-profit estáticas se expresan en pips y se convierten a precio usando el tamaño de tick y los decimales del instrumento.
   - Un trailing stop se activa una vez que el precio avanza *TrailingStop + TrailingStep* pips en ganancia. El stop se desplaza entonces a `precio actual - TrailingStop` para largos (o `precio actual + TrailingStop` para cortos) y solo se mueve si el nuevo stop está al menos `TrailingStep` pips más allá del nivel anterior.
   - Las salidas manuales se emiten si el precio toca el stop o el objetivo configurados.

## Parámetros

- **Volume** – volumen de la orden en lotes.
- **Stop Loss (pips)** – distancia para el stop de protección; establecer en 0 para deshabilitar.
- **Take Profit (pips)** – distancia para el objetivo de ganancia; establecer en 0 para deshabilitar.
- **Trailing Stop (pips)** – distancia del trailing stop; establecer en 0 para deshabilitar el trailing.
- **Trailing Step (pips)** – avance mínimo más allá del trailing stop antes de que se mueva el stop (debe ser positivo cuando el trailing está habilitado).
- **Reverse Signals** – invertir la interpretación largo/corto del cruce del oscilador.
- **Close Opposite** – cerrar una posición existente antes de abrir una nueva operación en la dirección opuesta.
- **Pre Smooth Period / Method** – período y tipo de media móvil usado para suavizar los datos OHLC brutos.
- **Post Smooth Period / Method** – parámetros de media móvil para suavizar el punto medio de Heiken Ashi.
- **Signal Period / Method** – parámetros de media móvil para la línea de señal del oscilador.
- **Candle Type** – fuente de velas usada para los cálculos (marco temporal predeterminado de 15 minutos).

## Notas de implementación

- La conversión reproduce el Heiken Ashi Smoothed Oscillator original encadenando indicadores de media móvil de StockSharp (SMA, EMA, SMMA, LWMA) para pre-suavizar precios, construir la serie Heiken Ashi y derivar la diferencia del oscilador.
- Las distancias en pips se traducen a desplazamientos de precio absolutos usando el tamaño de tick y la precisión decimal del instrumento, coincidiendo con el manejo de 3/5 dígitos de MetaTrader.
- Las comprobaciones manuales de stop/objetivo y el trailing stop basado en pasos se ejecutan en cada vela terminada, reflejando de cerca el comportamiento de la versión MQL.
- Las señales se procesan solo cuando todos los valores requeridos están disponibles; los estados parciales del indicador se ignoran hasta que se hayan acumulado suficientes datos.

No se proporciona traducción Python en este directorio.
