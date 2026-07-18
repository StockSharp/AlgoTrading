# Estrategia de Ruptura Diaria con Sombra Diaria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas diarias utilizando las dos últimas velas diarias completadas. Cierra cualquier posición abierta al inicio de cada nuevo día.

## Detalles

- **Criterios de entrada**:
  - Largo: El día anterior cierra por encima del máximo del cuerpo de la vela anterior y abre por debajo de ese nivel.
  - Corto: El día anterior cierra por debajo del mínimo del cuerpo de la vela anterior y abre por encima de ese nivel.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La posición se cierra al inicio de un nuevo día.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = 1 Day
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
