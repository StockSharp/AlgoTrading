# Estrategia Fractal RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia adaptativa basada en el indicador Fractal RSI.
Fractal RSI ajusta la longitud del cálculo del RSI usando la dimensión fractal del movimiento del precio,
permitiendo que el oscilador reaccione más rápido en mercados de tendencia y más lento en condiciones laterales.

La estrategia abre posiciones cuando el indicador cruza niveles predefinidos.
Puede operar con la tendencia detectada o en su contra dependiendo del modo elegido.

## Detalles

- **Criterios de entrada**:
  - *Modo Tendencia - Directo*:
    - Compra: el valor cruza por debajo de `LowLevel`
    - Venta: el valor cruza por encima de `HighLevel`
  - *Modo Tendencia - Contra*:
    - Compra: el valor cruza por encima de `HighLevel`
    - Venta: el valor cruza por debajo de `LowLevel`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: Stop-loss y take-profit fijos opcionales
- **Valores predeterminados**:
  - `CandleType` = `TimeSpan.FromHours(4).TimeFrame()`
  - `FractalPeriod` = 30
  - `NormalSpeed` = 30
  - `HighLevel` = 60
  - `LowLevel` = 40
  - `StopLoss` = 1000 puntos
  - `TakeProfit` = 2000 puntos
- **Filtros**:
  - Categoría: Tendencia / Oscilador
  - Dirección: Ambos
  - Indicadores: Fractal Dimension, RSI
  - Stops: Sí
  - Complejidad: Uso avanzado de indicadores
  - Marco temporal: 4H (configurable)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
