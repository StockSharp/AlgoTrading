# H4L4 Estrategia de Rompimiento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de rompimiento diario que calcula los niveles H4 y L4 a partir del máximo, mínimo y cierre del día anterior.
Al inicio de cada día se coloca un límite de venta en H4 y un límite de compra en L4.
Todas las posiciones abiertas y órdenes pendientes se cancelan antes de enviar las nuevas órdenes.
Se aplican stop loss y take profit de protección usando distancias basadas en ticks.

## Detalles

- **Criterios de entrada**: Límite de venta en H4 y límite de compra en L4 derivados de la vela del día anterior.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss o take profit.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `TakeProfit` = 57
  - `StopLoss` = 7
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
