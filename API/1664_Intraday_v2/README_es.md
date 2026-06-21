# Estrategia Intradía v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un enfoque de reversión a la media intradía utilizando dos conjuntos de Bandas de Bollinger. Las bandas externas (desviación 2.4) definen las zonas de entrada, mientras que las bandas internas (desviación 1) gestionan las salidas. Niveles opcionales de stop-loss y take-profit cierran posiciones cuando el precio se mueve en contra de la operación en un monto configurable.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio de cierre cae por debajo de la banda inferior externa.
  - **Corto**: El precio de cierre sube por encima de la banda superior externa.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Largo: El precio cruza hacia arriba la banda inferior interna o toca el stop-loss/take-profit.
  - Corto: El precio cruza hacia abajo la banda superior interna o toca el stop-loss/take-profit.
- **Stops**: Stop-loss y take-profit absolutos configurables.
- **Filtros**: Ninguno.
