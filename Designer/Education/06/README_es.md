# Esquema de Cubos Matemáticos y Fórmulas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este archivo de esquema demuestra la utilización de cubos matemáticos y fórmulas de la sección "Matemáticas" en la herramienta Designer, centrándose específicamente en cómo emplear estos elementos en estrategias de trading.

## Descripción General

El esquema explora el uso de fórmulas para tomar decisiones de trading basadas en la comparación del precio de cierre de un valor con sus parámetros estadísticos calculados mediante el Simple Moving Average (SMA) y la desviación estándar.

## Detalles de la Estrategia

- **Condición de Venta**: La estrategia emite una orden de venta si el precio de cierre de la vela anterior es mayor que el valor SMA de los últimos 20 períodos más tres veces la desviación estándar del mismo período.
- **Condición de Compra**: Se ejecuta una orden de compra si el precio de cierre de la vela anterior es menor que el valor SMA de los últimos 20 períodos menos tres veces la desviación estándar.

## Cambios en la Versión 5

- **Sección de Matemáticas**: En la versión 5 del software Designer, la sección "Matemáticas" ha sido eliminada. Todos los cubos que se encontraban anteriormente en esta sección han sido consolidados en un único cubo "Fórmula", simplificando el proceso de diseño e implementación.
- **Cubo de Apertura de Posición**: El cubo "Abrir Posición" ha sido reemplazado por el cubo "Registrar Orden" en la versión 5, reflejando cambios en cómo se procesan las órdenes dentro de la plataforma.

Este esquema muestra eficazmente cómo aprovechar cálculos matemáticos avanzados para crear estrategias de trading dinámicas y estadísticamente fundamentadas. La integración de estos elementos dentro de un esquema de trading puede mejorar significativamente los procesos de toma de decisiones al basarlos en análisis cuantitativo.
