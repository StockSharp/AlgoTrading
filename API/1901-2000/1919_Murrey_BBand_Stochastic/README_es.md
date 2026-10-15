# Estrategia Murrey Math con BBand y Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera reversiones en las líneas extremas de Murrey Math usando Bandas de Bollinger y un Oscilador Estocástico como confirmación.

El método calcula los niveles de Murrey a partir de los precios más altos y más bajos durante un marco configurable. Cuando el precio se aproxima a la línea 0/8 durante condiciones de sobreventa, la estrategia compra. Cuando el precio se acerca a la línea 8/8 durante condiciones de sobrecompra, vende. Un filtro de ancho mínimo de Bandas de Bollinger evita operar en mercados laterales.

## Detalles

- **Criterios de entrada**
  - **Largo**: El cierre está dentro del *Entry Margin* por encima de la línea 0/8, Estocástico <= 21 y ancho de Bandas de Bollinger >= umbral.
  - **Corto**: El cierre está dentro del *Entry Margin* por debajo de la línea 8/8, Estocástico >= 79 y ancho de Bandas de Bollinger >= umbral.
- **Largo/Corto**: Ambos.
- **Criterios de salida**
  - Las posiciones largas se cierran en la línea 1/8 o si el precio cae por debajo de la línea -2/8.
  - Las posiciones cortas se cierran en la línea 7/8 o si el precio sube por encima de la línea +2/8.
- **Stops**: Las líneas de Murrey (-2/8 o +2/8) actúan como stops de protección.
- **Filtros**
  - Filtro de ancho de Bandas de Bollinger.
  - Filtro del oscilador estocástico.
