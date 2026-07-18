# Sistema de Seguimiento de Tendencia Gemini
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que compra retrocesos hacia la SMA de 50 días dentro de una tendencia alcista fuerte confirmada por la SMA de 200 días y el filtro anual de Rate of Change.

## Detalles

- **Criterios de entrada**: El precio recupera por encima de la SMA 50 tras un retroceso reciente en una tendencia alcista confirmada.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cruce de muerte de la SMA 50 por debajo de la SMA 200 o stop catastrófico.
- **Stops**: Stop catastrófico opcional.
- **Valores predeterminados**:
  - `Sma50Length` = 50
  - `Sma200Length` = 200
  - `RocPeriod` = 252
  - `RocMinPercent` = 15m
  - `UseCatastrophicStop` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: SMA, RateOfChange, Lowest
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
