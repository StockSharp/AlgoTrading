# Estrategia DCA Simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia coloca una orden base y añade órdenes de seguridad cuando el precio se desvía en un porcentaje especificado. Sale una vez que el precio alcanza un take profit calculado desde el precio de entrada promedio. El tamaño de cada orden de seguridad se multiplica por un factor.

## Parámetros
- Tipo de vela
- Tamaño de la orden base (moneda de cotización)
- Desviación de precio para la orden de seguridad (%)
- Máximo de órdenes de seguridad
- Take profit (%)
- Multiplicador del tamaño de la orden
