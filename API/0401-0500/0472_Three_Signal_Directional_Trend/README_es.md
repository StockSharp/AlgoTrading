# Estrategia Three Signal Directional Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Three Signal Directional Trend combina MACD, el oscilador estocástico y la tasa de cambio de la media móvil para determinar la dirección de la tendencia. Cada indicador vota a favor de condiciones largas o cortas y las posiciones se abren cuando al menos dos indicadores coinciden. El método busca capturar movimientos direccionales amplios filtrando el ruido mediante múltiples señales de confirmación.

## Detalles

- **Criterios de entrada:**
  - Al menos dos de tres señales coinciden.
  - **Largo**: señal MACD en alza, estocástico por debajo de la zona de sobreventa, MA ROC positivo.
  - **Corto**: señal MACD en baja, estocástico por encima de la zona de sobrecompra, MA ROC negativo.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `AvgLength` = 50
  - `RocLength` = 1
  - `AvgRocLength` = 10
  - `StochLength` = 14
  - `SmoothK` = 3
  - `Overbought` = 80
  - `Oversold` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdAvgLength` = 9
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MACD, Stochastic, SMA, ROC
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
