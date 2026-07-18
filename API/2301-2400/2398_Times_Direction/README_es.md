# Estrategia de Dirección por Tiempos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia basada en tiempo abre una única posición larga o corta durante una ventana predefinida y la cierra durante otra ventana. La dirección de entrada es configurable y el sistema monitorea niveles opcionales de stop-loss y take-profit. El enfoque se basa únicamente en velas terminadas sin utilizar indicadores.

## Detalles

- **Criterios de entrada**:
  - Cuando el tiempo de la vela actual está dentro de `[OpenTime, OpenTime + TradeInterval)` y no hay posición abierta, entrar en la dirección configurada.
- **Criterios de salida**:
  - Cerrar la posición cuando el tiempo está dentro de `[CloseTime, CloseTime + TradeInterval)`.
  - Salir adicionalmente si se alcanzan los niveles de stop-loss o take-profit.
- **Largo/Corto**: Configurable.
- **Stops**: Stop-loss y take-profit en unidades de precio relativas al precio de entrada.
- **Valores predeterminados**:
  - `Trade` = Sell.
  - `OpenTime` = 1970-01-01 00:00.
  - `CloseTime` = 3000-01-01 00:00.
  - `TradeInterval` = 1 minuto.
  - `StopLoss` = 1000.
  - `TakeProfit` = 2000.
  - `Volume` = 0.1.
- **Filtros**:
  - Categoría: Basada en tiempo
  - Dirección: Único
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Corto plazo
