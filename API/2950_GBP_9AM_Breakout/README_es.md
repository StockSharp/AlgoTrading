# Estrategia de Ruptura GBP a las 9 AM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de Ruptura GBP a las 9 AM** replica el antiguo asesor experto de MetaTrader "GBP9AM" en StockSharp. El sistema prepara un straddle alrededor de la apertura de Londres (9:00 hora local) colocando órdenes buy-stop y sell-stop a distancias configurables del precio actual. Su objetivo es capturar el movimiento de impulso posterior a la apertura, manteniendo una gestión de riesgo disciplinada mediante niveles de stop-loss y take-profit medidos en pips.

## Lógica de trading

1. La estrategia monitorea velas terminadas de un marco temporal configurable (1 minuto por defecto) para trabajar con marcas de tiempo del mercado.
2. Cada nuevo día de trading restablece el estado de configuración para que solo se prepare un straddle por sesión.
3. Una vez que el tiempo de la vela alcanza el "Look Hour" y el "Look Minute" configurados, la estrategia:
   - Cancela cualquier orden activa restante y cierra posiciones abiertas para evitar conflictos.
   - Calcula los precios de entrada, stop-loss y take-profit ajustados en pips utilizando el paso de precio del instrumento.
   - Coloca tanto una orden buy-stop como una sell-stop a las distancias en pips especificadas del último precio de cierre.
4. Cuando un lado se ejecuta, la orden pendiente opuesta se cancela de inmediato. La estrategia luego sigue la acción del precio para salir de la posición una vez que se alcanza el nivel de stop-loss o take-profit intradía.
5. Un "Close Hour" diario opcional obliga a la estrategia a aplanar posiciones y eliminar órdenes pendientes al final de la sesión de Londres.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Tamaño de orden utilizado para ambos lados del straddle.
| `LookHour` | Hora del mercado (0-23) que representa las 9 AM de Londres en su fuente de datos.
| `LookMinute` | Offset de minutos dentro del look hour cuando se deben preparar las órdenes.
| `CloseHour` | Hora en la que todas las posiciones y órdenes se cierran forzosamente.
| `UseCloseHour` | Activa o desactiva el comportamiento de cierre automático por hora.
| `TakeProfitPips` | Distancia en pips desde el precio de entrada hasta el objetivo de beneficio para ambas direcciones.
| `BuyDistancePips` | Distancia en pips por encima del precio actual para la orden buy-stop.
| `SellDistancePips` | Distancia en pips por debajo del precio actual para la orden sell-stop.
| `BuyStopLossPips` | Distancia de stop-loss en pips para posiciones largas.
| `SellStopLossPips` | Distancia de stop-loss en pips para posiciones cortas.
| `CandleType` | Suscripción de velas utilizada para timing y gestión de salida (por defecto marco temporal de 1 minuto).

Todas las distancias en pips se adaptan automáticamente a cotizaciones FX de 3 o 5 dígitos multiplicando el paso de precio del mercado por diez cuando es necesario, replicando el asesor experto original.

## Gestión de riesgos

- La estrategia siempre emite objetivos simétricos de stop-loss y take-profit alrededor del precio de activación para mantener un perfil de riesgo equilibrado.
- La liquidación al final del día garantiza que la cuenta no tenga exposición nocturna a menos que el parámetro `UseCloseHour` esté desactivado.
- Dado que las órdenes se reemiten solo una vez por día, la estrategia evita el sobretrading durante sesiones de rango.

## Notas de uso

1. Establezca `LookHour` para que coincida con las 9 AM hora de Londres en la zona horaria de su broker. Por ejemplo, si el feed es UTC+1, use `LookHour = 10`.
2. Calibre las distancias en pips para adaptarse a la volatilidad actual del GBP/USD o su par GBP preferido.
3. Implemente la estrategia en símbolos FX que expongan bid/ask confiable y metadatos de paso de precio para que los cálculos de pips sean precisos.
4. Monitoree los márgenes del broker: valores más grandes de `Volume` pueden requerir ajustes en el apalancamiento de la cuenta, como lo hacía la versión MQL original.

## Archivos

- `CS/Gbp9AmBreakoutStrategy.cs` – Implementación en C# usando el API de alto nivel de StockSharp.
- `README.md` – Documentación en inglés (este archivo).
- `README_ru.md` – Documentación en ruso.
- `README_zh.md` – Documentación en chino.

La implementación en Python se omite intencionalmente según los requisitos del proyecto.
