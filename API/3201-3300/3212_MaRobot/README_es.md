# Estrategia de MaRobot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Implementa un sistema de cruce de medias móviles basado en barras que opera en un marco temporal intradía configurable mientras usa filtros diarios de ADX y RSI.
- Usa los bindings de alto nivel de StockSharp para calcular dos medias móviles simples junto con detectores de oscilación `Lowest`/`Highest` e indicadores diarios `AverageDirectionalIndex` y `RelativeStrengthIndex`.
- Recrea la lógica de protección original de MT4: take-profit por porcentaje, stop-loss basado en oscilaciones, y un stop de break-even opcional una vez que se alcanza una ganancia mínima.

## Indicadores
- `SimpleMovingAverage` (rápida y lenta) en el marco temporal principal.
- `Lowest` / `Highest` para capturar los precios extremos de las últimas `BackClose` velas para la colocación del stop.
- Valores diarios de `AverageDirectionalIndex` y `RelativeStrengthIndex` para filtros de fuerza de tendencia e impulso.

## Parámetros
- `CandleType` – marco temporal principal (predeterminado: velas de 15 minutos).
- `FastPeriod`, `SlowPeriod` – longitudes de las líneas SMA rápida y lenta.
- `AdxThreshold` – valor máximo permitido del ADX diario para habilitar nuevas entradas.
- `RsiThreshold` – nivel de RSI diario para entradas largas (la entrada corta usa `100 - RsiThreshold`).
- `TakeProfitRatio` – distancia fraccional entre el precio de entrada y el objetivo de ganancia.
- `StopLossPoints` – distancia del stop de protección (en puntos de instrumento) que se activa al alcanzar `ProtectThreshold`.
- `ProtectThreshold` – proporción mínima de ganancia abierta que activa el stop de protección.
- `BackClose` – número de velas completadas usadas para el cálculo del stop de máximo/mínimo de oscilación.
- `DailyAdxPeriod`, `DailyRsiPeriod` – longitudes de los indicadores diarios.

## Reglas de trading
1. Trabajar solo en velas terminadas para coincidir con el asesor experto de MT4.
2. Esperar hasta que todos los indicadores estén completamente formados y los valores diarios estén disponibles.
3. **Filtros de entrada**:
   - Rechazar nuevas posiciones cuando el ADX diario supera `AdxThreshold`.
   - La entrada larga requiere que la SMA rápida cruce por encima de la SMA lenta y el RSI diario esté por debajo de `RsiThreshold`.
   - La entrada corta requiere que la SMA rápida cruce por debajo de la SMA lenta y el RSI diario esté por encima de `100 - RsiThreshold`.
4. Al entrar, almacenar el extremo de oscilación (`Lowest` para largos, `Highest` para cortos) como referencia de stop manual.
5. **Lógica de salida** mientras una posición está activa:
   - Cerrar con `TakeProfitRatio` de ganancia medido desde el precio de entrada almacenado.
   - Cerrar si el cierre de la vela rompe el nivel de stop de oscilación almacenado.
   - Cerrar en un cruce de media móvil opuesto.
   - Después de que la ganancia supere `ProtectThreshold`, armar un stop de estilo break-even desplazado por `StopLossPoints` (redondeado al tamaño de tick) y cerrar si el precio retrocede a través de él.
6. Restablecer todo el estado interno cuando la posición neta vuelve a cero.

## Notas
- Todos los comentarios en el código C# se mantienen en inglés según las directrices del repositorio.
- La estrategia depende únicamente de suscripciones de alto nivel de StockSharp a través de `Bind`, evitando búferes de indicadores manuales.
- La traducción a Python se omite intencionalmente según las instrucciones de la tarea.
