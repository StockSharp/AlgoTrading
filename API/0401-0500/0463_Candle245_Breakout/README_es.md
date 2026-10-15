# Estrategia de Ruptura del Vela de las 2:45 AM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia intradía monitorea la vela de las 2:45 AM y opera rupturas de su máximo o mínimo dentro de las siguientes pocas barras. Cuando el precio supera el máximo de la vela, entra en una posición larga; cuando el precio cae por debajo del mínimo de la vela, abre una posición corta. Las posiciones se cierran al final de la ventana de observación si no ocurre una ruptura opuesta.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio rompe por encima del máximo de la vela de las 2:45 AM dentro de las siguientes `LookForwardBars` velas.
  - **Corto**: El precio rompe por debajo del mínimo de la vela de las 2:45 AM dentro de las siguientes `LookForwardBars` velas.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Fin de la ventana de observación o ruptura opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `TargetHour` = 2
  - `TargetMinute` = 45
  - `LookForwardBars` = 2
  - `CandleType` = velas de 45 minutos
- **Filtros**:
  - Categoría: Ruptura basada en tiempo
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
