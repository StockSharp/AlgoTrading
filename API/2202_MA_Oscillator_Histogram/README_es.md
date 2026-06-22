# Estrategia de Histograma del Oscilador MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una traducción del experto MQL5 **Exp_MAOscillatorHist.mq5**. Utiliza la diferencia entre una Media Móvil Simple (SMA) rápida y una lenta para formar un oscilador. Las señales de trading se generan cuando el oscilador forma mínimos o máximos locales, que se interpretan como posibles reversiones de tendencia.

## Lógica de trading
1. Se calculan dos SMA en el marco temporal de velas seleccionado:
   - **SMA rápida** con un período más corto.
   - **SMA lenta** con un período más largo.
2. El valor del oscilador es la SMA rápida menos la SMA lenta.
3. La estrategia rastrea los tres últimos valores del oscilador. Un mínimo local ocurre cuando el valor más antiguo es mayor que el anterior y el anterior es menor que el actual. Un máximo local es lo opuesto.
4. Cuando se detecta un mínimo local:
   - Cerrar posiciones cortas (si está permitido).
   - Abrir una nueva posición larga (si está permitido).
5. Cuando se detecta un máximo local:
   - Cerrar posiciones largas (si está permitido).
   - Abrir una nueva posición corta (si está permitido).

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| **Fast Period** | Período de la SMA rápida. |
| **Slow Period** | Período de la SMA lenta. |
| **Enable Buy Open** | Si es verdadero, se pueden abrir posiciones largas. |
| **Enable Sell Open** | Si es verdadero, se pueden abrir posiciones cortas. |
| **Enable Buy Close** | Si es verdadero, las posiciones largas se pueden cerrar con señales opuestas. |
| **Enable Sell Close** | Si es verdadero, las posiciones cortas se pueden cerrar con señales opuestas. |
| **Candle Type** | Marco temporal de las velas utilizadas para los cálculos. |

## Notas
- La estrategia utiliza la API de alto nivel de StockSharp con `SubscribeCandles` y vinculación de indicadores.
- `StartProtection` está habilitado con órdenes de mercado para una ejecución más segura.
- No se proporciona versión en Python.
