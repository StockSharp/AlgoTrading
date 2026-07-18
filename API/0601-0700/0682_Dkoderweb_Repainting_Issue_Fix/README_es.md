# Estrategia Dkoderweb Repainting Issue Fix
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia detecta patrones armónicos mediante un enfoque de zigzag simple y opera cuando el precio vuelve a un nivel de retroceso de Fibonacci. Cuando se forma un patrón alcista y el precio retrocede hasta la ventana de entrada, la estrategia abre una posición larga con niveles predefinidos de take‑profit y stop‑loss. Un patrón bajista activa la misma lógica en la dirección opuesta.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Patrón armónico ABCD y precio de cierre en o por debajo del nivel Fibonacci de entrada.
  - **Corto**: Patrón armónico ABCD y precio de cierre en o por encima del nivel Fibonacci de entrada.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - El precio alcanza los niveles Fibonacci de take‑profit o stop‑loss.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `TradeSize` = 1
  - `EntryRate` = 0.382
  - `TakeProfitRate` = 0.618
  - `StopLossRate` = -0.618
- **Filtros**:
  - Categoría: Reconocimiento de patrones
  - Dirección: Ambos
  - Indicadores: ZigZag
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio

