# Captura de Volatilidad RSI-Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia combina bandas de Bollinger dinámicas con un filtro RSI opcional para capturar oscilaciones de volatilidad.

## Detalles
- **Criterios de entrada**: Precio cruzando la banda de Bollinger adaptativa con confirmación RSI opcional.
- **Largo/Corto**: Configurable mediante `Direction`.
- **Criterios de salida**: Precio cruzando el lado opuesto de la banda trailing.
- **Stops**: No.
- **Valores predeterminados**:
  - `BollingerLength` = 50
  - `Multiplier` = 2.7183m
  - `UseRsi` = true
  - `RsiPeriod` = 10
  - `RsiSmaPeriod` = 5
  - `BoughtRangeLevel` = 55m
  - `SoldRangeLevel` = 50m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Configurable
  - Indicadores: Bollinger, RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
