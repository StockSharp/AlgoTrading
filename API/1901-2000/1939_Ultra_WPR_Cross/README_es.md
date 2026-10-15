# Estrategia de Cruce Ultra WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica un oscilador Williams %R suavizado por dos medias móviles. El cruce de las líneas rápida y lenta suavizadas genera señales de trading. Una posición larga se abre cuando la línea rápida sube por encima de la línea lenta, y una posición corta se abre cuando la línea rápida cae por debajo de la línea lenta.

El enfoque busca seguir el momentum emergente mientras limita el riesgo con niveles de take-profit y stop-loss configurables.

## Detalles
- **Criterios de entrada**:
  - **Largo**: La línea rápida cruza por encima de la línea lenta
  - **Corto**: La línea rápida cruza por debajo de la línea lenta
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - **Largo**: Salida cuando la línea rápida cruza por debajo de la línea lenta
  - **Corto**: Salida cuando la línea rápida cruza por encima de la línea lenta
- **Stops**: Sí, take-profit y stop-loss basados en precio
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromHours(4)
  - `WprPeriod` = 13
  - `FastLength` = 3
  - `SlowLength` = 53
  - `TakeProfit` = 0.2m
  - `StopLoss` = 0.1m
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Williams %R, Moving Average
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: H4
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
