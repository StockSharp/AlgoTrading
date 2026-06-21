# Estrategia Crypto SUSDT 10 min
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una sencilla estrategia de cruce de EMA que entra largo cuando el precio cierra por encima de la EMA y abre por debajo de ella, y entra corto en la condición opuesta. El stop loss y el take profit se definen como porcentajes del precio de entrada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `close > EMA` y `open < EMA`
  - **Corto**: `close < EMA` y `open > EMA`
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Take profit o stop loss.
- **Stops**: Sí, tanto take profit como stop loss.
- **Valores predeterminados**:
  - `CandleType` = 10 minutos
  - `EmaLength` = 24
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `OrderPercent` = 30
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
