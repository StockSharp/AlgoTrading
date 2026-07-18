# Estrategia MA + BB + SuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia combina un cruce de medias móviles con la confirmación del SuperTrend
y utiliza las Bollinger Bands para las salidas. Se abre una posición larga cuando la
MA de señal cruza por encima de la MA base y el precio está por encima de la línea
SuperTrend. Las posiciones cortas se abren en el cruce opuesto bajo un SuperTrend
bajista. Las posiciones se cierran cuando el precio toca la Bollinger Band lejana o
cuando el precio cruza el SuperTrend en dirección contraria.

## Detalles

- **Criterios de entrada**:
  - La MA de señal cruza la MA base en la dirección del SuperTrend.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**:
  - Toque de la Bollinger Band opuesta o cambio del SuperTrend.
- **Stops**: El SuperTrend actúa como stop trailing.
- **Valores predeterminados**:
  - Longitud MA señal = 89, ratio MA = 1.08.
  - Longitud BB = 30, ancho BB = 3.
  - Período SuperTrend = 20, factor SuperTrend = 4.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MA, Bollinger Bands, SuperTrend
  - Stops: SuperTrend
  - Complejidad: Moderado
  - Marco temporal: Corto/medio
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
