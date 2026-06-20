# Estrategia SMI Ergódica Adaptativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia SMI Ergódica Adaptativa utiliza el oscilador True Strength Index (TSI) con una línea de señal EMA para detectar reversiones desde extremos de sobrecompra o sobreventa. Se abre una posición larga cuando el TSI cruza por encima del umbral de sobreventa mientras se mantiene por encima de su línea de señal. Se abre una posición corta cuando el TSI cruza por debajo del umbral de sobrecompra y está por debajo de la línea de señal.

## Detalles

- **Criterios de entrada**:
  - TSI cruza por encima de sobreventa y TSI > señal (largo).
  - TSI cruza por debajo de sobrecompra y TSI < señal (corto).
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La señal inversa activa la operación opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `LongLength` = 12
  - `ShortLength` = 5
  - `SignalLength` = 5
  - `OversoldThreshold` = -0.4
  - `OverboughtThreshold` = 0.4
- **Filtros**:
  - Categoría: Oscilador de momentum
  - Dirección: Largo/Corto
  - Indicadores: True Strength Index, EMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
