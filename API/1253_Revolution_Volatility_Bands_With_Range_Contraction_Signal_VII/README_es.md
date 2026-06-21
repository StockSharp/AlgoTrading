# Estrategia Revolution de Bandas de Volatilidad con Señal de Contracción de Rango VII
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia construye un envolvente alrededor del precio usando medias móviles exponenciales y detecta cuándo se contrae la distancia entre las bandas. Cuando se observa la contracción y el precio rompe por encima o por debajo de las bandas suavizadas, la estrategia abre una posición en la dirección del rompimiento.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El rango se contrae y el precio de cierre cruza por encima de la banda suavizada superior.
  - **Corto**: El rango se contrae y el precio de cierre cruza por debajo de la banda suavizada inferior.
- **Criterios de salida**: rompimiento opuesto.
- **Indicadores**: envolvente basado en EMA.
- **Marco temporal**: cualquiera.
