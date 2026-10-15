# Estrategia de Trend Is Your Friend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Trend Is Your Friend es un sistema de seguimiento de tendencia multitemporal inspirado en el asesor experto de MetaTrader original. Alinea el impulso intradía con un filtro MACD de marco temporal superior, mientras el riesgo se gestiona mediante salidas de Bandas de Bollinger, objetivos clásicos de stop-loss y take-profit, un bloqueo de break-even opcional y gestión de trailing stop.

La estrategia trabaja en un marco temporal base configurable (predeterminado: 1 hora) y analiza la estructura de velas para un patrón de impulso a corto plazo: una vela bajista seguida de una vela alcista más fuerte para operaciones largas, o el inverso para operaciones cortas. Estos patrones deben estar de acuerdo con un filtro de tendencia de media móvil y una señal MACD mensual antes de que se abra una posición.

## Lógica de entrada
1. Calcular una EMA rápida y una LWMA lenta en el marco temporal de entrada.
2. Rastrear las últimas dos velas completadas para formar un patrón de impulso:
   - **Configuración larga:** la vela de hace dos barras es bajista, la vela anterior es alcista y de mayor magnitud.
   - **Configuración corta:** la vela de hace dos barras es alcista, la vela anterior es bajista y de menor magnitud.
3. Confirmar la configuración con el filtro de tendencia de media móvil (MA rápida por encima de la MA lenta para operaciones largas, por debajo para cortas).
4. Confirmar la tendencia a largo plazo con una señal MACD calculada en el marco temporal superior (predeterminado: mensual). La línea MACD debe estar por encima de la línea de señal para operaciones largas y por debajo para cortas.
5. Cuando todos los filtros se alinean, abrir una posición al mercado con el volumen configurado.

## Lógica de salida
- **Salida por Bandas de Bollinger:** las posiciones largas se cierran cuando el precio cierra por encima de la banda superior; las posiciones cortas cuando el precio cierra por debajo de la banda inferior.
- **Take-profit / stop-loss:** distancias fijas opcionales medidas en pips. La implementación convierte pips a distancia de precio mediante el paso de precio del valor.
- **Break-even:** opcional, mueve el stop de protección al precio de entrada (o más allá) después de que se haya alcanzado un umbral de ganancia configurable.
- **Trailing stop:** opcional, se activa después de un umbral de ganancia y sigue el precio por una distancia fija de pip. El trailing stop comparte el mismo almacenamiento con el nivel de break-even.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| Entry Candle | Tipo de vela para la lógica de entrada | 1 hora |
| MACD Candle | Marco temporal superior usado para el filtro MACD | 30 días |
| Fast MA | Longitud de la EMA rápida | 8 |
| Slow MA | Longitud de la LWMA lenta | 20 |
| Bollinger Length | Período de las Bandas de Bollinger | 20 |
| Bollinger Width | Multiplicador de desviación estándar de las Bandas de Bollinger | 2.0 |
| Stop Loss (pips) | Distancia de stop de protección | 20 |
| Take Profit (pips) | Distancia del objetivo de ganancia | 50 |
| Use Break-Even | Habilitar ajuste de break-even | true |
| Break-Even Trigger | Ganancia (pips) requerida para mover el stop | 10 |
| Break-Even Offset | Desplazamiento aplicado al stop de break-even | 5 |
| Use Trailing | Habilitar trailing stop | true |
| Trailing Activation | Ganancia (pips) requerida para activar el trailing | 40 |
| Trailing Distance | Distancia (pips) mantenida por el trailing stop | 40 |

## Notas
- La estrategia almacena solo las últimas dos velas completadas para evitar búferes históricos pesados.
- Los datos MACD se suscriben desde el marco temporal superior configurado con agregación habilitada, permitiendo que las señales mensuales se construyan a partir de datos diarios cuando sea necesario.
- La conversión de pip a precio usa el paso de precio del valor. Los instrumentos con definiciones de pip no estándar pueden requerir ajuste de parámetros.
