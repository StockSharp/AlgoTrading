# Estrategia Follow Your Heart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una adaptación a StockSharp del asesor experto MetaTrader "Follow Your Heart". Analiza las últimas varias velas y suma los cambios relativos de sus precios de apertura, cierre, máximo y mínimo. Se abre una posición larga cuando todos los cambios están por encima de un umbral y el valor combinado es positivo. Se abre una posición corta en las condiciones opuestas. Solo puede existir una posición a la vez.

Las posiciones están protegidas por límites de beneficio y pérdida medidos en moneda de cuenta y por take-profit/stop-loss en puntos. Las sesiones de trading opcionales permiten señales solo dentro de horas especificadas.

## Parámetros
- `Bars` – número de velas usadas para acumular cambios de precio. Por defecto: 6.
- `Level` – umbral para los cambios de apertura y cierre. Por defecto: 2.3.
- `ProfitBuy` – objetivo de beneficio monetario para salir de posición larga. Por defecto: 75.
- `ProfitSell` – objetivo de beneficio monetario para salir de posición corta. Por defecto: 56.
- `LossBuy` – umbral de pérdida monetaria para salir de posición larga. Por defecto: -54.
- `LossSell` – umbral de pérdida monetaria para salir de posición corta. Por defecto: -51.
- `TakeProfit` – take profit en puntos. Por defecto: 550.
- `StopLoss` – stop loss en puntos. Por defecto: 550.
- `TradingHoursOn` – activar filtrado por sesión. Por defecto: true.
- `OpenHourBuy` / `CloseHourBuy` – horas permitidas para señales de compra. Por defecto: 6 / 12.
- `OpenHourSell` / `CloseHourSell` – horas permitidas para señales de venta. Por defecto: 4 / 10.
- `CandleType` – marco temporal de velas. Por defecto: 1 minuto.

## Lógica de la estrategia
1. Para cada vela terminada se calcula el cambio relativo de apertura, cierre, máximo y mínimo respecto a la vela anterior y se actualizan las sumas móviles.
2. Si no existe ninguna posición:
   - **Compra** cuando la suma total es positiva, los cambios de apertura y cierre están por encima de `Level`, y el cambio de cierre es mayor que el de apertura durante la sesión de compra.
   - **Venta** cuando la suma total es negativa, los cambios de apertura y cierre están por debajo de `-Level`, y el cambio de cierre es menor que el de apertura durante la sesión de venta.
3. Cuando existe una posición, se cierra si el beneficio o la pérdida supera los límites monetarios configurados o si el precio se mueve `TakeProfit`/`StopLoss` puntos.

## Notas
- Solo se usan órdenes de mercado.
- La gestión monetaria del código original está simplificada; el volumen de la posición se toma de la propiedad `Volume` de la estrategia.
