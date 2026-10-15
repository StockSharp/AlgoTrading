# Estrategia Grim Slash
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grim Slash es una estrategia de acción del precio simple que compra cuando el mínimo de la vela actual toca el cierre anterior y sale cuando el máximo alcanza el máximo anterior. El riesgo se gestiona con take profit y stop loss de porcentaje fijo.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: El mínimo actual toca o cae por debajo del cierre anterior.
- **Criterios de salida**: El máximo actual toca o supera el máximo anterior.
- **Stops**: Take profit del 15%, stop loss del 5%.
- **Valores predeterminados**:
  - `TakeProfitPercent` = 15
  - `StopLossPercent` = 5
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Solo largos
  - Indicadores: Ninguno
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
