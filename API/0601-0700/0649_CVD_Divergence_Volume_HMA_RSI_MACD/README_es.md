# Estrategia CVD Divergencia Volumen HMA RSI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina Hull Moving Averages, RSI, MACD, filtro de volumen y divergencia del delta de volumen acumulado (CVD) para identificar oportunidades de tendencia.

Las posiciones largas se abren cuando HMA20 está por encima de HMA50, el RSI muestra momentum alcista, el histograma MACD sube, el volumen supera su media y el CVD forma divergencia alcista o aumenta. Las posiciones cortas reflejan estas condiciones de forma inversa.

## Detalles
- **Criterios de entrada**:
  - **Largo**: HMA20 > HMA50 y precio > HMA20; RSI entre 40 y `RsiOverbought`; línea MACD sobre señal e histograma subiendo; volumen > SMA * `VolumeMultiplier`; divergencia CVD alcista o CVD creciente.
  - **Corto**: HMA20 < HMA50 y precio < HMA20; RSI entre `RsiOversold` y 60; línea MACD bajo señal e histograma bajando; volumen > SMA * `VolumeMultiplier`; divergencia CVD bajista o CVD decreciente.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Precio < HMA20 o RSI > `RsiOverbought` o línea MACD cruza bajo señal.
  - **Corto**: Precio > HMA20 o RSI < `RsiOversold` o línea MACD cruza sobre señal.
- **Stops**: No.
- **Valores predeterminados**:
  - `Hma20Length` = 20
  - `Hma50Length` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: HMA, RSI, MACD, Volumen, CVD
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
