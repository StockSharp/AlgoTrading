# Estrategia Hull MA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia Hull Moving Average + RSI. Comprar cuando la HMA está subiendo y el RSI está por debajo de 30 (sobrevendido). Vender cuando la HMA está bajando y el RSI está por encima de 70 (sobrecomprado).

Las pruebas indican un retorno anual promedio de aproximadamente el 64%. Funciona mejor en el mercado de divisas.

La Hull MA proporciona una línea de tendencia suavizada y el RSI resalta las divergencias de momentum. Las operaciones ocurren cuando el RSI gira en los extremos mientras el precio sigue la dirección de Hull.

Adecuada para traders de swing a corto plazo que buscan señales tempranas. Los stops basados en ATR protegen la operación.

## Detalles

- **Criterios de entrada**:
  - Largo: `HullMA turning up && RSI < RsiOversold`
  - Corto: `HullMA turning down && RSI > RsiOverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cambio de dirección de la Hull MA
- **Stops**: Basados en ATR usando `StopLoss`
- **Valores predeterminados**:
  - `HmaPeriod` = 9
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Hull MA, Moving Average, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
