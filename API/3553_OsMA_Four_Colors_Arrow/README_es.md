# Estrategia de flecha de cuatro colores de OsMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia recrea el comportamiento del asesor experto MetaTrader "OsMA Four Colors Arrow" dentro del marco StockSharp. El EA original reacciona a las flechas de colores producidas por el indicador que lo acompaña cada vez que el histograma OsMA (MACD) cambia de fase. En la versión StockSharp, el mismo comportamiento se modela monitoreando los cruces por cero del histograma MACD: un cruce alcista (el histograma se mueve de negativo a positivo) desencadena entradas largas, mientras que un cruce bajista desencadena entradas cortas. El modo inverso opcional invierte la lógica para pruebas de cobertura o reversión a la media.

La plantilla funciona solo con velas terminadas y puede imponer una sesión de negociación diaria similar al filtro de tiempo que ofrece la versión MQL. La gestión de dinero integrada incluye un volumen de operaciones configurable, un límite en el número de posiciones agregadas y una protección automática de stop-loss/take-profit/trailing expresada en pips.

## Lógica de trading

1. Suscríbase al período de tiempo seleccionado y calcule un histograma MACD (OsMA) utilizando longitudes configurables rápidas, lentas y de señal EMA.
2. Cuando se cierra una vela, comprueba el signo del histograma:
   - Histograma cruzando por encima de cero → flecha alcista → señal de compra.
   - Histograma cruzando por debajo de cero → flecha bajista → señal de venta.
3. Aplicar funciones opcionales antes de enviar un pedido:
   - Filtro de dirección (solo largo, solo corto o ambos).
   - Modo inverso para invertir señales.
   - Cierre la exposición opuesta existente antes de abrir la nueva operación.
   - Limite a una posición activa o acumule hasta la exposición máxima configurada.
4. Las órdenes de mercado se envían con el tamaño de lote configurado. `StartProtection` convierte las entradas de pips en compensaciones de precios absolutos para ejecutar automáticamente la gestión de stop-loss, take-profit y trailing.
5. Las operaciones se ignoran fuera de la sesión intradiaria permitida cuando el filtro de tiempo está habilitado.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `CandleType` | Plazo utilizado para los cálculos y la generación de señales. |
| `FastPeriod` / `SlowPeriod` / `SignalPeriod` | EMA longitudes para el MACD histograma (OsMA). |
| `StopLossPips` / `TakeProfitPips` | Objetivos de riesgo en pips. Establezca en cero para desactivar. |
| `TrailingActivatePips` | Beneficio (en pips) requerido antes de que el trailing stop pueda moverse. |
| `TrailingStopPips` | Distancia de seguimiento en pips. Zero desactiva el módulo de seguimiento. |
| `TrailingStepPips` | Pips adicionales que se deben ganar antes de apretar nuevamente el trailing stop. |
| `MaxPositions` | Unidades de posición agregadas máximas (`TradeVolume` múltiplos). Cero significa ilimitado. |
| `ReverseSignals` | Invertir la dirección de entrada (compra ↔ venta). |
| `DirectionMode` | Restrinja las señales a solo largas, solo cortas o ambas. |
| `CloseOppositePositions` | Cierre cualquier exposición opuesta antes de actuar sobre la nueva señal. |
| `OnlyOnePosition` | Si `true`, evita agregar a una posición ya abierta en la misma dirección. |
| `UseTimeControl` | Habilite el filtro de sesión de negociación intradiaria. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Límites de la sesión (el final puede ser antes del inicio para cubrir sesiones nocturnas). |
| `TradeVolume` | Volumen de pedidos en lotes. |

## Notas

- Las entradas de trailing-stop imitan el EA: el trailing está disponible solo después de `TrailingActivatePips` y se mueve en pasos definidos por `TrailingStepPips`.
- La estrategia requiere que el valor tenga un `PriceStep` y un `Decimals` válidos para convertir pips en compensaciones de precios. Los incumplimientos se reducen a una unidad de precio absoluta si el instrumento no los proporciona.
- Si `MaxPositions` es mayor que uno, la estrategia puede ampliarse gradualmente agregando `TradeVolume` repetidamente respetando el límite máximo de exposición.
- Cuando `UseTimeControl` está habilitado y las horas de inicio y finalización coinciden, el comercio se deshabilita para evitar sesiones ambiguas.
- La lógica actúa sólo sobre velas cerradas; no hay ningún envío de orden dentro de la barra, que coincida con el comportamiento de la plantilla MQL.
