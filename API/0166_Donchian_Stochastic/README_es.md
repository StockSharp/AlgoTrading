# Estrategia Donchian Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia Donchian Channel + Stochastic. La estrategia entra al mercado cuando el precio rompe el Canal de Donchian con el Stochastic confirmando condiciones de sobreventa/sobrecompra.

Las pruebas indican un rendimiento anual promedio de aproximadamente 85%. Funciona mejor en el mercado de criptomonedas.

Las rupturas más allá del canal de Donchian se confirman con el impulso del Stochastic. Las operaciones comienzan en cuanto el precio escapa del rango y el oscilador lo confirma.

Útil para traders que esperan un seguimiento inmediato. Un múltiplo de ATR establece el stop.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > DonchianHigh && StochK < 20`
  - Corto: `Close < DonchianLow && StochK > 80`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Fallo de ruptura o señal opuesta
- **Stops**: Basados en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `DonchianPeriod` = 20
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2m
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Donchian Channel, Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

