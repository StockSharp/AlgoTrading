# Sistema de contracanal Donchain
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El **Sistema de contracanal Donchain** reproduce el asesor experto MetaTrader 4 de 2005 de Michal Rutka. Vigila los giros en un canal Donchian de 20 días calculado sobre velas diarias. Cuando la banda inferior gira hacia arriba, la estrategia supone que los vendedores no lograron llevar el precio a nuevos mínimos y compran en la siguiente sesión en el mercado. Cuando la banda superior gira hacia abajo, la estrategia lo interpreta como una pérdida de impulso en los repuntes y vende en corto en el mercado. Las paradas de protección siempre están alineadas con la banda Donchian opuesta para que las salidas reflejen la lógica original de gestión de paradas.

Sólo se permite una entrada cada 24 horas, coincidiendo con la regla del artículo que restringe el sistema a como máximo un pedido por día. Esta implementación utiliza el API de alto nivel de StockSharp con enlaces de indicadores para que los valores de Donchian lleguen junto con cada vela completa.

## Lógica de trading
1. Suscríbase al `CandleType` configurado (diario por defecto) y evalúe un indicador `DonchianChannels` con el `ChannelPeriod` seleccionado.
2. Cada vez que se acaba una vela:
   - Si hay una posición larga abierta, mueva el nivel de parada a la banda inferior actual cuando suba y salga si el mínimo de la vela toca ese nivel.
   - Si hay una posición corta abierta, mueva el nivel de parada a la banda superior actual cuando caiga y salga si el máximo de la vela toca ese nivel.
   - Si no hay ninguna posición, omita las entradas cuando la última operación ocurrió hace menos de `TradeCooldown`.
   - Vaya en largo cuando la banda inferior Donchian de la vela anterior sea más alta que la vela anterior, lo que indica un repunte en el piso del canal. Establezca la parada inicial en la banda inferior actual.
   - Vaya en corto cuando la banda superior Donchian de la vela anterior sea más baja que la vela anterior, lo que indica una caída en el techo del canal. Establezca la parada inicial en la banda superior actual.
3. Continúe siguiendo el stop a lo largo de las bandas hasta que el precio se invierta a través de ellas, lo que cierra la posición.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Volume` | `1` | Tamaño del pedido para entradas largas y cortas. |
| `ChannelPeriod` | `20` | Número de velas utilizadas para calcular las bandas superior e inferior Donchian. |
| `TradeCooldown` | `1 day` | Período mínimo de espera antes de que se permita una nueva entrada. |
| `CandleType` | `Daily` | Serie de velas sobre las que se calcula el Canal Donchian. |

## Indicadores y datos
- **Donchian Canales**: proporciona los límites de canal superior e inferior utilizados para la detección de cambios de tendencia y para paradas dinámicas.
- **Velas diarias (predeterminado)**: tiempos de cierre de suministro necesarios para el tiempo de reutilización de 24 horas y para evaluar los turnos del indicador.

## Notas de implementación
- La estrategia utiliza `BindEx` para recibir un `DonchianChannelsValue` escrito en el controlador de velas, lo que garantiza que ambas bandas estén disponibles simultáneamente.
- Las paradas se simulan monitoreando los máximos y mínimos de las velas con respecto al valor de la banda almacenado, tal como el EA original actualizaba su stop-loss en cada nueva barra.
- El temporizador de recuperación se actualiza solo con nuevas entradas, reflejando el script fuente que impedía múltiples entradas dentro del mismo día de negociación.
