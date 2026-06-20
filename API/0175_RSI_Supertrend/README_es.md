# Rsi Supertrend Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en los indicadores RSI y Supertrend. Entra largo cuando el RSI está en sobrevendido (< 30) y el precio está por encima de Supertrend. Entra corto cuando el RSI está en sobrecomprado (> 70) y el precio está por debajo de Supertrend.

Las pruebas indican un retorno anual promedio de aproximadamente 112%. Funciona mejor en el mercado forex.

El oscilador RSI define los extremos de momentum mientras Supertrend apunta a la dirección predominante. Las operaciones ocurren cuando el RSI se alinea con el color de Supertrend.

Funciona para traders que aprecian una salida estilo trailing stop. La configuración de ATR protege aún más la posición.

## Detalles

- **Criterios de entrada**:
  - Largo: `RSI < 30 && Close > Supertrend`
  - Corto: `RSI > 70 && Close < Supertrend`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cambio de Supertrend
- **Stops**: Trailing con Supertrend
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
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

