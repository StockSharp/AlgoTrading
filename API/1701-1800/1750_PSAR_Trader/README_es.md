# Estrategia PSAR Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia PSAR Trader actúa sobre los cambios en el indicador Parabolic SAR. Cuando el SAR se desplaza por debajo del precio se abre una posición larga, y cuando el SAR se desplaza por encima del precio se abre una posición corta. Un ajuste opcional "Close On Opposite" invierte la posición cuando aparece una señal contraria. El trading ocurre solo durante las horas de sesión configuradas. El stop-loss y take-profit son gestionados por el módulo de protección.

## Detalles

- **Criterios de entrada**: Precio cruzando el Parabolic SAR.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce SAR contrario o inversión de posición.
- **Stops**: Sí, fijos mediante parámetros.
- **Valores predeterminados**:
  - `SarStep` = 0.001m
  - `SarMaxStep` = 0.2m
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `TakeValue` = 50 (absolute)
  - `StopValue` = 50 (absolute)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Fijo
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
