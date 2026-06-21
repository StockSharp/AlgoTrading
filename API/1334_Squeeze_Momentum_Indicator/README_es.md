# Estrategia de Indicador de Momentum de Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Squeeze Momentum Indicator detecta contracción de volatilidad cuando las Bandas de Bollinger caen dentro de los Canales de Keltner. Se abre una posición larga cuando el squeeze se libera hacia arriba con momentum creciente y el precio por encima de la EMA de 100 períodos. Se toman cortos en una liberación hacia abajo con momentum decreciente y el precio por debajo de la EMA. Las posiciones salen cuando el momentum se revierte.

## Detalles

- **Criterios de entrada**:
  - Las Bandas de Bollinger se mueven fuera de los Canales de Keltner (liberación del squeeze).
  - **Largo**: El momentum aumenta, precio por encima del cierre anterior y EMA100, y el color del squeeze cambia de negro a gris.
  - **Corto**: El momentum disminuye, precio por debajo del cierre anterior y EMA100, y el color cambia de gris a negro.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - El momentum se revierte.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `BbLength` = 20
  - `BbMultiplier` = 2
  - `KcLength` = 20
  - `KcMultiplier` = 1.5
  - `EmaLength` = 100
- **Filtros**:
  - Categoría: Ruptura de volatilidad
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Keltner Channels, Linear Regression, EMA
  - Stops: Ninguno
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
