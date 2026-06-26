# Estrategia de Retroceso Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión de StockSharp del asesor experto de MetaTrader **"ICHMOKU RETRACEMENT"**. Mantiene la idea original de operar retrocesos de Ichimoku que ocurren dentro de una tendencia de marco temporal superior mientras son filtrados por lecturas de momentum a largo plazo y del MACD. La implementación de StockSharp se enfoca en la claridad, la reutilización de indicadores y el control de riesgo a través de la API de alto nivel.

## Idea de trading

1. **Filtro de tendencia** – la estrategia busca un sesgo alcista o bajista usando un par de Medias Móviles Linealmente Ponderadas (LWMA). Un contexto alcista requiere que la LWMA rápida esté por encima de la LWMA lenta, mientras que un contexto bajista requiere la relación opuesta.
2. **Retroceso Ichimoku** – después de detectar una tendencia, la vela anterior debe tocar cualquiera de las líneas de Ichimoku (Tenkan-sen, Kijun-sen o los dos tramos de avance). La vela actual debe volver a abrirse en el lado de la tendencia de la línea tocada, señalando un retroceso de momentum.
3. **Confirmación de momentum** – la relación de momentum de cierre a cierre debe desviarse de su valor neutral (100) al menos un umbral configurable. La relación se calcula en el mismo marco temporal usado para el indicador Ichimoku.
4. **Filtro macro** – un MACD mensual (12/26/9) confirma la dirección dominante a largo plazo. Las operaciones largas requieren la línea principal del MACD por encima de la línea de señal, las cortas requieren lo contrario.
5. **Gestión de órdenes** – la estrategia mantiene como máximo una posición neta. Los niveles de stop-loss y take-profit de protección se colocan en pips y se evalúan en cada vela terminada.

## Parámetros

| Nombre | Descripción | Por defecto |
|--------|-------------|-------------|
| `Signal Candle Type` | Marco temporal usado para los cálculos de LWMA, Ichimoku y momentum. | Velas de 1 hora |
| `Macro Candle Type` | Marco temporal superior usado para el filtro de tendencia MACD. | Velas de 30 días |
| `Fast LWMA` | Período para la media móvil linealmente ponderada rápida. | 6 |
| `Slow LWMA` | Período para la media móvil linealmente ponderada lenta. | 85 |
| `Tenkan Period` | Período de Ichimoku Tenkan-sen. | 9 |
| `Kijun Period` | Período de Ichimoku Kijun-sen. | 26 |
| `Span B Period` | Período de Ichimoku Senkou Span B. | 52 |
| `Momentum Period` | Lookback para la relación de momentum de cierre a cierre. | 14 |
| `Momentum Threshold` | Desviación absoluta mínima de 100 requerida por la relación de momentum. | 0.3 |
| `Take Profit (pips)` | Distancia del take-profit expresada en pips. | 50 |
| `Stop Loss (pips)` | Distancia del stop-loss expresada en pips. | 20 |

El parámetro base `Volume` controla el tamaño de las nuevas órdenes. Cuando aparece una señal de reversión, la estrategia cierra la posición actual (si la hay) y abre una nueva posición en la dirección opuesta usando contratos `Volume + |Position|`.

## Reglas de trading

### Entradas largas
- LWMA rápida > LWMA lenta.
- Línea principal MACD > línea de señal MACD en el marco temporal macro.
- Desviación de la relación de momentum ≥ umbral.
- El mínimo de la vela anterior tocó al menos un nivel de Ichimoku y la vela actual abrió de vuelta por encima de ese nivel.
- La posición neta debe ser plana o corta.

### Entradas cortas
- LWMA rápida < LWMA lenta.
- Línea principal MACD < línea de señal MACD en el marco temporal macro.
- Desviación de la relación de momentum ≥ umbral.
- El máximo de la vela anterior tocó al menos un nivel de Ichimoku y la vela actual abrió de vuelta por debajo de ese nivel.
- La posición neta debe ser plana o larga.

### Salidas
- Una posición larga se cierra cuando el mínimo de la vela alcanza el stop-loss o el máximo alcanza el nivel de take-profit.
- Una posición corta se cierra cuando el máximo de la vela alcanza el stop-loss o el mínimo alcanza el nivel de take-profit.

## Diferencias vs. EA original

- Las escalas de gestión de dinero, los movimientos de break-even y las características de trailing de la versión MQL no se replican; el control de riesgo se limita a niveles fijos de stop-loss y take-profit.
- StockSharp trabaja con una posición neta única, por lo que la pila de órdenes martingala se reemplaza por una entrada por dirección.
- Se omiten las alertas, el correo electrónico y las notificaciones push del entorno MetaTrader.

## Notas de uso

1. Agregar la estrategia a un proyecto StockSharp Designer o Shell.
2. Seleccionar el instrumento deseado y ajustar el `Signal Candle Type` para que coincida con el marco temporal objetivo.
3. Asegurarse de que el `Macro Candle Type` pueda sintetizarse a partir de los datos disponibles (la suscripción usa `allowBuildFromSmallerTimeFrame`).
4. Ajustar el stop-loss, el take-profit y el umbral de momentum según la volatilidad del instrumento.

Los comentarios incluidos describen cada paso de decisión para que la lógica pueda adaptarse o extenderse fácilmente.
