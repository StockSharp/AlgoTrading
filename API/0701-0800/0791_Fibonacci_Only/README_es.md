# Estrategia Exclusiva Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Exclusiva Fibonacci utiliza niveles de retroceso personalizados del 19% y 82.56% derivados de las últimas 100 velas. La estrategia entra cuando el precio toca o rompe estos niveles con confirmación de la dirección de la vela. Admite entradas opcionales por ruptura, stop loss basado en ATR, trailing stop y siete tomas de ganancia escalonadas.

## Detalles

- **Criterios de entrada**: toque o ruptura de niveles Fibonacci con confirmación
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop loss, trailing stop o objetivos de take profit
- **Stops**: ATR o porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minutos
- **Filtros**:
  - Categoría: Fibonacci
  - Dirección: Ambos
  - Indicadores: Highest, Lowest, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
