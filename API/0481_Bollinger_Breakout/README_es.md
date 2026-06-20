# Estrategia de Rompimiento Bollinger 4H
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Rompimiento Bollinger 4H opera rompimientos de Bandas de Bollinger en el gráfico de cuatro horas. Las posiciones largas se abren cuando el precio cruza por encima de la banda inferior con confirmación de volumen y tendencia. Las posiciones cortas se abren cuando el precio cruza por debajo de la banda superior y el RSI está por debajo de un umbral.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cierre cruza por encima de la banda inferior, volumen por encima de su SMA y precio por encima de la SMA de tendencia.
  - **Corto**: El cierre cruza por debajo de la banda superior, volumen por encima de su SMA, precio por debajo de la SMA de tendencia, RSI < 85.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: El cierre cruza por encima de la banda superior.
  - **Corto**: El cierre cruza por debajo de la banda inferior.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 1.8
  - `VolumeLength` = 20
  - `TrendLength` = 80
  - `RsiLength` = 14
  - `UseLongSignals` = True
  - `UseShortSignals` = True
- **Filtros**:
  - Categoría: Ruptura de tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, SMA de volumen, SMA de tendencia, RSI
  - Stops: Ninguno
  - Complejidad: Moderado
  - Marco temporal: 4H
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
