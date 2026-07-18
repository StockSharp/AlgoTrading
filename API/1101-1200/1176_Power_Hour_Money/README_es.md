# Estrategia Power Hour Money
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera durante sesiones seleccionadas de Nueva York y abre posiciones cuando todos los marcos temporales principales coinciden.
Se abre una posición larga cuando las velas de mes, semana, día y hora cierran por encima de su apertura.
Se abre una posición corta cuando todas cierran por debajo.
Los trailing stops opcionales protegen las ganancias y las posiciones pueden cerrarse a las 16:45.

## Detalles
- **Entrada**: largo cuando todos los marcos temporales son verdes, corto cuando todos son rojos.
- **Filtro de sesión**: NY 9:30-11:30, extendida 8:00-16:00 o todas las sesiones.
- **Trailing stop**: porcentual para el lado largo y corto.
- **Fin del día**: cierre opcional de todas las posiciones a las 16:45.
