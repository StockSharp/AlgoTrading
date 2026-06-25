# Estrategia VR Overturn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Recrea el asesor experto MetaTrader «VR---Overturn» utilizando las APIs de alto nivel de StockSharp.
- Mantiene solo una posición abierta a la vez y evalúa inmediatamente la siguiente operación una vez que la anterior se cierra.
- Diseñado para traders discrecionales que desean reversión automática de posición con dimensionamiento martingala o anti-martingala.

## Lógica de trading
1. **Posición inicial** – la estrategia abre la primera operación en la dirección configurada (`FirstPositionDirection`) con el volumen base (`BaseVolume`).
2. **Stop loss / take profit** – las órdenes de salida protectoras se adjuntan automáticamente utilizando `StopLossPips` y `TakeProfitPips`. El motor convierte pips en desplazamientos de precio absolutos analizando el paso de precio del instrumento (los instrumentos de 3 y 5 dígitos reciben el ajuste ×10 igual que en el asesor original).
3. **Procesamiento del cierre de posición** – cuando una posición se cierra por cualquier orden protectora, la estrategia registra:
   - Lado de la operación cerrada (largo o corto).
   - Volumen ejecutado.
   - PnL realizado (diferencia entre precio de entrada y de salida).
4. **Dimensionamiento de la siguiente entrada** – el resultado almacenado decide el lado y el tamaño del lote de la siguiente orden.
   - Las operaciones ganadoras mantienen la misma dirección, las perdedoras invierten la dirección.
   - El modo martingala multiplica el tamaño de la posición tras una pérdida y lo restablece al volumen base tras una ganancia.
   - El modo anti-martingala multiplica el tamaño de la posición tras una ganancia y lo restablece al volumen base tras una pérdida.
5. **Redondeo de lote** – el tamaño calculado se ajusta al paso de volumen más cercano del instrumento antes de enviar una orden de mercado.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `FirstPositionDirection` | Dirección de la primera operación (Buy/Sell). | Buy |
| `Mode` | Régimen de dimensionamiento: Martingale (incremento tras pérdidas) o AntiMartingale (incremento tras ganancias). | Martingale |
| `BaseVolume` | Volumen inicial de la posición. Se usa cuando se reinicia una secuencia. | 0.1 |
| `StopLossPips` | Distancia al stop loss en pips. | 30 |
| `TakeProfitPips` | Distancia al take profit en pips. | 90 |
| `LotMultiplier` | Multiplicador aplicado durante el paso de expansión (tras pérdida para martingala, tras ganancia para anti-martingala). | 1.6 |

## Gestión de riesgos
- Utiliza `StartProtection` para adjuntar órdenes de stop-loss y take-profit en cada entrada.
- Las distancias de stop y objetivo son desplazamientos de precio absolutos derivados de los valores de pip configurados.
- No se aplica lógica de trailing adicional, por lo que el riesgo está completamente controlado por las órdenes protectoras y las reglas de reversión de posición.

## Notas operacionales
- La estrategia no depende de velas ni indicadores; reacciona puramente a confirmaciones de operaciones (`OnOwnTradeReceived`).
- Si una orden protectora se ejecuta parcialmente, la estrategia acumula el monto restante hasta que la posición esté plana antes de actuar nuevamente.
- Los valores de comisión y swap no están disponibles en las operaciones de StockSharp, por lo que la comparación de beneficios usa solo la diferencia de precio. Considere ampliar los stops o multiplicadores si su broker cobra comisiones significativas.
- Funciona con cualquier instrumento que proporcione metadatos de paso de precio y volumen; verifique ambos antes de implementar en producción.
