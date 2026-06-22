# Estrategia de Pendiente JMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia monitorea la pendiente de la Media Móvil Jurik (JMA). Se abre una posición cuando la pendiente cruza el cero o cuando cambia su dirección dependiendo del modo seleccionado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La pendiente cruza por debajo de cero o gira hacia arriba (dependiente del modo).
  - **Corto**: La pendiente cruza por encima de cero o gira hacia abajo.
- **Largo/Corto**: Largo y corto.
- **Criterios de salida**:
  - La señal opuesta invierte la posición.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `JMA Length` = 14
  - `JMA Phase` = 0
  - `Mode` = Breakdown
  - `Candle Type` = Marco temporal 4h
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: JMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: 4h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
