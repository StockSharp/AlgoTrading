# Estrategia BnB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port del Asesor Experto de MetaTrader 5 "Exp_BnB". Utiliza el indicador personalizado BnB (Bulls and Bears) que mide la presión alcista y bajista dentro de cada vela y las suaviza con una media móvil exponencial.

## Cómo funciona

1. Para cada vela finalizada, la estrategia calcula los valores de bulls y bears.
2. Ambas series se suavizan con EMA.
3. Cuando la línea bulls cruza por encima de la línea bears:
   - Se cierra cualquier posición corta.
   - Se abre una posición larga.
4. Cuando la línea bears cruza por encima de la línea bulls:
   - Se cierra cualquier posición larga.
   - Se abre una posición corta.
5. Los niveles de stop loss y take profit se gestionan en puntos de precio absolutos.

## Parámetros

- `Candle Type` – marco temporal de las velas utilizadas para los cálculos.
- `EMA Length` – período de suavizado para bulls y bears.
- `Stop Loss` – distancia al stop de protección en puntos de precio.
- `Take Profit` – distancia al objetivo de beneficio en puntos de precio.
- `Allow Long Entry` – habilitar la apertura de posiciones largas.
- `Allow Short Entry` – habilitar la apertura de posiciones cortas.
- `Allow Long Exit` – habilitar el cierre de posiciones largas.
- `Allow Short Exit` – habilitar el cierre de posiciones cortas.

## Notas

El indicador original soporta múltiples métodos de suavizado. En este port, el filtro universal se aproxima con una media móvil exponencial estándar.
