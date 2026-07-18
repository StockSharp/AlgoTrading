# Stochastic Tres Períodos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Stochastic Tres Períodos** alinea señales rápidas del estocástico con confirmación de dos marcos temporales superiores. Las operaciones se abren cuando el oscilador rápido cruza mientras ambos marcos temporales superiores coinciden.

## Detalles

- **Criterios de entrada**: %K rápido cruza %D con lectura opuesta hace `ShiftEntrance` barras; ambos estocásticos de marcos temporales superiores muestran %K por encima de %D; el precio de cierre debe moverse en la dirección de la señal.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce opuesto del estocástico rápido medido en la vela anterior.
- **Stops**: Stop loss y take profit fijos en puntos mediante `StartProtection`.
- **Valores predeterminados**:
  - `CandleType1` = 5m
  - `CandleType2` = 15m
  - `CandleType3` = 30m
  - `KPeriod1` = 5
  - `KPeriod2` = 5
  - `KPeriod3` = 5
  - `KExitPeriod` = 5
  - `ShiftEntrance` = 3
  - `TakeProfitPoints` = 30
  - `StopLossPoints` = 10
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Stochastic
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
