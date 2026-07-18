# Estrategia de Ruptura de Canal Daydream
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Daydream Channel Breakout es una conversión directa del asesor experto original de MetaTrader "Daydream" al framework de estrategias de alto nivel de StockSharp. La lógica opera contra movimientos extremos: cuando el precio perfora la banda inferior del canal Donchian, el algoritmo compra esperando un rebote; cuando el precio se extiende por encima de la banda superior, abre exposición corta. Todas las salidas se gestionan mediante un take profit "virtual" expresado en pips, por lo que no quedan órdenes nativas en el libro de la bolsa.

## Lógica de la Estrategia

- Construir un canal Donchian a partir de las `ChannelPeriod` velas completadas anteriores (la barra actual queda excluida, igualando la implementación de MT5).
- Entrar **largo** cuando el precio de cierre cae por debajo de la banda inferior anterior. La exposición corta existente se aplana implícitamente porque el volumen de la orden incluye el tamaño absoluto de la posición.
- Entrar **corto** cuando el precio de cierre rompe por encima de la banda superior anterior. La exposición larga existente se cierra de la misma manera.
- Solo se permite una entrada por vela. Tras enviar una orden, la estrategia espera la apertura de la siguiente barra para generar una nueva señal.
- Cada posición abierta se monitorea en busca de un objetivo de beneficio virtual. Cuando el beneficio no realizado supera `TakeProfitPips` (convertido a distancia de precio mediante la heurística del tamaño del pip), la posición se cierra a mercado.

## Parámetros

| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `OrderVolume` | Tamaño de lote enviado con cada nueva operación. El importe real de la orden también incluye el valor absoluto de la posición contraria para aplanar antes de revertir. | `0.1` | Coincide con el tamaño de lote predeterminado en MT5. |
| `TakeProfitPips` | Distancia de take profit virtual expresada en pips. | `50` | El tamaño del pip se deriva de `Security.PriceStep`; los instrumentos de 3 o 5 dígitos se multiplican automáticamente por 10. |
| `ChannelPeriod` | Número de velas completadas utilizadas para calcular el canal Donchian. | `25` | Usa el mismo período de retrospectiva que el EA original. |
| `CandleType` | Tipo de vela suscrita para los cálculos. | `TimeSpan.FromHours(1).TimeFrame()` | Se puede cambiar a cualquier tipo de vela de StockSharp. |

## Flujo de Señales

1. **Suscripción de datos**: la estrategia se suscribe al tipo de vela proporcionado mediante el parámetro `CandleType` y vincula un indicador de canal Donchian usando `BindEx`.
2. **Verificación de take profit virtual**: la primera acción en cada vela terminada es medir la distancia entre el precio de cierre y el precio de entrada promedio. Si se alcanza el umbral, la posición se cierra y no se evalúa ninguna nueva entrada para esa barra.
3. **Actualización del canal**: una vez que las bandas superior e inferior están disponibles, los valores anteriores se almacenan en caché para reflejar la lógica "shift=1" de MQL. Las señales usan la banda anterior, no la actualizada con la vela actual.
4. **Decisión de entrada**:
   - Precio < banda inferior anterior → comprar `OrderVolume + Math.Max(0, -Position)`.
   - Precio > banda superior anterior → vender `OrderVolume + Math.Max(0, Position)`.
5. **Registro y visualización**: se generan mensajes de registro informativos para cada entrada y salida por take profit. Si hay un área de gráfico disponible en Designer u otros productos de StockSharp, las velas, el canal Donchian y las operaciones se dibujan automáticamente.

## Gestión de Riesgos

- Solo se implementa un take profit virtual. No existe stop-loss ni salida trailing en el algoritmo original, por lo que el riesgo debe controlarse externamente (por ejemplo, con protecciones a nivel de cartera).
- Dado que las órdenes revierten sumando la posición absoluta, la estrategia puede piramidear en la misma dirección si aparecen señales consecutivas en diferentes velas.
- El asistente de tamaño de pip multiplica el paso de precio por diez para símbolos de 3 o 5 dígitos para emular la conversión `Point()` a pip de MT5. Para instrumentos con tamaños de tick no convencionales, puede anular la lógica o usar una distancia personalizada ajustando `TakeProfitPips`.

## Notas de Uso

- La estrategia está diseñada para un comportamiento de reversión a la media. Funciona mejor en mercados en rango donde los movimientos sobreextendidos tienden a revertir.
- Los backtests deben incluir ajustes realistas de spread y comisión, porque las entradas ocurren en órdenes de mercado después de rupturas del canal.
- Considere combinar la estrategia con filtros de sesión o stops basados en volatilidad cuando opere en mercados en vivo.
- La implementación depende exclusivamente de la API de alto nivel de StockSharp (sin colecciones de indicadores manuales ni descargas históricas), por lo que es compatible con Designer, Shell y Runner sin modificaciones.
