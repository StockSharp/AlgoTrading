# Estrategia Ride Alligator Williams
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el indicador Alligator de Bill Williams. Las líneas de labios, dientes y mandíbula se calculan desde el precio medio usando medias móviles suavizadas con longitudes derivadas de un período base a través de la razón áurea. Se abre una posición larga cuando los labios cruzan por encima de la mandíbula mientras los dientes permanecen por debajo. Se abre una posición corta cuando los labios cruzan por debajo de la mandíbula mientras los dientes permanecen por encima. Para una posición abierta, un trailing stop sigue la línea de la mandíbula.

## Parámetros
- **Base Period** – período raíz usado para derivar las longitudes del Alligator.
- **Candle Type** – marco temporal de las velas de entrada.

## Indicadores
- Media Móvil Suavizada (labios, dientes, mandíbula)

## Reglas de entrada
- Largo cuando los labios cruzan por encima de la mandíbula y los dientes están por debajo.
- Corto cuando los labios cruzan por debajo de la mandíbula y los dientes están por encima.

## Reglas de salida
- Un cruce opuesto cierra la posición.
- El trailing stop en la línea de la mandíbula sale cuando el precio la cruza.
