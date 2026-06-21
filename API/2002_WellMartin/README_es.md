# Estrategia WellMartin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

La estrategia **WellMartin** es un sistema de reversión a la media que combina las Bandas de Bollinger y el Índice Direccional Promedio (ADX). Entra en posiciones largas cuando el precio rompe por debajo de la banda de Bollinger inferior durante una baja fortaleza de tendencia, y entra en posiciones cortas cuando el precio rompe por encima de la banda superior bajo las mismas condiciones. Las posiciones se cierran cuando el precio alcanza la banda opuesta o golpea los niveles de take profit o stop loss configurados.

## Parámetros

- **CandleType** – serie de velas utilizada para los cálculos.
- **BollingerPeriod** – período para las Bandas de Bollinger.
- **BollingerWidth** – multiplicador de desviación estándar para las Bandas de Bollinger.
- **AdxPeriod** – período para el indicador ADX.
- **AdxLevel** – umbral ADX; las operaciones se realizan solo cuando el valor ADX está por debajo de este nivel.
- **Volume** – volumen de operación para cada entrada.
- **TakeProfit** – objetivo de ganancia en unidades de precio.
- **StopLoss** – límite de pérdida en unidades de precio.

## Lógica

1. Suscribirse a datos de velas y calcular las Bandas de Bollinger y ADX.
2. Cuando no hay posición abierta:
   - **Comprar** si el precio de cierre está por debajo de la banda inferior y el ADX está por debajo del umbral.
   - **Vender** si el precio de cierre está por encima de la banda superior y el ADX está por debajo del umbral.
3. Rastrear el lado de la última operación ejecutada y permitir entradas solo en la misma dirección o cuando no se han realizado operaciones.
4. Cuando en una posición larga:
   - Salir si el precio toca la banda superior, alcanza el take profit o golpea el stop loss.
5. Cuando en una posición corta:
   - Salir si el precio toca la banda inferior, alcanza el take profit o golpea el stop loss.

## Notas

Esta implementación utiliza un volumen de operación fijo. La versión MQL original aumentaba el volumen después de una operación perdedora; este comportamiento puede añadirse más tarde si se requiere.
