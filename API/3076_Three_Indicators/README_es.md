# Estrategia Tres Indicadores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión de StockSharp del expert original **"Three indicators"** de MQL5. Evalúa tres osciladores clásicos—MACD, Oscilador Estocástico y RSI—en cada vela terminada del marco temporal seleccionado. Solo cuando todos los filtros se alinean la estrategia entra en una posición, asegurando que cada operación siga una confirmación multi-indicador consistente.

## Lógica de trading
1. **Filtro de dirección de vela** – compara el precio de apertura de la vela terminada actual con el precio de apertura de la anterior. Una apertura más alta favorece operaciones largas, una apertura más baja favorece cortos.
2. **Filtro de pendiente MACD** – observa la pendiente de la línea principal MACD (diferencia entre el valor MACD principal actual y el anterior). Un MACD en caída favorece posiciones largas, un MACD en ascenso favorece cortos, exactamente como en el experto de origen.
3. **Filtro de sesgo estocástico** – comprueba si el valor %D está por debajo o por encima del punto medio 50. Valores por debajo de 50 apoyan largos, valores por encima de 50 apoyan cortos.
4. **Filtro de sesgo RSI** – usa el valor RSI relativo a 50. Valores por debajo de 50 autorizan largos, valores por encima de 50 autorizan cortos.

Solo si **los cuatro filtros** apoyan la misma dirección abrirá la estrategia una nueva operación. Si aparece una señal opuesta mientras hay una posición abierta, la estrategia revierte inmediatamente enviando una única orden de mercado que cierra la exposición existente y abre la nueva dirección, reflejando el comportamiento de la lógica MQL original.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco temporal de las velas suministradas a la estrategia. Predeterminado: 1 minuto. |
| `TradeVolume` | Volumen usado al abrir una posición o revertir al lado opuesto. |
| `MacdFastPeriod` | Longitud de la EMA rápida dentro del cálculo MACD. |
| `MacdSlowPeriod` | Longitud de la EMA lenta dentro del cálculo MACD. |
| `MacdSignalPeriod` | Longitud de la EMA para la línea de señal MACD. |
| `MacdPriceType` | Precio aplicado al indicador MACD (Close, Open, High, Low, Median, Typical, Weighted). |
| `StochasticKPeriod` | Período de retroceso para la línea %K. |
| `StochasticDPeriod` | Período de suavizado para la línea %D. |
| `StochasticSlowing` | Suavizado adicional aplicado a %K antes de calcular %D. |
| `RsiPeriod` | Período de promediado usado por el filtro RSI. |
| `RsiPriceType` | Precio aplicado al alimentar el indicador RSI. |

## Indicadores
- **MACD (Convergencia/Divergencia de Medias Móviles)** – configurado con los períodos rápido, lento y de señal especificados por el usuario.
- **Oscilador Estocástico** – usa la implementación de StockSharp con longitudes %K/%D y suavizado configurables.
- **Índice de Fuerza Relativa (RSI)** – proporciona la confirmación de impulso final.

## Notas de comportamiento
- La estrategia procesa solo **velas terminadas**, mejorando la estabilidad en comparación con el desencadenador basado en ticks del experto original.
- La pausa de 30 segundos presente en la versión MQL se elimina; las reversiones se emiten inmediatamente con la orden de mercado combinada.
- El suavizado estocástico usa la implementación de media móvil predeterminada de StockSharp, que corresponde al suavizado estándar basado en SMA del script original.
- La selección de fuente de precio para MACD y RSI se proporciona a través del enum `IndicatorAppliedPrice`, coincidiendo con las opciones disponibles en MetaTrader (Close, Open, High, Low, Median, Typical, Weighted).

## Gestión de riesgo
No se colocan órdenes de stop-loss o take-profit automáticamente. La gestión de posición está impulsada exclusivamente por la lógica de reversión multi-indicador. Agregar controles de riesgo externos si se requiere.

## Consejos de uso
1. Seleccionar el instrumento y marco temporal deseados a través de `CandleType`.
2. Ajustar los parámetros del indicador para adecuarse a la volatilidad del mercado y la frecuencia de señales.
3. Monitorear los objetos de gráfico añadidos por la estrategia (velas más los tres indicadores) para validar la alineación de señales.
4. Combinar con gestión de dinero externa si se requieren stops fijos o objetivos de beneficio.
