# Reversión por Vela Martillo (Hammer Candle Reversal)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Las velas martillo (Hammer) a menudo marcan una reversión intradía después de que la presión vendedora se disipa. Esta estrategia busca el patrón martillo y entra en largo, anticipando un rebote.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 64%. Funciona mejor en el mercado de divisas (forex).

El sistema requiere una sombra inferior de al menos el doble del cuerpo y poca sombra superior. Una vez identificado, compra con el tamaño de posición establecido y espera el beneficio o el stop-loss.

## Detalles

- **Criterios de entrada**: Vela martillo detectada.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Stop-loss o salida discrecional.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Solo largos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
