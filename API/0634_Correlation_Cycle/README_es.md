# Estrategia de Ciclo de Correlación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa los conceptos de Ciclo de Correlación, Ángulo de Correlación y Estado del Mercado de John Ehlers. Calcula la correlación entre el precio y los componentes seno/coseno para obtener un ángulo de correlación. Cuando el ángulo es estable y está por encima de cero, la estrategia entra en una posición larga. Cuando el ángulo es estable y está por debajo de cero, entra en una posición corta.

## Parámetros
- Tipo de vela
- Período
- Umbral de estado del mercado
