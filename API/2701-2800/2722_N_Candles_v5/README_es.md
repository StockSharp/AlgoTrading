# Estrategia N Candles v5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La estrategia N Candles v5 busca rachas de velas idénticas y abre una operación
en la misma dirección tan pronto como aparece la racha requerida. La implementación
MQL original de Vladimir Karputov ha sido traducida a la API de alto nivel de
StockSharp. La estrategia opera únicamente en velas cerradas y puede ejecutarse en
cualquier marco temporal, siendo las velas de una hora el valor predeterminado para
la versión de StockSharp.

## Lógica de Trading
1. Cuando una vela cierra, la estrategia la clasifica como alcista (cierre por encima
   de apertura), bajista (cierre por debajo de apertura) o neutral (cierre igual a apertura).
2. Las velas alcistas consecutivas aumentan el contador de racha alcista mientras
   reinician el contador bajista, y viceversa para las velas bajistas. Las velas
   neutrales reinician ambos contadores.
3. Si el contador de racha alcista alcanza el valor configurado de `CandlesCount` y
   la posición neta actual es plana o corta, la estrategia envía una compra a mercado.
   La exposición corta se cubre primero y luego se añade el `TradeVolume` configurado
   para establecer una posición largo.
4. Si el contador de racha bajista alcanza `CandlesCount` y la posición es plana
   o larga, la estrategia vende a mercado, cubriendo primero cualquier exposición
   larga antes de entrar corto.
5. Las operaciones solo se abren dentro de la ventana opcional de sesión de trading
   definida por `StartHour` y `EndHour`. Las acciones de protección (take profit,
   stop loss y trailing) continúan operando fuera de la sesión para garantizar que
   las posiciones se gestionen de forma segura.
6. La estrategia se niega a aumentar la exposición más allá de `MaxNetVolume`,
   reflejando la salvaguarda de volumen de la versión MQL.

## Gestión de Riesgo
- **Take Profit / Stop Loss** – expresados en pips y convertidos a distancias de
  precio absolutas usando el paso de precio del instrumento. Ambos niveles son
  opcionales y pueden desactivarse estableciendo el valor correspondiente en cero.
- **Trailing Stop** – se activa después de que el precio avanza `TrailingStopPips`
  desde el precio de entrada. Una vez activo, el stop se ajusta cada vez que el precio
  se mueve un `TrailingStepPips` adicional en la dirección de la operación.
- **Filtro de Sesión** – `UseTradingHours` habilita el filtro de hora de inicio y fin,
  impidiendo nuevas entradas fuera de la ventana seleccionada mientras permite que
  la gestión de riesgo cierre posiciones.
- **Volumen Neto Máximo** – la posición absoluta (larga o corta) nunca puede superar
  `MaxNetVolume`.

## Parámetros
| Parámetro | Descripción | Por defecto |
|-----------|-------------|-------------|
| `TradeVolume` | Tamaño de orden usado para nuevas entradas. | `1` |
| `CandlesCount` | Número de velas idénticas consecutivas requeridas para una señal. | `3` |
| `TakeProfitPips` | Distancia del take profit en pips (0 desactiva). | `50` |
| `StopLossPips` | Distancia del stop loss en pips (0 desactiva). | `50` |
| `TrailingStopPips` | Distancia que activa el trailing stop (0 desactiva). | `10` |
| `TrailingStepPips` | Progreso adicional requerido antes de ajustar el trailing stop. | `4` |
| `UseTradingHours` | Habilita el filtro de horas de trading. | `true` |
| `StartHour` | Primera hora (0–23) cuando se permiten nuevas posiciones. | `11` |
| `EndHour` | Última hora (0–23) cuando se permiten nuevas posiciones. | `18` |
| `MaxNetVolume` | Tamaño máximo absoluto de posición permitido. | `2` |
| `CandleType` | Tipo de datos de vela a analizar. Por defecto velas de 1 hora. | `TimeSpan.FromHours(1)` |

## Notas de Uso
- La estrategia se suscribe a datos de velas a través de la API de alto nivel
  `SubscribeCandles` y funciona con cualquier instrumento que proporcione series de velas.
- Dado que la lógica se basa en barras completadas, es más adecuada para marcos
  temporales intradía o superiores donde el ruido de mercado entre cierres es menos impactante.
- Ajuste la configuración de riesgo basada en pips según el tamaño del tick del instrumento.
- Al desplegar en instrumentos con diferencias de spread significativas, verifique los
  parámetros del trailing stop para que no sea activado por una ampliación normal del spread.
