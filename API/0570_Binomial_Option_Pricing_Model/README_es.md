# Modelo de Valoración de Opciones Binomial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este módulo calcula el precio teórico de una opción usando un árbol binomial de dos pasos. Admite estilos americano o europeo y opciones Call o Put para diferentes clases de activos. La volatilidad se estima mediante la desviación estándar de los precios de cierre.

No se generan señales de trading; la estrategia registra el precio calculado de la opción para cada vela finalizada.

## Detalles
- **Función**: Valoración de opciones (sin operaciones)
- **Parámetros**: Strike Price, Risk Free Rate, Dividend Yield, Asset Class, Option Style, Option Type, Minutes/Hours/Days to expiry, Timeframe
- **Indicadores**: Standard Deviation
- **Largo/Corto**: N/A
- **Stops**: Ninguno
