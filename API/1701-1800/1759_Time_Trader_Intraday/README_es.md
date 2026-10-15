# Estrategia de Operador Intradía por Tiempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia abre posiciones largas y/o cortas en un momento específico del día con distancias predefinidas de stop loss y take profit. Es útil para probar entradas basadas en tiempo sin ninguna confirmación de indicadores.

## Detalles

- **Criterios de entrada**: Activador basado en tiempo en la hora y minuto configurados.
- **Largo/Corto**: Ambas direcciones (configurable).
- **Criterios de salida**: Stop protector o objetivo.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `TradeHour` = 0
  - `TradeMinute` = 0
  - `AllowBuy` = true
  - `AllowSell` = true
  - `TakeProfitTicks` = 20
  - `StopLossTicks` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Otro
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Fijo
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
