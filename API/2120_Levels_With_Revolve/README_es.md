# Estrategia de Niveles con Revolve
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre operaciones cuando el precio de mercado cruza un nivel definido por el usuario. Se coloca una orden de compra cuando el precio sube a través del nivel y una orden de venta cuando cae por debajo de él. El sistema puede opcionalmente revertir una posición existente si aparece la señal contraria. También admite distancias opcionales de stop-loss y take-profit medidas en unidades de precio.

La estrategia se suscribe a velas y reacciona únicamente cuando una vela está completamente formada. Todos los cálculos se realizan sobre el precio de cierre de cada vela terminada. Cuando el modo de reversión está activado, la posición actual se cierra y se abre una nueva en la dirección opuesta en la siguiente señal.

## Detalles

- **Criterios de entrada**:
  - Largo: el precio de cierre cruza por encima de `LevelPrice`.
  - Corto: el precio de cierre cruza por debajo de `LevelPrice`.
- **Largo/Corto**: Ambos direcciones.
- **Reversión**: Opcional, controlada por `EnableReversal`.
- **Stops**: Stop-loss y take-profit opcionales en unidades de precio.
- **Valores predeterminados**:
  - `LevelPrice` = 100.
  - `StopLoss` = 0 (desactivado).
  - `TakeProfit` = 0 (desactivado).
  - `EnableReversal` = false.
  - `CandleType` = marco temporal de 1 minuto.
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Opcional
  - Complejidad: Simple
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
