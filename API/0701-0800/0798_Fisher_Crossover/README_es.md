# Estrategia de Cruce Fisher
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa el indicador Fisher Transform para entrar en posiciones largas cuando el indicador cruza al alza su valor previo mientras está por debajo de 1. Las posiciones se cierran cuando el indicador cruza a la baja su valor previo mientras está por encima de 1.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Fisher crosses above previous Fisher` && `Fisher < 1`
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - `Fisher crosses below previous Fisher` && `Fisher > 1`
- **Stops**: No
- **Valores predeterminados**:
  - `Fisher Length` = 9
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: Fisher Transform
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
