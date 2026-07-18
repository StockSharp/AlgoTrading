# Estrategia RoNz Auto SL TS TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que abre posiciones en el cruce de EMA y gestiona automáticamente los niveles de stop-loss y take-profit.  
Tras la entrada, establece el stop y el objetivo iniciales, luego opcionalmente bloquea el beneficio y activa un trailing stop.

## Detalles

- **Criterios de entrada**:
  - Largo: `EMA10 < EMA20 && EMA10 > EMA100`
  - Corto: `EMA10 > EMA20 && EMA10 < EMA100`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop loss, take profit, bloqueo de beneficio o trailing stop
- **Stops**: Sí
- **Valores predeterminados**:
  - `TakeProfit` = 500
  - `StopLoss` = 250
  - `LockProfitAfter` = 100
  - `ProfitLock` = 60
  - `TrailingStop` = 50
  - `TrailingStep` = 10
- **Filtros**:
  - Categoría: Gestión de riesgos
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: SL/TP/Trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
