# Estrategia CC Trend 2 Bajista Corto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia solo corta que vende cuando el cierre anterior está por debajo del máximo dinámico de Fibonacci y EMA21 está por debajo de EMA55. Sale cuando el precio cruza por encima de EMA200 con ganancia no negativa o cuando el cierre anterior sube por encima del nivel Fibonacci 0.236 y no aparece nueva señal corta.

## Detalles

- **Criterios de entrada**:
  - Corto: cierre anterior por debajo del máximo de Fibonacci y EMA21 por debajo de EMA55
- **Largo/Corto**: Corto
- **Criterios de salida**:
  - El precio cruza por encima de EMA200 con ganancia
  - Cierre anterior por encima del nivel Fibonacci 0.236 sin nueva señal corta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `FibLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Corto
  - Indicadores: EMA, Fibonacci
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
