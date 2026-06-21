# Estrategia de Reversión Pin Bar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza velas Pin Bar con un filtro de tendencia y stops y objetivos basados en ATR. Un Pin Bar alcista por encima de la SMA abre una posición larga, mientras que uno bajista por debajo de ella abre una posición corta. Las entradas se omiten cuando la volatilidad es demasiado baja.

## Detalles

- **Criterios de entrada**: Pin Bar en dirección de la tendencia con mecha larga, cuerpo pequeño y ATR por encima de `MinAtr`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss o take-profit basado en ATR.
- **Stops**: Sí, múltiplos de ATR.
- **Valores predeterminados**:
  - `TrendLength` = 50
  - `MaxBodyPct` = 0.30
  - `MinWickPct` = 0.66
  - `AtrLength` = 14
  - `StopMultiplier` = 1
  - `TakeMultiplier` = 1.5
  - `MinAtr` = 0.0015
  - `CandleType` = 1 hour
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: SMA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
