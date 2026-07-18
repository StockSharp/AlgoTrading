# Estrategia Heiken-Ashi Suavizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Las velas Heiken-Ashi suavizadas con EMA destacan la aceleración en los movimientos de precio. Se abre una posición larga cuando una vela alcista suavizada tiene un cuerpo más grande que la anterior. La posición se cierra cuando el cuerpo bajista se expande.

## Detalles

- **Criterios de entrada**: vela Heiken-Ashi suavizada alcista con cuerpo más grande que la anterior
- **Largo/Corto**: Largo
- **Criterios de salida**: el cuerpo bajista se expande
- **Stops**: No
- **Valores predeterminados**:
  - `EmaLength` = 40
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Largo
  - Indicadores: EMA, Heikin-Ashi
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
