# Estrategia LSMA: Cálculo Alternativo Rápido y Simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza una aproximación rápida de la Media Móvil de Mínimos Cuadrados (LSMA) calculada como `3 × WMA − 2 × SMA`. Se abre una posición larga cuando el precio cruza por encima de la LSMA, y una posición corta cuando cruza por debajo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cierre cruza por encima de la LSMA.
  - **Corto**: El cierre cruza por debajo de la LSMA.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - Longitud 25.
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: WMA, SMA
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: No especificado
