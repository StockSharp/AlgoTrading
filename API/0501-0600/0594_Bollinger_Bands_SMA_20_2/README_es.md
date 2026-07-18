# Estrategia de Bollinger Bands SMA 20-2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza Bollinger Bands construidas a partir de una media móvil simple de 20 períodos con un multiplicador de 2 desviaciones estándar. Va en largo cuando el precio cruza por encima de la banda inferior y en corto cuando el precio cruza por debajo de la banda superior. Las posiciones se revierten con señales opuestas sin stop losses explícitos.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close` cruza por encima de la banda inferior.
  - **Corto**: `Close` cruza por debajo de la banda superior.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Bollinger Length` = 20
  - `Bollinger Multiplier` = 2
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
