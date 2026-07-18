# Estrategia de Tendencia RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Tendencia RSI** utiliza el Índice de Fuerza Relativa (RSI) para detectar reversiones de tendencia y gestiona las posiciones con un trailing stop basado en ATR. El sistema abre una posición larga cuando el RSI cruza por encima de un umbral de sobrecompra y entra en una posición corta cuando el RSI cae por debajo de un umbral de sobreventa. El riesgo se controla usando un trailing stop derivado del Rango Verdadero Promedio (ATR), lo que permite que el nivel de stop se adapte a la volatilidad actual.

Esta implementación está diseñada con fines educativos y demuestra cómo construir una estrategia StockSharp de alto nivel usando vinculaciones de indicadores. La estrategia opera solo en velas completadas y no hace referencia a valores anteriores de indicadores directamente, alineándose con las mejores prácticas de StockSharp.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `RSI(t) > BuyLevel` y `RSI(t-1) <= BuyLevel`.
  - **Corto**: `RSI(t) < SellLevel` y `RSI(t-1) >= SellLevel`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Trailing stop basado en múltiplo de ATR.
- **Stops**: Sí, trailing stop dinámico.
- **Valores predeterminados**:
  - `RSI Period` = 14.
  - `BuyLevel` = 73.
  - `SellLevel` = 27.
  - `ATR Period` = 100.
  - `ATR Multiple` = 3.
- **Filtros**:
  - Categoría: Seguimiento de tendencia.
  - Dirección: Ambos.
  - Indicadores: RSI, ATR.
  - Stops: Sí.
  - Complejidad: Intermedio.
  - Marco temporal: Cualquiera (velas de 1 minuto por defecto).
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Moderado.

