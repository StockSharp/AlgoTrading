# Estrategia de Ruptura TMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aprovecha las rupturas relativas a una Media Móvil Triangular (TMA). Observa una serie de velas configurable y compara el cierre de la vela anterior con el valor TMA más o menos los offsets definidos por el usuario. Se abre una posición larga cuando el cierre anterior está por encima de `TMA + UpLevel`, y se abre una posición corta cuando está por debajo de `TMA - DownLevel`. Las señales opuestas revierten la posición.

## Parámetros

- **TMA Length** – período utilizado para calcular la Media Móvil Triangular.
- **Upper Level** – offset de precio añadido al TMA para detectar señales largas.
- **Lower Level** – offset de precio sustraído del TMA para detectar señales cortas.
- **Candle Type** – marco temporal de las velas utilizadas por la estrategia.

## Cómo Funciona

1. Se suscribe a la serie de velas seleccionada.
2. Vincula un indicador de Media Móvil Triangular a las velas.
3. En cada vela finalizada:
   - Almacena los valores anteriores de TMA y precio de cierre.
   - Verifica si el cierre anterior superó el nivel superior o inferior.
   - Envía órdenes de mercado para abrir o revertir posiciones según corresponda.
4. Grafica velas, línea del indicador y operaciones propias para análisis visual.

## Notas

La estrategia utiliza órdenes de mercado sin gestión de stop-loss ni take-profit. Está pensada con fines educativos y debe ampliarse con controles de riesgo adecuados antes de operar en real.
