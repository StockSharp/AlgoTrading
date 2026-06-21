# Estrategia Voss Predictor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el filtro predictivo Voss de John Ehlers con un filtro de paso de banda para anticipar el movimiento del precio. Una posición larga se abre cuando el filtro predictivo sube por encima de la salida del paso de banda, mientras que una posición corta se abre cuando cae por debajo.

## Detalles

- **Entrada**: El filtro predictivo Voss cruza por encima del filtro de paso de banda.
- **Salida**: El filtro predictivo Voss cruza por debajo del filtro de paso de banda.
- **Tipo**: Seguimiento de tendencia.
- **Stops**: Ninguno.
