# MACD Doble
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El MACD Doble utiliza dos indicadores MACD con diferentes velocidades. Una posición se abre solo cuando ambos MACD coinciden en la dirección.

El primer MACD es rápido y reacciona velozmente. El segundo es más lento y confirma la tendencia antes de tomar operaciones.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Ambas líneas MACD por encima de sus líneas de señal.
  - **Corto**: Ambas líneas MACD por debajo de sus líneas de señal.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Stop loss opcional.
- **Valores predeterminados**:
  - `FastLength1` = 12
  - `SlowLength1` = 26
  - `SignalLength1` = 9
  - `MaType1` = Ema
  - `FastLength2` = 24
  - `SlowLength2` = 52
  - `SignalLength2` = 9
  - `MaType2` = Ema
  - `StopLossPercent` = 2
  - `CandleType` = tf(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo y Corto
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
