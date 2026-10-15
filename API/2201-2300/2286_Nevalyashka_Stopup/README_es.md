# Estrategia Nevalyashka Stopup
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia alterna la dirección de posición después de cada operación, imitando el juguete "Nevalyashka" que cambia de lado. Utiliza un enfoque martingala: si una operación se cierra con pérdida, las distancias de stop-loss y take-profit para la siguiente operación se multiplican por un coeficiente. Después de una operación rentable, las distancias vuelven a sus valores base y la estrategia puede opcionalmente detener el trading.

La dirección inicial es corta. Cada vez que se cierra una posición, se abre la nueva posición en dirección opuesta con el volumen preconfigurado.

## Detalles

- **Criterios de entrada**:
  - La primera operación vende a mercado.
  - Las operaciones siguientes siempre entran en la dirección opuesta a la última operación cerrada.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La posición se cierra cuando el precio alcanza la distancia de take-profit o stop-loss desde la entrada.
- **Stops**: Sí, stop-loss y take-profit fijos en puntos. Las distancias aumentan por el coeficiente martingala tras pérdidas.
- **Valores predeterminados**:
  - `StopLossPoints` = 150
  - `TakeProfitPoints` = 50
  - `OrderVolume` = 0.1
  - `MartingaleCoeff` = 1.5
  - `StopAfterProfit` = false
- **Filtros**:
  - Categoría: Reversión / Martingala
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
