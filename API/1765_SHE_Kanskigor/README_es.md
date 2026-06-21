# Estrategia SHE Kanskigor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia diaria abre una sola posición cada día basándose en la dirección de la vela del día anterior. A la hora configurada compra si el día anterior cerró por debajo de su apertura y vende si cerró por encima. Un take-profit y stop-loss fijos medidos en pasos de precio gestionan el riesgo. Solo se permite una operación por día.

## Detalles

- **Criterios de entrada**: A `StartTime` comparar la apertura y cierre del día anterior; comprar cuando `open > close`, vender cuando `open < close`
- **Largo/Corto**: Ambos
- **Criterios de salida**: take profit o stop loss
- **Stops**: Sí
- **Valores predeterminados**:
  - `Volume` = 0.1
  - `StartTime` = 00:05
  - `TakeProfit` = 350
  - `StopLoss` = 550
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
