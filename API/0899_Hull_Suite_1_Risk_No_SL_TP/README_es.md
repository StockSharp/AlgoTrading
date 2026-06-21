# Estrategia Hull Suite – Riesgo 1%, Sin SL/TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Hull Suite abre posiciones largas cuando la media móvil Hull seleccionada sube en comparación con dos barras atrás, y abre posiciones cortas cuando cae. No se utiliza stop loss ni take profit.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El valor de Hull es mayor que el valor de hace dos barras.
  - **Corto**: El valor de Hull es menor que el valor de hace dos barras.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Revertir posición ante señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `HullLength` = 55
  - `Mode` = Hma
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: HMA, EHMA, THMA
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: 5m
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
