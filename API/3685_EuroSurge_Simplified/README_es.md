# Estrategia simplificada de EuroSurge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convierte el asesor experto MetaTrader 4 **"EuroSurge Simplified"** en la API de alto nivel de StockSharp.
- Opera con velas terminadas y evalúa una colección de indicadores clásicos (MA, RSI, MACD, Bollinger Bandas, Stochastic) para encontrar entradas.
- Aplica un período de recuperación configurable entre operaciones y adjunta niveles de obtención de ganancias/detención de pérdidas expresados en incrementos de precios.
- Admite múltiples modos de tamaño de posición: volumen fijo, porcentaje de saldo y porcentaje de capital.

## Señales
1. **Tendencia de media móvil** (opcional): un SMA rápido de 20 períodos debe estar por encima (largo) o por debajo (corto) de un SMA configurable más lento.
2. Filtro **RSI** (opcional): RSI debe permanecer por debajo del umbral largo para permitir compras y por encima del umbral corto para permitir ventas.
3. **MACD confirmación** (opcional): la línea MACD debe ser mayor que (larga) o menor que (corta) la línea de señal.
4. **Bollinger Filtro de bandas** (opcional): el precio debe superar la banda inferior para largos o la banda superior para cortos.
5. Filtro **Stochastic** (opcional): %K y %D deben permanecer por debajo de 50 para posiciones largas o por encima de 50 para posiciones cortas.

Todos los filtros habilitados deben coincidir antes de que la estrategia envíe una orden de mercado. La exposición opuesta se aplana antes de abrir una nueva posición, reflejando la lógica MetaTrader de reemplazar las operaciones abiertas.

## Gestión del riesgo
- Las distancias de stop-loss y take-profit se definen en incrementos de precio (MetaTrader “puntos”).
- La estrategia registra automáticamente órdenes de protección con `SetStopLoss` y `SetTakeProfit` inmediatamente después de abrir una posición.
- Las operaciones se bloquean hasta que haya transcurrido el intervalo configurado en minutos desde la última orden ejecutada.

## Dimensionamiento de posiciones
- **FixedSize**: opera con el `FixedVolume` configurado.
- **BalancePercent**: asigna una fracción del saldo inicial de la cartera y aproxima el volumen dividiendo por el último precio de cierre.
- **EquityPercent**: se comporta igual pero se basa en el capital de la cartera actual.
- Los volúmenes se ajustan al paso de volumen de seguridad y se sujetan entre los límites mínimo/máximo del intercambio.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `TradeSizeType` | Modo de dimensionamiento de la posición (fijo, saldo %, equidad %).
| `FixedVolume` | Volumen utilizado cuando `TradeSizeType = FixedSize`.
| `TradeSizePercent` | Porcentaje aplicado en tamaño basado en porcentaje.
| `TakeProfitPoints` / `StopLossPoints` | Distancias de protección en los escalones de precios.
| `MinTradeIntervalMinutes` | Enfriamiento entre operaciones.
| `MaPeriod` | Longitud del SMA lento (el SMA rápido se fija en 20 en línea con el EA).
| `RsiPeriod`, `RsiBuyLevel`, `RsiSellLevel` | RSI configuración y umbrales.
| `MacdFast`, `MacdSlow`, `MacdSignal` | MACD parámetros.
| `BollingerLength`, `BollingerWidth` | Bollinger Configuración de bandas.
| `StochasticLength`, `StochasticK`, `StochasticD` | Stochastic parámetros del oscilador.
| `UseMa`, `UseRsi`, `UseMacd`, `UseBollinger`, `UseStochastic` | Alternar filtros individuales.
| `CandleType` | Plazo utilizado para la evaluación de la señal.

## MetaTrader Diferencias
- El EA original valida el volumen frente a las restricciones específicas del corredor. El puerto refleja esto al ajustarse a StockSharp pasos de volumen y respetar el volumen mínimo/máximo cuando esté disponible.
- Los niveles de protección se convierten en incrementos de precios mediante StockSharp ayudantes en lugar de la aritmética de precios manual.
- Todos los valores del indicador se consumen a través del enlace de alto nivel API sin llamadas directas a `GetValue`.

## Consejos de uso
1. Adjunte la estrategia a una cartera y un valor, luego configure el período de tiempo a través de `CandleType`.
2. Ajuste los interruptores del indicador para reproducir o simplificar el comportamiento original de EA.
3. Aumente `MinTradeIntervalMinutes` si necesita menos operaciones; disminúyalo para entradas más frecuentes.
4. Verifique que `TakeProfitPoints` y `StopLossPoints` coincidan con el tamaño de marca del símbolo.
