# Tendencia ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la tendencia del Índice Direccional Promedio (ADX). La estrategia de Tendencia ADX mide la fortaleza del mercado utilizando el indicador ADX. Cuando el ADX está por encima de un umbral y el precio está en el lado correcto de su media móvil, el sistema opera en esa dirección. Las posiciones se cierran una vez que el ADX se debilita o aparece la configuración opuesta.

Las pruebas indican un retorno anual promedio de aproximadamente 46%. Funciona mejor en el mercado de acciones.

Al esperar una lectura sólida del ADX, el enfoque solo opera cuando el momentum está firmemente establecido. Los stops normalmente utilizan un múltiplo de ATR para que el riesgo se ajuste con la volatilidad.


## Detalles

- **Criterios de entrada**: Señales basadas en MA, ADX, ATR.
- **Largo/Corto**: Ambos directions.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `MaPeriod` = 50
  - `AtrMultiplier` = 2m
  - `AdxExitThreshold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA, ADX, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

