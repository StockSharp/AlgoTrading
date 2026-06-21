# Estrategia Fibo Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Fibo Stop mueve el stop de protección a lo largo de los niveles de retroceso de Fibonacci definidos por dos precios: inicio y fin. La estrategia abre una posición en la dirección del nivel de inicio al nivel de fin y mueve el stop a cada nuevo nivel de Fibonacci una vez que el precio lo cruza.

## Algoritmo
1. Determinar la dirección desde el precio de inicio al precio de fin. Si el fin es mayor que el inicio, se abre una posición larga; de lo contrario, una posición corta.
2. Calcular los niveles de Fibonacci: 0%, 23.6%, 38.6%, 50%, 61.8%, 78.6%, 100%, 127% basados en el rango.
3. El stop inicial se coloca detrás del nivel de inicio usando el desplazamiento especificado en pasos de precio.
4. A medida que el precio de mercado se mueve y cruza el siguiente nivel de Fibonacci, el stop se mueve a ese nivel menos/más el desplazamiento.
5. La posición se cierra cuando el precio alcanza el stop móvil.

## Parámetros
- `FiboStart` – precio base donde comienza el cálculo de Fibonacci.
- `FiboEnd` – precio final que define el rango de Fibonacci.
- `OffsetPoints` – número de pasos de precio añadidos detrás de cada nivel de Fibonacci para colocar el stop.
- `CandleType` – serie de velas utilizada para monitorear el precio.

## Notas
La estrategia utiliza solo velas completadas y no depende del historial de valores del indicador. Está diseñada como ejemplo de gestión de un trailing stop con la API de alto nivel de StockSharp.
