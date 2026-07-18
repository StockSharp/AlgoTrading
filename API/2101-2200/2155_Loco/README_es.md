# Estrategia Loco
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el indicador "Loco" escrito originalmente en MQL5. El indicador analiza los precios de las velas y asigna un color (verde o magenta). Un cambio de color señala una reversión de tendencia.

## Lógica
- El indicador calcula una serie utilizando un precio configurable (cierre por defecto) y una longitud de retroceso.
- Cuando el color cambia de magenta a verde, la estrategia cierra cualquier posición corta y abre una posición larga.
- Cuando el color cambia de verde a magenta, la estrategia cierra cualquier posición larga y abre una posición corta.

## Parámetros
- **Candle Type** – tipo de velas utilizadas en la estrategia.
- **Length** – número de barras para comparar el precio.
- **Price Type** – precio utilizado en el cálculo del indicador.

## Notas
La estrategia utiliza una implementación personalizada del indicador Loco. No se proporciona versión en Python.
