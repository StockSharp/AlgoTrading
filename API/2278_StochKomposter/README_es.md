# Estrategia Stoch Komposter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port del experto MQL5 **Exp_iStochKomposter**. Utiliza el Oscilador Estocástico para detectar reversiones de momentum y opera cuando la línea %K cruza umbrales predefinidos.

## Cómo Funciona

- Calcula el Oscilador Estocástico en el marco temporal seleccionado.
- Genera una señal de **compra** cuando %K cruza por encima del nivel inferior (por defecto 30).
- Genera una señal de **venta** cuando %K cruza por debajo del nivel superior (por defecto 70).
- En cada señal, la estrategia cierra cualquier posición opuesta y abre una nueva posición en la dirección de la señal usando órdenes de mercado.
- Se aplican niveles opcionales de stop loss y take profit mediante `StartProtection`.

## Parámetros

| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `KPeriod` | Período de cálculo de la línea %K | 5 |
| `DPeriod` | Período de suavizado de la línea %D | 3 |
| `UpLevel` | Umbral de sobrecompra para activar ventas | 70 |
| `DownLevel` | Umbral de sobreventa para activar compras | 30 |
| `StopLoss` | Stop loss absoluto en unidades de precio | 1000 |
| `TakeProfit` | Take profit absoluto en unidades de precio | 2000 |
| `CandleType` | Marco temporal para cálculos | 1 hora |

## Notas

- La estrategia opera solo en velas finalizadas.
- No calcula los niveles ATR del indicador original; se usaban solo para posicionar flechas en la versión MQL.
- El tamaño de posición se define mediante la propiedad `Volume` de la estrategia.
