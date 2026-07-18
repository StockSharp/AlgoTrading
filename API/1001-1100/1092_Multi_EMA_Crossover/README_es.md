# Estrategia de Cruce Multi-EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia abre posiciones largas separadas para cuatro pares de EMA cuando la EMA rápida cruza por encima de la más lenta. Cada posición se cierra cuando su EMA rápida cae por debajo de la EMA lenta.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La EMA rápida cruza por encima de la EMA lenta en cualquiera de los pares (1/5, 3/10, 5/20, 10/40).
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - La EMA rápida cae por debajo de la EMA lenta para el par respectivo.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `EMA1` = 1
  - `EMA3` = 3
  - `EMA5` = 5
  - `EMA10` = 10
  - `EMA20` = 20
  - `EMA40` = 40
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
