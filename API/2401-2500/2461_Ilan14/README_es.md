# Estrategia Ilan14
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ilan14 es una estrategia de grilla de cobertura que abre posiciones largas y cortas simultáneamente. Cuando el mercado se mueve en contra de un lado una distancia definida en pips por el usuario, la estrategia añade una nueva orden en esa dirección con su volumen multiplicado por el **Lot Exponent**. Se rastrea el precio promedio de la posición y una vez que el precio revierte la distancia de **Take Profit** configurada, todas las órdenes de ese lado se cierran.

Parámetros:
- **Pip Step** – distancia en pips entre las órdenes de la grilla.
- **Lot Exponent** – multiplicador aplicado al volumen de cada orden adicional.
- **Max Trades** – número máximo de órdenes por dirección.
- **Take Profit** – objetivo de ganancia en pips desde el precio promedio ponderado.
- **Initial Volume** – volumen de la primera orden.
- **Candle Type** – marco temporal para la suscripción de velas.

La implementación utiliza la API de alto nivel de StockSharp con suscripciones a velas y evita colecciones de datos manuales. Ambos lados de la grilla se gestionan de forma independiente, lo que permite a la estrategia capturar rebotes después de movimientos adversos.
