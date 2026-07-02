# Diagrama de Estrategia de Medias Móviles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este archivo contiene una representación diagramática de una estrategia de trading basada en medias móviles, diseñada mediante la Galería de Estrategias de la plataforma Designer. La estrategia utiliza el concepto de medias móviles para generar señales de compra y venta basadas en sus cruces, un método popular en los mercados financieros para evaluar el impulso y confirmar tendencias.

![schema](schema.png)

## Descripción General de la Estrategia

La estrategia incorpora dos medias móviles:

- **Media Móvil a Corto Plazo**: una [media móvil](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) más rápida que reacciona más rápidamente a los cambios de precio.
- **Media Móvil a Largo Plazo**: una [media móvil](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) más lenta que proporciona una imagen más suavizada de las tendencias de precios.

## Reglas de Entrada y Salida

- **Señal de Compra**: la estrategia genera una señal de [compra](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) cuando la media móvil a corto plazo [cruza](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) por encima de la media móvil a largo plazo, lo que sugiere una tendencia alcista.
- **Señal de Venta**: por el contrario, se emite una señal de [venta](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) cuando la media móvil a corto plazo [cruza](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) por debajo de la media móvil a largo plazo, indicando una posible tendencia bajista.

## Detalles del Diagrama

El diagrama presenta visualmente el flujo lógico de la estrategia:

- **Cálculo de Medias Móviles**: los nodos calculan las medias móviles basándose en parámetros definidos por el usuario, como el período y el tipo de media móvil (por ejemplo, simple, exponencial).
- **Nodos de Comparación**: evalúan las condiciones de cruce para determinar si entrar o salir de posiciones.
- **Acciones de Trading**: nodos que ejecutan órdenes de compra o venta basándose en los resultados de la evaluación de los nodos de comparación.

## Uso

Los traders pueden importar este diagrama en la plataforma Designer para:
- probar la estrategia usando datos históricos para comprender su efectividad;
- modificar los parámetros de las medias móviles o la lógica para adaptarse mejor a necesidades de trading específicas o condiciones de mercado;
- desplegar la estrategia en un entorno de trading en vivo después de pruebas suficientes.

## Valor Educativo

Este diagrama de estrategia sirve como herramienta educativa para que los principiantes comprendan los fundamentos del análisis técnico y el diseño de estrategias. También proporciona una base para el desarrollo de estrategias más complejas para usuarios avanzados.

Este archivo forma parte de una colección integral de estrategias de trading proporcionadas en la plataforma Designer, destinada a mejorar las habilidades de trading y las capacidades de desarrollo de estrategias de los usuarios.
