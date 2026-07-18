# Estrategia de Valor PPP de Divisas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Valor PPP de Divisas busca desajustes de precios en relación con la paridad del poder adquisitivo (PPP). Las divisas que cotizan por debajo de su valor PPP se compran, mientras que las que cotizan por encima se venden en corto. La cartera se rebalancea mensualmente para mantener la exposición largo/corto.

Dado que los datos de PPP se actualizan con poca frecuencia, las operaciones solo se realizan cuando el ajuste requerido supera un importe mínimo en USD. El código de ejemplo proporciona el marco para clasificar las divisas, pero deja el cálculo real del PPP como marcador de posición.

## Detalles

- **Universo**: Conjunto de pares de divisas con estimaciones de PPP disponibles.
- **Señal**: Largo en las `K` divisas más infravaloradas y corto en las `K` más sobrevaloradas.
- **Rebalanceo**: Mensual.
- **Posicionamiento**: Largo/Corto, igual ponderación.
- **Parámetros**:
  - `Universe` – divisas negociables.
  - `K` – número de divisas a ir largo y corto.
  - `MinTradeUsd` – tamaño mínimo de operación en USD.
  - `CandleType` – marco temporal de las velas (predeterminado: 1 día).
- **Nota**: La obtención de la desviación PPP (`TryGetPPPDeviation`) no está implementada y debe ser proporcionada por el usuario.
