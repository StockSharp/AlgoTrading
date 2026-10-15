# Estrategia de Spread WTI-Brent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La operación apunta al diferencial de precio entre el petróleo WTI y el Brent. Cuando el spread se desvía de las normas históricas, el sistema apuesta por una reversión a la media tomando una posición larga en un grado y corta en el otro.

Las posiciones se renuevan con los futuros del mes próximo y se cierran cuando el spread converge.

## Detalles

- **Datos**: Precios de los futuros del mes próximo de WTI y Brent.
- **Entrada**: Largo en el grado más barato y corto en el más caro cuando el spread > umbral.
- **Salida**: Cerrar cuando el spread vuelve al promedio o en la renovación del contrato.
- **Instrumentos**: Futuros de petróleo crudo.
- **Riesgo**: Neutralidad en dólares con stop ante ampliación del spread.

