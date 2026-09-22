# Plantilla de Estrategia Ultimate
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de impulso RSI con dos EMA como filtro de tendencia. Las señales se evalúan en velas finalizadas, mientras que el take profit y el stop loss porcentuales permanecen activos durante la pausa de 80 velas.

## Detalles

- **Criterios de entrada**: Largo cuando el RSI cruza 50 al alza y la EMA rápida está sobre la lenta; corto cuando cruza 50 a la baja y la EMA rápida está bajo la lenta.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce opuesto del RSI, take profit o stop loss.
- **Stops**: Stop loss y take profit en porcentaje.
- **Pausa**: 80 velas finalizadas después de una entrada por señal o una salida por RSI; no desactiva las protecciones de take profit ni stop loss.
- **Valores predeterminados**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 3
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: RSI, EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
