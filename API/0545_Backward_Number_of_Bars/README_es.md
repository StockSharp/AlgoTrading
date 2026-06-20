# Estrategia de Número Retroactivo de Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia mantiene una posición larga solo durante las barras más recientes contadas hacia atrás desde el tiempo actual. Demuestra cómo restringir el trading a una ventana histórica móvil.

## Detalles

- **Criterios de entrada**: El tiempo de la vela está dentro de las últimas *N* barras desde el tiempo de inicio.
- **Criterios de salida**: El tiempo de la vela cae fuera de esta ventana.
- **Largo/Corto**: Solo largos.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Bar count` = 50
  - `Candle type` = velas de 1 minuto
- **Filtros**:
  - Categoría: Basado en tiempo
  - Dirección: Largo
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
