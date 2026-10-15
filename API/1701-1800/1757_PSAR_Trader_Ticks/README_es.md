# Estrategia PSAR Trader Ticks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Parabolic SAR. PSAR Trader Ticks sigue los puntos del indicador Parabolic SAR y reacciona cuando el precio cruza de un lado al otro. Abre una posición larga cuando el precio se mueve por encima del SAR y una posición corta cuando el precio se mueve por debajo de él. El trading puede restringirse a un rango de tiempo específico, y las posiciones existentes pueden cerrarse opcionalmente cuando aparece una señal contraria. La estrategia también aplica niveles de take-profit y stop-loss medidos en ticks.

## Detalles

- **Criterios de entrada**: Precio cruzando el indicador Parabolic SAR.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal contraria (opcional), stop-loss o take-profit.
- **Stops**: Take-profit y stop-loss en ticks.
- **Valores predeterminados**:
  - `Step` = 0.001m
  - `Maximum` = 0.2m
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Take-profit, Stop-loss
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
