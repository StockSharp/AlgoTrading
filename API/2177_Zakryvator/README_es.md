# Estrategia Zakryvator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Zakryvator es un módulo de gestión de riesgo que monitorea la posición abierta actual y la cierra cuando la pérdida no realizada supera un umbral predefinido. La pérdida permitida depende del volumen de la posición, replicando la lógica del script MQL original donde diferentes tamaños de lote corresponden a diferentes drawdowns máximos.

Esta estrategia no genera entradas por sí misma. Se espera que las posiciones sean abiertas manualmente o por otra estrategia. Zakryvator simplemente protege la cuenta saliendo automáticamente de operaciones con pérdidas.

## Detalles

- **Criterios de entrada**: Ninguno. La estrategia solo gestiona posiciones existentes.
- **Criterios de salida**: Cierra la posición actual una vez que la pérdida alcanza el umbral configurado para su volumen.
- **Largo/Corto**: Ambas direcciones son compatibles.
- **Stops**: Utiliza límites de pérdida monetaria fijos que varían con el tamaño de la posición.
- **Filtros**: Sin filtros adicionales.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Min001002` | Pérdida máxima para posiciones con volumen ≤ 0.02 lotes. |
| `Min002005` | Pérdida máxima para posiciones con volumen entre 0.02 y 0.05 lotes. |
| `Min00501` | Pérdida máxima para posiciones con volumen entre 0.05 y 0.10 lotes. |
| `Min0103` | Pérdida máxima para posiciones con volumen entre 0.10 y 0.30 lotes. |
| `Min0305` | Pérdida máxima para posiciones con volumen entre 0.30 y 0.50 lotes. |
| `Min051` | Pérdida máxima para posiciones con volumen entre 0.50 y 1 lote. |
| `MinFrom1` | Pérdida máxima para posiciones con volumen mayor a 1 lote. |

## Comportamiento

1. La estrategia se suscribe a ticks de operaciones para rastrear precios en tiempo real.
2. En cada tick calcula el PnL no realizado usando el precio actual y el precio de entrada promedio.
3. Si la pérdida supera el umbral correspondiente al volumen de la posición actual, la posición se cierra a mercado.

Esto convierte a Zakryvator en una herramienta simple pero efectiva para limitar los drawdowns según el tamaño de la operación.
