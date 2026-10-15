# Estrategia de Barras de Volumen Superpuesto Equivolumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia renderiza cajas ponderadas por volumen para emular barras de equivolumen sobre velas de precio. Calcula una suma acumulativa de volumen y escala el ancho de cada caja en relación con la actividad reciente. Se puede configurar una media móvil del volumen.

La estrategia no coloca operaciones; está pensada como un ejemplo visual del uso de la API de alto nivel de StockSharp para dibujo personalizado en gráficos.
