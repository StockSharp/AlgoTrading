# Estrategia de Largo en Máximo/Mínimo del Día Anterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia va largo cuando el precio rompe por encima del máximo o mínimo del día anterior durante una sesión especificada y el ADX indica un momentum alcista en fortalecimiento.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el cierre cruza por encima del máximo o mínimo del día anterior con ADX en ascenso durante la sesión.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - stop dinámico y objetivos de beneficio o al final de la sesión.
- **Stops**: Trailing stop.
- **Valores predeterminados**:
  - `MaxProfit` = 150.
  - `MaxStopLoss` = 15.
  - `AdxLength` = 11.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo
  - Indicadores: ADX
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
