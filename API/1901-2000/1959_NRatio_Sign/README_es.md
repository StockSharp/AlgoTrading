# Estrategia NRatio Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia emplea el indicador NRatio, un oscilador basado en NRTR que mide la distancia normalizada entre el precio y un nivel dinámico de seguimiento. Las señales de trading ocurren cuando el NRatio cruza umbrales predefinidos. Dependiendo del modo seleccionado, el sistema reacciona ya sea a rupturas más allá de los límites superior e inferior o a reversiones de regreso dentro de ellos.

El enfoque puede operar en ambos lados del mercado y usa gestión de riesgo basada en porcentajes para las salidas. El suavizado de la métrica de distancia se realiza con una media móvil exponencial, permitiendo que la estrategia responda rápidamente mientras filtra el ruido.

## Detalles

- **Criterios de entrada**:
  - **Modo In**:
    - **Largo**: `NRatio` cruza por encima de `UpLevel`.
    - **Corto**: `NRatio` cruza por debajo de `DownLevel`.
  - **Modo Out**:
    - **Largo**: `NRatio` cruza por encima de `DownLevel`.
    - **Corto**: `NRatio` cruza por debajo de `UpLevel`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop de protección.
- **Stops**: Sí, take-profit y stop-loss en porcentaje.
- **Valores predeterminados**:
  - `CandleType` = velas de 4 horas
  - `Kf` = 1
  - `Length` = 3
  - `Fast` = 2
  - `Sharp` = 2
  - `UpLevel` = 80
  - `DownLevel` = 20
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: NRTR, EMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
