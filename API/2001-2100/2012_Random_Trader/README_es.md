# Estrategia de Operador Aleatorio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Abre una posición larga o corta aleatoriamente cuando no hay posición abierta. Cada operación utiliza valores fijos de take profit y stop loss medidos en unidades de precio.

## Detalles

- **Criterios de entrada**: sin posición y elección aleatoria
- **Largo/Corto**: Ambos
- **Criterios de salida**: el precio alcanza el take profit o el stop loss
- **Stops**: Sí
- **Valores predeterminados**:
  - `Volume` = 1
  - `TakeProfit` = 10
  - `StopLoss` = 10
- **Filtros**:
  - Categoría: Otro
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Tick
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
