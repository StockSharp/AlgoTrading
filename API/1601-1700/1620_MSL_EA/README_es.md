# Estrategia MSL EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

MSL EA es una estrategia de ruptura que construye líneas dinámicas de soporte y resistencia a partir de extremos locales recientes. La estrategia detecta máximos y mínimos fractales de corto plazo, los ajusta por una distancia especificada en ticks, y abre posiciones cuando el precio cierra más allá de estos niveles. Fue convertida desde la implementación original en MQL4.

## Cómo funciona

1. El algoritmo rastrea los máximos y mínimos de las velas para determinar los extremos locales.
2. El máximo más alto y el mínimo más bajo entre los últimos *Level* extremos detectados se almacenan como líneas de resistencia y soporte.
3. Cada línea se desplaza por *Distance* ticks para tener en cuenta el ruido del mercado.
4. Cuando el precio de cierre rompe por encima de la línea superior, se abre una posición larga; cuando rompe por debajo de la línea inferior, se abre una posición corta.
5. El número de operaciones simultáneas está limitado por *Max Trades*.

## Parámetros

- **Max Trades** – máximo de posiciones abiertas permitidas.
- **Level** – número de extremos locales utilizados para construir los niveles.
- **Distance** – desplazamiento desde el extremo en ticks al colocar las líneas.
- **Candle Type** – marco temporal de las velas procesadas por la estrategia.

## Notas

Esta versión en C# utiliza la API de alto nivel de StockSharp e incluye comentarios en inglés. Las funciones de gestión de riesgo de la librería auxiliar original de MQL4 se simplifican a comprobaciones básicas de posición.
