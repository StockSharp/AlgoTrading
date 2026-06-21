# Vela Doji Mejorada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera velas Doji con reglas de confirmación simples y gestión de riesgo-recompensa fija. Entra cuando aparece un Doji y la vela o su predecesora confirma la dirección cerrando más allá de la apertura con pequeñas mechas. Las órdenes de protección utilizan un stop-loss en pips y un take-profit definido por una relación riesgo-recompensa.

## Detalles

- **Criterios de entrada**: Vela Doji (cuerpo <= 30% del rango). Si es alcista con mecha inferior <=1% o la vela anterior es alcista, ir largo. Si es bajista con mecha superior <=1% o la vela anterior es bajista, ir corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Take-profit o stop-loss, o cualquier nuevo Doji que cierre la posición.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RiskRewardRatio` = 2.0m
  - `StopLossPips` = 5
  - `SmaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Candlestick
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
