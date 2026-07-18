# Estrategia Forex Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Traducción del asesor experto de MetaTrader «Forex Profit». La estrategia espera la alineación de tres medias móviles exponenciales y la confirmación del Parabolic SAR antes de entrar en operaciones al cierre de cada vela terminada. El riesgo se controla mediante distancias asimétricas de stop-loss y take-profit, un stop móvil y un bloqueo de ganancias adicional basado en EMA.

## Detalles

- **Criterios de entrada**:
  - Largo: `EMA10` por encima de `EMA25` y `EMA50`, la `EMA10` de la barra anterior en o por debajo de `EMA50`, y Parabolic SAR por debajo del cierre anterior.
  - Corto: `EMA10` por debajo de `EMA25` y `EMA50`, la `EMA10` de la barra anterior en o por encima de `EMA50`, y Parabolic SAR por encima del cierre anterior.
  - Las señales se evalúan solo una vez por vela completada.
- **Criterios de salida**:
  - Cerrar largo cuando `EMA10` gira por debajo de su valor anterior *y* la ganancia actual supera el `ProfitThreshold`.
  - Cerrar corto cuando `EMA10` gira por encima de su valor anterior *y* la ganancia actual supera el `ProfitThreshold`.
  - Niveles de stop-loss y take-profit protectores establecidos al abrir la orden (diferentes distancias para largos vs cortos).
  - El stop móvil se activa cuando el precio se mueve `TrailingStopPoints` más allá de la entrada y se actualiza en incrementos de `TrailingStepPoints`.
- **Stops**: Sí — stop-loss fijo, take-profit fijo y gestión de stop móvil.
- **Valores predeterminados**:
  - `FastEmaLength` = 10
  - `MediumEmaLength` = 25
  - `SlowEmaLength` = 50
  - `TakeProfitBuyPoints` = 55
  - `TakeProfitSellPoints` = 65
  - `StopLossBuyPoints` = 60
  - `StopLossSellPoints` = 85
  - `TrailingStopPoints` = 74
  - `TrailingStepPoints` = 5
  - `ProfitThreshold` = 10
  - `SarAcceleration` = 0.02
  - `SarMaxAcceleration` = 0.2
  - `Volume` = 1
  - `CandleType` = marco temporal de 1 hora
- **Notas adicionales**:
  - Las distancias de stop/objetivo se expresan en pasos de precio del instrumento y se convierten automáticamente usando el tamaño del tick del instrumento.
  - Las salidas basadas en ganancias dependen de la ganancia total de la posición (incluyendo el volumen) convertida de ticks de precio a la moneda de la cuenta.
  - La lógica de trailing mantiene el stop detrás de los movimientos de precio sin sobrepasar el paso configurado.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo/Corto
  - Indicadores: EMA, Parabolic SAR
  - Stops: Sí (fijos + trailing)
  - Complejidad: Intermedio
  - Marco temporal: Configurable (por defecto 1 hora)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
