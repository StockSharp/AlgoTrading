# Divergencia + EMA + RSI Cerrar Solo comprar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia transfiere el asesor experto MetaTrader "Divergencia + ema + rsi solo compra cercana" al asesor experto de alto nivel de StockSharp API. Actúa sobre **velas de 5 minutos** mientras consulta datos **horarios** y **diarios** para confirmar la alineación de la tendencia y las condiciones de sobreventa. Los pedidos son sólo largos. Las entradas requieren una divergencia alcista del histograma MACD que se confirma mediante un cruce estocástico horario dentro de una estrecha banda de sobreventa y por una estructura diaria ascendente EMA. Las salidas se basan en un exceso fijo de RSI combinado con una protección opcional de stop-loss y take-profit administrada por el marco.

## Lógica de trading

1. **Filtro de tendencias de marco temporal más alto**
   - El EMA(9) diario debe estar por encima de EMA(20) para garantizar una tendencia alcista predominante.
   - El último cierre de 5 minutos debe permanecer por debajo del EMA(9) diario para que se intenten entradas largas desde retrocesos.

2. **Confirmación estocástica horaria**
   - El valor %K estocástico horario completado más reciente debe estar entre `StochasticLowerBound` (predeterminado 0) y `StochasticUpperBound` (predeterminado 40).
   - %K debe haber cruzado por encima de %D en la última barra horaria (%K actual > %D mientras que el %K anterior ≤ %D anterior).

3. **MACD activador de divergencia (5 minutos)**
   - El histograma MACD (línea MACD menos línea de señal) debe mejorar al menos `MacdThreshold` mientras que el cierre de 5 minutos establece un mínimo más bajo en comparación con la vela anterior. Esto se aproxima a la divergencia alcista utilizada por el EA original.

4. **Ejecución de entrada**
   - Cuando todos los filtros se alinean y no hay ninguna posición larga abierta, la estrategia envía una compra de mercado. Si existe una posición corta inesperada, el volumen solicitado aumenta para neutralizarla antes de pasar a largo.

5. **Reglas de salida**
   - Una salida protectora RSI cierra la larga cuando el RSI de 5 minutos cruza por encima de `RsiExitLevel` (por defecto 77).
   - `StartProtection` activa los niveles de stop-loss y take-profit convertidos de pips en distancias de precios siempre que los parámetros correspondientes sean positivos.

6. **Gestión de pedidos**
   - Todas las órdenes activas se cancelan antes de enviar una nueva orden de compra de mercado para evitar ejecuciones duplicadas.
   - El volumen predeterminado es el parámetro `TradeVolume` y se puede ajustar para optimización.

## Parámetros

| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `CandleType` | Plazo principal para MACD, RSI y ejecución. | velas de 5 minutos |
| `HourTimeFrame` | Marco de tiempo horario utilizado por el filtro estocástico. | 1 hora |
| `DayTimeFrame` | Plazo diario para la confirmación de tendencia de EMA. | 1 dia |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Estructura MACD en el período de tiempo principal. | 6 / 13 / 5 |
| `MacdThreshold` | Aumento mínimo del histograma de MACD para aceptar una divergencia. | 0.0003 |
| `DailyFastPeriod` / `DailySlowPeriod` | EMA períodos diarios. | 9 / 20 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Configuración estocástica horaria. | 30 / 5 / 9 |
| `StochasticUpperBound` / `StochasticLowerBound` | Rango %K horario aceptado. | 40/0 |
| `RsiPeriod` | RSI duración en el período de tiempo principal. | 7 |
| `RsiExitLevel` | RSI valor que fuerza salidas largas. | 77 |
| `TradeVolume` | Tamaño de pedido base para compras. | 0,01 |
| `StopLossPips` | Distancia de stop-loss en pips (0 desactivaciones). | 100 |
| `TakeProfitPips` | Distancia de toma de ganancias en pips (0 inhabilitaciones). | 200 |

## Notas

- La estrategia se suscribe a tres flujos de datos: el período de tiempo principal configurado, una serie horaria y una serie diaria. Cada flujo impulsa su propio conjunto de indicadores a través de `Bind`/`BindEx` para mantener la implementación concisa y basada en eventos.
- Los valores del indicador solo se procesan en velas terminadas para reflejar los parámetros de cambio del EA original.
- La detección de divergencia MACD utiliza el cierre de la barra anterior y el valor del histograma como una aproximación simple pero sólida de la lógica generada por el constructor a partir del archivo fuente MQL.
- `StartProtection` maneja el stop-loss y la toma de ganancias para permanecer sincronizados con los llenados del corredor y admitir pruebas retrospectivas o operaciones en vivo sin replicación manual de órdenes.
