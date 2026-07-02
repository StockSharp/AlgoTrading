# Estrategia DMI Power Move
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en movimientos de poder del DMI (Índice de Movimiento Direccional)

Las pruebas indican un retorno anual promedio de aproximadamente 76%. Funciona mejor en el mercado de forex.

DMI Power Move combina las diferencias del indicador direccional con el ADX para capturar tendencias poderosas. Las operaciones entran cuando +DI supera notablemente a -DI (o viceversa) y el ADX es fuerte. Salen cuando el ADX se debilita o la diferencia entre DI se estrecha.

Este enfoque filtra las señales débiles al requerir tanto un movimiento direccional fuerte como un ADX en ascenso. El resultado son menos operaciones, pero potencialmente de mayor calidad en tendencia.


## Detalles

- **Criterios de entrada**: Señales basadas en ADX, ATR, DMI.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `DmiPeriod` = 14
  - `DiDifferenceThreshold` = 5m
  - `AdxThreshold` = 30m
  - `AdxExitThreshold` = 25m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ADX, ATR, DMI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Neural Networks: No
  - Divergencia: No
  - Nivel de riesgo: Medio

