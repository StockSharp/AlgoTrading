# Macd Pattern Trader Todo v0.01
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el asesor experto "MacdPatternTraderAll v0.01" MetaTrader. Ejecuta seis patrones de entrada independientes basados ​​en MACD en el mismo flujo de velas, gestiona el riesgo con niveles adaptables de stop-loss y take-profit, realiza una toma de ganancias por etapas y, opcionalmente, aplica una regla de tamaño de martingala lenta después de ciclos perdedores.

## Características principales

- **Seis configuraciones MACD**: cada patrón utiliza sus propios períodos rápidos/lentos EMA y niveles de umbral (`Pattern1`… `Pattern6`). Los patrones se pueden activar o desactivar de forma independiente.
- **Niveles de riesgo dinámicos**: los niveles de stop-loss se derivan de máximos y mínimos recientes con compensaciones configurables, mientras que los niveles de toma de ganancias se repiten sobre bloques de barras sucesivos para reflejar la implementación original MQL.
- **Filtro de sesión**: la estrategia opera solo dentro de la ventana configurable `StartTime` / `StopTime` cuando `UseTimeFilter` está habilitado.
- **Salidas parciales**: las posiciones rentables se escalan en dos pasos una vez que los filtros EMA/SMA confirman el impulso, siguiendo la lógica `ActivePosManager` original.
- **Martingala lenta**: cuando `UseMartingale` es verdadero, el tamaño de la siguiente operación se duplica después de un ciclo perdedor y se reinicia después de cualquier ciclo rentable.

## Lógica de entrada por patrón

1. **Patrón 1 (etiqueta `Pattern1`)**
   - Se arma poco después de que la línea principal MACD empuja por encima de `Pattern1MaxThreshold` y luego gira con una secuencia baja alta.
   - Se arma mucho después de estirarse por debajo de `Pattern1MinThreshold` y producir una secuencia baja más alta.
2. **Patrón 2 (etiqueta `Pattern2`)**
   - Cuenta las oscilaciones alrededor de la línea cero. Los cortos se activan cuando falla un swing positivo cerca de `Pattern2MinThreshold`. Los largos aparecen cuando una oscilación negativa se desvanece cerca de `Pattern2MaxThreshold`. El algoritmo reproduce las comprobaciones de distancia originales comparando valores absolutos MACD (`valueMin2` / `valueCurr2`).
3. **Patrón 3 (etiqueta `Pattern3`)**
   - Rastrea hasta tres picos MACD descendentes (o ascendentes) para detectar un "triple gancho". Sólo cuando todos los umbrales intermedios (`Pattern3MaxThreshold`, `Pattern3MaxLowThreshold`, `Pattern3MinThreshold`, `Pattern3MinHighThreshold`) coinciden se permiten nuevas posiciones.
4. **Patrón 4 (etiqueta `Pattern4`)**
   - Observa los picos de MACD fuera de `Pattern4MaxThreshold`/`Pattern4MinThreshold` seguidos de intentos fallidos de llegar a nuevos extremos. Se conserva un contador adicional (`Pattern4AdditionalBars`) por motivos de compatibilidad.
5. **Patrón 5 (etiqueta `Pattern5`)**
   - Implementa la ruptura de la zona neutral utilizada en el asesor experto. Los pantalones cortos requieren un rebote desde debajo de `Pattern5MinThreshold` de vuelta dentro de la zona neutral y otro fallo. Los largos siguen la secuencia reflejada alrededor de `Pattern5MaxThreshold`.
6. **Patrón 6 (etiqueta `Pattern6`)**
   - Cuenta el número de barras consecutivas por encima o por debajo de los niveles de umbral. Después de gastar más de `Pattern6TriggerBars` dentro del área de sobrecompra/sobreventa y regresar por debajo/por encima del umbral, la estrategia abre una operación a menos que `Pattern6MaxBars` bloquee la señal.

Cada patrón utiliza los métodos auxiliares `TryOpenLong` / `TryOpenShort`, lo que garantiza que las paradas y los objetivos se calculen antes de emitir cualquier orden.

## Gestión de riesgos y comercio

- **Stop-loss**: `CalculateStopPrice` escanea las velas terminadas `stopBars` más recientes (excluyendo la activa) y aplica el punto configurado `offset`. Los precios se ajustan para instrumentos de 3/5 decimales al igual que en la versión MQL.
- **Take-profit**: `CalculateTakeProfit` recorre bloques consecutivos de `takeBars` velas hasta que no se encuentra ningún extremo nuevo, imitando el bucle anidado `iLowest`/`iHighest` del código original.
- **Salidas parciales**: `ManageActivePositions` cierra un tercio de la posición con `ProfitThreshold` ganancia cuando el precio se confirma con `ema2`. Se activa una segunda salida de tamaño medio cuando el precio alcanza el filtro combinado `(sma3 + ema4) / 2`.
- **Salidas duras**: `CheckRiskManagement` emite salidas completas del mercado una vez que se tocan los niveles almacenados de stop-loss o take-profit.
- **Martingale control**: `OnOwnTradeReceived` acumula PnL realizado para el ciclo de plano a plano actual. Cuando la posición vuelve a estabilizarse, `AdjustVolumeOnFlat` restablece el volumen a `InitialVolume` después de las ganancias o lo duplica después de las pérdidas si `UseMartingale` está habilitado.

## Parámetros

Todas las perillas de configuración están expuestas a través de propiedades `StrategyParam<T>` para su optimización en StockSharp Designer.

- **General**: `CandleType`, `InitialVolume`, `UseTimeFilter`, `StartTime`, `StopTime`, `UseMartingale`.
- **Patrones 1 a 6**: recuentos de barras de stop-loss/take-profit, compensaciones, MACD períodos rápidos/lentos y niveles de umbral que coinciden con las entradas externas del script MQL.
- **Administrador de posición**: EMA/SMA longitudes (`EmaPeriod1`, `EmaPeriod2`, `SmaPeriod3`, `EmaPeriod4`) utilizadas en el filtro de salida parcial.

Todos los valores predeterminados reflejan las variables `extern` de `MacdPatternTraderAll v0.01`.

## Notas de uso

- La estrategia espera que un símbolo con `PriceStep` y `Decimals` válidos calcule las compensaciones correctamente.
- Proporcione una serie de velas a través de `CandleType` (por ejemplo, `TimeSpan.FromMinutes(5).TimeFrame()`).
- Cuando se activan varios patrones simultáneamente, la estrategia abrirá solo una posición porque cada llamada de entrada recalcula el volumen deseado combinado y elimina las paradas opuestas.
- La lógica de salida por etapas funciona con posiciones agregadas, por lo que se producen cierres parciales incluso si varios patrones comparten la misma dirección comercial.
