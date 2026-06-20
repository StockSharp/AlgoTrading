# Estrategia de Gastos en R&D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de corte transversal clasifica las acciones por su ratio de gastos en investigación y desarrollo (R&D) respecto al valor de mercado. Al inicio de cada mes se compra el quintil superior de empresas con mayor intensidad en R&D mientras que se vende en corto el quintil inferior, apostando a que el gasto intensivo en R&D predice un rendimiento superior futuro.

Los pesos se asignan de manera equitativa dentro de cada lado y se rebalancean mensualmente usando datos de precio diarios.

## Detalles

- **Universo**: lista de acciones con datos de R&D.
- **Señal**: gastos en R&D divididos por la capitalización de mercado.
- **Cartera**: largo en el quintil más alto, corto en el quintil más bajo.
- **Rebalanceo**: mensual.
- **Control de riesgo**: operaciones omitidas cuando el valor de la orden está por debajo de `MinTradeUsd`.
