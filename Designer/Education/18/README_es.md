# Esquema de Estrategia de Trading en Pares
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este esquema presenta una estrategia de trading en pares basada en el valor relativo de dos valores. Incorpora un enfoque único para identificar y capitalizar las discrepancias de precio entre dos activos correlacionados.

## Descripción General

El trading en pares es una estrategia neutral respecto al mercado que consiste en comprar un activo y simultáneamente vender otro cuando su ratio de precio se desvía de la norma histórica. Este esquema utiliza el ejemplo de dos valores específicos: SBER@TQBR y GAZP@TQBR.

## Lógica de la Estrategia

- **Cálculo del Índice**: La estrategia calcula un índice basado en la fórmula `SBER@TQBR / GAZP@TQBR`. Este índice ayuda a determinar la fortaleza o debilidad relativa de una acción comparada con la otra.
- **Condición de Compra**: Si el índice sube, indicando que SBER@TQBR se está encareciendo en relación a GAZP@TQBR, la estrategia compra el activo más barato (GAZP@TQBR) y vende el más caro (SBER@TQBR).
- **Condición de Venta**: Si el índice cae, sugiriendo que SBER@TQBR se está abaratando en relación a GAZP@TQBR, la estrategia compra el activo más caro (SBER@TQBR) y vende el más barato (GAZP@TQBR).

## Características Clave

- **Valores Redondeados**: Utiliza el operador `round` para convertir los valores del índice calculados a enteros. Esta simplificación facilita la toma de decisiones al proporcionar señales más claras y accionables.
- **Neutralidad de Mercado**: Busca beneficiarse de la convergencia del ratio de precios hacia su promedio histórico, independientemente de la dirección general del mercado.

## Aplicación y Beneficios

- **Mitigación de Riesgo**: Al operar en pares históricamente correlacionados, la estrategia minimiza el riesgo de mercado, ya que las ganancias de un lado a menudo compensan las pérdidas del otro.
- **Aprovechamiento de Ineficiencias de Precio**: La estrategia aprovecha las ineficiencias temporales en los precios de los valores emparejados, que se espera que eventualmente reviertan a su media.

## Ejecución

- **Condiciones de Configuración**: Antes de implementar la estrategia, asegúrese de que ambos valores sean monitoreados de cerca para detectar divergencias significativas que puedan desencadenar operaciones.
- **Dinámica Operacional**: El monitoreo continuo y la recalibración de los niveles umbrales de compra y venta basados en datos históricos y condiciones de mercado son cruciales para el éxito de la estrategia.

El esquema presentado no solo describe un marco sólido para el trading en pares, sino que también destaca la importancia de herramientas matemáticas como el redondeo para simplificar decisiones de trading complejas.
