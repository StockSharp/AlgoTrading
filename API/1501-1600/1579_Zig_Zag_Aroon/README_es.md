# Estrategia Zig Zag Aroon
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia combina la detección simple de pivotes Zig Zag con el indicador Aroon. Compra cuando Aroon Up cruza por encima de Aroon Down y el último pivote es un máximo. Las posiciones cortas se abren cuando Aroon Down cruza por encima de Aroon Up y el último pivote es un mínimo.

## Detalles

- **Criterios de entrada**: Cruce de Aroon con el pivote Zig Zag correspondiente.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `ZigZagDepth` = 5
  - `AroonLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Aroon, ZigZag
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
