# Estrategia Hoffman Heiken Bias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hoffman Heiken Bias combina un grupo de medias móviles con un modelo de volumen neto Heikin Ashi para medir la dirección de la tendencia. Se abre una posición larga cuando la SMA rápida sube por encima de la EMA rápida mientras que todas las medias de más largo plazo se mantienen por debajo de ella y la regresión del volumen neto es positiva. Las posiciones cortas se activan en las condiciones contrarias.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `SMA(5) > EMA(18)` && todas las medias más largas por debajo de `EMA(18)` && regresión de volumen neto > 0.
  - **Corto**: `SMA(5) < EMA(18)` && todas las medias más largas por encima de `EMA(18)` && regresión de volumen neto < 0.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Fast SMA` = 5
  - `Fast EMA` = 18
  - `Net volume length` = 25
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, EMA, ATR, Linear Regression
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
