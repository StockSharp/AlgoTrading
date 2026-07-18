# Estrategia SupportResistTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de rompimiento portada desde MetaTrader que combina un filtro de tendencia EMA a largo plazo con niveles dinámicos de soporte y resistencia. Observa el rango de oscilación reciente, espera que el precio rompa el techo o suelo anterior en la dirección de la tendencia, y gestiona posiciones con trailing stops por pips escalonados.

## Detalles

- **Criterios de entrada**: el precio cierra más allá del máximo (largo) o mínimo (corto) del período `Lookback` anterior y la barra abre por encima/debajo de la EMA `MaPeriod`
- **Largo/Corto**: Ambos
- **Criterios de salida**: el trailing stop se activa o una posición rentable cruza de vuelta por la banda de soporte/resistencia actualizada
- **Stops**: stop inicial en la banda opuesta, trail tras movimientos de +20/+40/+60 pips (asegurando 10/20/30 pips respectivamente)
- **Valores predeterminados**:
  - `Lookback` = 55
  - `MaPeriod` = 500
  - `CandleType` = 1 minuto
  - `OrderVolume` = 0.1
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: EMA, Highest, Lowest
  - Stops: Trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
