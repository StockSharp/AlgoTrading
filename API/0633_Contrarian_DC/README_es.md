# Estrategia Contrarian DC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Contrarian DC opera en contra de los rompimientos del Canal Donchian. Compra cuando el precio penetra la banda inferior y vende cuando el precio toca la banda superior. Después de un stop-loss, las entradas en la misma dirección se pausan durante un número de velas. La gestión del riesgo utiliza stop-loss y take-profit simétricos basados en una relación riesgo/recompensa.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Mínimo del precio <= Donchian bajo && pausa satisfecha
  - **Corto**: Máximo del precio >= Donchian alto && pausa satisfecha
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Stop**: Stop-loss porcentual
  - **Objetivo**: Take-profit basado en riesgo/recompensa
  - **Banda**: Cerrar al alcanzar la banda opuesta
- **Stops**: Sí, basados en porcentaje
- **Valores predeterminados**:
  - `DonchianPeriod` = 20
  - `RiskRewardRatio` = 1.7m
  - `StopLossPercent` = 0.3m
  - `PauseCandles` = 3
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Donchian Channel
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
