# Estrategia EURUSD V2.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de reversión a la media para EURUSD que utiliza una media móvil simple (SMA) de largo plazo y un filtro de volatilidad basado en el Rango Verdadero Promedio (ATR).

## Lógica de la estrategia

- Calcular una SMA de longitud *MA Length* en el tipo de vela elegido.
- Entrar **corto** cuando el precio está por encima de la SMA y retrocede dentro de *Buffer* pips mientras el ATR está por debajo de *ATR Threshold*.
- Entrar **largo** cuando el precio está por debajo de la SMA y se acerca dentro de *Buffer* pips con ATR bajo.
- El tamaño de la posición se deriva del balance de la cuenta y el *Risk Factor Z*.
- El stop-loss y el take-profit se colocan a distancias fijas en pips desde el precio de entrada.
- Tras la salida, el sistema espera que el precio se aleje *Noise Filter* pips del nivel de entrada antes de permitir una nueva operación.

## Parámetros

- **MA Length** – período de la media móvil simple (predeterminado 218).
- **Buffer (pips)** – distancia máxima desde la SMA para activar la entrada (predeterminado 0).
- **Stop Loss (pips)** – distancia del stop-loss desde la entrada (predeterminado 20).
- **Take Profit (pips)** – distancia del take-profit desde la entrada (predeterminado 350).
- **Noise Filter (pips)** – distancia para restablecer el permiso de trading (predeterminado 50).
- **ATR Length** – período de cálculo del ATR (predeterminado 200).
- **ATR Threshold (pips)** – ATR máximo para permitir nuevas posiciones (predeterminado 40).
- **Max Spread (pips)** – spread máximo permitido (predeterminado 4).
- **Risk Factor Z** – factor de gestión monetaria (predeterminado 2).
- **Candle Type** – período de las velas procesadas (predeterminado 15 minutos).

Esta estrategia utiliza órdenes de mercado para entradas y salidas.
