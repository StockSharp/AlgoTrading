# Estrategia Gordago EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un puerto del histórico asesor experto "Gordago EA" de MetaTrader 5. La estrategia opera en el marco temporal base (predeterminado M3) mientras lee señales MACD de un gráfico intradía superior y un filtro estocástico de un gráfico horario. Preserva los parámetros originales de stop/take y la lógica de trailing, pero utiliza la API de alto nivel de StockSharp para las suscripciones de datos y la gestión de órdenes.

## Lógica de la estrategia

- **Datos de mercado**
  - Velas de ejecución principal: configurables, predeterminado velas de tres minutos.
  - Velas MACD: configurables, predeterminado velas de doce minutos.
  - Velas estocásticas: configurables, predeterminado velas de una hora.
- **Indicadores**
  - MACD (rápido 12, lento 26, señal 9) calculado en el marco temporal MACD.
  - Oscilador estocástico (longitud 5, suavizado %K 3, %D 3) calculado en el marco temporal estocástico.
- **Condiciones de entrada**
  - **Compra**: valor MACD actual superior al anterior, MACD anterior por debajo de cero, %K estocástico por debajo del umbral de compra (predeterminado 37) y en alza respecto al valor anterior.
  - **Venta**: valor MACD actual inferior al anterior, MACD anterior por encima de cero, %K estocástico por encima del umbral de venta (predeterminado 96) y en caída respecto al valor anterior.
- **Colocación de órdenes**
  - El volumen de la orden es fijo; cambiar de dirección compensa automáticamente cualquier posición opuesta antes de abrir una nueva.
  - Existen distancias separadas de stop-loss/take-profit para operaciones largas y cortas (predeterminados: 40/70 pips para largo, 10/40 pips para corto).
- **Salidas**
  - Los niveles protectores de stop-loss y take-profit se comprueban en cada vela base finalizada.
  - Un trailing stop se activa cuando el precio avanza más allá de la distancia de trailing configurada más el paso de trailing; una vez activado sigue avanzando hacia el mercado por la distancia de trailing.
  - El trailing puede introducir un stop de protección incluso cuando el stop original estaba desactivado, reflejando el EA fuente.

## Parámetros

- `OrderVolume` – volumen de operación en lotes.
- `StopLossBuyPips` / `TakeProfitBuyPips` – distancias de stop-loss y take-profit para el lado largo (en pips).
- `StopLossSellPips` / `TakeProfitSellPips` – distancias de stop-loss y take-profit para el lado corto (en pips).
- `TrailingStopPips` – distancia de trailing en pips; establecer en cero para deshabilitar el trailing.
- `TrailingStepPips` – beneficio adicional mínimo (en pips) antes de que el trailing stop pueda avanzar.
- `StochasticBuyLevel` / `StochasticSellLevel` – umbrales del oscilador para entradas largas y cortas.
- `CandleType` – marco temporal de trabajo para la lógica de ejecución.
- `MacdCandleType` – marco temporal utilizado para alimentar el indicador MACD.
- `StochasticCandleType` – marco temporal utilizado para alimentar el oscilador estocástico.
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – períodos MACD.
- `StochasticLength`, `StochasticSignalPeriod`, `StochasticSmoothing` – períodos del oscilador estocástico.

## Notas de implementación

- Las distancias en pips se convierten a precios usando el `PriceStep` del instrumento. Si el paso tiene tres o cinco dígitos fraccionarios, la estrategia lo multiplica por diez, reproduciendo el ajuste de pip usado en la implementación MQL original para cotizaciones forex de 3/5 dígitos.
- El trailing stop se ignora cuando `TrailingStopPips` es positivo pero `TrailingStepPips` no lo es; en ese caso se registra una advertencia.
- Debido a que la versión de StockSharp trabaja con eventos de cierre de velas, la lógica de protección se ejecuta una vez por vela finalizada en lugar de en cada tick como en la versión MT5. El comportamiento de gestión de operaciones sigue las reglas originales.
- Solo se proporciona la implementación en C#; no se incluye traducción ni carpeta de Python por petición.
