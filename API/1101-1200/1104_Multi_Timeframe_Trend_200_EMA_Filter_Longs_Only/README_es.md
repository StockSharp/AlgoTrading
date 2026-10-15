# Seguimiento de Tendencia Multitemporal con Filtro EMA 200 - Solo Largos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia abre largos cuando la EMA rápida está por encima de la EMA lenta en gráficos de 5, 15 y 30 minutos y el precio está por encima de la EMA 200 en el gráfico de 5 minutos. La posición se cierra si cualquier marco temporal se vuelve bajista o el precio cae por debajo de la EMA 200.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA rápida > EMA lenta en marcos temporales de 5, 15 y 30 minutos y cierre > EMA 200 (5m).
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - La tendencia de cualquier marco temporal se vuelve negativa o cierre < EMA 200 (5m).
- **Stops**:
  - Stop Loss: porcentaje.
  - Toma de ganancias: porcentaje.
- **Valores predeterminados**:
  - `Fast EMA Length` = 9
  - `Slow EMA Length` = 21
  - `200 EMA Length` = 200
  - `Stop Loss %` = 1
  - `Take Profit %` = 3
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: base 5m con confirmación 15m y 30m
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
