# Estrategia de Momentum de Fondos Mutuos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia rota trimestralmente entre un conjunto de fondos mutuos. Al final de cada trimestre, los fondos se clasifican por su rendimiento de los últimos seis meses. El capital se asigna al fondo líder para el siguiente trimestre, permitiendo a los inversores a largo plazo seguir el momentum persistente en productos de gestión activa.

Solo se mantiene un fondo a la vez. Se utilizan datos de precio diarios y el rebalanceo ocurre durante los primeros tres días de negociación de enero, abril, julio y octubre.

## Detalles

- **Universo**: lista de fondos mutuos.
- **Señal**: clasificación por rendimiento total de 126 días (seis meses).
- **Rebalanceo**: trimestral en los primeros días de negociación del nuevo trimestre.
- **Posicionamiento**: totalmente largo en el fondo de mayor rango.
- **Control de riesgo**: omitir operación cuando el valor de la orden esté por debajo de `MinTradeUsd`.
