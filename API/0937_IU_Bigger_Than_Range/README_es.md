# Estrategia IU Mayor que el Rango
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura que abre operaciones cuando el cuerpo de la vela es mayor que el rango previo de las velas recientes.

El sistema compara el cuerpo de la vela actual con el rango entre el open/close más alto y el open/close más bajo durante un período de retrospectiva configurable. Si el cuerpo supera el rango previo, entra en la dirección de la vela y gestiona el riesgo mediante métodos de stop configurables.

## Detalles

- **Criterios de entrada**: Cuerpo de vela mayor que el rango previo; dirección basada en el cuerpo de la vela.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss o take-profit.
- **Stops**: Vela anterior, ATR o niveles de swing.
- **Valores predeterminados**:
  - `LookbackPeriod` = 22
  - `RiskToReward` = 3
  - `StopLossMethod` = PreviousHighLow
  - `AtrLength` = 14
  - `AtrFactor` = 2m
  - `SwingLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Highest, Lowest, ATR
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
