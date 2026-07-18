# Estrategia Recolectora de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión del algoritmo MQL original `TrendCollector.mq4`. Combina la detección de tendencia usando dos medias móviles exponenciales con filtros de momentum y volatilidad.

## Lógica de la estrategia

- **EMA rápida vs EMA lenta** – La estrategia sigue la tendencia principal comparando una EMA rápida con una EMA lenta.
- **Oscilador Estocástico** – Determina condiciones de sobrecompra y sobreventa. Las posiciones largas se abren cuando el valor estocástico está por debajo del umbral inferior y la EMA rápida está por encima de la EMA lenta. Las posiciones cortas se activan cuando el valor estocástico está por encima del umbral superior y la EMA rápida está por debajo de la EMA lenta.
- **Filtro de volatilidad ATR** – Las operaciones solo ocurren cuando el valor ATR actual está por debajo del límite de volatilidad, evitando períodos de alta volatilidad.
- **Ventana de trading** – Las órdenes se generan solo entre las horas de inicio y fin configuradas.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| FastMaLength | Período de la EMA rápida | 4 |
| SlowMaLength | Período de la EMA lenta | 204 |
| StochasticPeriod | Período del oscilador estocástico | 14 |
| StochasticUpper | Nivel superior del estocástico | 80 |
| StochasticLower | Nivel inferior del estocástico | 20 |
| AtrPeriod | Período del ATR | 14 |
| AtrLimit | Valor ATR máximo permitido para operar | 2 |
| StartHour | Hora de inicio de la ventana de trading | 5 |
| EndHour | Hora de fin de la ventana de trading | 24 |
| CandleTimeFrame | Marco temporal de las velas procesadas | 5 minutos |

La versión de Python no está disponible actualmente.
