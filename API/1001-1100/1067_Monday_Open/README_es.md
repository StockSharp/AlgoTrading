# Estrategia de Apertura del Lunes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compra al comienzo de la semana y cierra la posición al cierre del martes dentro de un rango de años especificado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: abrir una posición larga el lunes.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - cerrar la posición larga el martes.
- **Stops**: No.
- **Valores predeterminados**:
  - `StartYear` = 2023.
  - `EndYear` = 2025.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Largo
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
