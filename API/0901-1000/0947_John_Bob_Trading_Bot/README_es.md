# Estrategia John Bob Trading Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura que combina niveles de máximo/mínimo de 50 barras con detección simple de fair value gaps. Abre cinco órdenes escalonadas con stop-loss basado en ATR y múltiples niveles de take profit.

## Detalles

- **Criterios de entrada**:
  - Largo: el precio cruza por encima del mínimo de 50 barras o aparece un fair value gap alcista
  - Corto: el precio cruza por debajo del máximo de 50 barras o aparece un fair value gap bajista
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - El precio alcanza uno de los cinco niveles de take profit
  - El precio alcanza el stop-loss basado en ATR
- **Stops**: Multiplicador ATR
- **Valores predeterminados**:
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: ATR, Highest, Lowest
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
