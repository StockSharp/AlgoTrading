# Estrategia X-Trail
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia genera operaciones cuando una media móvil simple rápida y una lenta,
calculadas sobre el precio mediano, se cruzan entre sí. La lógica refleja el script
MQL original **X_trail.mq4** que usaba alertas en dichos cruces.

Se abre una posición larga cuando la MA rápida permanece por encima de la MA lenta en la
vela actual y la anterior, mientras estaba por debajo dos velas atrás. El patrón opuesto
activa una posición corta. Las posiciones se invierten en cada nueva señal.

## Detalles

- **Criterios de entrada**:
  - **Largo**: MA rápida > MA lenta en las últimas dos velas terminadas y MA rápida estaba por debajo de la MA lenta dos velas antes.
  - **Corto**: MA rápida < MA lenta en las últimas dos velas terminadas y MA rápida estaba por encima de la MA lenta dos velas antes.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cruce opuesto (inversión de posición).
- **Stops**: Ninguno.
- **Indicadores**:
  - Dos medias móviles simples calculadas a partir del precio mediano.
