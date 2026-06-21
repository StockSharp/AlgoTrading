# Estrategia Autostop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de utilidad que establece automáticamente take profit y stop loss para posiciones abiertas.
No genera señales de trading. Las posiciones abiertas externamente quedan protegidas mediante distancias fijas.

## Detalles

- **Criterios de entrada**: Ninguno, las órdenes se gestionan fuera de la estrategia.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Solo órdenes de protección.
- **Stops**: Utiliza StartProtection para colocar take profit y stop loss fijos.
- **Valores predeterminados**:
  - `MonitorTakeProfit` = true
  - `MonitorStopLoss` = true
  - `TakeProfitTicks` = 30
  - `StopLossTicks` = 30
- **Filtros**:
  - Categoría: Gestión de riesgos
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Fijo
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
