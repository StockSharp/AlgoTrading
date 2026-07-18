# Estrategia de impulso RRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **RRS Impulse Strategy** es una versión StockSharp de alto nivel del MetaTrader asesor experto "RRS Impulse". El robot original
filtros de bandas combinados RSI, Stochastic y Bollinger, rotados entre varios modos de intensidad de señal y paradas protectoras utilizadas y
salidas finales virtuales. Esta versión de C# mantiene el mismo comportamiento pero se basa exclusivamente en el StockSharp nivel alto API: vela
las suscripciones alimentan los indicadores, mientras que `BuyMarket`, `SellMarket` y `ClosePosition` ejecutan las órdenes.

## Lógica de trading

1. **Modos de indicador**: elija entre cuatro opciones:
   - `Rsi`: opera con el oscilador cuando sale de la zona de sobrecompra/sobreventa.
   - `Stochastic`: requiere que %K y %D estén por encima o por debajo de los niveles configurados.
   - `BollingerBands`: reacciona ante cierres por encima de la banda superior o por debajo de la banda inferior.
   - `RsiStochasticBollinger`: se activa solo cuando los tres filtros confirman la misma dirección.
2. **Dirección comercial**: `Trend` sigue el indicador (la sobrecompra conduce a posiciones cortas, la sobreventa a posiciones largas). `CounterTrend` desvanece el
movimiento (la sobrecompra desencadena posiciones largas, la sobreventa activa posiciones cortas).
3. **Intensidad de la señal**: controla cuántos períodos de tiempo deben acordar antes de ingresar a una operación:
   - `SingleTimeFrame`: utilice únicamente el plazo base proporcionado por `CandleType`.
   - `MultiTimeFrame`: requiere alineación entre velas M1, M5, M15, M30, H1 y H4.
   - `Strong`: céntrese en el impulso intradiario marcando M1, M5, M15 y M30.
   - `VeryStrong`: exige confirmación de la escalera M1...H4 completa. Cuando el modo de indicador combinado está habilitado en cada período de tiempo
debe satisfacer *los* tres filtros.
4. **Gestión de riesgos**: cada posición rastrea el precio de ejecución promedio y monitorea tres condiciones de salida:
   - distancia fija de stop-loss en pips;
   - distancia fija de obtención de beneficios en pips;
   - trailing stop activado una vez que la ganancia excede `TrailingStartPips` y mantenido por `TrailingGapPips`.
Siempre que la dirección cambia, la estrategia llama a `ClosePosition()` primero para aplanarse y solo abre la operación opuesta después
el siguiente tick de confirmación.

## Parámetros

| grupo       | Nombre | Descripción |
|-------------|------|-------------|
| Datos        | `CandleType` | Serie de velas base procesadas para decisiones comerciales. |
| Órdenes      | `TradeVolume` | Volumen utilizado al enviar órdenes de mercado. |
| Riesgo        | `StopLossPips`, `TakeProfitPips`, `TrailingStartPips`, `TrailingGapPips` | Salidas protectoras virtuales expresadas en pips. |
| Señales     | `IndicatorMode`, `TradeDirection`, `SignalStrength` | Cambios de comportamiento copiados del bloque de entrada MQL. |
| RSI         | `RsiPeriod`, `RsiUpperLevel`, `RsiLowerLevel` | Configuración RSI para detección de sobrecompra/sobreventa. |
| Stochastic  | `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing`, `StochasticUpperLevel`, `StochasticLowerLevel` | Configuraciones lentas del oscilador estocástico. |
| Bollinger   | `BollingerPeriod`, `BollingerDeviation` | Bollinger Multiplicador de desviación y retroceso de bandas. |

Todos los parámetros admiten rangos de optimización idénticos a la versión MetaTrader donde tenía sentido (por ejemplo, detenerse y tomar distancias).
u umbrales del oscilador).

## Requisitos de datos

La estrategia necesita velas diminutas para la escalera de confirmación. Cuando `SignalStrength` solicita plazos adicionales, la estrategia
agrega automáticamente las suscripciones requeridas (`GetWorkingSecurities` las anuncia en el motor). No se utilizan comillas de nivel 1;
sólo los precios de cierre de las velas terminadas impulsan las entradas y salidas. Por lo tanto, la lógica de protección reproduce la parada/objetivo "virtual".
comportamiento del robot original.

## Notas sobre la conversión

- La rotación aleatoria de símbolos de EA se eliminó intencionalmente. Las estrategias StockSharp funcionan con un solo `Security`, por lo que el
El puerto se concentra en hacer coincidir la lógica del indicador y la gestión de riesgos, dejando la rotación del instrumento en manos del usuario.
- La gestión de órdenes se basa en el mercado: cuando la dirección cambia o se activa una condición de protección, se llama a `ClosePosition()`,
reflejando los bucles MetaTrader que se repitieron a través de los tickets.
- La conversión mantiene todos los comentarios en inglés y utiliza pestañas para sangría para cumplir con las pautas del repositorio.
