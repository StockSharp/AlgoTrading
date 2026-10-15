# Estrategia DCA con Cobertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo después de que tres velas consecutivas cierren por encima del EMA y entra en corto después de que tres velas consecutivas cierren por debajo. Se añaden posiciones adicionales cuando el precio se mueve en contra de la última entrada en un porcentaje determinado. La posición completa se cierra una vez que el precio se mueve el porcentaje de take profit desde el precio de entrada promedio.

## Parámetros
- Tipo de vela
- Longitud del EMA
- Intervalo DCA %
- Take profit %
- Tamaño inicial de posición

