# Estrategia de Oscilaciones de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Oscilaciones de Liquidez rastrea los máximos y mínimos pivote recientes para definir resistencia y soporte. Una operación larga ocurre cuando el mínimo cruza por encima del soporte mientras el cierre permanece por debajo de la resistencia. Una operación corta se activa cuando el máximo cruza por debajo de la resistencia mientras el cierre se mantiene por encima del soporte. La gestión de riesgo utiliza un stop loss por debajo/encima del nivel con un buffer y un take profit al doble de esa distancia, generando un riesgo-recompensa de 1:2.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El mínimo cruza por encima del soporte y el cierre < resistencia.
  - **Corto**: El máximo cruza por debajo de la resistencia y el cierre > soporte.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop loss en el nivel o con buffer.
  - Take profit a 2× la distancia de riesgo.
- **Stops**: Stop loss y take profit.
- **Valores predeterminados**:
  - `Lookback` = 5
  - `StopLossBuffer` = 0.5
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Pivot highs/lows
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: 1h (predeterminado)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
