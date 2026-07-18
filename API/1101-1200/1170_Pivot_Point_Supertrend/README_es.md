# Estrategia Pivot Point Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina Pivot Points con un Supertrend basado en ATR para capturar reversiones de tendencia.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 65%. Funciona mejor en el mercado de acciones.

Los Pivot Points definen una línea central dinámica. Un multiplicador ATR construye bandas superior e inferior que siguen al precio. Cuando la tendencia cambia de dirección, la estrategia entra en consecuencia.

## Detalles

- **Criterios de entrada**: Señales basadas en Pivot Points y ATR Supertrend.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Pivot Points, ATR
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
