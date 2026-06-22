# Estrategia DeMarker Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa el oscilador DeMarker para detectar posibles reversiones de tendencia. En cada vela completada (marco temporal de 4 horas por defecto), el valor de DeMarker se compara con umbrales superior e inferior configurables. Cuando el oscilador sube por encima del umbral inferior (0.3 por defecto), la estrategia entra en una posición larga y cierra cualquier posición corta. Cuando el oscilador cae por debajo del umbral superior (0.7 por defecto), entra en una posición corta y cierra cualquier posición larga. Las posiciones se mantienen hasta que aparece una señal opuesta.

## Detalles

- **Criterios de entrada**:
  - **Largo**: DeMarker cruza hacia arriba a través del nivel inferior.
  - **Corto**: DeMarker cruza hacia abajo a través del nivel superior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno por defecto.
- **Filtros**: Ninguno.
