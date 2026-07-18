# Estrategia de Canal XMA Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia implementa un sistema de ruptura de canal basado en el concepto XMA Ichimoku. Construye un canal dinámico alrededor de una media suavizada de los máximos y mínimos recientes y genera operaciones cuando la acción del precio confirma una ruptura con un retroceso.

## Cómo funciona

1. **Valores máximos y mínimos**: Para cada vela finalizada, la estrategia calcula el máximo más alto y el mínimo más bajo durante periodos de retrospección configurables.
2. **Línea media suavizada**: El punto medio entre los valores máximo y mínimo se suaviza utilizando una media móvil simple.
3. **Construcción del canal**: Las bandas superior e inferior se derivan de la línea media suavizada aplicando desplazamientos porcentuales.
4. **Lógica de trading**:
   - Si el cierre anterior estaba por encima de la banda superior previa y el cierre actual vuelve por debajo de la banda superior actual, la estrategia abre una posición larga y cierra cualquier corta existente.
   - Si el cierre anterior estaba por debajo de la banda inferior previa y el cierre actual vuelve por encima de la banda inferior actual, la estrategia abre una posición corta y cierra cualquier larga existente.

## Parámetros

- **Up Period** – periodo de retrospección para el precio más alto.
- **Down Period** – periodo de retrospección para el precio más bajo.
- **MA Length** – longitud de la media móvil de suavizado.
- **Up Percent** – porcentaje añadido a la línea media para formar la banda superior.
- **Down Percent** – porcentaje restado de la línea media para formar la banda inferior.
- **Candle Type** – marco temporal de las velas utilizadas para los cálculos.

## Notas de uso

- Las operaciones se ejecutan con órdenes de mercado.
- Solo se procesan velas finalizadas para evitar señales falsas.
- La estrategia cierra las posiciones opuestas antes de abrir una nueva.

## Descargo de responsabilidad

Este ejemplo se proporciona únicamente con fines educativos. Pruébelo exhaustivamente antes de usarlo en trading en vivo.
