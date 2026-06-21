# Estrategia Supertrend EMA Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina Supertrend con confirmación de tendencia por EMA y filtro de volumen. Entra en reversiones de Supertrend cuando el precio está por encima o debajo de la EMA y el volumen supera su EMA. Implementa stop loss basado en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: Supertrend gira al alza, precio por encima de EMA, volumen por encima de Volume EMA
  - Corto: Supertrend gira a la baja, precio por debajo de EMA, volumen por encima de Volume EMA
- **Largo/Corto**: Configurable
- **Criterios de salida**: Reversión de Supertrend o stop loss basado en ATR
- **Stops**: Múltiplo de ATR
- **Valores predeterminados**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `EmaLength` = 21
  - `StartDate` = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)
  - `AllowLong` = true
  - `AllowShort` = false
  - `SlMultiplier` = 2m
  - `UseVolumeFilter` = true
  - `VolumeEmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Supertrend, EMA, Volume EMA, ATR
  - Stops: ATR
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
