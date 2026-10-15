# Estrategia DSL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia combina Líneas de Señal Discontinuas (DSL) con bandas ATR y un oscilador Beluga. Se abre una posición larga cuando el precio se mantiene por encima de la línea DSL durante tres barras y el oscilador cruza por encima de su línea DSL inferior. Las posiciones cortas se abren en las condiciones opuestas. Cada operación utiliza la banda DSL correspondiente como stop y un objetivo de riesgo-beneficio para el take profit.

## Detalles

- **Criterios de entrada**:
  - Banda superior de DSL por encima de la línea inferior para largos; banda inferior por debajo de la línea superior para cortos.
  - Apertura y cierre de vela por encima (o por debajo) de la línea DSL durante tres barras consecutivas.
  - Señal de cruce del oscilador DSL-Beluga.
- **Largo/Corto**: Largo y corto.
- **Criterios de salida**:
  - Stop loss en la banda DSL.
  - Take profit en múltiplo de riesgo-beneficio.
- **Stops**: Sí, basados en ATR.
- **Valores predeterminados**:
  - `Length` = 34
  - `Offset` = 30
  - `BandsWidth` = 1
  - `RiskReward` = 1.5
  - `BelugaLength` = 10
  - `DslFastMode` = true
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: DSL, ATR, RSI
  - Stops: Sí
  - Complejidad: Alto
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
