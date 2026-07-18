# Estrategia ICT con Indicador y Trading en Papel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia almacena los máximos y mínimos de los bloques de órdenes y toma posiciones largas cuando el precio de cierre cruza por encima del último máximo del bloque de órdenes. La posición larga se cierra cuando el mínimo del bloque de órdenes almacenado cruza por encima del precio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio de cierre cruza por encima del último máximo del bloque de órdenes.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Salir del largo cuando el mínimo del bloque de órdenes cruza por encima del precio.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: Price action
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
