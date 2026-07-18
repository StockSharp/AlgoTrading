# Estrategia de Flechas de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas cuando el precio de cierre se mueve más allá de los extremos recientes.
Calcula los precios de cierre máximos y mínimos durante un período configurable.
Se detecta una nueva tendencia alcista cuando el cierre supera el máximo reciente,
mientras que una tendencia bajista comienza cuando el cierre cae por debajo del mínimo reciente.

Cuando se detecta una nueva tendencia alcista, se pueden cerrar posiciones cortas existentes y abrir posiciones largas opcionales.
A la inversa, una nueva tendencia bajista permite cerrar posiciones largas y opcionalmente abrir cortas.
La estrategia procesa solo velas completadas y utiliza la API de alto nivel de StockSharp.

## Parámetros
- **Period** – número de barras para determinar los extremos recientes.
- **Candle Type** – marco temporal de las velas.
- **Open Long** – permitir la apertura de posiciones largas.
- **Open Short** – permitir la apertura de posiciones cortas.
- **Close Long** – permitir el cierre de posiciones largas.
- **Close Short** – permitir el cierre de posiciones cortas.

## Lógica
1. Suscribirse a los datos de velas del marco temporal seleccionado.
2. Rastrear los cierres más altos y más bajos durante el período utilizando los indicadores `Highest` y `Lowest`.
3. Cuando el precio rompe por encima del cierre más alto, señalar tendencia alcista; cuando está por debajo del cierre más bajo, señalar tendencia bajista.
4. Entrar o salir de posiciones según la nueva tendencia y las opciones habilitadas.
