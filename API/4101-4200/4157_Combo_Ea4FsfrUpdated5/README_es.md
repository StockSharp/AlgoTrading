# Combo EA4 FSF R Actualizado 5 Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una StockSharp conversión del MetaTrader asesor experto "Combo_EA4FSFrUpdated5". Combina cinco módulos técnicos diferentes (medias móviles, RSI, oscilador estocástico, parabólico SAR y retraso cero MACD) para validar cada decisión comercial. Una posición se abre solo cuando **todos** los módulos habilitados apuntan a la misma dirección, recreando la estricta lógica de consenso del EA original. También se conservan la gestión de seguimiento opcional, las salidas automáticas basadas en señales y la capacidad de girar en la dirección opuesta después del cierre.

## Pila de indicadores
- **Promedios móviles**: tres promedios configurables (MA1, MA2, MA3) con búferes basados en ATR que reducen las señales cruzadas falsas. Cinco modos de agregación diferentes replican las opciones "MA_MODE" de EA.
- **Índice de fuerza relativa (RSI)**: múltiples modos de confirmación que incluyen sobrecompra/sobreventa clásica, detección de tendencias basada en pendientes, un modo combinado y validación basada en zonas.
- **Stochastic oscilador**: longitudes rápidas/lentas/de desaceleración con filtrado de banda alta/baja opcional.
- **Parabolic SAR**: proporciona una verificación de la polaridad de la tendencia con respecto al cierre de la vela anterior.
- **Retraso cero MACD**: utiliza promedios móviles exponenciales de retraso cero para igualar el indicador `ZeroLag_MACD.mq4` incluido. Admite tres modos de señal (estructura de tendencia, cruce de línea cero o combinado).
- **Rango verdadero promedio (ATR)**: impulsa las distancias de stop-loss/take-profit y los buffers de cruce de MA.

## Lógica comercial
### Condiciones de entrada
1. Los valores del indicador para todos los módulos habilitados deben estar disponibles (la estrategia espera automáticamente el calentamiento).
2. Para cada módulo habilitado se calcula una dirección alcista o bajista según su modo:
   - **Promedios móviles**: combinaciones MA1/MA2/MA3 con buffers ATR para confirmar cambios de dirección.
   - **RSI**: cuatro modos que cubren umbrales, impulso y lógica de zona.
   - **Stochastic** – Confirmación cruzada K/D con filtros alto/bajo opcionales.
   - **Parabolic SAR**: requiere que el precio esté por encima o por debajo del valor SAR de la vela anterior.
   - **Retraso cero MACD**: alineación de tendencia, confirmación cruzada de línea cero o ambas.
3. Si **cada** módulo habilitado devuelve `Buy`, la estrategia envía una orden de compra de mercado. Si cada módulo devuelve `Sell`, se emite una orden de venta de mercado. De lo contrario no se abre ninguna operación.

### Condiciones de salida
- **Salidas basadas en señales**: cuando `AutoClose` está habilitado, la misma lógica de consenso se evalúa utilizando los indicadores de salida dedicados (`UseMaClosing`, `UseMacdClosing`, etc.). Una posición larga se cierra cuando todos los módulos de salida habilitados coinciden en una señal bajista; una posición corta se cierra cuando coinciden en una señal alcista. Si `OpenOppositeAfterClose` es verdadero, la posición opuesta se pone en cola inmediatamente después del llenado de cierre.
- **Niveles de protección**: los niveles iniciales de stop-loss y take-profit se derivan del valor actual de ATR (`AtrPeriod`) multiplicado por `AtrMultiplier`. El búfer de pips del EA se emula con el tamaño de paso del instrumento. Las operaciones largas utilizan `ATR × multiplier − buffer` para paradas y `ATR × multiplier + buffer` para objetivos (reflejado para cortos).
- **Trailing stop**: cuando `UseTrailingStop` está habilitado, el precio de stop se ajusta en cada vela terminada utilizando la distancia del punto configurada (`TrailingStop`).
- **Salidas duras**: si el precio alcanza el límite de pérdidas o la intrabarra de toma de ganancias, la posición se cierra inmediatamente y no se activa ninguna entrada opuesta.

### Tamaño de posición
- **Modo estático**: cuando `UseStaticVolume` es verdadero, las operaciones se realizan con el parámetro fijo `StaticVolume`.
- **Modo dinámico**: de lo contrario, la estrategia deriva un tamaño aproximado del valor actual de la cartera y `RiskPercent`, volviendo a la base `Volume` si los datos de la cartera o los precios no están disponibles.

