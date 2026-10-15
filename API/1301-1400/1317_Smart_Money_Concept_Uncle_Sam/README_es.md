# Estrategia de Concepto Smart Money - Uncle Sam
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de ruptura monitorea los máximos y mínimos de oscilación recientes. Se abre una operación larga cuando el precio cierra por encima del último pivote alto, mientras que se abre una operación corta cuando el precio cierra por debajo del último pivote bajo. Se puede activar un filtro de media móvil opcional para operar solo con la tendencia predominante.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cierre cruza por encima del pivote alto más reciente (y por encima de la MA si está activada).
  - **Corto**: El cierre cruza por debajo del pivote bajo más reciente (y por debajo de la MA si está activada).
- **Largo/Corto**: Ambos.
- **Indicadores**: Detección de pivotes, Media Móvil (opcional).
- **Marco temporal**: Configurable.
- **Complejidad**: Moderado.
