# Estrategia Blau SM Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión en C# del expert original de MetaTrader 5 `Exp_BlauSMStochastic` construido alrededor del oscilador Blau SM Stochastic. El indicador mide la distancia entre el precio y el rango de trading reciente, aplica múltiples etapas de suavizado y compara el resultado con una línea de referencia suavizada. La estrategia trabaja en velas completadas (marco temporal de 4 horas por defecto) y permite el trading en ambas direcciones.

## Lógica del indicador
1. Calcular el máximo más alto y el mínimo más bajo durante `LookbackLength` barras.
2. Construir una serie de precio sin tendencia: `sm = price - (HH + LL) / 2` donde `price` es el tipo de precio aplicado.
3. Suavizar la serie sin tendencia secuencialmente mediante tres medias móviles con longitudes `FirstSmoothingLength`, `SecondSmoothingLength` y `ThirdSmoothingLength` usando el `SmoothMethod` seleccionado (SMA, EMA, SMMA o LWMA).
4. Suavizar el semirango `(HH - LL) / 2` con la misma secuencia triple para normalizar la volatilidad.
5. Formar la línea principal del oscilador como `100 * smoothed(sm) / smoothed(range)`.
6. Suavizar la línea principal con `SignalLength` para obtener la línea de señal.

El parámetro `Phase` se mantiene por compatibilidad con la versión MQL pero no es utilizado por el motor de suavizado simplificado.

## Modos de trading
- **Breakdown**: monitorea los cruces de cero de la línea principal. Un cruce de positivo a no positivo abre un largo y cierra cortos. Un cruce de negativo a no negativo abre un corto y cierra largos.
- **Twist**: rastrea giros de momentum. Si la línea principal forma un mínimo local (el valor sube después de bajar), se desencadena una entrada larga, mientras que un máximo local (el valor baja después de subir) desencadena un corto. Las posiciones opuestas se cierran en consecuencia.
- **CloudTwist**: observa los cruces entre la línea principal y la línea de señal. Un cruce descendente de la línea principal a través de la línea de señal abre un largo y cierra cortos, mientras que un cruce ascendente abre un corto y cierra largos.

Los interruptores de entrada y salida (`EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit`) permiten deshabilitar operaciones específicas manteniendo los cálculos del indicador intactos.

## Gestión de riesgo
`TakeProfitPoints` y `StopLossPoints` se convierten a distancias de precio absolutas usando el paso de precio del instrumento y se pasan al bloque de protección incorporado a través de `StartProtection`. Establézcalos en cero para deshabilitar el límite correspondiente.

## Parámetros
- `CandleType` *(DataType, predeterminado: marco temporal de 4 horas)* – marco temporal usado para la suscripción de velas y cálculos de indicadores.
- `Mode` *(BlauSmStochasticModes, predeterminado: Twist)* – selecciona el modo de generación de señales (Breakdown, Twist, CloudTwist).
- `SignalBar` *(int, predeterminado: 1)* – número de barras para desplazar los valores del indicador al evaluar señales, reproduciendo la lógica `SignalBar` original.
- `LookbackLength` *(int, predeterminado: 5)* – barras usadas para calcular los valores más altos y más bajos.
- `FirstSmoothingLength` *(int, predeterminado: 20)* – longitud de la primera etapa de suavizado.
- `SecondSmoothingLength` *(int, predeterminado: 5)* – longitud de la segunda etapa de suavizado.
- `ThirdSmoothingLength` *(int, predeterminado: 3)* – longitud de la tercera etapa de suavizado.
- `SignalLength` *(int, predeterminado: 3)* – longitud de suavizado de la línea de señal.
- `SmoothMethod` *(BlauSmSmoothMethods, predeterminado: EMA)* – familia de medias móviles aplicada a todas las etapas de suavizado (SMA, EMA, SMMA, LWMA).
- `PriceType` *(BlauSmAppliedPrices, predeterminado: Close)* – precio aplicado usado para alimentar el oscilador (cierre, apertura, máximo, mínimo, mediana, típico, ponderado, simple, cuartil, variantes de seguimiento de tendencia, Demark).
- `EnableLongEntry` *(bool, predeterminado: true)* – permitir la apertura de posiciones largas.
- `EnableShortEntry` *(bool, predeterminado: true)* – permitir la apertura de posiciones cortas.
- `EnableLongExit` *(bool, predeterminado: true)* – permitir el cierre de posiciones largas.
- `EnableShortExit` *(bool, predeterminado: true)* – permitir el cierre de posiciones cortas.
- `TakeProfitPoints` *(int, predeterminado: 2000)* – distancia de take-profit fija expresada en puntos del instrumento.
- `StopLossPoints` *(int, predeterminado: 1000)* – distancia de stop-loss fija expresada en puntos del instrumento.

## Notas
- El motor de suavizado actualmente soporta medias móviles clásicas (SMA, EMA, SMMA, LWMA). Los modos exóticos de la biblioteca MQL (JMA, JurX, etc.) no están disponibles en StockSharp y por lo tanto no están incluidos.
- Phase se preserva como parámetro por completitud; ajústelo solo con fines de documentación.
- Funciona con cualquier símbolo soportado por StockSharp. Ajuste el tipo de vela, las longitudes de suavizado y los stops para que coincidan con la volatilidad del instrumento.
