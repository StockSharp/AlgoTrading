# Estrategia de Modelo de Sesgo de Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Cuenta cierres alcistas frente a bajistas en una ventana y opera en la dirección del sesgo dominante cuando la volatilidad es suficiente. Utiliza objetivos ATR y cierra la posición tras un número máximo de barras.

## Detalles
- **Criterios de entrada**: Ratio de sesgo por encima de `BiasThreshold` para largo o por debajo de `1 - BiasThreshold` para corto con rango superior a `RangeMin`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop, toma de beneficios o se alcanza `MaxBars`.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `BiasWindow` = 10
  - `BiasThreshold` = 0.6
  - `RangeMin` = 0.05
  - `RiskReward` = 2
  - `MaxBars` = 20
  - `AtrLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: ATR, SMA, Highest, Lowest
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
