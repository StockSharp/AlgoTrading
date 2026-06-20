# Estrategia de la Semana de Expiración de Opciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia en Python compra y mantiene un ETF de renta variable solo durante la semana de expiración de opciones. A partir del lunes anterior al tercer viernes de cada mes se compra el ETF y la posición se cierra al cierre del viernes. La idea explota la fortaleza a corto plazo que suele observarse durante la semana de expiración.

Fuera de esta ventana, la cartera permanece en efectivo. Se utilizan velas diarias y las operaciones se envían como órdenes de mercado una vez al día.

## Detalles

- **Instrumento**: un único ETF de renta variable.
- **Señal**: regla de calendario para la semana que termina el tercer viernes.
- **Período de tenencia**: apertura del lunes al cierre del viernes de la semana de expiración.
- **Posicionamiento**: totalmente invertido durante la ventana, sin posición en otros momentos.
- **Control de riesgo**: operación omitida cuando el valor de la orden está por debajo de `MinTradeUsd`.
