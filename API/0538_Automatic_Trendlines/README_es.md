# Estrategia de Líneas de Tendencia Automáticas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Construye líneas de tendencia dinámicas de soporte y resistencia conectando los máximos y mínimos pivot recientes. Se genera una señal larga cuando el precio cierra por encima de la línea de resistencia, mientras que una señal corta se activa cuando el precio cae por debajo de la línea de soporte.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cierre cruza por encima de la línea de tendencia de resistencia.
  - **Corto**: El cierre cruza por debajo de la línea de tendencia de soporte.
- **Criterios de salida**:
  - Señal opuesta o reversión de posición.
- **Indicadores**:
  - Líneas de tendencia basadas en pivots.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `LeftBars` = 100
  - `RightBars` = 15
- **Filtros**:
  - Seguimiento de tendencia
  - Marco temporal único
  - Indicadores: líneas de tendencia pivot
  - Stops: ninguno
  - Complejidad: Bajo
