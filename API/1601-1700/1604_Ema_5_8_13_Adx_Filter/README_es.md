# Estrategia EMA 5-8-13 con Filtro ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera cruces de EMA en 5 y 8 períodos usando una EMA de 13 períodos para las salidas. Un filtro ADX opcional confirma la fuerza de la tendencia. Las posiciones largas ocurren cuando EMA5 cruza por encima de EMA8 y ADX supera el umbral. Las posiciones cortas se inician ante la señal opuesta.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA5 cruza por encima de EMA8 y ADX > umbral.
  - **Corto**: EMA5 cruza por debajo de EMA8 y ADX > umbral.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - **Largo**: cierre < EMA13
  - **Corto**: cierre > EMA13
- **Stops**: No.
- **Valores predeterminados**:
  - `ADX threshold` = 20
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Corto plazo
