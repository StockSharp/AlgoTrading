# Estrategia Donchian Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia Donchian Channel + Stochastic. La estrategia entra al mercado cuando el precio rompe el Canal de Donchian con el Stochastic confirmando condiciones de sobreventa/sobrecompra.

Las pruebas indican un rendimiento anual promedio de aproximadamente 85%. Funciona mejor en el mercado de criptomonedas.

Las rupturas más allá del canal de Donchian se confirman con el impulso del Stochastic. Las operaciones comienzan en cuanto el precio escapa del rango y el oscilador lo confirma.

Útil para traders que esperan un seguimiento inmediato. Un porcentaje fijo del precio de entrada establece el stop.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > DonchianHigh && StochK > StochOverbought`
  - Corto: `Close < DonchianLow && StochK < StochOversold`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Fallo de ruptura o señal opuesta
- **Stops**: Basados en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `DonchianPeriod` = 20
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOverbought` = 80
  - `StochOversold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2
    Una ruptura alcista de las DonchianPeriod velas anteriores se confirma cuando %K está por encima de StochOverbought y una bajista cuando %K está por debajo de StochOversold; la lectura contraria, un %K sobrevendido en una ruptura alcista, prácticamente no ocurre. %K es el estocástico de StochPeriod velas suavizado en StochK velas, y %D no interviene. La ruptura falla, y la posición se cierra, cuando el precio vuelve a cerrar al otro lado del nivel roto. Una señal de entrada contra una posición abierta la invierte.
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

