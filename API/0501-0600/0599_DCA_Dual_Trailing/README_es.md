# Estrategia DCA con Doble Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando una EMA rápida cruza por encima de una EMA lenta. Se colocan hasta dos órdenes de seguridad cuando el precio cae por umbrales basados en ATR o porcentaje. Las posiciones están protegidas por un trailing stop estándar y un trailing stop secundario de bloqueo habilitado después de un umbral de ganancia.

## Parámetros
- Tipo de vela
- Longitud de EMA rápida
- Longitud de EMA lenta
- Usar filtro de fecha
- Fecha de inicio
- Usar espaciado ATR
- Longitud de ATR
- Multiplicador ATR para OrdSeg1
- Multiplicador ATR para OrdSeg2
- Porcentaje de respaldo OrdSeg1
- Porcentaje de respaldo OrdSeg2
- Barras de enfriamiento
- Tamaño de orden base USD
- Tamaño de orden de seguridad 1 USD
- Tamaño de orden de seguridad 2 USD
- Porcentaje de trailing stop
- Porcentaje de activación de bloqueo
- Porcentaje de trailing de bloqueo