## Parámetros
| grupo | Parámetro | Descripción |
|-------|-----------|-------------|
| Entradas | `UseMa` | Habilite la confirmación de media móvil. |
| Entradas | `MaMode` | Selecciona la combinación MA (rápida/media, media/lenta, combinada, etc.). |
| Indicadores | `Ma1Period`, `Ma2Period`, `Ma3Period` | Períodos de las tres medias móviles. |
| Indicadores | `Ma1BufferPeriod`, `Ma2BufferPeriod` | periodo ATRs utilizados como buffer para verificaciones cruzadas de MA. |
| Indicadores | `Ma1Method`, `Ma2Method`, `Ma3Method` | Tipos de cálculo de media móvil (SMA, EMA, SMMA, LWMA). |
| Indicadores | `Ma1Price`, `Ma2Price`, `Ma3Price` | Precio aplicado para cada media móvil. |
| Entradas | `UseRsi` | Habilite la confirmación RSI. |
| Indicadores | `RsiPeriod` | RSI período de cálculo. |
| Entradas | `RsiMode` | RSI modo de confirmación (sobrecompra/sobreventa, tendencia, combinado, zona). |
| Entradas | `RsiBuyLevel`, `RsiSellLevel` | Umbrales para la lógica de sobreventa/sobrecompra. |
| Entradas | `RsiBuyZone`, `RsiSellZone` | Umbrales de zona para el modo 4. |
| Entradas | `UseStochastic` | Habilite la confirmación estocástica. |
| Indicadores | `StochasticK`, `StochasticD`, `StochasticSlowing` | Parámetros K/D/lento. |
| Entradas | `UseStochasticHighLow` | Requerir estocástico para romper las bandas altas/bajas configuradas. |
| Entradas | `StochasticHigh`, `StochasticLow` | Umbrales estocásticos superior e inferior. |
| Entradas | `UseSar` | Habilite la confirmación parabólica SAR. |
| Indicadores | `SarStep`, `SarMax` | SAR configuración de aceleración. |
| Entradas | `UseMacd` | Habilite la confirmación MACD sin retraso. |
| Indicadores | `MacdFast`, `MacdSlow`, `MacdSignal` | MACD parámetros. |
| Indicadores | `MacdPrice` | Precio aplicado para MACD. |
| Entradas | `MacdMode` | MACD modo de confirmación. |
| Riesgo | `UseTrailingStop`, `TrailingStop` | Alternancia de trailing stop y distancia (en puntos). |
| Riesgo | `UseStaticVolume`, `StaticVolume`, `RiskPercent` | Controles de tamaño de posición. |
| Riesgo | `AtrPeriod`, `AtrMultiplier` | ATR configuración para la gestión de riesgos. |
| Salidas | `AutoClose` | Habilite la lógica de consenso de salida. |
| Salidas | `OpenOppositeAfterClose` | Gire en la dirección opuesta después de una salida basada en señales. |
| Salidas | `UseMaClosing`, `MaModeClosing` | Configuración de salida de media móvil. |
| Salidas | `UseMacdClosing`, `MacdModeClosing` | MACD salir de la configuración. |
| Salidas | `UseRsiClosing`, `RsiModeClosing` | RSI salir de la configuración. |
| Salidas | `UseStochasticClosing` | Stochastic palanca de salida. |
| Salidas | `UseSarClosing` | SAR palanca de salida. |
| generales | `CandleType` | Periodo de tiempo principal (velas predeterminadas de 5 minutos). |

## Notas
- La estrategia opera una posición neta a la vez (larga, corta o plana), reflejando la restricción de "máximo de órdenes iguales" de MetaTrader con un enfoque más simple y amigable con StockSharp.
- Las entradas opuestas pendientes se ponen en cola solo para salidas basadas en señales y se omiten si un stop-loss o take-profit cierra la operación.
- Debido a que los requisitos de margen de cuenta son específicos de cada corredor, el tamaño de la posición dinámica utiliza una fórmula aproximada basada en el riesgo; verifique el volumen resultante antes de la implementación en vivo.
- Asegúrese de que los indicadores de retraso cero MACD y ATR tengan suficiente historial de preparación antes de esperar operaciones, tal como en el EA original.
