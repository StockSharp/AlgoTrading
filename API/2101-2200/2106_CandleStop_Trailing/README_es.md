# Estrategia CandleStop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
Esta estrategia implementa la gestión de stop trailing basada en el enfoque CandleStop. Analiza velas completadas y mueve el nivel de stop solo en la dirección de la operación. El algoritmo se basa en canales de Donchian con períodos de retrospección separados para posiciones largas y cortas, lo que lo hace adecuado para adjuntar a operaciones manuales u otras estrategias de entrada.

## Parámetros
- **Up Trail Periods** – número de velas utilizadas para calcular el máximo más alto para el trailing de posiciones cortas.
- **Down Trail Periods** – número de velas utilizadas para calcular el mínimo más bajo para el trailing de posiciones largas.
- **Candle Type** – marco temporal de las velas utilizadas para el análisis.

## Lógica de la Estrategia
1. Esperar una posición existente. La estrategia no abre operaciones por sí sola.
2. Para posiciones largas:
   - Calcular el mínimo más bajo durante *Down Trail Periods*.
   - Mover el stop a este valor si es mayor que el stop anterior.
   - Si el precio toca o cae por debajo del stop, salir de la posición con una orden de mercado.
3. Para posiciones cortas:
   - Calcular el máximo más alto durante *Up Trail Periods*.
   - Mover el stop a este valor si es menor que el stop anterior.
   - Si el precio toca o sube por encima del stop, recomprar la posición con una orden de mercado.

## Notas de Uso
- Diseñado para su uso con la API de alto nivel de StockSharp y suscripciones de velas.
- Adecuado para proteger posiciones abiertas manualmente o por otras estrategias.
- La salida del gráfico incluye velas, líneas de canal y operaciones ejecutadas para visualización.
