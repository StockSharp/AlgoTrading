# Logistic RSI STOCH ROC AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia aplica un mapa logístico a un indicador seleccionado (AO, ROC, RSI, Stochastic) y opera cuando la desviación estándar con signo cruza el cero.

## Detalles

- **Criterios de entrada**: La desviación estándar con signo cruza por encima de cero.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: La desviación estándar con signo cruza por debajo de cero.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Indicator` = LogisticDominance
  - `Length` = 13
  - `LenLd` = 5
  - `LenRoc` = 9
  - `LenRsi` = 14
  - `LenSto` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: AwesomeOscillator, RateOfChange, RelativeStrengthIndex, StochasticOscillator, Highest
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
