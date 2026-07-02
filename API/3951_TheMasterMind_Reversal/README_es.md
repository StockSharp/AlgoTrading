# La estrategia de reversión de MasterMind (puerto StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Puerto del asesor experto MetaTrader 4 "TheMasterMind" que combina un oscilador Stochastic con Williams %R para capturar inversiones extremas.
- Implementado con API de alto nivel de StockSharp usando suscripciones de velas y enlaces de indicadores.
- Negocia un único valor y reacciona solo ante velas terminadas, reflejando el estilo de ejecución original de "negociación al cierre".

## Lógica de trading
1. **Preparación de indicadores**
   - `StochasticOscillator` ofrece la línea de señal %D con suavizado %K/%D configurable y longitud total retrospectiva.
   - `WilliamsR` mide la ubicación relativa del cierre dentro del rango máximo/mínimo reciente.
2. **Reglas de entrada**
   - **Compre** cuando `%D <= 3` _y_ `Williams %R <= -99.5`, lo que indica una sobreventa estocástica extrema junto con una profunda penetración del WPR por debajo del límite inferior.
   - **Vender** cuando `%D >= 97` _y_ `Williams %R >= -0.5`, lo que indica una sobrecompra extrema confirmada por el Williams %R que se mantiene cerca de 0.
   - Si existe una posición opuesta, primero se aplana y luego se envía una nueva orden de mercado con el volumen base configurado.
3. **Reglas de salida**
   - Las señales inversas cierran la posición actual y cambian la dirección (una posición a la vez, coincidiendo con el modo de cobertura deshabilitada utilizado en el script MQL).
   - Los servicios opcionales `StartProtection` stop-loss, take-profit y trailing stop manejan salidas protectoras exactamente una vez por inicio de estrategia.

## Gestión del riesgo
- Los parámetros `StopLoss`, `TakeProfit`, `UseTrailingStop`, `TrailingStop` y `TrailingStep` se asignan a los controles de administración de dinero del EA original.
- Todas las distancias se expresan en unidades de precio absoluto para mantener la independencia del corredor. Déjelos en `0` para desactivar la función de protección respectiva.
- `StartProtection` se activa automáticamente cuando al menos una de las distancias de protección es distinta de cero.

## Parámetros de estrategia
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `TradeVolume` | Tamaño de lote base para cada entrada nueva. | `1` |
| `StochasticPeriod` | Lookback total para el oscilador estocástico. | `100` |
| `KPeriod` | %K longitud de suavizado. | `3` |
| `DPeriod` | %D longitud de la señal. | `3` |
| `WilliamsPeriod` | Longitud retrospectiva para Williams %R. | `100` |
| `StochasticBuyThreshold` | Límite superior por debajo del cual %D debe permanecer para permitir posiciones largas. | `3` |
| `StochasticSellThreshold` | Límite inferior por encima del cual %D debe permanecer para permitir ventas en corto. | `97` |
| `WilliamsBuyLevel` | Nivel de sobreventa de Williams %R. | `-99.5` |
| `WilliamsSellLevel` | Nivel de sobrecompra de Williams %R. | `-0.5` |
| `StopLoss` | Distancia absoluta de stop-loss. | `0` |
| `TakeProfit` | Distancia absoluta de obtención de beneficios. | `0` |
| `UseTrailingStop` | Habilita la protección de seguimiento cuando `true`. | `false` |
| `TrailingStop` | Distancia absoluta de trailing stop. | `0` |
| `TrailingStep` | Paso aplicado mientras se arrastra. | `0` |
| `CandleType` | Plazo para la suscripción de la vela principal (predeterminado 15 minutos). | `15m time frame` |

## Notas de implementación
- La estrategia se suscribe a una única serie de velas a través de `SubscribeCandles(CandleType)` y vincula los indicadores estocástico y Williams %R usando `BindEx`.
- Las decisiones comerciales se toman solo cuando se cumplen `candle.State == CandleStates.Finished` y `IsFormedAndOnlineAndAllowTrading()`.
- Los ayudantes de gráficos (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) se invocan cuando hay un área de gráfico disponible para visualizar los indicadores y las operaciones.
- Las declaraciones de registro (`LogInfo`) reflejan las cadenas de alerta originales, lo que ayuda a rastrear el proceso de decisión durante las operaciones en vivo o las pruebas retrospectivas.
