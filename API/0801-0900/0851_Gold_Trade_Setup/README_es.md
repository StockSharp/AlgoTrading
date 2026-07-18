# Estrategia de Configuración de Operaciones del Oro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la Media Móvil Adaptativa de Kaufman y SuperTrend.
Vende cuando AMA sube y SuperTrend cambia a tendencia alcista.
Compra cuando AMA baja y SuperTrend cambia a tendencia bajista.

## Detalles

- **Criterios de entrada**: Dirección de AMA con cambio de SuperTrend.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Niveles fijos de objetivo y stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AmaLength` = 14
  - `FastLength` = 2
  - `SlowLength` = 30
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `TargetMultiplier` = 3.0
  - `RiskMultiplier` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: KAMA, SuperTrend
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
