# Estrategia de Price Action Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port en C# del asesor experto "PRICE_ACTION" de MetaTrader. Combina fractales de Williams con medias móviles ponderadas, filtros de Momentum y MACD para operar rupturas confirmadas por la acción del precio en el marco temporal seleccionado.

## Idea

1. Analizar solo velas completadas; cada decisión se toma al cierre de barra del marco temporal configurado.
2. Detectar nuevos fractales alcistas o bajistas usando una ventana de 5 velas. Un fractal bajista nuevo señala un posible soporte, mientras que un fractal alcista señala una posible resistencia.
3. Confirmar el sesgo direccional con dos medias móviles ponderadas linealmente (LWMA). Las operaciones largas requieren que la LWMA rápida esté por encima de la lenta; las cortas requieren lo contrario.
4. Validar el Momentum comprobando la desviación absoluta del indicador Momentum desde el nivel neutro de 100 en el marco temporal superior.
5. Usar un filtro MACD (12,26,9 por defecto): los setups alcistas exigen que el MACD esté por encima de su línea de señal, los bajistas exigen el MACD por debajo de la señal.
6. Una vez que todos los filtros coinciden, entrar en la dirección de la ruptura y gestionar la posición con stops fijos, un trailing stop y un desplazamiento de break-even opcional.

## Reglas de entrada

- **Entrada larga**
  - Se forma un nuevo fractal bajista en la vela actual (patrón de cinco barras).
  - Fast LWMA &gt; Slow LWMA.
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`.
  - Línea principal MACD &gt; línea de señal MACD.
  - El tamaño de la posición se basa en el volumen de la estrategia y está limitado por `MaxPositionUnits`.

- **Entrada corta**
  - Se forma un nuevo fractal alcista en la vela actual.
  - Fast LWMA &lt; Slow LWMA.
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`.
  - Línea principal MACD &lt; línea de señal MACD.

## Reglas de salida

- Stop-loss fijo (`StopLossPoints`) y take-profit fijo (`TakeProfitPoints`) expresados en pasos de precio.
- Trailing stop opcional (`TrailingStopPoints`) que sigue el precio más favorable una vez que la posición gana al menos la distancia de trailing.
- Protección de break-even opcional: después de alcanzar `BreakEvenTriggerPoints` el stop se desplaza a `EntryPrice ± BreakEvenOffsetPoints`.
- Las salidas se realizan con órdenes de mercado; todos los cálculos se basan en máximos/mínimos de velas para detectar impactos en el stop.

## Gestión de posición

- La estrategia mantiene una posición agregada única por símbolo.
- `Volume` define el tamaño de orden base. Al revertir, la estrategia primero cierra la exposición opuesta y luego abre una nueva posición con el tamaño solicitado.
- `MaxPositionUnits` limita el valor absoluto de la posición para evitar sobredimensionamiento.

## Parámetros

- `CandleType` – marco temporal utilizado para cada indicador y decisión (equivalente a la variable MQL `T`).
- `FastMaPeriod` / `SlowMaPeriod` – longitudes de las medias móviles ponderadas (`FastMA`, `SlowMA`).
- `MomentumPeriod` – longitud de retrovisión del Momentum (fijado en 14 en el script MQL).
- `MomentumThreshold` – desviación absoluta mínima de 100 requerida para confirmar el Momentum (`Mom_Buy`/`Mom_Sell`).
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – configuración del MACD (12/26/9 por defecto).
- `StopLossPoints`, `TakeProfitPoints` – distancias en pasos de precio para órdenes protectoras (`Stop_Loss`, `Take_Profit`).
- `TrailingStopPoints` – distancia del trailing stop (`TrailingStop`).
- `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` – activador y desplazamiento de break-even (`WHENTOMOVETOBE`, `PIPSTOMOVESL`).
- `FractalLifetime` – número de velas que un fractal detectado permanece válido (`CandlesToRetrace`).
- `MaxPositionUnits` – tamaño máximo absoluto de posición (restricción `Max_Trades` en unidades de lote).
- `EnableTrailing`, `EnableBreakEven`, `UseStopLoss`, `UseTakeProfit` – interruptores para los mecanismos de salida respectivos.

## Diferencias respecto al EA original

- Características a nivel de cartera como take-profit basado en dinero, stop de capital y alertas por email/notificación no están implementadas.
- Las rutinas de optimización de lotes de MetaTrader están simplificadas; la estrategia usa la normalización de volumen de StockSharp.
- Las órdenes protectoras se ejecutan con salidas de mercado en lugar de modificaciones de órdenes pendientes porque StockSharp gestiona el riesgo de manera diferente.

## Archivos

- `CS/PriceActionFractalStrategy.cs` – implementación de la estrategia en C#.
- La versión en Python aún no está disponible.
