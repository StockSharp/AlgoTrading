# Estrategia de Indicador Faith
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia evalúa la "fe" del mercado midiendo la expansión del volumen cuando el precio alcanza máximos o mínimos más altos o más bajos. Una calificación positiva sugiere que los compradores dominan, mientras que una calificación negativa indica que prevalecen los vendedores. La estrategia opera en transiciones entre calificaciones positivas y negativas.

## Detalles

- **Criterios de entrada:** la calificación Faith cruza por encima de cero → comprar; cruza por debajo de cero → vender.
- **Largo/Corto:** ambos.
- **Criterios de salida:** señal opuesta.
- **Indicadores:** Highest, SMA.
