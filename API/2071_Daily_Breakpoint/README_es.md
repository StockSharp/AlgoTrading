# Estrategia de Ruptura Diaria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas desde la apertura diaria. Al comienzo de cada nuevo día se almacena el precio de apertura. Cuando el precio se aleja de este nivel por un número de puntos definido por el usuario, y la barra anterior está dentro de un rango de tamaño configurable, la estrategia entra en la dirección de la ruptura.

## Lógica de entrada

- Si la barra anterior es alcista y el precio sube por encima de la apertura diaria en **Break Point** puntos, se abre una posición larga.
- Si la barra anterior es bajista y el precio cae por debajo de la apertura diaria en **Break Point** puntos, se abre una posición corta.
- El tamaño de la barra anterior debe estar entre **Last Bar Min** y **Last Bar Max** puntos.
- El nivel de ruptura debe estar dentro del cuerpo de la barra anterior.

## Gestión del riesgo

- El **Take Profit** y **Stop Loss** opcionales se miden en puntos desde el precio de entrada.
- Se puede activar un trailing stop con los parámetros **Trailing Start**, **Trailing Stop** y **Trailing Step**. Cuando el precio se mueve a favor en *Trailing Start* puntos, el stop se establece en *Trailing Stop* puntos desde la entrada y luego sigue con incrementos de *Trailing Step*.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| Candle Type | Marco temporal de las velas procesadas. |
| Break Point | Distancia desde la apertura diaria para activar una operación (puntos). |
| Last Bar Min | Tamaño mínimo de la barra anterior (puntos). |
| Last Bar Max | Tamaño máximo de la barra anterior (puntos). |
| Trailing Start | Movimiento de precio para iniciar el trailing stop (puntos). |
| Trailing Stop | Distancia inicial del trailing stop (puntos). |
| Trailing Step | Paso para mover el trailing stop (puntos). |
| Take Profit | Distancia de take profit (puntos). |
| Stop Loss | Distancia de stop loss (puntos). |

## Notas

La estrategia opera solo en velas terminadas y utiliza órdenes de mercado para entradas y salidas. Almacena variables internas para los datos de la barra anterior y el nivel del trailing stop.
