# Estrategia de Fuerza Promedio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Fuerza Promedio utiliza un oscilador que mide dónde se ubica el cierre dentro del máximo más alto y el mínimo más bajo de un período de referencia, y suaviza el resultado con una media móvil. Los valores positivos señalan presión alcista mientras que los valores negativos muestran fuerza bajista.

La estrategia abre una posición larga cuando el valor suavizado de la Fuerza Promedio está por encima de cero y abre una posición corta cuando está por debajo de cero.

## Detalles

- **Criterios de entrada**:
  - Average Force > 0 → Comprar.
  - Average Force < 0 → Vender.
- **Largo/Corto**: Ambas posiciones largas y cortas.
- **Criterios de salida**:
  - La posición se invierte cuando la Fuerza Promedio cruza cero en dirección opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Period` = 18
  - `Smooth` = 6
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Highest, Lowest, SMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
