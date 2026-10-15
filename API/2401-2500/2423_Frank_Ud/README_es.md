# Estrategia de Cobertura en Cuadrícula Frank Ud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Cobertura en Cuadrícula Frank Ud** es un puerto directo del asesor experto MetaTrader "Frank Ud" a la API de alto nivel de StockSharp. El bot mantiene simultáneamente cestas largas y cortas en el mismo instrumento y luego realiza promediado estilo martingala cuando el precio se aleja contra la cesta activa. Todo el manejo de señales se realiza sobre actualizaciones de mejor bid/ask (Level 1), lo que hace que la estrategia sea adecuada para ejecución de baja latencia o backtesting tick a tick.

## Lógica de negociación
1. **Cobertura inicial** – cuando no hay posiciones abiertas, la estrategia abre inmediatamente una orden de compra y una de venta de mercado con el mismo volumen. Cada orden recibe un stop-loss y take-profit expresados en pips.
2. **Gestión de stop/take** – mientras ambas cestas existen, se respetan sus niveles de protección. Cuando el precio alcanza un nivel de protección se cierra la cesta correspondiente.
3. **Gestión unilateral** – cuando solo quedan posiciones de compra o solo de venta, la estrategia:
   - Calcula el precio de entrada promedio ponderado por volumen de la cesta activa.
   - Reasigna el take-profit común al precio promedio ± distancia configurada.
   - Elimina el stop-loss (el EA original se basa puramente en el take-profit a partir de este punto).
4. **Paso de martingala** – si el precio se mueve contra la cesta activa en más del paso configurado, la estrategia duplica el multiplicador y abre una nueva orden de mercado. El método auxiliar `AdjustVolume` mantiene cada orden alineada con el paso de volumen, el mínimo y el máximo del instrumento.
5. **Reinicio del ciclo** – una vez que todas las cestas están cerradas, el multiplicador se restablece a 1 y comienza un nuevo ciclo de cobertura.

## Parámetros
- `TakeProfitPips` – distancia entre el precio promedio de la cesta y el objetivo de take-profit colectivo (predeterminado 12 pips).
- `StopLossPips` – distancia del stop de protección usada solo para las primeras órdenes de cobertura (predeterminado 12 pips).
- `StepPips` – movimiento adverso requerido antes de añadir la siguiente orden de martingala (predeterminado 16 pips).
- `AutoLot` – cuando es `true`, la estrategia usa `LotSize`; de lo contrario opera con el volumen mínimo del instrumento.
- `LotSize` – tamaño de lote base personalizado usado junto con el multiplicador de martingala cuando `AutoLot` está activado.

## Notas de implementación
- La conversión usa la API de alto nivel `Strategy`: las suscripciones Level 1 impulsan la lógica, y el posicionamiento de órdenes se basa en los ayudantes `BuyMarket`/`SellMarket`.
- El seguimiento de posición es interno: la estrategia almacena el precio de entrada y el volumen de cada orden de cesta para poder reproducir las reglas de promediado de MetaTrader originales.
- El multiplicador (`_multiplier`) refleja la variable `Coefficient` del EA y se duplica después de cada orden adicional. Una vez que todas las operaciones están cerradas el multiplicador se reinicia a `1`.
- `AdjustVolume` emula la función MQL5 `LotCheck` ajustando los volúmenes solicitados al paso de negociación y a los límites del contrato permitidos.
- La estrategia requiere una cuenta con habilitación de cobertura, ya que mantiene cestas largas y cortas simultáneamente, igual que el EA fuente.

## Archivos
- `CS/FrankUdStrategy.cs` – implementación principal de la estrategia con comentarios en inglés explicando cada bloque.
- `README.md` – este documento.
- `README_ru.md` – traducción al ruso.
- `README_zh.md` – traducción al chino simplificado.
