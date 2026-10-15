# Estrategia de Captura de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Trend Catcher** combina el Parabolic SAR con múltiples medias móviles simples para capturar movimientos direccionales. Espera a que el precio cruce el Parabolic SAR en la dirección de las medias rápidas predominantes y luego gestiona la posición mediante reglas dinámicas de stop-loss y trailing.

Se abre una operación cuando la última vela cierra en el lado opuesto del Parabolic SAR respecto a la vela anterior, mientras las medias rápidas confirman el movimiento. El stop-loss inicial se calcula a partir de la distancia al punto SAR y está limitado por valores mínimos y máximos. Los objetivos de beneficio se definen como un múltiplo de la distancia del stop. Cuando el precio avanza una cantidad especificada, el stop se mueve al punto de equilibrio con un pequeño desplazamiento y luego sigue al precio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close[0] > SAR && Close[1] < SAR_prev && FastMA > SlowMA && Close > FastMA2`.
  - **Corto**: `Close[0] < SAR && Close[1] > SAR_prev && FastMA < SlowMA && Close < FastMA2`.
- **Criterios de salida**:
  - Se alcanzan los niveles de stop-loss o take-profit.
  - Trailing stop activado tras el umbral de beneficio.
  - Una señal contraria cierra la posición existente.
- **Stops**: Stop-loss dinámico basado en SAR con ajustes opcionales de punto de equilibrio y trailing.
- **Valores predeterminados**:
  - `SlowMaPeriod = 200`
  - `FastMaPeriod = 50`
  - `FastMa2Period = 25`
  - `SarStep = 0.004`
  - `SarMax = 0.2`
  - `SlMultiplier = 1`
  - `TpMultiplier = 1`
  - `MinStopLoss = 10`
  - `MaxStopLoss = 200`
  - `ProfitLevel = 500`
  - `BreakevenOffset = 1`
  - `TrailingThreshold = 500`
  - `TrailingDistance = 10`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR, SMA
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
