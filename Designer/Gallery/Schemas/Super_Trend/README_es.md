# Diagrama de la estrategia de giro con Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Supertrend dibuja una única línea que se sitúa por debajo del precio en una tendencia alcista y por encima en una bajista, a una distancia de varios rangos verdaderos medios respecto al precio mediano. El diagrama opera en el momento en que el cierre atraviesa esa línea: compra el paso hacia arriba, vende el paso hacia abajo y mantiene el lado hasta el siguiente giro.

![schema](schema.svg)

## Resumen de la estrategia

- El indicador Supertrend se calcula sobre velas cerradas: el periodo del ATR fija cuán lejos queda la línea del precio y el multiplicador escala esa distancia.
- Un conversor toma el precio de cierre de cada vela y un bloque de cruce lo compara con la línea Supertrend, disparando solo en la vela en la que realmente se cruzan.
- Tras la primera señal la estrategia está siempre en mercado: no hay stop ni objetivo, solo el giro de la línea.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre cruza por encima de la línea Supertrend y la posición todavía no es larga. La orden compra Volume más el valor absoluto de la posición actual: abre un largo desde plano o convierte un corto directamente en largo.
- **Entrada en corto**: El cierre cruza por debajo de la línea Supertrend y la posición todavía no es corta. La orden vende Volume más el valor absoluto de la posición actual: abre un corto desde plano o convierte un largo directamente en corto.
- **Salida**: No hay salida propia ni stop de protección: solo el giro contrario de la línea saca de la posición, invirtiéndola con una única orden.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| ATR period | 10 | Periodo del ATR sobre el que se construye la línea Supertrend. |
| ATR multiplier | 3 | Multiplicador aplicado al ATR, que fija la separación de la línea respecto al precio mediano. |
| Volume | 1 | Volumen base de la orden, en lotes; en una inversión se le suma la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque de indicador con Supertrend y, a través de un conversor, aporta el precio de cierre de esa misma vela.
- Ambos entran en el bloque de cruce, cuya salida es la señal larga, mientras que un NO lógico sobre ella es la señal corta.
- Cada señal se une mediante una Y lógica a la comparación de la posición con cero, de modo que una entrada nunca aumenta una posición ya abierta en ese lado.
- Un bloque de fórmula calcula Volume más la posición en valor absoluto y alimenta la entrada de volumen de ambos bloques de modificación: así una orden a mercado invierte la posición.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
