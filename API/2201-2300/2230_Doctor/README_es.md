# Estrategia Doctor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia #15233 "Doctor" convertida de MQL a StockSharp.

## Descripción general
La estrategia combina varios indicadores clásicos para detectar la dirección de la tendencia y el momentum:

- **Detección de pendiente** usando una Media Móvil Ponderada de 40 períodos para evaluar la dirección de la tendencia.
- **Ubicación lineal** mediante una Media Móvil Ponderada de 400 períodos comparada con los máximos y mínimos de las últimas tres velas.
- **Confirmación de momentum** con el Índice de Fuerza Relativa de períodos 14 y 5.
- **Filtro de reversión de tendencia** del Parabolic SAR.

Se abre una posición larga cuando todas las condiciones alcistas se alinean, y una posición corta cuando todas las bajistas se alinean. Las posiciones existentes se cierran ante señales opuestas o cuando se alcanzan los niveles de protección. Un trailing stop opcional avanza el stop loss una vez que se alcanza la mitad de la distancia del stop.

## Parámetros
- `StopLossTicks` – distancia del stop loss en ticks.
- `TakeProfitTicks` – distancia del take profit en ticks.
- `TrailingStop` – activa la lógica del trailing stop.
- `CandleType` – marco temporal usado para las velas (predeterminado 30 minutos).

## Reglas de trading
1. **Comprar** cuando:
   - La pendiente de WMA(40) es ascendente.
   - WMA(400) está por encima de los máximos de las últimas tres velas.
   - RSI(14) está por encima de 50 y RSI(5) está por debajo de RSI(14).
   - No hay posición larga abierta.
2. **Vender** cuando:
   - La pendiente de WMA(40) es descendente.
   - WMA(400) está por debajo de los mínimos de las últimas tres velas.
   - RSI(14) está por debajo de 50 y RSI(5) está por encima de RSI(14).
   - No hay posición corta abierta.
3. **Salir** cuando ocurren las condiciones opuestas o se alcanzan los niveles de stop loss/take profit. El trailing stop actualiza el nivel de stop después de que el precio se mueve la mitad de la distancia del stop a favor.

## Indicadores
- Media Móvil Ponderada (40, 400)
- Índice de Fuerza Relativa (14, 5)
- Parabolic SAR
