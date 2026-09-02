# Diagrama de la estrategia de ruptura por zonas de Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El nombre habla de ruptura, pero lo que se opera es el rebote: el diagrama espera una vela cuya zona inferior haya atravesado la banda inferior de Bollinger mientras el mercado sigue por encima de su EMA 50, y compra esa caída. La imagen simétrica vende un pico por encima de la banda superior. La posición se abandona en cuanto el precio vuelve a la banda media. La confirmación por RSI del código original (por debajo de 45 para largos y por encima de 55 para cortos) se omite aquí para que el diagrama siga siendo legible: apenas restringe una señal que ya exige una vela más allá de la banda.

![schema](schema.svg)

## Resumen de la estrategia

- Las bandas de Bollinger (20, 1.5) marcan el borde estirado del rango en velas de 30 minutos, mientras que la EMA 50 indica de qué lado de la tendencia está el mercado.
- En lugar de comparar un solo precio con la banda, el diagrama construye una zona de penetración a partir de la propia vela: el 30% del rango de la vela medido hacia arriba desde su mínimo para largos y hacia abajo desde su máximo para cortos.
- Solo se entra desde posición plana y la banda media de Bollinger es la única salida para ambas direcciones.

## Reglas de entrada y salida

- **Entrada en largo**: La zona mínimo + 30% del rango de la vela queda por debajo de la banda inferior de Bollinger, la vela es bajista (cierre por debajo de la apertura), el cierre está por encima de la EMA 50 y la posición es plana. Se compra un lote a mercado.
- **Entrada en corto**: La zona máximo - 30% del rango de la vela queda por encima de la banda superior de Bollinger, la vela es alcista (cierre por encima de la apertura), el cierre está por debajo de la EMA 50 y la posición es plana. Se vende un lote a mercado.
- **Salida**: Un largo se cierra en la primera vela que cierra en la banda media o por encima de ella, y un corto en la primera que cierra en la banda media o por debajo; ambas salidas son bloques de cierre de posición, así que cada una actúa solo sobre el lado realmente abierto.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Bollinger Length | 20 | Periodo de suavizado de las bandas de Bollinger. |
| Bollinger Width | 1.5 | Multiplicador de la desviación típica de las bandas; 1.5 las mantiene estrechas, de modo que las velas las alcanzan a menudo. |
| EMA Length | 50 | Periodo de la EMA que decide el lado de la tendencia. |
| Candle Zone, share of range | 0.3 | Parte del rango de la vela que debe quedar más allá de la banda para considerarla penetrada. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:30:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Cuatro bloques convertidores extraen apertura, máximo, mínimo y cierre de la vela; otros tres leen las bandas superior, inferior y media.
- Dos bloques de fórmula construyen las zonas de penetración, mínimo + (máximo - mínimo) * porcentaje y máximo - (máximo - mínimo) * porcentaje, a partir de una misma constante.
- Cada Y lógica reúne cuatro señales: la zona más allá de la banda, la dirección de la vela, el lado de la EMA y la posición plana obtenida al comparar el bloque de posición con cero.
- El par de comparaciones de salida contrasta el cierre con la banda media y acciona dos bloques de cierre, dejando el diagrama libre para la siguiente señal.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
