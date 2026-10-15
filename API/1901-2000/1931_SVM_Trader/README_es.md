# Estrategia SVM Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La Estrategia SVM Trader demuestra cómo una combinación de indicadores técnicos clásicos puede aproximar el comportamiento de un modelo de máquinas de vectores de soporte (SVM) para generar señales de trading. El ejemplo MQL original entrenaba dos SVMs separados para decisiones de compra y venta. En esta conversión a StockSharp, emulamos el proceso de decisión con un simple sistema de puntuación derivado de siete indicadores:

- **Bears Power** y **Bulls Power** – miden el equilibrio entre vendedores y compradores.
- **Average True Range (ATR)** – captura la volatilidad actual.
- **Momentum** – verifica la aceleración del precio.
- **Moving Average Convergence Divergence (MACD)** – identifica la dirección de la tendencia.
- **Stochastic Oscillator** – detecta niveles de sobrecompra y sobreventa.
- **Force Index** – combina el movimiento del precio y el volumen.

Cada indicador contribuye a una puntuación acumulada. Cuando la puntuación supera un umbral, la estrategia abre una posición larga; cuando la puntuación cae por debajo del umbral opuesto, se abre una posición corta. Esta configuración refleja el paso de clasificación del enfoque SVM original manteniendo la implementación ligera y transparente.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `CandleType` | Marco temporal de velas para los cálculos. |
| `Volume` | Volumen de orden para nuevas operaciones. |
| `TakeProfit` | Distancia para el take-profit en unidades absolutas de precio. |
| `StopLoss` | Distancia para el stop-loss en unidades absolutas de precio. |
| `RiskExposure` | Volumen máximo de posición acumulada permitido. |

## Lógica de Trading

1. Suscribirse a velas del tipo especificado y enlazar todos los indicadores usando la API de alto nivel.
2. Para cada vela terminada, recuperar los valores del indicador desde la devolución de llamada de enlace.
3. Calcular una puntuación:
   - Bulls Power mayor que Bears Power
   - Momentum por encima de cero
   - Línea MACD por encima de su línea de señal
   - Estocástico %K por encima de %D
   - Force Index por encima de cero
4. Si al menos tres condiciones son verdaderas y la posición actual no es positiva, se coloca una orden de compra de mercado.
5. Si dos o menos condiciones son verdaderas y la posición actual no es negativa, se coloca una orden de venta de mercado.
6. `StartProtection` aplica tanto el stop-loss como el take-profit para cada posición abierta.

## Notas

- Los períodos de los indicadores están fijos a los valores del ejemplo MQL original (principalmente 13 para simetría y suavidad).
- El sistema de puntuación es un proxy simplificado de la clasificación SVM y puede reemplazarse con un modelo más avanzado si es necesario.
- `RiskExposure` evita la sobreasignación limitando el tamaño total de posición.
- La estrategia usa tabulaciones para la indentación y comentarios en inglés según las convenciones del proyecto.

## Descargo de responsabilidad

Esta estrategia se proporciona con fines educativos. Demuestra el enlace de indicadores y la gestión básica de riesgos en StockSharp. Úsela y modifíquela bajo su propio riesgo.
