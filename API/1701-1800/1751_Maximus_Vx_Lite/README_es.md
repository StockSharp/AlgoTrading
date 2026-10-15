# Estrategia Maximus vX Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia intenta operar rupturas de zonas de consolidación a corto plazo. Busca rangos de precios compactos en el gráfico de 15 minutos y abre operaciones cuando el precio se aleja de estos rangos una distancia especificada.

## Lógica de la estrategia

1. Para cada vela de 15 minutos finalizada, se calculan el máximo más alto y el mínimo más bajo de las últimas 40 velas.
2. Si la distancia entre estos extremos es inferior al parámetro **Range**, se asume una zona de consolidación.
3. Tras esperar el período **Delay Open** sin nuevas operaciones, una ruptura por encima del límite superior más **Distance** puntos activa una posición larga, mientras que una ruptura por debajo del límite inferior menos **Distance** puntos activa una posición corta.
4. Se aplica un **Stop Loss** fijo y un trailing stop de **Trail** puntos una vez abierta la posición.
5. Los límites de consolidación se reinician tras transcurrir las horas de **Period**.

## Parámetros

- `DelayOpen` – Horas de espera antes de abrir una nueva operación.
- `Distance` – Distancia de ruptura desde el límite de consolidación en puntos.
- `Period` – Horas tras las cuales se recalculan los niveles de consolidación.
- `Range` – Tamaño máximo de la zona de consolidación en puntos.
- `StopLoss` – Stop loss inicial en puntos.
- `Trail` – Distancia del trailing stop en puntos.

## Notas

La estrategia usa solo la API de alto nivel: las velas se reciben mediante `SubscribeCandles` y los valores de indicadores se vinculan con `Bind`. Las órdenes se envían con los métodos `BuyMarket` y `SellMarket`. Los comentarios en el código fuente están escritos en inglés.
