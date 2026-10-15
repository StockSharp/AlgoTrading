# Estrategia Aroon Horn Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Aroon Horn Sign** busca reversiones de tendencia usando el indicador Aroon.
Monitorea las líneas Aroon Up y Aroon Down en velas de marcos temporales superiores. Cuando la
línea Aroon Up cruza por encima de la línea Aroon Down y se mantiene por encima del nivel 50,
esto señala una posible reversión alcista. La estrategia cierra cualquier posición corta
y abre una nueva posición larga. Por el contrario, cuando Aroon Down domina por encima de 50,
cualquier posición larga existente se cierra y se inicia una posición corta.

El enfoque utiliza niveles fijos de take-profit y stop-loss expresados en unidades de precio.
Estos niveles se activan a través del módulo de protección de riesgo incorporado.
Dado que la lógica se basa solo en los valores de Aroon, funciona en diferentes
mercados y marcos temporales sin filtros adicionales.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: `Aroon Up` > `Aroon Down` y `Aroon Up` >= 50.
  - **Corto**: `Aroon Down` > `Aroon Up` y `Aroon Down` >= 50.
- **Criterios de salida**:
  - Las posiciones largas se cierran cuando aparece una condición de entrada corta.
  - Las posiciones cortas se cierran cuando aparece una condición de entrada larga.
- **Stops**: Stop-loss y take-profit fijos usando `StartProtection`.
- **Valores predeterminados**:
  - `AroonPeriod` = 9
  - `CandleType` = velas de 4 horas
  - `TakeProfit` = 2000 (unidades de precio)
  - `StopLoss` = 1000 (unidades de precio)
- **Filtros**:
  - Categoría: Reversión de tendencia
  - Dirección: Largo y Corto
  - Indicadores: Aroon
  - Complejidad: Simple
  - Nivel de riesgo: Medio
