# Estrategia del clasificador de patrones Mnist
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Origen

La estrategia es un puerto StockSharp del experto MetaTrader 5 **TestMnistOnnx.mq5** (MQL ID 47225). El guión original expone un interactivo
lienzo donde el usuario dibuja dígitos que están clasificados por un modelo MNIST ONNX incluido. La versión StockSharp mantiene el espíritu de
reconocimiento de patrones, pero reemplaza el lienzo dibujado a mano con una matriz rodante construida con velas terminadas.

## Concepto

1. Una ventana móvil de `LookbackPeriod` velas completadas (predeterminada 28) se trata como una cuadrícula de 28 × 28 similar a una imagen MNIST.
2. Se combinan varias características estadísticas (compresión de rango, fuerza de tendencia, impulso, desviación RSI y normalización ATR)
en una puntuación sintética de "confianza" que imita la probabilidad de la red neuronal producida por el experto MQL.
3. Las características resultantes se asignan a una de las diez clases de patrón (`0`–`9`). Cada clase representa un régimen de mercado.
(plano, tendencia, ruptura, retroceso, reversión, etc.).
4. Cuando la clase detectada coincide con el `TargetClass` seleccionado por el usuario y la confianza sintética es superior a `ConfidenceThreshold`,
la estrategia abre o invierte una posición en la dirección indicada. Las posiciones se aplanan si la clase cambia o el
la confianza cae por debajo del umbral.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `LookbackPeriod` | 28 | Número de velas terminadas que se convierten en la cuadrícula tipo MNIST. |
| `TargetClass` | 1 | Índice de clase (0–9) que debería desencadenar acciones comerciales. |
| `ConfidenceThreshold` | 0,6 | Probabilidad sintética mínima que permite el envío de pedidos. |
| `Volume` | 1 | Volumen de pedidos para nuevas posiciones. |
| `CandleType` | plazo de 5 minutos | Tipo de datos suscrito para actualizaciones de velas. |

## Clases de patrones

| clase | Significado |
|-------|---------|
| 0 | Consolidación plana o de baja volatilidad. |
| 1 | Tendencia alcista sostenida. |
| 2 | Tendencia bajista sostenida. |
| 3 | Ruptura al alza con un fuerte seguimiento. |
| 4 | Ruptura a la baja con fuerte seguimiento. |
| 5 | Amplio rango volátil sin sesgo claro. |
| 6 | Retroceso alcista dentro de una tendencia alcista. |
| 7 | Retroceso bajista dentro de una tendencia bajista. |
| 8 | Inversión alcista después de una caída prolongada. |
| 9 | Reversión bajista después de un avance prolongado. |

## Reglas de trading

- Intercambia únicamente velas completadas para permanecer sincronizado con el experto original que reaccionó a los dibujos finalizados.
- Utiliza órdenes de mercado (`BuyMarket`, `SellMarket`) y se aplana antes de revertir para imitar el comportamiento de posición única del
guión original.
- La escala de confianza está limitada a `[0, 1]`. El aumento de `ConfidenceThreshold` filtra las señales más débiles.
- La estrategia no gestiona paradas de protección; Se espera que la gestión de riesgos se configure externamente en StockSharp.

## Consejos de uso

- Seleccione un tipo de vela que refleje el ritmo del mercado que desea analizar. Los plazos más cortos reaccionan más rápido pero son más ruidosos.
- Optimice `TargetClass` y `ConfidenceThreshold` juntos: algunas clases son naturalmente más raras y pueden requerir umbrales más bajos.
- El clasificador de patrones sintéticos es determinista; no hay dependencia de bibliotecas de tiempo de ejecución ONNX externas.
- Combínelo con las herramientas de protección contra riesgos integradas disponibles en StockSharp (como `StartProtection`) para controlar la exposición.

## Diferencias con el original

- El dibujo interactivo y la inferencia ONNX se reemplazan por un análisis de velas totalmente automatizado.
- La "confianza" es una combinación determinista de indicadores más que una probabilidad de red neuronal.
- Se agrega lógica comercial para convertir el reconocimiento de patrones en órdenes procesables.
- El archivo de recursos MNIST no es necesario en el entorno StockSharp.
