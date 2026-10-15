# Estrategia Multi Arbitración
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Multi Arbitración** es un port StockSharp del asesor experto MetaTrader "Multi_arbitration 1.000". El script original evalúa continuamente las posiciones de compra y venta existentes, añade nuevas operaciones en la dirección con menor beneficio flotante, y realiza una liquidación global una vez que se alcanzan los objetivos generales de beneficio. Esta implementación en C# mantiene la lógica de decisión central adaptándola al modelo de portfolio de compensación de StockSharp y a la API de estrategias de alto nivel.

La estrategia:
- Abre una posición larga inicial tan pronto como llega la primera vela finalizada.
- Compara el beneficio no realizado de la dirección activa con la dirección alternativa para decidir si se requiere un giro.
- Fuerza una posición plana cuando se supera el objetivo de beneficio configurado o cuando la presión de posición crece más allá de un límite configurable.
- Utiliza solo órdenes de mercado (`BuyMarket` / `SellMarket`) para mantener simplicidad y ejecución rápida.

## Lógica de Trading
1. **Orden inicial** – La primera vela finalizada activa una orden de mercado larga con el volumen de operación configurado. Esto reproduce la entrada inmediata al mercado del experto MetaTrader.
2. **Comparación de beneficios** – En cada vela finalizada, la estrategia calcula el PnL flotante de la dirección actual:
   - Beneficio largo = `(close - entry) * volume`
   - Beneficio corto = `(entry - close) * volume`
3. **Selección de posición** – Si la dirección alternativa funcionaría mejor actualmente que la activa, la estrategia gira la posición enviando una orden de mercado dimensionada para cubrir la exposición existente y abrir una nueva posición en la nueva dirección. Cuando no hay posición abierta, el algoritmo por defecto realiza una entrada larga, coincidiendo con el asesor experto original.
4. **Guardia de límite de posiciones** – Un parámetro configurable `MaxOpenPositions` refleja la comprobación MetaTrader contra `LimitOrders()`. Cuando la exposición combinada larga/corta alcanza este límite y la estrategia es rentable, aplana el libro para evitar el apalancamiento excesivo.
5. **Salida por objetivo de beneficio** – Cuando el PnL de la cuenta (realizado + no realizado) supera el umbral `ProfitForClose`, la estrategia cierra todas las posiciones, exactamente como la comprobación original `Equity - Balance`.

## Parámetros
| Nombre | Descripción | Por defecto |
| ---- | ----------- | ------- |
| `TradeVolume` | Volumen utilizado para cada orden de mercado. Representa el tamaño mínimo de lote en el EA original. | `1` |
| `ProfitForClose` | Umbral de beneficio que activa una salida global una vez superado. | `300` |
| `MaxOpenPositions` | Número máximo de posiciones simultáneas permitidas antes de que la estrategia fuerce un aplanamiento. Actúa como equivalente a `limit - 15`. | `15` |
| `CandleType` | Tipo de datos de vela que sincroniza las decisiones de operación. Por defecto es marco temporal de 1 minuto. | `velas de 1 minuto` |

## Notas de Implementación
- StockSharp usa un modelo de posición de compensación, por lo que la estrategia solo puede mantener una dirección neta a la vez. Los giros se manejan dimensionando las órdenes de mercado para cerrar la exposición existente y abrir una nueva posición en la dirección opuesta.
- La llamada `StartProtection()` se utiliza para heredar el manejo de riesgo incorporado (por ejemplo, stop-out en posiciones distintas de cero cuando se detiene la estrategia).
- Todas las variables de estado (`_entryPrice`, `_currentSide`, `_initialOrderPlaced`) se reinician en `OnReseted` para soportar reinicios y simulaciones repetidas sin datos obsoletos.
- La estrategia solo reacciona a **velas finalizadas** para evitar el doble conteo de beneficios en barras parcialmente formadas.

## Recomendaciones de Uso
- Alinee el parámetro `TradeVolume` con el tamaño de lote del instrumento o el multiplicador del contrato.
- El valor `ProfitForClose` debe establecerse usando la misma moneda que el PnL de la cuenta (por ejemplo, USD para cuentas FX).
- Aumente o disminuya `MaxOpenPositions` dependiendo de cuán agresivamente quiera que la estrategia acumule exposición antes de forzar un aplanamiento.
- Debido a que la estrategia siempre comienza con una operación larga, considere iniciarla manualmente cuando las entradas largas sean aceptables para el instrumento negociado.

## Diferencias con la Versión MetaTrader
- El modo de cobertura de MetaTrader permite posiciones largas y cortas simultáneas, mientras que este port opera en un entorno de compensación. La lógica de decisión aún compara la rentabilidad direccional, pero solo se mantiene una posición neta en cualquier momento.
- Las comprobaciones específicas de la plataforma (permisos de trading del terminal, selección del tipo de relleno, números mágicos de cuenta) se reemplazan con equivalentes StockSharp como `StartProtection()` y suscripciones de velas.
- Los diagnósticos comentados del archivo MQL no se reproducen; confíe en el registro StockSharp si se requiere información en tiempo de ejecución.
