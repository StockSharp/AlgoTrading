# Fib Hurst Ruptura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Fib Hurst Ruptura combina niveles de retroceso de Fibonacci del marco temporal diario con un filtro de exponente de Hurst. El cruce del precio por los niveles clave de Fibonacci en la dirección de la tendencia predominante activa las entradas, mientras que un stop del 2% y una relación riesgo-recompensa de 1:2 gestionan el riesgo.

## Detalles

- **Criterios de entrada**:
  - Largo: El cierre cruza por encima del nivel 61.8% y Hurst diario > 0.5
  - Corto: El cierre cruza por debajo del nivel 38.2% y Hurst diario < 0.5
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop-loss o take-profit
- **Stops**: Sí
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `HurstPeriod` = 50
  - `MaxTradesPerDay` = 5
  - `MaxTotalTrades` = 510
  - `RiskPercent` = 2m
  - `RiskReward` = 2m
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Hurst, Fibonacci
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
