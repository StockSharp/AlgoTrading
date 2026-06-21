# Estrategia IU de Cruce de MA en Marco Temporal Superior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia IU Higher Timeframe MA Cross opera cuando una media móvil rápida calculada en un marco temporal seleccionado por el usuario cruza una media móvil más lenta posiblemente de otro marco temporal. Se abre una posición larga en un cruce alcista y una posición corta en un cruce bajista. El stop-loss se coloca en el extremo de la vela anterior y el take profit utiliza una relación riesgo/recompensa configurable.

## Detalles
- **Datos**: Velas de marcos temporales especificados.
- **Criterios de entrada**:
  - **Largo**: MA1 cruza por encima de MA2.
  - **Corto**: MA1 cruza por debajo de MA2.
- **Criterios de salida**: Stop-loss o take profit alcanzado.
- **Stops**: Máximo/mínimo de la vela anterior con multiplicador `RiskToReward`.
- **Valores predeterminados**:
  - `Ma1CandleType` = 60m
  - `Ma1Length` = 20
  - `Ma1Type` = MovingAverageTypeEnum.Exponential
  - `Ma2CandleType` = 60m
  - `Ma2Length` = 50
  - `Ma2Type` = MovingAverageTypeEnum.Exponential
  - `RiskToReward` = 2
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo y Corto
  - Indicadores: Media Móvil
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
