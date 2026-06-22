# Estrategia de Ruptura de Tres Líneas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera reversiones detectadas por el indicador Three Line Break.
El indicador compara el máximo y mínimo actuales con el máximo más alto y el mínimo más bajo de las N velas completadas anteriores.
Una ruptura por encima del máximo reciente durante una tendencia bajista señala una nueva tendencia alcista y activa una entrada larga; una ruptura por debajo del mínimo reciente durante una tendencia alcista activa una entrada corta.
Las posiciones se invierten en cada señal.

## Detalles

- **Criterios de entrada**:
  - Largo: `Downtrend` cambia a `Uptrend`
  - Corto: `Uptrend` cambia a `Downtrend`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta (inversión de posición)
- **Stops**: No
- **Valores predeterminados**:
  - `LinesBreak` = 3
  - `CandleType` = TimeSpan.FromHours(12).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Highest, Lowest (lógica Three Line Break)
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Swing
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
