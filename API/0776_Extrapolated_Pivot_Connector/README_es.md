# Estrategia Extrapolated Pivot Connector
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Conecta máximos y mínimos de pivotes recientes para construir líneas de soporte y resistencia. Una señal de compra ocurre cuando el precio cierra por encima de la línea de resistencia, mientras que una señal de venta se activa cuando el precio cae por debajo de la línea de soporte.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cierre cruza por encima de la línea de resistencia.
  - **Corto**: El cierre cruza por debajo de la línea de soporte.
- **Criterios de salida**:
  - Señal opuesta o reversión de posición.
- **Indicadores**:
  - Líneas de soporte/resistencia basadas en pivotes.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `PivotLength` = 100
  - `HighStart` = 1
  - `HighEnd` = 0
  - `LowStart` = 1
  - `LowEnd` = 0
- **Filtros**:
  - Seguimiento de tendencia
  - Marco temporal único
  - Indicadores: líneas de pivote
  - Stops: ninguno
  - Complejidad: Bajo
