# Estrategia MMA de Rompimiento por Volumen I
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rompimientos cuando el precio de cierre cruza una Media Móvil Suavizada (SMMA) de largo plazo.
Se abre una posición larga cuando el precio sube por encima de la SMMA y una posición corta cuando cae por debajo.
Las posiciones se cierran cuando el precio se mueve en contra de la operación y cruza una Media Móvil Exponencial (EMA).

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio de cierre cruza por encima de SMMA(200).
  - **Corto**: El precio de cierre cruza por debajo de SMMA(200).
- **Criterios de salida**:
  - **Largo**: El precio de cierre cae por debajo de EMA(5).
  - **Corto**: El precio de cierre sube por encima de EMA(5).
- **Largo/Corto**: Ambos.
- **Stops**: Sin stop-loss fijo, la salida está determinada por la señal EMA.
- **Valores predeterminados**:
  - `SMMA period` = 200
  - `EMA period` = 5
  - `Candle type` = velas de 5 minutos
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Medias Móviles
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
