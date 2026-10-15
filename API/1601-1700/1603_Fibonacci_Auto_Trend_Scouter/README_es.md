# Estrategia Fibonacci Auto Trend Scouter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza dos extremos móviles basados en números Fibonacci para rastrear tendencias emergentes. La ventana corta (8) sigue los máximos y mínimos recientes, mientras que la ventana larga (21) proporciona contexto. Se abre una posición larga cuando el máximo de corto plazo supera el máximo de largo plazo. Se abre una posición corta cuando el mínimo de corto plazo cae por debajo del mínimo de largo plazo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: máximo de corto plazo > máximo de largo plazo.
  - **Corto**: mínimo de corto plazo < mínimo de largo plazo.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La posición se invierte ante la señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `Short period` = 8
  - `Long period` = 21
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Medio plazo
