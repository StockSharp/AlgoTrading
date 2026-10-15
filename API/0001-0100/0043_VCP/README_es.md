# Estrategia Volatility Contraction Pattern
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia VCP busca una secuencia de rangos de precio que se van estrechando. A medida que cada rango se contrae, se acumula energía para un rompimiento. El sistema mide el tamaño del rango y espera una ruptura por encima del máximo más alto o por debajo del mínimo más bajo.

Las pruebas indican un retorno anual promedio de aproximadamente 166%. Funciona mejor en el mercado de acciones.

Una vez observada la contracción, un rompimiento más allá de los extremos recientes desencadena una operación en esa dirección. El cruce del precio con la media móvil se utiliza para gestionar las salidas.

Este enfoque busca capturar movimientos explosivos tras una compresión de volatilidad.

## Detalles

- **Criterios de entrada**: Contracción del rango y luego ruptura del máximo/mínimo reciente.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El precio cruza la MA o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Range, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

