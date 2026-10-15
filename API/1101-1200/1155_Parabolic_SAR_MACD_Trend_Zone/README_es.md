# Parabolic SAR con Confirmación de MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el indicador Parabolic SAR con la confirmación del MACD. Se abre una posición cuando el precio cruza el SAR en una dirección respaldada por el MACD, con el objetivo de capturar reversiones de tendencia.

## Detalles

- **Criterios de entrada**: El precio cruza el SAR y la línea MACD está en el mismo lado que su línea de señal.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto de precio/SAR o MACD.
- **Stops**: No.
- **Valores predeterminados**:
  - `SarStart` = 0.02m
  - `SarIncrement` = 0.02m
  - `SarMax` = 0.2m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR, MACD
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
