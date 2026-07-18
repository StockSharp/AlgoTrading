# Estrategia de Centro de Gravedad OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el oscilador **Center of Gravity OSMA** para detectar posibles reversiones de tendencia.
El oscilador multiplica medias móviles simples y ponderadas, suaviza el resultado dos veces y rastrea
los cambios de dirección. Cuando el indicador forma un mínimo local y gira hacia arriba, la estrategia
cierra posiciones cortas y puede abrir una nueva posición larga. Cuando un máximo local gira hacia abajo,
las posiciones largas se cierran y opcionalmente se abren posiciones cortas.

## Cómo Funciona
1. El precio de cierre se utiliza como entrada para el indicador personalizado.
2. El indicador calcula:
   - Media móvil simple (`SMA`) con longitud `Period`.
   - Media móvil ponderada (`WMA`) con la misma longitud.
   - Producto de estos dos promedios.
   - Dos pasos de suavizado adicionales con longitudes `SmoothPeriod1` y `SmoothPeriod2`.
3. Reglas de trading:
   - Si el valor anterior era menor que el valor anterior a él y el valor actual es mayor que el anterior, el oscilador giró hacia arriba. Cualquier posición corta se cierra y puede abrirse una larga.
   - Si el valor anterior era mayor que el valor anterior a él y el valor actual es menor que el anterior, el oscilador giró hacia abajo. Cualquier posición larga se cierra y puede abrirse una corta.
   - Los valores opcionales de stop loss y take profit en unidades de precio protegen las posiciones abiertas.

## Parámetros
- `Period` – período base para SMA y WMA.
- `SmoothPeriod1` – longitud de la primera etapa de suavizado.
- `SmoothPeriod2` – longitud de la segunda etapa de suavizado.
- `StopLoss` – distancia de stop loss en unidades de precio (0 para desactivar).
- `TakeProfit` – distancia de take profit en unidades de precio (0 para desactivar).
- `BuyPosOpen` – permitir abrir posiciones largas.
- `SellPosOpen` – permitir abrir posiciones cortas.
- `BuyPosClose` – permitir cerrar posiciones largas ante una señal de venta.
- `SellPosClose` – permitir cerrar posiciones cortas ante una señal de compra.
- `CandleType` – tipo de vela (marco temporal) para los cálculos.

## Notas
- Solo se proporciona la versión en C#. La carpeta de Python está intencionalmente ausente.
- Use tabulaciones para la indentación al modificar el código.
