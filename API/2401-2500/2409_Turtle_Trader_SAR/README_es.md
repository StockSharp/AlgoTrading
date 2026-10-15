# Estrategia Turtle Trader SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Turtle Trader SAR convierte el sistema Turtle original de MQL5 con un trailing Parabolic SAR opcional a StockSharp C#.
La estrategia opera rupturas de canales Donchian, dimensiona posiciones por riesgo basado en ATR y puede piramidear operaciones ganadoras.

## Cómo Funciona

1. **Cálculo de Indicadores**
   - ATR de 20 períodos para volatilidad.
   - Canales Donchian para `ShortPeriod` y `ExitPeriod`.
   - Parabolic SAR opcional para stops de seguimiento.
2. **Dimensionamiento de Posición**
   - Cada entrada arriesga `RiskFraction` del capital actual.
   - El tamaño de la unidad está limitado por `MaxUnits`.
3. **Criterios de Entrada**
   - Cierre por encima del máximo de `ShortPeriod` -> comprar.
   - Cierre por debajo del mínimo de `ShortPeriod` -> vender.
4. **Piramidación**
   - Agrega nueva unidad cada movimiento de `AddInterval` ATR a favor hasta `MaxUnits`.
5. **Criterios de Salida**
   - Ruptura opuesta de `ExitPeriod`.
   - Stop ATR usando `StopAtr` y take profit opcional `TakeAtr`.
   - Si `UseSar` es true, también se aplica el stop Parabolic SAR.

## Parámetros

- `ExitPeriod` = 10
- `ShortPeriod` = 20
- `LongPeriod` = 55
- `RiskFraction` = 0.01
- `MaxUnits` = 4
- `AddInterval` = 1
- `StopAtr` = 1
- `TakeAtr` = 1
- `UseSar` = false
- `SarStep` = 0.02
- `SarMax` = 0.2
- `CandleType` = 1 day

## Etiquetas

- **Categoría**: Seguimiento de tendencia
- **Dirección**: Ambos
- **Indicadores**: ATR, Highest, Lowest, Parabolic SAR
- **Stops**: ATR / SAR
- **Complejidad**: Intermedio
- **Marco temporal**: Diario
- **Estacionalidad**: No
- **Redes neuronales**: No
- **Divergencia**: No
- **Nivel de riesgo**: Medio
