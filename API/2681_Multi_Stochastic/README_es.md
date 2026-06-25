# Estrategia Multi Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Multi Stochastic es una implementación de alto nivel de StockSharp del asesor experto de MetaTrader 5 "Multi Stochastic (barabashkakvn's edition)". Monitorea hasta cuatro pares de divisas simultáneamente y se basa en señales sincronizadas de las lecturas del Oscilador Stochastic (5, 3, 3). La estrategia abre una única posición de mercado por símbolo cuando ocurre un cruce desde zona de sobreventa o sobrecompra y cierra las operaciones mediante objetivos fijos de stop-loss y take-profit basados en pips.

## Lógica de trading
- Cada símbolo configurado recibe su propio Oscilador Stochastic (longitud 5, suavizado %K 3, suavizado %D 3).
- Una señal larga se produce cuando el %K actual está por debajo del OversoldLevel (predeterminado 20), la barra anterior tenía %K por debajo de %D, y la barra actual cierra con %K cruzando hacia arriba %D.
- Una señal corta se produce cuando el %K actual está por encima del OverboughtLevel (predeterminado 80), la barra anterior tenía %K por encima de %D, y la barra actual cierra con %K cruzando hacia abajo %D.
- Solo se permite una posición abierta por instrumento. Las señales adicionales se ignoran hasta que la posición existente esté cerrada.

## Gestión de riesgo
- Los valores de stop-loss y take-profit se expresan en pips. La estrategia convierte automáticamente pips a distancias de precio absolutas multiplicando por el paso de precio del instrumento y ajustando para cotizaciones forex de 3 o 5 dígitos (pip = paso × 10 para esos instrumentos).
- Las posiciones largas se cierran cuando el mínimo de la vela toca el nivel de stop-loss o el máximo de la vela alcanza el nivel de take-profit.
- Las posiciones cortas se cierran cuando el máximo de la vela toca el nivel de stop-loss o el mínimo de la vela alcanza el nivel de take-profit.

## Parámetros
- `CandleType` – marco temporal usado para todas las velas suscritas (predeterminado: 1 hora).
- `StochasticLength` – longitud base del Oscilador Stochastic (predeterminado: 5).
- `StochasticKPeriod` – período de suavizado para %K (predeterminado: 3).
- `StochasticDPeriod` – período de suavizado para %D (predeterminado: 3).
- `OversoldLevel` – umbral usado para detectar condiciones de sobreventa (predeterminado: 20).
- `OverboughtLevel` – umbral usado para detectar condiciones de sobrecompra (predeterminado: 80).
- `StopLossPips` – distancia al stop protector en pips (predeterminado: 50).
- `TakeProfitPips` – distancia al objetivo de beneficio en pips (predeterminado: 10).
- `UseSymbol1` … `UseSymbol4` – habilita el trading para el slot de símbolo respectivo (predeterminado: true).
- `Symbol1` … `Symbol4` – instrumentos negociados por cada slot. Symbol 1 utiliza el instrumento principal de la estrategia cuando no se especifica.

## Notas de implementación
- Cada suscripción de símbolo es independiente. Cada una usa `SubscribeCandles` con `BindEx` para recibir actualizaciones de `StochasticOscillatorValue` junto con los datos de velas.
- Los valores anteriores de %K y %D se almacenan en caché por símbolo para emular la lógica de detección de cruce de MT5.
- Los parámetros de riesgo se recalculan para cada entrada, y los niveles de stop/take se reinician después de que una posición se cierra o cuando no existe ninguna posición.
- Las órdenes se envían con `BuyMarket`/`SellMarket` usando la propiedad `Volume` compartida, cumpliendo la restricción de posición única del experto original.

## Diferencias con la versión MT5
- La versión StockSharp aprovecha suscripciones de alto nivel en lugar de llamadas manuales de actualización de tasas.
- La detección del tamaño del pip se basa en `Security.PriceStep` y `Security.Decimals`. Si los metadatos no están disponibles, los stops y objetivos permanecen deshabilitados para evitar cálculos de riesgo incorrectos.
- Los hooks de registro y dibujo de gráficos están listos para su extensión pero no son necesarios para el comportamiento principal.

## Consejos de uso
1. Asigna los instrumentos deseados a los slots de símbolos y ajusta el marco temporal de velas para que coincida con tu horizonte de trading.
2. Asegúrate de que las distancias de stop-loss y take-profit sean compatibles con el tamaño del tick del instrumento para evitar cierres inmediatos.
3. Deshabilita los slots de símbolos no usados para reducir el consumo de recursos cuando se monitorean menos instrumentos.
