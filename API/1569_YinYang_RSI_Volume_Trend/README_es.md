# Estrategia de Tendencia de Volumen RSI YinYang
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Tendencia de Volumen RSI YinYang utiliza zonas de precio ponderadas por volumen y un filtro RSI para detectar reversiones de tendencia. La estrategia compra cuando el precio sale de la zona inferior y vende cuando sale de la zona superior. Los niveles opcionales de stop-loss y take-profit se basan en zonas dinámicas.

## Detalles

- **Criterios de entrada**: El precio cruza fuera de las zonas de compra calculadas con opciones de reinicio de disponibilidad.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El precio alcanza la zona opuesta o activa el stop-loss/take-profit opcional.
- **Stops**: Opcional.
- **Valores predeterminados**:
  - `TrendLength` = 80
  - `UseTakeProfit` = true
  - `UseStopLoss` = true
  - `StopLossMultiplier` = 0.1
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: VWMA, EMA, RSI
  - Stops: Opcional
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
