# Estrategia del Modelo Captain Backtest
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Rastrea el rango de precios de la sesión temprana para establecer un sesgo diario. Opera rupturas durante la ventana de operaciones después de un retroceso.

## Detalles

- **Sesgo**: El máximo o mínimo del rango matutino define el sesgo largo o corto.
- **Entrada**: Ruptura por encima/debajo de la vela anterior una vez cumplidas las condiciones de retroceso.
- **Largo/Corto**: Ambos.
- **Salida**: Riesgo/recompensa fijo o fin de la ventana de operaciones.
- **Stops**: Distancia fija en puntos.
- **Valores predeterminados**:
  - PrevRangeStart = 06:00
  - PrevRangeEnd = 10:00
  - TakeStart = 10:00
  - TakeEnd = 11:15
  - TradeStart = 10:00
  - TradeEnd = 16:00
  - Risk = 25
  - Reward = 75
