# Demostración de señal universal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el experto MetaTrader 5 "Señal universal" utilizando StockSharp API de alto nivel. Evalúa ocho patrones de mercado ponderados y los agrega en una única puntuación compuesta. Cuando la puntuación cruza umbrales configurables, la estrategia abre o cierra posiciones largas y cortas, utilizando opcionalmente órdenes de límite pendientes que vencen después de un número determinado de barras.

## Parámetros de estrategia
- `CandleType`: datos de velas utilizados para el análisis.
- `SignalThresholdOpen`: puntuación compuesta mínima requerida para abrir una posición.
- `SignalThresholdClose`: puntuación contraria necesaria para salir de una posición existente.
- `PriceLevel` – compensación de precio para colocar entradas de límite pendientes (0 significa ejecución de mercado).
- `StopLevel` / `TakeLevel`: distancias absolutas de stop-loss y take-profit utilizadas por el módulo de protección integrado.
- `SignalExpiration` – número de barras después de las cuales se cancelan las entradas pendientes aún activas.
- `Pattern0Weight`… `Pattern7Weight`: peso aplicado a cada patrón antes de la agregación.
- `UniversalWeight`: multiplicador final aplicado a la suma de todas las contribuciones del patrón.
- `ShortMaPeriod`, `LongMaPeriod`, `RsiPeriod`, `BollingerPeriod`, `BollingerWidth`, `TrendSmaPeriod`, `VolumeSmaPeriod`: configuraciones de indicadores utilizadas dentro de las comprobaciones de patrones.

## Lógica de trading
1. Suscríbase al flujo de velas configurado y vincule EMA, RSI, MACD señal, Bollinger bandas y SMA de soporte.
2. Después de cada vela terminada, calcule ocho patrones booleanos (alineación de tendencia, RSI impulso, MACD histograma, Bollinger posicionamiento, dirección de la vela y expansión de volumen).
3. Multiplica cada patrón por su peso, suma las contribuciones y aplica el peso global para obtener la puntuación final.
4. Cierre las posiciones abiertas cuando la puntuación cruce el umbral de cierre en la dirección opuesta.
5. Abra nuevas posiciones largas o cortas cuando la puntuación supere el umbral de apertura. Si `PriceLevel` es positivo, envíe una orden limitada compensada por la distancia configurada y cancélela automáticamente después de `SignalExpiration` barras.
6. `StartProtection` establece niveles fijos de stop-loss y take-profit para todas las posiciones utilizando los asistentes de gestión de riesgos de StockSharp.

La conversión mantiene el flujo de trabajo de ponderación flexible del experto MQL5 original mientras sigue las convenciones de codificación StockSharp y el procesamiento basado en indicadores.
