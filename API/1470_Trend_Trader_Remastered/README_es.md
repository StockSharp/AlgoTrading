# Estrategia Trend Trader Remastered
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia usa el indicador Parabolic SAR para seguir tendencias. Se envía una orden de compra cuando el precio cruza por encima del SAR y una orden de venta cuando el precio cruza por debajo. Un cruce opuesto cierra la posición actual.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio cruza por encima del PSAR.
  - **Corto**: El precio cruza por debajo del PSAR.
- **Salidas**: Un cruce opuesto del PSAR cierra la operación.
- **Stops**: Sin stops adicionales.
- **Valores predeterminados**:
  - `Start` = 0.02
  - `Increment` = 0.02
  - `Max` = 0.2
