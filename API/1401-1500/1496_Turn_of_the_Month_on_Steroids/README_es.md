# Estrategia de Cambio de Mes on Steroids
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia estacional que compra cerca del final de cada mes después de dos cierres consecutivos a la baja y sale cuando un RSI corto señala condiciones de sobrecompra.

## Detalles

- **Criterios de entrada**: día del mes por encima del umbral y caída de dos días
- **Largo/Corto**: Largo
- **Criterios de salida**: RSI por encima del umbral
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `DayOfMonth` = 25
  - `RsiLength` = 2
  - `RsiThreshold` = 65
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Solo largos
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
