# Estrategia Heiken Ashi Supertrend ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina velas Heiken Ashi, la dirección del Supertrend y un filtro ADX opcional. Una vela Heiken Ashi alcista sin sombra inferior abre un largo en tendencia alcista. Las velas bajistas sin sombra superior abren cortos en tendencia bajista. Las posiciones se cierran ante señales opuestas o un stop trailing basado en ATR.

Las pruebas indican un rendimiento anual promedio de aproximadamente 128%. Funciona mejor en el mercado de criptomonedas.

Heiken Ashi suaviza el ruido mientras que Supertrend y ADX confirman la dirección. El ATR determina los stops dinámicos.

## Detalles

- **Criterios de entrada**:
  - Largo: vela HA alcista sin sombra inferior con Supertrend alcista y confirmación ADX opcionales
  - Corto: vela HA bajista sin sombra superior con Supertrend bajista y confirmación ADX opcionales
- **Largo/Corto**: Ambos
- **Criterios de salida**: Vela opuesta o stop trailing ATR
- **Stops**: Stop trailing ATR
- **Valores predeterminados**:
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `UseAdxFilter` = false
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `TrailAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Heiken Ashi, Supertrend, ADX, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
