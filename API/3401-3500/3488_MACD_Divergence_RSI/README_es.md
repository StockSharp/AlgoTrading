# MACD Divergencia RSI Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Puerto del MetaTrader asesor experto **"Macd diver rsi mt4"** al API de alto nivel de StockSharp.
- Intercambia un solo símbolo usando filtros RSI combinados con MACD reconocimiento de divergencia para invertir el tiempo.
- Sólo se puede abrir una posición de mercado a la vez; la estrategia espera el estado plano antes de emitir una nueva señal.

## Lógica de señal
1. Cada vela terminada del período de tiempo seleccionado alimenta cuatro indicadores vinculados a la estrategia:
   - Dos instancias `RelativeStrengthIndex` independientes (para filtros de sobreventa y sobrecompra) muestrearon una barra hacia atrás.
   - Dos indicadores `MovingAverageConvergenceDivergence` con EMA rápido/lento configurable y longitudes de señal.
2. **Configuración alcista**
   - La barra anterior RSI debe estar por debajo del umbral de sobreventa configurable.
   - Los valores MACD más recientes deben formar una caída local por debajo de un umbral dinámico (equivalente a 3 pips en el instrumento actual).
   - Los datos históricos se escanean para localizar una caída anterior MACD y el mínimo asociado del precio. La divergencia se confirma cuando
el mínimo MACD sube mientras que el precio alcanza un mínimo más bajo (divergencia regular) o el mínimo MACD cae mientras el precio alcanza un mínimo más alto
bajo (divergencia oculta), que coincide con la lógica MQL original.
   - Cuando se confirma y la estrategia no tiene ninguna posición abierta, se envía una compra de mercado con configuraciones de riesgo y volumen específicas de la dirección.
3. **La configuración bajista** refleja las reglas alcistas con el filtro de sobrecompra RSI y los picos MACD. La divergencia se valida mediante
comparando los máximos anteriores con el actual.
4. Inmediatamente después de una entrada, la estrategia convierte las distancias de stop-loss y take-profit configuradas de pips a unidades de precio.
(respetando las reglas originales de formato de puntos) y las aplica a través de `SetStopLoss` / `SetTakeProfit`.

## Parámetros
- `LowerRsiPeriod`, `LowerRsiThreshold`: asignar a `inp1_Lo_RSIperiod`/`inp1_Ro_Value`.
- `BullishFastEma`, `BullishSlowEma`, `BullishSignalSma` – asignar a `inp2_fastEMA` / `inp2_slowEMA` / `inp2_signalSMA`.
- `BullishVolume`, `BullishStopLossPips`, `BullishTakeProfitPips` – asignar a `inp3_VolumeSize`, `inp3_StopLossPips`, `inp3_TakeProfitPips`.
- `UpperRsiPeriod`, `UpperRsiThreshold`: asignar a `inp4_Lo_RSIperiod`/`inp4_Ro_Value`.
- `BearishFastEma`, `BearishSlowEma`, `BearishSignalSma` – asignar a `inp5_fastEMA` / `inp5_slowEMA` / `inp5_signalSMA`.
- `BearishVolume`, `BearishStopLossPips`, `BearishTakeProfitPips` – asignar a `inp6_VolumeSize`, `inp6_StopLossPips`, `inp6_TakeProfitPips`.
- `CandleType`: fuente del período de tiempo para todos los cálculos.

## Notas de implementación
- El umbral de divergencia MACD se deriva del tamaño de punto actual del instrumento y equivale a 3 pips, lo que coincide con el valor predeterminado de 0,0003.
utilizado por la versión MQL.
- La vela, MACD y el historial de precios se almacenan en listas limitadas (600 elementos) para reproducir las ventanas de escaneo de divergencia sin
Asignación de grandes matrices.
- La estrategia utiliza `SubscribeCandles(...).Bind(...)` para actualizar todos los indicadores en una sola pasada y los procesos solo finalizan
velas, al igual que la ejecución original del bloque de una vez por barra.
- Las distancias de pips se convierten en compensaciones de precio absoluto antes de llamar a `SetStopLoss` y `SetTakeProfit`, reproduciendo el
reglas de formato de puntos declaradas en la parte superior de la fuente MQL.
