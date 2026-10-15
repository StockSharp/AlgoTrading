# Estrategia de Compra/Venta Basada en Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera con ladrillos Renko creados con un tamaño basado en ATR. Se abre una posición larga cuando el cierre de Renko cruza por encima de su apertura. Se abre una posición corta cuando el cierre cruza por debajo de la apertura.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cierre cruza por encima de la apertura.
  - **Corto**: El cierre cruza por debajo de la apertura.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - Longitud ATR 10.
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Renko
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Sin base temporal
