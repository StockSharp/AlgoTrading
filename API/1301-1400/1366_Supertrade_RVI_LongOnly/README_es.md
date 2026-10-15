# Estrategia Supertrade RVI Solo Largos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza el Índice de Volatilidad Relativa (RVI) cruzando por encima de 20 para abrir operaciones largas. El stop loss y el take profit se establecen a partir del porcentaje de riesgo y la relación de recompensa.

## Detalles

- **Criterios de entrada**: RVI cruza por encima del umbral
- **Largo/Corto**: Largo
- **Criterios de salida**: Stop loss o take profit
- **Stops**: Sí
- **Valores predeterminados**:
  - `RviLength` = 10
  - `EmaLength` = 14
  - `RviThreshold` = 20
  - `RiskPercent` = 1
  - `RewardRatio` = 3
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo
  - Indicadores: StdDev, EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

