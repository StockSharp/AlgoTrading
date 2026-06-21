# IU Canal EMA Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertido del script de TradingView "IU EMA Channel Strategy". La estrategia opera cuando el precio cruza los canales EMA construidos desde los máximos y mínimos. El stop-loss se establece en el extremo de la vela anterior y el take profit se calcula usando una relación riesgo/recompensa.

## Detalles

- **Criterios de entrada**: El cierre cruza por encima del EMA alto para largo, por debajo del EMA bajo para corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss en el extremo de la vela anterior o take profit por relación riesgo/recompensa.
- **Stops**: Sí, stop fijo y objetivo.
- **Valores predeterminados**:
  - `EmaLength` = 100
  - `RiskToReward` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Variable
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
