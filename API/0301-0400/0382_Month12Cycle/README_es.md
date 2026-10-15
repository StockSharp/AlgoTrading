# Estrategia del Ciclo de 12 Meses
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia en Python implementa la anomalía del ciclo de 12 meses. Las acciones se clasifican por el rendimiento obtenido hace un año en el mes calendario correspondiente. Cada mes se compra el decil superior y se vende en corto el decil inferior, creando una cartera neutral al mercado basada en el rendimiento anual rezagado.

El sistema utiliza datos diarios para aproximar los cierres mensuales y rebalancea al inicio de cada mes. Los tamaños de posición se escalan para mantener la exposición en dólares equilibrada entre los lados largo y corto.

## Detalles

- **Universo**: lista de valores definida por el usuario.
- **Señal**: ordenar por el cambio porcentual respecto al mismo mes del año anterior.
- **Cartera**: largo en el decil superior, corto en el decil inferior con apalancamiento por tramo definido por `Leverage`.
- **Rebalanceo**: mensual.
- **Datos**: velas diarias agregadas en precios de fin de mes.
