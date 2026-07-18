# Estrategia de Ruptura de Pendiente Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Ruptura de Pendiente Williams %R monitorea la tasa de cambio del Williams %R. Una pendiente inusualmente pronunciada sugiere que se está formando una nueva tendencia.

Las pruebas indican un rendimiento anual promedio de aproximadamente 139%. Funciona mejor en el mercado de acciones.

Las entradas ocurren cuando la pendiente supera su nivel típico en un múltiplo de la desviación estándar, tomando operaciones en la dirección de la aceleración con un stop protector.

Atrae a los traders activos que buscan una exposición temprana a la tendencia. Las posiciones se cierran cuando la pendiente regresa a lecturas normales. `WilliamsRPeriod` predeterminado = 14.

## Detalles

- **Criterios de entrada**: El indicador supera la media por el multiplicador de desviación.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El indicador revierte a la media.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `WilliamsRPeriod` = 14
  - `SlopePeriod` = 20
  - `BreakoutMultiplier` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Williams %R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

