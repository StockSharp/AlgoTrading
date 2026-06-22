# Estrategia de Dos MA Cuatro Niveles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el experto de MetaTrader "2MA_4Level" usando la API de alto nivel de StockSharp. Opera un único instrumento con dos medias móviles suavizadas (SMMA) calculadas sobre el precio mediano y observa cinco zonas relativas de cruce entre las curvas rápida y lenta. Las entradas solo se permiten cuando no hay posición abierta, y cada operación está protegida por offsets de stop-loss y take-profit en pips.

## Lógica

- Se calcula una SMMA rápida y una lenta sobre la serie de velas seleccionada (por defecto 50 y 130 períodos).
- Se evalúan los valores anteriores y actuales de la SMMA en la vela completada para detectar un cruce.
- El cruce se verifica contra cinco umbrales construidos a partir de la MA lenta:
  - la MA lenta sin offset,
  - MA lenta + pips de `MostTopLevel`,
  - MA lenta + pips de `TopLevel`,
  - MA lenta - pips de `LowermostLevel`,
  - MA lenta - pips de `LowerLevel`.
- Cuando la MA rápida cruza por encima de cualquier umbral, se abre una posición larga (si está flat). Un cruce por debajo de cualquier umbral abre una posición corta.
- Los niveles de stop-loss y take-profit se adjuntan a través de `StartProtection` usando el valor de pip del instrumento (`Security.PriceStep`).

La estrategia nunca piramida posiciones: una nueva operación solo puede abrirse después de que la anterior haya sido cerrada por stop o por objetivo.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `FastPeriod` | 50 | Longitud de la media móvil suavizada rápida. Debe ser menor que `SlowPeriod`. |
| `SlowPeriod` | 130 | Longitud de la media móvil suavizada lenta. |
| `MostTopLevel` | 500 | Offset superior (en pips) para la confirmación alcista/bajista más amplia. Debe ser mayor que `TopLevel`. |
| `TopLevel` | 250 | Offset superior (en pips) para la confirmación alcista/bajista secundaria. |
| `LowerLevel` | 250 | Offset inferior (en pips) para la confirmación bajista/alcista secundaria. Debe ser menor que `LowermostLevel`. |
| `LowermostLevel` | 500 | Offset inferior (en pips) para la confirmación bajista/alcista más amplia. |
| `TakeProfitPips` | 55 | Distancia desde la entrada al take-profit, expresada en pips. |
| `StopLossPips` | 260 | Distancia desde la entrada al stop-loss, expresada en pips. |
| `CandleType` | Marco temporal de 15 minutos | Serie de velas usada para los cálculos de SMMA y el procesamiento de señales. |

## Detalles de implementación

- El precio mediano (`(High + Low) / 2`) alimenta ambas SMMA, coincidiendo con la configuración de MT5 que usa `PRICE_MEDIAN`.
- La prueba de cruce compara la última vela completada con la anterior, eliminando cualquier dependencia de barras parcialmente formadas.
- `StartProtection` conecta el stop-loss y take-profit una sola vez al inicio, por lo que cada orden hereda automáticamente los límites de riesgo configurados.
- La estrategia se detiene durante `OnStarted` si se proporcionan combinaciones de parámetros no válidas (p. ej., `FastPeriod >= SlowPeriod`).

## Notas de uso

1. Adjunta la estrategia a un instrumento con un `PriceStep` definido; de lo contrario, la conversión de pips toma un valor de `1` por defecto.
2. Adecuada para cuentas de cobertura en MT5; en StockSharp se comporta igual al garantizar solo una posición abierta a la vez.
3. Los hooks de optimización (`SetCanOptimize`) están habilitados para ambos períodos de MA, lo que permite ejecutar barridos de parámetros directamente desde el optimizador de StockSharp.
4. Dado que la estrategia depende exclusivamente de salidas por stop-loss y take-profit, asegúrate de que las distancias configuradas estén alineadas con la volatilidad del instrumento para evitar exposición prolongada.

## Archivos

- `CS/TwoMaFourLevelStrategy.cs` – Implementación en C# de la lógica de trading.
- `README_ru.md` – Documentación en ruso.
- `README_zh.md` – Documentación en chino.
