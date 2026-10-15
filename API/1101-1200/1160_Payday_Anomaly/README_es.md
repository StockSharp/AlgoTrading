# Estrategia de Anomalía del Día de Pago
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre una posición larga en los días de pago seleccionados (1, 2, 16 y 31 de cada mes) y cierra la posición al día siguiente.

## Detalles

- **Criterios de entrada**:
  - **Largo**: abrir una posición larga en los días seleccionados del mes.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - cerrar la posición larga cuando el día no esté seleccionado.
- **Stops**: No.
- **Valores predeterminados**:
  - `Trade1st` = true.
  - `Trade2nd` = true.
  - `Trade16th` = true.
  - `Trade31st` = true.
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
