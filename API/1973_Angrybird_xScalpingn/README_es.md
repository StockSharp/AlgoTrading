# Angrybird xScalpingn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Angrybird xScalpingn es una estrategia de scalping de estilo martingala. Abre una operación inicial basada en la dirección del precio a corto plazo y un filtro RSI. Cuando el precio se mueve en contra de la posición abierta en un paso dinámico derivado del rango reciente, la estrategia añade otra operación con el volumen multiplicado por un factor. Todas las posiciones se cierran cuando el CCI muestra un fuerte movimiento contrario o cuando se alcanza el stop-loss o el take-profit.

## Detalles

- **Criterios de entrada**: La operación inicial sigue la dirección de cierre reciente con un filtro RSI. Las operaciones adicionales se abren cuando el precio se mueve en contra de la posición en el paso calculado.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Reversión del CCI o stop-loss/take-profit protector.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Volume` = 0.01
  - `LotExponent` = 2
  - `DynamicPips` = true
  - `DefaultPips` = 12
  - `Depth` = 24
  - `Del` = 3
  - `TakeProfit` = 20
  - `StopLoss` = 500
  - `Drop` = 500
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `MaxTrades` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Grid
  - Dirección: Ambos
  - Indicadores: RSI, CCI
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
