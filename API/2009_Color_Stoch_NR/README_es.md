# Estrategia de Stochastic NR en Color
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera usando un oscilador Stochastic con varios modos seleccionables. Cada modo define cómo se interpretan las líneas %K y %D para generar señales de compra y venta.

Modos:

- **Breakdown** – largo cuando %K cruza por encima del nivel 50, corto cuando cae por debajo.
- **OscTwist** – reacciona a los cambios de dirección de %K.
- **SignalTwist** – reacciona a los cambios de dirección de %D.
- **OscDisposition** – largo cuando %K cruza por encima de %D, corto cuando cruza por debajo.
- **SignalBreakdown** – opera cuando %D cruza el nivel 50.

Las señales opuestas cierran posiciones existentes y abren nuevas en la dirección contraria. El control de riesgo se gestiona mediante niveles fijos de stop-loss y take-profit en porcentaje.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Depende del modo seleccionado, ver arriba.
  - **Corto**: Depende del modo seleccionado, ver arriba.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o protección de stop.
- **Stops**: Sí, `StopLossPercent` y `TakeProfitPercent`.
- **Valores predeterminados**:
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Mode` = `OscDisposition`
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 2
  - `CandleType` = 4 hour
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Stochastic
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: 4H
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
