# Estrategia de Tendencia Cuantitativa — Largo en Tendencia Alcista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compra cuando el precio cierra por encima del máximo pivote más reciente detectado en ventanas de retroceso configurables. Los niveles de soporte y resistencia se obtienen de los máximos y mínimos pivote. Las posiciones están protegidas por take-profit y stop-loss basados en porcentaje.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio de cierre cruza por encima del último máximo pivote.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - El precio de cierre cruza por debajo del último mínimo pivote.
  - El último máximo pivote se vuelve inferior al último mínimo pivote.
- **Stops**: Sí, take-profit y stop-loss en porcentaje.
- **Valores predeterminados**:
  - `PivotLeft` = 46
  - `PivotRight` = 32
  - `StopLossPercent` = 1.75
  - `TakeProfitPercent` = 2
