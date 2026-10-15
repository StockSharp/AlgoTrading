# Estrategia del cazador de resonancia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Resonance Hunter es el StockSharp puerto del MetaTrader asesor experto `Exp_ResonanceHunter`. Supervisa tres pares de divisas correlacionados por ranura y busca impulso sincrónico en sus osciladores Stochastic. Cuando los osciladores resuenan en la misma dirección, la estrategia abre una posición en el símbolo principal, mientras que los símbolos secundario y de confirmación actúan como filtros. La operación se cierra tan pronto como el instrumento principal pierde impulso o cuando se alcanza el stop loss configurado.

Hay tres ranuras preconfiguradas:

1. El EURUSD negoció con EURJPY y USDJPY como confirmaciones.
2. GBPUSD negoció con GBPJPY y USDJPY.
3. AUDUSD negoció con AUDJPY y USDJPY.

Cada ranura se puede habilitar o deshabilitar de forma independiente y puede usar su propio período de tiempo y parámetros de indicador.

## Parámetros
Todos los parámetros están agrupados por ranura (Ranura 1–3). Cada grupo comparte las siguientes configuraciones:

| Parámetro | Descripción |
| --- | --- |
| `{Slot} Enabled` | Permite el comercio de la ranura. |
| `{Slot} Primary` | Instrumento negociado por la estrategia y utilizado para señales de salida. |
| `{Slot} Secondary` | Segundo instrumento que participa en la verificación de resonancia. |
| `{Slot} Confirmation` | Tercer instrumento utilizado en la verificación de resonancia. |
| `{Slot} Candle Type` | Plazo aplicado a los tres instrumentos (predeterminado = 1 hora). |
| `{Slot} K Period` | Stochastic %K retrospectiva. |
| `{Slot} D Period` | Período de suavizado para %D. |
| `{Slot} Slowing` | Suavizado adicional para %K. |
| `{Slot} Volume` | Volumen de pedidos en lotes. La exposición opuesta existente se compensa. |
| `{Slot} Stop Loss` | Distancia de stop-loss estilo MetaTrader en puntos. Establezca en 0 para desactivar la parada de protección. |

## Lógica de trading
1. Para cada instrumento configurado, se calcula un `StochasticOscillator` con los parámetros seleccionados en las velas completadas.
2. Una vez que las últimas velas de los tres instrumentos comparten el mismo tiempo de apertura, se evalúan las diferencias `%K - %D`:
   * La diferencia positiva marca un impulso ascendente (`Up`), la diferencia negativa marca un impulso a la baja (`Down`).
   * Reglas de consistencia adicionales del indicador original ajustan los impulsos comparando la magnitud de cada par.
3. Una **entrada larga** se genera cuando los tres impulsos apuntan hacia arriba. Aparece una **entrada corta** cuando los tres impulsos apuntan hacia abajo.
4. Antes de enviar nuevas órdenes, la estrategia cierra las posiciones existentes si el símbolo principal indica un impulso opuesto (refleja los buffers `UpStop`/`DnStop` del indicador).
5. Después de ingresar una posición, se calcula un precio de parada de protección utilizando el último cierre y la distancia `{Slot} Stop Loss`. En cada nueva vela primaria se comprueba el tope y, si se supera, la posición se cierra inmediatamente.

Los pedidos se enrutan a través de `BuyMarket`/`SellMarket`. La exposición existente en el símbolo principal se compensa para que la estrategia pueda revertirse directamente cuando sea necesario.

## Notas
* La estrategia requiere datos de velas sincronizados para los tres instrumentos dentro de cada ranura. Si un símbolo va por detrás, la señal se pospone hasta que las marcas de tiempo de las barras se alineen.
* Los niveles de stop se emulan dentro de la estrategia (no se envían órdenes de stop reales) para que coincidan con el comportamiento MetaTrader.
* Los valores de parámetros predeterminados reproducen el asesor experto original, pero se pueden optimizar a través de la interfaz `Param`.
