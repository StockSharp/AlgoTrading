# Estrategia de Gráfico en Vivo Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia emula un gráfico clásico de ladrillos Renko y opera ante cambios en la dirección del ladrillo. Fue convertida del script de MetaTrader **RenkoLiveChart_v600**.

## Lógica

La estrategia construye ladrillos Renko usando velas temporales terminadas. Cuando el precio se mueve al menos el tamaño de caja seleccionado desde el precio del último ladrillo, se forma un nuevo ladrillo. Se abre una posición larga en un ladrillo ascendente y una posición corta en un ladrillo descendente.

## Parámetros

- **Candle Type** – marco temporal de las velas de entrada utilizadas para la construcción de ladrillos.
- **Brick Size** – paso de precio que define la altura de un ladrillo Renko.
- **Brick Offset** – desplazamiento inicial en ladrillos aplicado al primer ladrillo.
- **Show Wicks** – mostrar mechas en el gráfico al dibujar las velas.

## Notas

- Las operaciones se ejecutan solo en velas completadas.
- La protección de posición se inicia automáticamente al arrancar la estrategia.
- Esta implementación se centra en el comportamiento central de Renko e ignora las funciones avanzadas del script original, como el manejo de archivos externos.
