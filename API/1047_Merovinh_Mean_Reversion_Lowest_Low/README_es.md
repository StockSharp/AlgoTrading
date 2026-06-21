# Merovinh - Reversión a la Media por Mínimo Más Bajo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compra cuando el mínimo más bajo actual de un período de retrospección rompe sucesivos mínimos anteriores un número configurable de veces. Cierra la posición una vez que aparece un nuevo máximo más alto dentro del mismo período.

## Parámetros
- Bars — longitud de retrospección para máximos/mínimos.
- Number Of Lows — número de mínimos consecutivos rotos requeridos para entrar.
- Start Date / End Date — rango de fechas de operación.
- Candle Type — tipo de velas.
