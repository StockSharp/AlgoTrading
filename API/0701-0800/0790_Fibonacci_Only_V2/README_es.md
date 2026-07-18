# Estrategia Fibonacci Exclusiva V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera en torno a los retrocesos Fibonacci del 19% y 82.56% calculados sobre 93 velas. Las entradas ocurren cuando el precio toca o rompe estos niveles con confirmación de vela. El riesgo se gestiona mediante un stop loss opcional basado en ATR y un stop trailing.

## Detalles

- **Criterios de entrada**: toque o ruptura de los niveles Fibonacci 19% / 82.56% con confirmación de vela
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop loss o trailing stop
- **Stops**: Sí
- **Valores predeterminados**:
  - `CandleType` = 15 minutos
- **Filtros**:
  - Categoría: Ruptura Fibonacci
  - Dirección: Ambos
  - Indicadores: ATR, Highest, Lowest
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
