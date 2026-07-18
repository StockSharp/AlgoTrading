# Sistema Color PEMA Envelopes Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El **Sistema Color PEMA Envelopes Digit** reproduce la lógica del experto MetaTrader
`Exp_Color_PEMA_Envelopes_Digit_System.mq5`. La estrategia evalúa los códigos de color
producidos por el indicador Color PEMA Envelopes: cuando una vela cierra fuera de la
banda superior o inferior, el indicador pinta un color especial, y una vez que el precio
vuelve a entrar en el canal, se activa una operación en la dirección de la ruptura.

## Cómo funciona
1. La estrategia construye una EMA Polinomial de ocho etapas (PEMA) usando longitudes fraccionarias,
   exactamente como en el indicador original. El resultado se redondea a la precisión configurada
   y se desplaza por el desplazamiento de precio opcional.
2. Los sobres superior e inferior se crean aplicando una desviación porcentual alrededor del valor PEMA.
3. Cada vela finalizada recibe un código de color según su relación con los sobres desplazados:
   - `4`/`3`: cierre por encima de la banda superior (cuerpo alcista/bajista).
   - `1`/`0`: cierre por debajo de la banda inferior (cuerpo alcista/bajista).
   - `2`: el precio permanece dentro del sobre.
4. La estrategia lee el color que ocurrió en la vela `SignalBar + 1` y lo compara con
el color de la vela `SignalBar`. Esto imita las llamadas `CopyBuffer` del asesor experto.
5. Cuando el color más antiguo indica una ruptura por encima de la banda superior y el color más reciente
vuelve dentro del canal, se permite una entrada larga (si está habilitada) y se cierra cualquier posición corta.
   La lógica espejo se usa para entradas cortas y para cerrar posiciones largas.
6. Los pedidos de stop-loss y take-profit protectores se gestionan a través del módulo de riesgo de StockSharp.

## Parámetros
- `CandleType` – marco temporal usado para análisis y trading.
- `TradeVolume` – cantidad enviada con órdenes de mercado.
- `EmaLength` – longitud fraccionaria usada por cada capa EMA en la cadena PEMA.
- `AppliedPrices` – precio fuente (cierre, apertura, mediano, ponderado, seguimiento de tendencia, DeMark, etc.).
- `DeviationPercent` – distancia porcentual para ambos sobres alrededor de PEMA.
- `Shift` – número de velas completadas usadas para desplazar la comparación del sobre.
- `PriceShift` – desplazamiento absoluto adicional aplicado a ambos sobres.
- `Digit` – dígitos de precisión adicionales al redondear la salida PEMA.
- `SignalBar` – cuántas velas cerradas atrás leer el color actual (el color más antiguo se toma una barra más atrás).
- `AllowBuyOpen` / `AllowSellOpen` – habilitar o deshabilitar nuevas entradas largas/cortas.
- `AllowBuyClose` / `AllowSellClose` – permitir cerrar posiciones largas/cortas en señales opuestas.
- `StopLossPoints` – distancia de stop protector en puntos de precio (multiplicado por `PriceStep`).
- `TakeProfitPoints` – distancia del objetivo de beneficio en puntos de precio.

## Valores predeterminados
- `CandleType = TimeSpan.FromHours(4).TimeFrame()`
- `TradeVolume = 1m`
- `EmaLength = 50.01m`
- `AppliedPrices = AppliedPrices.Close`
- `DeviationPercent = 0.1m`
- `Shift = 1`
- `PriceShift = 0m`
- `Digit = 2`
- `SignalBar = 1`
- `AllowBuyOpen = true`
- `AllowSellOpen = true`
- `AllowBuyClose = true`
- `AllowSellClose = true`
- `StopLossPoints = 1000m`
- `TakeProfitPoints = 2000m`

## Filtros
- **Categoría**: Ruptura / Re-entrada al canal
- **Dirección**: Largo/Corto
- **Indicadores**: Sobres de EMA Polinomial
- **Stops**: Sí (stop-loss y take-profit basados en puntos)
- **Marco temporal**: Swing (por defecto 4H)
- **Nivel de riesgo**: Moderado – opera solo cuando el precio regresa de un extremo
- **Estacionalidad**: Ninguna
- **Redes neuronales**: No
- **Divergencia**: No
