# Estrategia de Operación Contrarian con MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un sistema contrarian semanal que evalúa máximos, mínimos anteriores y una media móvil para abrir operaciones al final de cada semana. La posición se mantiene durante una semana independientemente de la dirección.

El método está diseñado para los principales pares de divisas, pero puede aplicarse a cualquier activo líquido con datos semanales.

## Detalles

- **Criterios de entrada**:
  - **Compra**: El cierre de la semana anterior está por encima del máximo más alto del período de análisis, o la media móvil está por encima de la apertura semanal.
  - **Venta**: El cierre de la semana anterior está por debajo del mínimo más bajo del período de análisis, o la media móvil está por debajo de la apertura semanal.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: La posición se cierra después de mantenerse durante una semana.
- **Stops**: Ninguno.
- **Marco temporal**: Velas semanales.
