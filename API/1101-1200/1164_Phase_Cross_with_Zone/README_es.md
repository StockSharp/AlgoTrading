# Estrategia de Cruce de Fase con Zona
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de ejemplo entra en largo cuando una SMA suavizada con desplazamiento positivo cruza por encima de una EMA con desplazamiento negativo. La posición se cierra cuando ocurre el cruce opuesto.

## Detalles

- **Criterios de entrada**: SMA + desplazamiento cruza por encima de EMA - desplazamiento.
- **Largo/Corto**: solo largo.
- **Criterios de salida**: cruce opuesto.
- **Stops**: ninguno.
- **Valores predeterminados**:
  - `Length` = 20.
  - `Offset` = 0.5.
- **Filtros**: ninguno.
- **Complejidad**: baja.
- **Marco temporal**: configurable.
