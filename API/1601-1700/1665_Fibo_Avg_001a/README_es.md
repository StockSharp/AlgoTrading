# Estrategia de Cruce de Medias Móviles Fibo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia convierte el asesor experto de MetaTrader **EA_Fibo_Avg_001a** al framework StockSharp.
Utiliza dos medias móviles suavizadas. La longitud de la media lenta es la suma del período base y un desplazamiento basado en Fibonacci.
Se abre una posición larga cuando la media rápida cruza por encima de la media lenta, mientras que una posición corta se abre en el cruce contrario.
Las posiciones se gestionan con stop-loss, take-profit y un trailing stop. La gestión de dinero opcional puede calcular el volumen de la orden a partir del tamaño de la cartera.

## Parámetros
- `CandleType` – tipo de datos de velas.
- `FiboNumPeriod` – longitud adicional añadida a la media móvil lenta.
- `MaPeriod` – período base de las medias móviles.
- `TrailingStop` – distancia del trailing stop en pasos de precio.
- `TakeProfit` – distancia del take-profit en pasos de precio.
- `StopLoss` – distancia del stop-loss en pasos de precio.
- `UseMoneyManagement` – activar la gestión de dinero simple.
- `PercentMm` – porcentaje de la cartera utilizado cuando la gestión de dinero está activada.
- `LotSize` – volumen de orden predeterminado cuando la gestión de dinero está desactivada.

## Lógica
1. Suscribirse a velas y calcular dos medias móviles suavizadas.
2. Cuando la media rápida cruza por encima de la lenta, comprar. Cuando cruza por debajo, vender.
3. Tras entrar en una posición, establecer niveles de stop-loss, take-profit y trailing.
4. Actualizar el trailing stop a medida que el precio se mueve a favor y cerrar posiciones cuando se alcanzan los niveles de protección o se produce el cruce opuesto.
