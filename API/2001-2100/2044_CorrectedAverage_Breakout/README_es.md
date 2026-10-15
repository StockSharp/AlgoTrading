# Estrategia de Rompimiento de CorrectedAverage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rompimientos relativos a una media móvil **CorrectedAverage**. El indicador suaviza el precio usando una media móvil y ajusta el factor de suavizado basándose en la desviación estándar de los cambios de precio.

Cuando el precio cierra por encima de la media corregida por un número especificado de puntos y luego retrocede al nivel de rompimiento, la estrategia abre una posición larga. La lógica inversa se aplica para operaciones cortas. El stop-loss y el take-profit se aplican en puntos de precio absolutos.

## Parámetros

- `Candle Type` – marco temporal de las velas usadas para los cálculos.
- `Length` – período para la media móvil y la desviación estándar.
- `MA Type` – tipo de media móvil (SMA, EMA, SMMA, LWMA).
- `Level Points` – distancia de rompimiento desde la media corregida en pasos de precio.
- `Stop Loss Points` – distancia del stop-loss desde el precio de entrada en pasos de precio.
- `Take Profit Points` – distancia del take-profit desde el precio de entrada en pasos de precio.
- `Enable Long` – permitir abrir posiciones largas.
- `Enable Short` – permitir abrir posiciones cortas.

## Lógica de operación

1. Calcular la media móvil y la desviación estándar.
2. Construir la media corregida usando valores previos y la relación de varianza para suavizar saltos repentinos.
3. Detectar rompimientos cuando la barra anterior cierra más allá de la media corregida más o menos el nivel configurado.
4. Después de un rompimiento, esperar a que la siguiente barra regrese al nivel de rompimiento y abrir una posición en la dirección del rompimiento.
5. Cerrar posiciones opuestas cuando aparece una nueva señal de rompimiento.
6. Aplicar protecciones de stop-loss y take-profit.

## Notas

Esta estrategia es una conversión del script MQL *Exp_CorrectedAverage.mq5*. Está destinada a fines educativos y requiere pruebas adicionales antes de usarse en operaciones reales.
