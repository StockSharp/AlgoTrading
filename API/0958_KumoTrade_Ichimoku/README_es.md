# Estrategia KumoTrade Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en Ichimoku Cloud y Stochastic Oscillator.
Entra en largo cuando el precio regresa por encima de Kijun con Stochastic sobrevendido y sin nube por delante.
Entra en corto cuando el precio cae por debajo de la nube con Stochastic sobrecomprado y Kumo bajista.

## Detalles

- **Criterios de entrada**:
  - Largo: `Low > Kijun && Kijun > Tenkan && Close < SenkouA && StochD < 29`
  - Corto: `Close < min(SenkouA, SenkouB) && High > Kijun && prevStochD > StochD >= 90`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Stop dinámico basado en ATR
- **Stops**: Trailing stop usando ATR * 3
- **Valores predeterminados**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochK` = 70
  - `StochD` = 15
  - `AtrPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Ichimoku Cloud, Stochastic, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
