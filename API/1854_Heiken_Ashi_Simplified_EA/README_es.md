# Estrategia Heiken Ashi Simplified EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema basado en patrones construido sobre velas Heikin Ashi. La estrategia observa una secuencia de aperturas y cierres anteriores de Heikin Ashi. Cuando tres cierres consecutivos suben (o bajan) por encima de sus respectivas aperturas mientras las aperturas forman una corrección decelerada, la siguiente vela puede desencadenar una operación de ruptura una vez que el precio se aleja de la última apertura de Heikin Ashi por una distancia mínima. El algoritmo escala posiciones hasta un límite definido.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Tres cierres HA anteriores están por encima de las aperturas anteriores y las aperturas forman una serie decreciente con diferencias cada vez menores.
  - **Corto**: Tres cierres HA anteriores están por debajo de las aperturas anteriores y las aperturas forman una serie creciente con diferencias cada vez mayores.
- **Largo/Corto**: Ambas direcciones
- **Criterios de salida**:
  - Señal opuesta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `CandleType` = 1 hora
  - `MaxPositions` = 3
  - `DistancePoints` = 300
  - `Volume` = 1
- **Filtros**:
  - Categoría: Ruptura de patrón
  - Dirección: Ambos
  - Indicadores: Heikin Ashi
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Por hora
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
