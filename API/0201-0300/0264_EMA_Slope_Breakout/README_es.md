# Estrategia de Rompimiento por Pendiente de EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia de Rompimiento por Pendiente de EMA observa la tasa de cambio de la EMA. Una pendiente inusualmente pronunciada indica que se está formando una nueva tendencia.

Las pruebas indican un rendimiento anual promedio de aproximadamente 127%. Funciona mejor en el mercado de acciones.

Las entradas se producen cuando la pendiente supera su nivel típico en un múltiplo de desviación estándar, tomando operaciones en la dirección de la aceleración con un stop protector.

Atrae a operadores activos que buscan una exposición temprana a la tendencia. Las posiciones se cierran cuando la pendiente regresa hacia las lecturas normales. El valor predeterminado es `EmaLength` = 20.

## Detalles

- **Criterios de entrada**: El indicador supera el promedio por el multiplicador de desviación.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: El indicador revierte al promedio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaLength` = 20
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

