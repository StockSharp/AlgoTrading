# Estrategia Vegas Tunnel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza cuatro EMAs para definir un túnel y stops opcionales basados en ATR.
Abre largo cuando el precio y la EMA rápida están por encima de las EMAs lentas y del túnel, y corto cuando están por debajo.

## Detalles

- **Criterios de entrada**: alineación de EMAs con el precio relativo al túnel
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop loss o take profit
- **Stops**: basados en ATR o EMA
- **Valores predeterminados**:
  - `RiskRewardRatio` = 2
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMult` = 1.5
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
