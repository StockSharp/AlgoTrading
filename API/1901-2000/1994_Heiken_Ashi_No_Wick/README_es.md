# Estrategia Heiken Ashi Sin Mecha
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera contra las velas Heiken Ashi que aparecen sin mechas. Una vela Heiken Ashi alcista cuyo cuerpo es mayor que el anterior y carece de sombra inferior genera una entrada en corto. Una vela bajista con un cuerpo más largo y sin sombra superior abre un largo. Las posiciones se cierran cuando se forma una vela opuesta sin la mecha correspondiente.

## Detalles

- **Criterios de entrada**: HA alcista sin mecha inferior y cuerpo mayor que el anterior para cortos; HA bajista sin mecha superior y cuerpo mayor que el anterior para largos
- **Largo/Corto**: Largo y Corto
- **Criterios de salida**: vela HA de color opuesto sin mecha
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = velas de 15 minutos
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Reversión
  - Indicadores: Heikin-Ashi
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
