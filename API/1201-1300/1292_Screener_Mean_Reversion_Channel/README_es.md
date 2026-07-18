# Estrategia de Revisión de Pantalla con Canal de Reversión a la Media
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera en un canal de reversión a la media construido a partir de una media móvil y el ATR. Vende cuando el precio cierra por encima de la banda superior y compra cuando el precio cierra por debajo de la banda inferior. Las posiciones se cierran cuando el precio regresa a la línea media.

## Detalles
- Entrada: cierre por encima de la banda superior -> corto, cierre por debajo de la banda inferior -> largo.
- Salida: precio que regresa cruzando la media.
- Indicadores: SMA y ATR.
- Stops: ninguno.
