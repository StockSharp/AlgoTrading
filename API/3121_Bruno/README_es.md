# Estrategia Bruno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El asesor experto Bruno es un sistema de seguimiento de tendencia escrito originalmente para MetaTrader 5. El puerto mantiene la misma cadena de confirmación: Average Directional Index (ADX) con movimiento direccional, un par de medias móviles exponenciales (EMA 8/21), MACD (13, 34, 8), un Oscilador Estocástico (21, 3, 3) y la pendiente de un Parabolic SAR (0.055, 0.21). Cada filtro que coincide con la dirección multiplica el tamaño de la orden por un factor configurable. Si tanto las señales largas como las cortas se amplían en la misma vela, el trading se omite para evitar órdenes en conflicto.

### Lógica de trading

- **Sesgo direccional**
  - La presión larga se fortalece cuando `+DI > -DI` y `+DI > 20`.
  - La presión corta se fortalece cuando `+DI < -DI` y `+DI < 40`.
- **Alineación de momentum**
  - La preferencia larga requiere EMA(8) por encima de EMA(21), Estocástico %K por encima de %D y %K por debajo del umbral de sobrecompra (predeterminado 80).
  - La preferencia corta requiere EMA(8) por debajo de EMA(21), Estocástico %K por debajo de %D y %K por encima del umbral de sobreventa (predeterminado 20).
- **Filtro MACD**
  - Sesgo largo: histograma MACD por encima de cero y línea principal MACD por encima de la línea de señal.
  - Sesgo corto: histograma MACD por debajo de cero y línea principal MACD por debajo de la línea de señal.
- **Pendiente del Parabolic SAR**
  - El sesgo largo se refuerza cuando los valores anteriores del SAR están subiendo mientras EMA(8) > EMA(21).
  - El sesgo corto se refuerza cuando los valores anteriores del SAR están bajando mientras EMA(8) < EMA(21).

Cada condición satisfecha multiplica el tamaño del lote base por `SignalMultiplier` (predeterminado 1.6). Solo un lado puede estar activo a la vez. Cuando se genera una señal final, la estrategia cierra cualquier posición opuesta, envía la orden de mercado con el volumen multiplicado y almacena el cierre actual como precio de entrada.

### Gestión de posiciones

- **Stop-loss / take-profit** – distancias fijas expresadas en pips ajustados, coincidiendo con la versión de MetaTrader. Si cualquiera de los niveles es alcanzado intrabarra, la posición se cierra inmediatamente.
- **Trailing stop** – se activa una vez que el beneficio flotante supera `TrailingStop + TrailingStep` pips. Entonces el stop se coloca detrás del precio por `TrailingStop` pips y solo avanza cuando la ganancia aumenta al menos `TrailingStep` pips más.
- **Gestión de conflictos** – si tanto los filtros largos como los cortos se activan en la misma vela, no se toma ninguna nueva operación.

### Parámetros

| Grupo | Nombre | Descripción |
| --- | --- | --- |
| Trading | `BaseVolume` | Tamaño inicial del lote antes de los multiplicadores. |
| Trading | `SignalMultiplier` | Multiplicador de volumen aplicado por cada filtro coincidente. |
| Gestión del riesgo | `StopLossPips` / `TakeProfitPips` | Distancias de protección en pips ajustados. Establezca en cero para deshabilitar. |
| Gestión del riesgo | `TrailingStopPips` / `TrailingStepPips` | Distancia de trailing y paso mínimo en pips ajustados. |
| Indicadores | `AdxPeriod`, `AdxPositiveThreshold`, `AdxNegativeThreshold` | Longitud del ADX y umbrales del DI. |
| Indicadores | `FastEmaPeriod`, `SlowEmaPeriod` | Longitudes de EMA usadas en la confirmación de tendencia. |
| Indicadores | `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuración del MACD. |
| Indicadores | `StochasticPeriod`, `StochasticKsmoothing`, `StochasticDsmoothing`, `StochasticOverbought`, `StochasticOversold` | Configuración del oscilador estocástico. |
| General | `CandleType` | Marco temporal usado para toda la cadena de señales (predeterminado 1 hora). |

### Notas

- El tamaño de pip ajustado sigue la convención de MetaTrader: los instrumentos con 3 o 5 dígitos decimales se multiplican por 10.
- El Parabolic SAR opera con paso de aceleración `0.055` y máximo `0.21`, reflejando los valores predeterminados del asesor experto.
- El puerto mantiene el estilo de gestión del dinero original (apilamiento de volumen) pero agrega la exposición en una única posición de StockSharp.
