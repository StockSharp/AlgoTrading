# Nivel de Mercado Adaptativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera basándose en el indicador Adaptive Market Level (AML). El indicador se adapta a la volatilidad actual y traza un nivel de precio dinámico. Se abre una posición larga cuando la línea AML gira hacia arriba y una posición corta cuando gira hacia abajo. Las posiciones opuestas se cierran en un cambio de color o cuando se activa el stop-loss/take-profit.

El sistema sigue tendencias de medio plazo y funciona en marcos temporales más altos por defecto.

## Detalles

- **Criterios de entrada**: La línea AML cambia de dirección hacia arriba para largos y hacia abajo para cortos.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cambio de dirección del AML o stop/objetivo.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Fractal` = 6
  - `Lag` = 7
  - `StopLossTicks` = 1000
  - `TakeProfitTicks` = 2000
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Adaptive Market Level
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: H4
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
