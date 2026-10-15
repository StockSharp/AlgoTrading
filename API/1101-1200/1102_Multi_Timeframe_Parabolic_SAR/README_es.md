# Parabolic SAR Multitemporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina señales de Parabolic SAR de múltiples marcos temporales. Las operaciones largas se activan cuando el precio se mantiene por encima de los niveles SAR seleccionados por los parámetros. Las operaciones cortas aparecen cuando el precio cae por debajo de los SAR elegidos. Hay disponibles stop loss, stop trailing y toma de ganancias opcionales.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Precio por encima del SAR según la configuración `LongSource`.
  - **Corto**: Precio por debajo del SAR según la configuración `ShortSource`.
- **Criterios de salida**:
  - Cruce opuesto del SAR o activación de protecciones.
- **Indicadores**:
  - Parabolic SAR en el marco temporal actual
  - Parabolic SAR opcional en marcos temporales superiores e inferiores
- **Stops**: Stop loss, stop trailing y toma de ganancias opcionales mediante StartProtection.
- **Valores predeterminados**:
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `StopLossPercent` = 1
  - `TrailingPercent` = 0.5
  - `TakeProfitPercent` = 2
- **Filtros**:
  - Marco temporal: principal 5m, superior 1d, inferior 1m
  - Indicadores: Parabolic SAR
  - Stops: opcional
  - Complejidad: Moderado
