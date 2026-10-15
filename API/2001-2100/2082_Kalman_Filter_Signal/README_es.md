# Estrategia de Señal del Filtro Kalman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza el indicador Kalman Filter para detectar cambios de dirección. La salida del filtro se compara con el precio o su pendiente según el modo de señal seleccionado. Cuando la señal se vuelve alcista, la estrategia abre una posición larga; cuando es bajista, abre una corta. Las posiciones se revierten ante señales opuestas. El stop loss y el take profit se aplican utilizando distancias absolutas.

## Detalles

- **Criterios de entrada**:
  - Largo: la señal cambia a alcista
  - Corto: la señal cambia a bajista
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: Stop loss y take profit absolutos
- **Valores predeterminados**:
  - `ProcessNoise` = 1.0
  - `MeasurementNoise` = 1.0
  - `CandleType` = TimeSpan.FromHours(3).TimeFrame()
  - `Mode` = SignalModes.Kalman
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Kalman Filter
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
