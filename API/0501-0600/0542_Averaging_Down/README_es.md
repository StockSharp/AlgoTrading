# Estrategia de Promediación a la Baja
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Promediación a la Baja compra cuando el Índice de Fuerza Relativa (RSI) cae por debajo de un umbral definido. Cada señal añade a la posición larga existente, promediando el precio de entrada. La estrategia sale cuando el precio de cierre rompe por encima del máximo de la barra anterior.

## Detalles

- **Criterios de entrada**:
  - RSI por debajo de `RsiBuyThreshold`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - El precio de cierre supera el máximo de la barra anterior.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RsiLength` = 10
  - `RsiBuyThreshold` = 33
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
