# Estrategia de Ruptura Charles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura basada en los niveles máximos y mínimos diarios. Busca que el precio se mueva más allá del rango del día anterior con un filtro de tendencia RSI y EMA. La estrategia calcula el máximo y mínimo diarios, los desplaza por un delta configurable y entra largo por encima del nivel superior o corto por debajo del nivel inferior cuando se confirman las condiciones de tendencia.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > DailyHigh + Delta` y `RSI > 55` y `FastEMA > SlowEMA`
  - Corto: `Close < DailyLow - Delta` y `RSI < 45` y `FastEMA < SlowEMA`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal contraria o protección
- **Stops**: Take profit y stop loss configurables en porcentaje
- **Valores predeterminados**:
  - `Delta` = 0.0002m
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `RsiPeriod` = 14
  - `TakeProfit` = 1m
  - `StopLoss` = 0.5m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: EMA, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
