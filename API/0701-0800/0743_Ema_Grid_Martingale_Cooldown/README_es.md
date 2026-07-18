# Estrategia EMA Grid Martingale con Cooldown
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa un sistema de grid solo largo basado en EMA con dimensionamiento martingala opcional y cooldown entre grids. Un nuevo grid comienza cuando ambas EMA rápidas cruzan por encima de sus contrapartes lentas. Las compras adicionales se realizan a intervalos fijos de pips, y la posición se cierra al precio promedio ponderado más un margen.
