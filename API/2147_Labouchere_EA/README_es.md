# Estrategia Labouchere EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina un cruce de Oscilador Estocástico con una secuencia de gestión monetaria Labouchere. El indicador Estocástico genera señales cuando %K cruza a %D. El sistema Labouchere ajusta el volumen de la operación después de cada posición cerrada: las pérdidas agregan un nuevo elemento igual a la suma del primer y último número de la secuencia, mientras que las ganancias eliminan estos elementos.

Las operaciones se toman solo en velas completadas. La secuencia puede reiniciarse opcionalmente cuando se eliminan todos los números. Un filtro de tiempo permite operar dentro de una ventana intradiaria específica, y las señales opuestas pueden cerrar posiciones existentes. Se admiten niveles fijos de stop-loss y take-profit (en pasos de precio).

## Detalles
- **Criterios de entrada**:
  - **Largo**: %K cruza por encima de %D.
  - **Corto**: %K cruza por debajo de %D.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Salida opcional por señal opuesta.
  - Stop-loss y take-profit fijos (si se configuran).
- **Stops**: Sí.
- **Gestión monetaria**: Secuencia Labouchere.
- **Valores predeterminados**:
  - `LotSequence` = "0.01,0.02,0.01,0.02,0.01,0.01,0.01,0.01"
  - `NewRecycle` = true
  - `StopLoss` = 40
  - `TakeProfit` = 50
  - `IsReversed` = false
  - `UseOppositeExit` = false
  - `UseWorkTime` = false
  - `StartTime` = 00:00
  - `StopTime` = 24:00
  - `KPeriod` = 10
  - `DPeriod` = 190
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
