# Estrategia Exp Multic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia multicuenta que opera un conjunto fijo de los principales pares Forex sin indicadores técnicos.
Para cada par, el algoritmo mantiene una dirección y un volumen. Tras un movimiento rentable el volumen aumenta; tras una pérdida, la dirección se invierte. Las operaciones se detienen y todas las posiciones se cierran cuando el beneficio o la pérdida global supera los umbrales especificados.

## Detalles

- **Criterios de entrada**:
  - Si no hay posición y el saldo de la cuenta supera `Margin`, se abre una posición en la dirección predefinida con `MinVolume`.
- **Largo/Corto**: Ambos, según la dirección interna de cada par.
- **Criterios de salida**:
  - Cerrar la posición cuando el beneficio supera `KClose * MinVolume`.
  - Invertir la dirección y cerrar cuando la pérdida supera `KChange * volumen actual`.
- **Stops**: Sin stops explícitos; el riesgo se controla mediante los umbrales de beneficio/pérdida.
- **Valores predeterminados**:
  - `Loss` = 1900
  - `Profit` = 4000
  - `Margin` = 5000
  - `MinVolume` = 0.01
  - `KChange` = 2100
  - `KClose` = 4600
- **Filtros**:
  - Categoría: Gestión monetaria
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Basado en ticks
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
