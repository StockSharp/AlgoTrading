# Estrategia MostasHaR15 Pivot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica el comportamiento del Asesor Experto original **MostasHaR15 Pivot** MQL5 utilizando la API de alto nivel de StockSharp. Combina cálculos clásicos de pivotes diarios de suelo con filtros de momentum del ADX, diferenciales de EMA y el histograma MACD (OsMA). La estrategia opera en un flujo de velas intradía (1 hora por defecto) y consume la vela diaria completada anterior para reconstruir el mapa de pivotes en cada barra.

## Lógica de trading
- **Cuadrícula de pivote** – los máximos, mínimos y cierres diarios anteriores se usan para calcular el pivote principal (P), tres niveles de resistencia (R1–R3), tres niveles de soporte (S1–S3) y seis puntos medios (M0–M5). El cierre de la vela actual se compara con esta escalera para identificar el segmento de soporte y resistencia circundante. Un caso especial heredado del EA mapea los precios entre M5 y R3 de vuelta al rango S3/M0.
- **Filtro de distancia** – las operaciones solo se permiten cuando la distancia al nivel de take-profit más cercano es mayor que `MinimumDistancePips` (14 pips por defecto), que coincide con los filtros originales `dif1`/`dif2`.
- **Las entradas largas** requieren todo lo siguiente:
  - La línea principal ADX supera `AdxThreshold` (20) y +DI está tanto subiendo como por encima de –DI.
  - La EMA de 5 períodos en los cierres de velas está al menos `EmaSlopePips` (5 pips) por encima de la EMA de 8 períodos en las aperturas de velas, y la barra anterior mostró el mismo ordenamiento alcista de EMA.
  - El histograma MACD (OsMA) aumentó en comparación con la barra anterior.
- **Las entradas cortas** replican las condiciones largas con fuerza de –DI, spread EMA bajista y un histograma MACD cayendo.
- Solo se permite una posición neta. Las órdenes se colocan con ejecución de mercado mediante `BuyMarket()`/`SellMarket()`.

## Gestión de posición
- **Stop-loss** – opcional, ubicado `StopLossPips` por debajo/encima del precio de entrada. Establecer el parámetro en `0` deshabilita el stop inicial, como en el EA.
- **Take-profit** – fijo en el límite de pivote más cercano que rodea el precio actual cuando se abre la posición.
- **Stop trailing** – replica la lógica trailing original. Una vez que el precio avanza más de `TrailingStopPips + TrailingStepPips` desde la entrada, el stop se mueve para mantener una distancia trailing de `TrailingStopPips`. El trailing puede deshabilitarse estableciendo `TrailingStopPips` en `0`.
- Si el stop-loss, trailing stop o take-profit se alcanza durante una vela, la posición se liquida al cierre de esa vela.

## Parámetros de estrategia
| Parámetro | Descripción | Por defecto |
|-----------|-------------|---------|
| `CandleType` | Serie de velas intradía usada para trading. | Marco temporal de 1 hora |
| `DailyCandleType` | Serie de velas diarias para cálculos de pivote. | Marco temporal de 1 día |
| `StopLossPips` | Distancia del stop-loss en pips. Establecer `0` para deshabilitar. | 20 |
| `TrailingStopPips` | Distancia del stop trailing en pips. | 5 |
| `TrailingStepPips` | Movimiento mínimo favorable antes de que el trailing se actualice. Debe ser >0 si el trailing está habilitado. | 5 |
| `MinimumDistancePips` | Distancia mínima en pips al límite de pivote más cercano antes de entrar a una operación. | 14 |
| `EmaSlopePips` | Separación requerida entre la EMA de cierre y la EMA de apertura. | 5 |
| `AdxThreshold` | Lectura mínima de ADX para operaciones largas y cortas. | 20 |
| `AdxPeriod` | Longitud del indicador ADX. | 14 |
| `EmaClosePeriod` | Período EMA aplicado a los cierres de velas. | 5 |
| `EmaOpenPeriod` | Período EMA aplicado a las aperturas de velas. | 8 |
| `MacdFastPeriod` | Período EMA rápida dentro del histograma MACD. | 12 |
| `MacdSlowPeriod` | Período EMA lenta dentro del histograma MACD. | 26 |
| `MacdSignalPeriod` | Período EMA de señal dentro del histograma MACD. | 9 |

## Notas de conversión
- La estrategia mantiene el comportamiento inusual del EA donde el rango de precios entre el nivel medio M5 y la resistencia R3 se mapea de vuelta al par de soporte/resistencia S3/M0.
- Todos los valores de indicadores se procesan solo en velas completadas. No se almacenan colecciones históricas; todo el estado se mantiene en campos escalares según las pautas del repositorio.
- Los comentarios en la estrategia permanecen en inglés según las instrucciones del repositorio.

## Consejos de uso
- Ajuste `CandleType` y `DailyCandleType` al aplicar la estrategia a mercados con diferentes sesiones de trading.
- Debido a que la lógica de stop-loss y trailing se evalúa en velas cerradas, puede aparecer slippage adicional en mercados rápidos en comparación con la ejecución a nivel de tick en el EA original.
