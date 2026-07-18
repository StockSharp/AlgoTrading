# Estrategia de Balance de Poder
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Balance de Poder evalúa la fuerza de los alcistas frente a los bajistas dentro de cada vela comparando el cierre con el rango de trading. Cuando este valor cruza por encima de un umbral positivo, indica una fuerte presión compradora.

La estrategia entra en una posición larga cuando el Balance de Poder cruza por encima del `Threshold` definido y sale cuando cae por debajo del umbral negativo.

## Detalles

- **Criterios de entrada**:
  - Balance of Power cruza por encima de `Threshold`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Balance of Power cruza por debajo de `-Threshold`.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Threshold` = 0.8
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo
  - Indicadores: Balance of Power
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
