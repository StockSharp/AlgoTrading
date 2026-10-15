# Estrategia SEMA SDI Webhook
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el cruce de EMA suavizada y confirmación mediante el índice direccional suavizado.
Compra cuando +DI > -DI y EMA rápida > EMA lenta. Vende cuando -DI > +DI y EMA rápida < EMA lenta.

## Detalles

- **Criterios de entrada**:
  - Largo: `+DI > -DI && FastEMA > SlowEMA`
  - Corto: `+DI < -DI && FastEMA < SlowEMA`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Take profit, stop-loss, trailing
- **Stops**: TP, SL, trailing
- **Valores predeterminados**:
  - `FastEmaLength` = 58
  - `SlowEmaLength` = 70
  - `SmoothLength` = 3
  - `DiLength` = 1
  - `TakeProfitPercent` = 25
  - `StopLossPercent` = 4.8
  - `TrailingPercent` = 1.9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, Directional Index
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
