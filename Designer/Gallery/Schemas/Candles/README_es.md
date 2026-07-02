# Diagrama de Uso Básico de la Fuente de Datos y el Cubo Chart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama proporciona una demostración sencilla de cómo usar la fuente de datos "Candles" y el cubo "Chart" dentro de la plataforma Designer. Está diseñado para ayudar a los usuarios a comprender los fundamentos de la obtención de datos de mercado y su visualización en formato de gráfico.

![schema](schema.png)

## Descripción general

El diagrama muestra la configuración básica necesaria para recuperar datos de velas de un instrumento financiero específico y mostrarlos en un gráfico. Sirve como ejemplo fundamental para quienes son nuevos en el uso de Designer o para quienes desean comenzar con técnicas simples de visualización de datos.

## Componentes del diagrama

- **Fuente de datos Candles**: Es el nodo principal que obtiene [datos de velas](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) del instrumento financiero seleccionado. Los usuarios pueden especificar el instrumento, el rango de datos y el marco temporal de la vela (por ejemplo, velas de 1 minuto, 5 minutos).
- **Cubo Chart**: Este nodo se usa para [representar gráficamente](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) los datos obtenidos en una interfaz gráfica. Puede mostrar varios atributos de las velas, como precios de apertura, máximo, mínimo y cierre.

## Funcionalidad

- **Recuperación de datos**: El diagrama comienza recuperando datos de velas usando los parámetros especificados en el cubo Fuente de Datos Candles.
- **Visualización de datos**: Los datos recuperados se pasan al cubo Chart, que representa las velas en un gráfico dentro del entorno de Designer.

## Caso de uso

Este diagrama es especialmente útil para:
- Nuevos usuarios que aprenden a configurar la recuperación y visualización de datos en Designer.
- Traders y analistas que buscan visualizar rápidamente datos de mercado para análisis.
- Propósitos educativos, demostrando la interacción básica entre los nodos fuente de datos y las herramientas de visualización dentro de la plataforma.

## Aplicación práctica

Al entender y usar esta configuración básica, los usuarios pueden:
- Configurar rápidamente representaciones visuales de datos de mercado para análisis en tiempo real o histórico.
- Ampliar el diagrama básico incorporando herramientas analíticas adicionales o indicadores disponibles en Designer.
- Usar el gráfico como bloque de construcción para estrategias de trading más complejas o estudios de datos.

Este diagrama es parte de un conjunto más amplio de recursos educativos disponibles en la plataforma Designer, orientados a mejorar la competencia de los usuarios en el manejo y visualización de datos.
