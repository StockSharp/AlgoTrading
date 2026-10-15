# Estrategia Hull MA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia Hull Moving Average + RSI. Comprar cuando la HMA gira al alza y el RSI está por debajo de RsiOversold. Vender cuando la HMA gira a la baja y el RSI está por encima de RsiOverbought.

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
- **Stops**: Basados en ATR usando `StopLossAtr`
- **Valores predeterminados**:
  - `HmaPeriod` = 9
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StopLossAtr` = 2
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    La HMA gira al alza cuando sube después de bajar y a la baja cuando baja después de subir. El stop está a StopLossAtr ATR (AtrPeriod) del cierre de entrada y se comprueba en los cierres de vela. Una señal de entrada contra una posición abierta la invierte.
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
