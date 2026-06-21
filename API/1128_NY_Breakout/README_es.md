# Estrategia de Ruptura NY
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera Rupturas del rango formado entre las 13:00 y las 13:30 UTC. Una vez cerrada la ventana, la estrategia entra cuando el precio rompe el máximo o mínimo de la sesión, apuntando al doble del rango y colocando el stop en el lado opuesto.

## Detalles

- **Criterios de entrada**:
  - Primera vela tras las 13:30 UTC cierra por encima del máximo de sesión -> largo.
  - Primera vela tras las 13:30 UTC cierra por debajo del mínimo de sesión -> corto.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Objetivo de beneficio en `RewardRisk` veces el rango.
  - Stop en el límite opuesto del rango.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RewardRisk` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
