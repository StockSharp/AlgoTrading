# Estrategia de Reversión MACD con Confirmación Stochastic 1D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que compra cuando la línea MACD cruza por encima de la señal con confirmación del oscilador Stochastic diario. La operación se cierra cuando el precio alcanza un stop loss basado en ATR o cae por debajo de un take profit EMA trailing.

## Detalles

- **Criterios de entrada**:
  - Largo: `MACD crosses above Signal && DailyK > DailyD && DailyK < 80`
- **Largo/Corto**: Solo largos
- **Stops**: Stop loss ATR y take profit EMA trailing
- **Valores predeterminados**:
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TrailingEmaLength` = 20
  - `StopLossAtrMultiplier` = 3.25m
  - `TrailingActivationAtrMultiplier` = 4.25m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Largo
  - Indicadores: MACD, Stochastic, ATR, EMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
