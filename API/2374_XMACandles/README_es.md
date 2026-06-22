# Estrategia XMA Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción
La estrategia XMA Candles monitorea la dirección de velas suavizadas calculadas a partir de la XMA (Media Móvil Exponencial) de los precios de apertura y cierre. Una vela se considera **alcista** cuando el precio de apertura suavizado está por debajo del precio de cierre suavizado, y **bajista** cuando el precio de apertura suavizado está por encima del precio de cierre suavizado. La estrategia reacciona a los cambios de color de estas velas suavizadas.

- Cuando aparece una nueva vela alcista después de una no alcista, la estrategia cierra cualquier posición corta y abre una posición larga.
- Cuando aparece una nueva vela bajista después de una no bajista, la estrategia cierra cualquier posición larga y abre una posición corta.

## Parámetros
- `Length` – número de períodos para suavizar los precios de apertura y cierre.
- `CandleType` – marco temporal de las velas utilizadas para los cálculos.
- `BuyPosOpen` – permite abrir posiciones largas.
- `SellPosOpen` – permite abrir posiciones cortas.
- `BuyPosClose` – permite cerrar posiciones largas cuando aparece una señal bajista.
- `SellPosClose` – permite cerrar posiciones cortas cuando aparece una señal alcista.
- `StopLoss` – stop de protección en porcentaje.
- `TakeProfit` – objetivo de beneficio en porcentaje.

## Reglas de trading
1. Esperar a que finalice cada vela del marco temporal seleccionado.
2. Calcular las medias móviles exponenciales para los precios de apertura y cierre.
3. Determinar el color de la vela:
   - Verde (alcista) si la apertura suavizada < cierre suavizado.
   - Roja (bajista) si la apertura suavizada > cierre suavizado.
4. Si el color cambia a alcista, cerrar cortos y opcionalmente abrir una posición larga.
5. Si el color cambia a bajista, cerrar largos y opcionalmente abrir una posición corta.
6. Los stops de protección y objetivos son gestionados por los controles de riesgo integrados.

Esta estrategia es una conversión del experto MQL5 original "Exp_XMACandles".
