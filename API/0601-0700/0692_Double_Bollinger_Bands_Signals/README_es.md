# Estrategia de Señales de Doble Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza dos conjuntos de Bollinger Bands. Compra cuando el precio cruza por encima de la banda inferior de 3 desviaciones estándar y vende cuando el precio cruza por debajo de la banda superior de 3 desviaciones estándar. Las posiciones se cierran en las bandas opuestas de 2 desviaciones estándar.

## Detalles

- **Criterios de entrada**:
  - Largo: el cierre cruza por encima de la banda inferior de 3 SD
  - Corto: el cierre cruza por debajo de la banda superior de 3 SD
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: el cierre cruza por encima de la banda superior de 2 SD
  - Corto: el cierre cruza por debajo de la banda inferior de 2 SD
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `Length` = 20
  - `Width1` = 2m
  - `Width2` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
