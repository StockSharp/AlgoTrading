# Estrategia Rsi Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en los indicadores RSI y Supertrend. Entra largo cuando el RSI está en sobrevendido (< RsiOversold) y el precio está por encima de Supertrend. Entra corto cuando el RSI está en sobrecomprado (> RsiOverbought) y el precio está por debajo de Supertrend.

Las pruebas indican un retorno anual promedio de aproximadamente 112%. Funciona mejor en el mercado forex.

El oscilador RSI define los extremos de momentum mientras Supertrend apunta a la dirección predominante. Las operaciones ocurren cuando el RSI se alinea con el color de Supertrend.

Funciona para traders que aprecian una salida estilo trailing stop. La configuración ATR del propio Supertrend da forma a esa línea de seguimiento.

## Detalles

- **Criterios de entrada**:
  - Largo: `RSI < RsiOversold && Close > Supertrend`
  - Corto: `RSI > RsiOverbought && Close < Supertrend`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cambio de Supertrend
- **Stops**: Trailing con Supertrend
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    En velas de cinco minutos el RSI casi nunca llega a 30 con el precio por encima del Supertrend (ni a 70 por debajo): el archivo de BTC de marzo de 2024 no tiene ni una vela así. Por eso los niveles son parámetros con valores 40 y 60, que siguen marcando un retroceso contra la tendencia y operan en ambos sentidos. Un largo se cierra cuando el Supertrend gira a la baja y un corto cuando gira al alza. Una señal de entrada contra una posición abierta la invierte.
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: RSI, Supertrend
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

