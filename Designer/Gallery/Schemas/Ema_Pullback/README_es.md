# Diagrama de la estrategia de entrada en retroceso a la EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un diagrama de tendencia que se niega a comprar la ruptura. Las dos medias móviles exponenciales deciden la dirección y la entrada espera a que el cierre vuelva a tocar la media rápida, de modo que la posición se abre a mejor precio dentro de un movimiento ya en marcha. La salida la decide la propia tendencia: la posición se cierra en cuanto las medias intercambian sus lugares.

![schema](schema.svg)

## Resumen de la estrategia

- Dos medias móviles exponenciales del cierre, una rápida de 8 y una lenta de 21, definen hacia qué lado se permite operar al diagrama.
- Un bloque de cruce vigila el cierre frente a la media rápida, así el retroceso se captura en la vela exacta en la que el precio vuelve a la media y no en cada vela cercana a ella.
- Entradas y salidas van por ramas separadas: dos bloques de modificación abren con el volumen de la orden y otros dos solo cierran lo que ya se mantiene.

## Reglas de entrada y salida

- **Entrada en largo**: La EMA rápida está por encima de la lenta, el cierre vuelve a bajar hasta la EMA rápida y la posición no es larga. La orden compra Volume más el valor absoluto de la posición actual: abre un largo desde plano o convierte un corto directamente en largo.
- **Entrada en corto**: La EMA rápida está por debajo de la lenta, el cierre vuelve a subir hasta la EMA rápida y la posición no es corta. La orden vende Volume más el valor absoluto de la posición actual: abre un corto desde plano o convierte un largo directamente en corto.
- **Salida**: El largo se cierra cuando la EMA rápida cae por debajo de la lenta, y el corto cuando la rápida sube por encima; ambos bloques de cierre actúan sobre toda la posición abierta, por lo que una señal repetida sin posición no hace nada. No hay stop de protección, tal como está escrita la estrategia original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Fast EMA length | 8 | Periodo de la media móvil exponencial rápida, aquella a la que retrocede el precio. |
| Slow EMA length | 21 | Periodo de la media móvil exponencial lenta, la que marca la dirección de la tendencia. |
| Volume | 1 | Volumen base de la orden, en lotes; en una inversión se le suma la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta ambas medias y un conversor que lee el precio de cierre.
- El bloque de cruce recibe la EMA rápida en su entrada superior y el cierre en la inferior, así su salida verdadera es el cierre volviendo hacia abajo a la media y un NO lógico sobre ella es el cierre volviendo hacia arriba.
- Dos bloques de comparación enfrentan las medias entre sí y otros cuatro comparan la posición con una constante cero compartida, lo que da tanto los filtros de entrada como los de salida.
- La rama de entrada toma su volumen de una fórmula que suma la posición absoluta a la constante de volumen, mientras que los dos bloques de cierre están configurados para cerrar la posición y no necesitan volumen alguno.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
