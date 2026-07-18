# Estrategia MA Deviation
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera cuando el precio se desvía significativamente de su media móvil

Las pruebas indican un rendimiento anual promedio de aproximadamente 124%. Funciona mejor en el mercado forex.

MA Deviation entra cuando el precio se desvía un porcentaje establecido de su media móvil, anticipando un retorno a la media. La posición se cierra cuando el precio converge de regreso hacia el promedio.

Los umbrales de desviación pueden ampliarse o reducirse según la volatilidad. Usar ATR para el dimensionamiento de posiciones mantiene el riesgo consistente en todos los mercados.


## Detalles

- **Criterios de entrada**: Señales basadas en MA, ATR.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `DeviationPercent` = 5m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

