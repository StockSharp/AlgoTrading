# Estrategia RSI a Largo Plazo 15min
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza señales de sobreventa del RSI combinadas con medias móviles a largo plazo y confirmación de volumen para entrar en posiciones largas. Compra cuando el RSI está por debajo de 30, la SMA de 250 períodos está por encima de la SMA de 500 períodos y el volumen es significativamente superior al promedio.

## Detalles

- **Criterios de entrada**: RSI por debajo de 30, SMA(250) por encima de SMA(500) y volumen mayor a 2,5 veces su SMA de 20 períodos
- **Largo/Corto**: Solo largos
- **Criterios de salida**: SMA(250) cruzando por debajo de SMA(500) o stop-loss
- **Stops**: Sí, porcentaje fijo
- **Valores predeterminados**:
  - `RsiLength` = 10
  - `VolumeSmaLength` = 20
  - `Sma1Length` = 250
  - `Sma2Length` = 500
  - `VolumeMultiplier` = 2.5
  - `StopLossPercent` = 5
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: RSI, SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
