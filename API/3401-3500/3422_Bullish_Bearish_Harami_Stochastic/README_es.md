# Estrategia Harami alcista y bajista Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Harami alcista y bajista Stochastic** es el puerto StockSharp del Expert Advisor de MetaTrader `expert_abh_bh_stoch.mq5` de la carpeta `MQL/310`. El experto original utiliza el reconocimiento de patrones de velas para configuraciones Bullish Harami y Bearish Harami y requiere una confirmación del oscilador estocástico. La versión C# mantiene la misma lógica usando StockSharp API de alto nivel y agrega registros detallados y resultados de gráficos para facilitar el monitoreo.

## Ideas centrales

- Detecte patrones de velas Harami alcistas y Harami bajistas utilizando las dos velas completadas anteriores.
- Confirme configuraciones alcistas con la línea estocástica %D por debajo de un umbral de sobreventa y configuraciones bajistas con %D por encima de un umbral de sobrecompra.
- Cierre las posiciones cortas cuando la línea estocástica %D rebote por encima de los umbrales de salida inferior o superior, y cierre las posiciones largas cuando %D caiga por debajo de esos umbrales.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Periodo de tiempo de la serie de velas utilizada para el reconocimiento de patrones. | `1 Hour` |
| `StochasticKPeriod` | Período retrospectivo para el cálculo estocástico de %K. | `47` |
| `StochasticDPeriod` | Período de suavizado para la línea %D. | `9` |
| `StochasticSlowing` | Suavizado adicional aplicado a %K (“desaceleración” MT5). | `13` |
| `MovingAveragePeriod` | Número de velas utilizadas para promediar el tamaño corporal para la validación del patrón. | `5` |
| `OversoldLevel` | Stochastic %D umbral para confirmar señales alcistas. | `30` |
| `OverboughtLevel` | Stochastic %D umbral para confirmar señales bajistas. | `70` |
| `ExitLowerLevel` | Nivel estocástico inferior que desencadena salidas. | `20` |
| `ExitUpperLevel` | Nivel estocástico superior que desencadena salidas. | `80` |

## Reglas de trading

### Entrada larga
1. Se detecta un patrón Harami alcista en las dos velas completadas más recientes (una pequeña vela alcista envuelta por una vela bajista más larga en una tendencia bajista).
2. La línea estocástica %D de la vela de confirmación está en `OversoldLevel` o menos.
3. Actualmente no hay ninguna posición larga abierta (`Position <= 0`).
4. La estrategia compra en el mercado por el `Volume` configurado, agregando cualquier exposición corta para invertir la posición si es necesario.

### Entrada corta
1. Se detecta un patrón Harami bajista (pequeña vela bajista dentro de una vela alcista larga durante una tendencia alcista).
2. El valor estocástico %D es igual o superior a `OverboughtLevel`.
3. No existe ninguna exposición corta (`Position >= 0`).
4. La estrategia vende en el mercado, cubriendo primero cualquier posición larga si es necesario.

### Salidas
- **Cubrir cortos:** Cuando el estocástico %D cruza hacia arriba a través de `ExitLowerLevel` o `ExitUpperLevel`, el algoritmo cubre toda la posición corta.
- **Cerrar posiciones largas:** Cuando el estocástico %D cruza hacia abajo a través de `ExitUpperLevel` o `ExitLowerLevel`, la posición larga se cierra.

## Archivos

- `CS/BullishBearishHaramiStochasticStrategy.cs`: implementación StockSharp de alto nivel de la estrategia.
- `README.md` — Documentación en inglés (este archivo).
- `README_ru.md` — Documentación rusa.
- `README_zh.md` — Documentación china.

> **Nota:** La versión de Python no está incluida según las instrucciones de conversión.
