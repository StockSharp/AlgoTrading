# Estrategia LeMan Trend Hist
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión simplificada del experto MQL5 original "LeManTrendHist". Se basa en un histograma basado en EMA para generar señales de trading.

## Idea

El algoritmo original calcula un histograma personalizado derivado de extremos de precio y rangos suavizados. Para esta muestra, el histograma se aproxima mediante una media móvil exponencial de los rangos de velas.

## Lógica de la estrategia

1. Calcular el valor EMA para cada vela completada.
2. Comparar los tres últimos valores EMA.
3. Cuando el valor del medio es menor que el más antiguo y el nuevo valor sube por encima de él, se abre una posición larga y se cierran las posiciones cortas.
4. Cuando el valor del medio es mayor que el más antiguo y el nuevo valor cae por debajo de él, se abre una posición corta y se cierran las posiciones largas.

## Parámetros

- **Candle Type** – marco temporal de las velas procesadas.
- **EMA Period** – longitud del EMA usado en el histograma de marcador de posición.
- **Signal Bar** – desplazamiento histórico para los valores del indicador (mantenido por compatibilidad, no se usa en la lógica simplificada).
- **Buy/Sell Open** – habilitar entradas largas o cortas.
- **Buy/Sell Close** – habilitar el cierre de posiciones existentes.

## Notas

El indicador real LeManTrendHist utiliza algoritmos de suavizado complejos que aún no están implementados. La implementación actual actúa como un marcador de posición y debe reemplazarse con el indicador completo para uso en producción.
