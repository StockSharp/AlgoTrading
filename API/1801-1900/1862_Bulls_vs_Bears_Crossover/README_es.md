# Estrategia de Cruce Bulls vs Bears
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia implementa un sistema de cruce basado en el indicador **Bulls vs Bears (BvsB)**. El indicador mide la distancia entre los precios máximo y mínimo de una vela y una media móvil. Cuando la distancia alcista cae por debajo de la distancia bajista, indica una presión al alza que se desvanece, y la estrategia abre una posición larga. Por el contrario, cuando la distancia alcista sube por encima de la distancia bajista, se abre una posición corta. Las posiciones existentes se cierran ante la señal opuesta o cuando se alcanzan los objetivos de beneficio o pérdida.

El tipo y período de la media móvil son configurables, lo que permite que la estrategia se adapte a diferentes mercados y marcos temporales. La gestión de riesgo se controla mediante niveles fijos de stop-loss y take-profit expresados en pasos de precio.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `MaType` | Método de cálculo de la media móvil (SMA, EMA, SMMA, WMA). |
| `MaLength` | Período de la media móvil. |
| `StopLoss` | Distancia de stop-loss en pasos de precio. |
| `TakeProfit` | Distancia de take-profit en pasos de precio. |
| `OpenLong` | Permitir apertura de posiciones largas en cruce alcista. |
| `OpenShort` | Permitir apertura de posiciones cortas en cruce bajista. |
| `CloseLong` | Permitir cierre de posiciones largas en cruce bajista. |
| `CloseShort` | Permitir cierre de posiciones cortas en cruce alcista. |
| `CandleType` | Marco temporal de las velas procesadas. |

## Cómo funciona

1. Suscribirse a la serie de velas especificada y calcular una media móvil.
2. Para cada vela finalizada, calcular las distancias alcista y bajista:
   - **Bull** = `(HighPrice - MA) / PriceStep`
   - **Bear** = `(MA - LowPrice) / PriceStep`
3. Detectar cruces entre los valores Bull y Bear.
4. Abrir o cerrar posiciones según la dirección del cruce y las opciones habilitadas.
5. Gestionar el riesgo usando los niveles de stop-loss y take-profit configurados.

Este enfoque simple pero flexible puede aplicarse a muchos instrumentos para medir el equilibrio entre las fuerzas alcistas y bajistas.
