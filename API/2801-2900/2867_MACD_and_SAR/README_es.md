# Estrategia MACD y SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto original de MetaTrader "MACD and SAR". Evalúa la relación entre las líneas principal y de señal del MACD junto con el nivel del SAR Parabólico en cada vela completada. Interruptores configurables permiten invertir cada comparación para que la misma plantilla pueda usarse tanto para configuraciones a contratendencia como a favor de tendencia. Se permiten múltiples entradas siempre que no se supere el número máximo configurado de posiciones apiladas.

Cuando aparece una señal de compra, la exposición corta existente se cierra y se abre un nuevo lote largo (si no se ha alcanzado el límite). Del mismo modo, una señal corta cierra primero los largos y luego añade un lote corto. No hay órdenes adicionales de stop-loss o take-profit; las operaciones se cierran únicamente cuando se genera la señal opuesta.

## Lógica de la estrategia

1. Esperar una vela completada del marco temporal configurado.
2. Leer los valores del MACD (principal, señal, histograma) y el nivel del SAR Parabólico calculado sobre precios de cierre.
3. Evaluar las siguientes comparaciones, cada una de las cuales puede invertirse con su correspondiente parámetro booleano:
   - Línea principal del MACD vs. línea de señal.
   - Línea de señal del MACD vs. el nivel cero.
   - SAR Parabólico vs. precio de cierre.
4. Si las tres comparaciones para el lado largo se cumplen y la estrategia aún tiene margen para apilar nuevas posiciones, comprar el tamaño de lote especificado (incluyendo el volumen necesario para cerrar cortos).
5. Si las tres comparaciones para el lado corto se cumplen y el límite de apilamiento lo permite, vender el tamaño de lote especificado (incluyendo el volumen necesario para cerrar largos).

## Parámetros

- `TradeVolume` — volumen por operación individual (predeterminado `0.1`).
- `MaxPositions` — número máximo de posiciones apiladas en una dirección (predeterminado `10`).
- `MacdFastPeriod` — período de la EMA rápida del MACD (predeterminado `12`).
- `MacdSlowPeriod` — período de la EMA lenta del MACD (predeterminado `26`).
- `MacdSignalPeriod` — período de suavizado de la señal del MACD (predeterminado `9`).
- `SarStep` — paso de aceleración del SAR Parabólico (predeterminado `0.02`).
- `SarMaximum` — aceleración máxima del SAR Parabólico (predeterminado `0.2`).
- `BuyMacdGreaterSignal` — si es `true`, requiere MACD principal > señal para largos; de lo contrario espera lo contrario (predeterminado `true`).
- `BuySignalPositive` — si es `true`, requiere MACD señal > 0 para largos; de lo contrario espera señal < 0 (predeterminado `false`).
- `BuySarAbovePrice` — si es `true`, requiere SAR por encima del precio para largos; de lo contrario espera precio por encima del SAR (predeterminado `false`).
- `SellMacdGreaterSignal` — si es `true`, requiere MACD principal > señal para cortos; de lo contrario espera MACD principal < señal (predeterminado `false`).
- `SellSignalPositive` — si es `true`, requiere MACD señal > 0 para cortos; de lo contrario espera señal < 0 (predeterminado `true`).
- `SellSarAbovePrice` — si es `true`, requiere SAR por encima del precio para cortos; de lo contrario espera precio por encima del SAR (predeterminado `true`).
- `CandleType` — tipo/marco temporal de vela usado para el procesamiento de datos (predeterminado `15` minutos).

## Notas adicionales

- La estrategia depende únicamente de los cruces de indicadores; no hay stops de protección ni objetivos de beneficio.
- El apilamiento de posiciones se implementa comparando el volumen absoluto de la posición con `MaxPositions * TradeVolume` con una pequeña tolerancia para manejar el redondeo.
- Todas las operaciones se ejecutan con órdenes de mercado. Asegúrese de que la configuración de volumen de la cartera coincida con los instrumentos que planea operar.
- Añada reglas opcionales de protección de cartera si necesita límites de drawdown o stops de seguimiento; ninguno está incluido por defecto.
