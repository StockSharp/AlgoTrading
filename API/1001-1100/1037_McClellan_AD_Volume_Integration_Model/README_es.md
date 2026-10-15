# Estrategia del Modelo de Integración de Volumen McClellan A-D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia construye una línea avance-descenso ponderada multiplicando el rango de precio de la barra por su volumen. Dos EMAs de esta línea ponderada forman un oscilador al estilo McClellan.

Se abre una posición larga cuando el oscilador cruza por encima de un umbral definido por el usuario después de haber estado por debajo. La operación se cierra automáticamente después de un número fijo de barras.

## Detalles

- **Entrada**: el oscilador cruza por encima de `Long Entry Threshold` desde abajo.
- **Salida**: posición cerrada después de `Exit After Bars` velas.
- **Largo/Corto**: solo largo.
- **Indicadores**: dos EMAs.
- **Stops**: Ninguno.
- **Marco temporal**: Configurable.
