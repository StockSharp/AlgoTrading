# Estrategia Combo Momentum Keltner Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina la comparación de momentum con un oscilador estocástico basado en Keltner.  
Las posiciones se escalan dinámicamente según el capital y se protegen con un stop loss fijo.

## Detalles

- **Criterios de entrada**:  
  - Largo: `Momentum > 0` y `KeltnerStoch < Threshold`  
  - Corto: `Momentum < 0` y `KeltnerStoch > Threshold`
- **Largo/Corto**: Ambos  
- **Criterios de salida**:  
  - Largo: `KeltnerStoch > Threshold`  
  - Corto: `KeltnerStoch < Threshold`
- **Stops**: `SlPoints` fijo por debajo/encima de la entrada  
- **Valores predeterminados**:  
  - `MomLength` = 7  
  - `KeltnerLength` = 9  
  - `KeltnerMultiplier` = 0.5  
  - `Threshold` = 99  
  - `AtrLength` = 20  
  - `SlPoints` = 1185  
  - `EnableScaling` = true  
  - `BaseContracts` = 1  
  - `InitialCapital` = 30000  
  - `EquityStep` = 150000  
  - `MaxContracts` = 15  
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:  
  - Categoría: Seguimiento de tendencia  
  - Dirección: Ambos  
  - Indicadores: Momentum, EMA, ATR  
  - Stops: Sí  
  - Complejidad: Intermedio  
  - Marco temporal: Medio plazo  
  - Estacionalidad: No  
  - Redes neuronales: No  
  - Divergencia: No  
  - Nivel de riesgo: Medio
