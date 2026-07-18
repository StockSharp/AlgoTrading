# Estrategia Exp Color TSI Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
- Conversión del asesor experto de MetaTrader 5 **Exp_ColorTSI-Oscillator** al framework StockSharp.
- Reconstruye el oscilador ColorTSI: un True Strength Index de doble suavizado con una línea de trigger retrasada y múltiples algoritmos de suavizado tomados de `SmoothAlgorithms.mqh`.
- Genera trades cuando el oscilador sube o baja con respecto a su trigger retrasado, replicando el estilo de "reversión de oscilación" utilizado por el EA original.

## Reconstrucción del indicador
- El precio aplicado se selecciona a través de la opción `ColorTsiAppliedPrice` (cierre, apertura, mediana, típico, ponderado, Demark, etc.).
- El momentum de precio (`diff = price[n] - price[n-1]`) y su valor absoluto se suavizan en dos etapas:
  1. **Primera etapa**: `ColorTsiSmoothingMethod` configurable (`Sma`, `Ema`, `Smma`, `Lwma`, `Jjma`, `Jurx`, `Parma`, `T3`, `Vidya`, `Ama`) con longitud `FirstLength` y fase `FirstPhase` para filtros tipo Jurik.
  2. **Segunda etapa**: opciones de método idénticas con `SecondLength`/`SecondPhase` aplicadas a la serie de momentum ya suavizada.
- La salida del oscilador es `TSI = 100 * smoothMomentum / smoothAbsMomentum`. Cuando el denominador es cero, el valor se ignora.
- La línea de trigger se obtiene retrasando el TSI por `TriggerShift` barras, reflejando la lógica del buffer de MetaTrader.
- Los valores históricos se almacenan para que `SignalBar` coincida con el patrón de acceso `CopyBuffer` de MetaTrader (índice `SignalBar` = barra cerrada más reciente examinada, `SignalBar + 1` = barra anterior, etc.).

## Reglas de trading
- Los cálculos se ejecutan en velas terminadas suministradas por `CandleType` (por defecto: marco temporal de 4 horas).
- Sea `TSI[k]` el valor del oscilador y `Trigger[k]` la serie retrasada.
- **Contexto alcista**: `TSI[SignalBar + 1] > Trigger[SignalBar + 1]` ⇒ la barra anterior mostró momentum alcista.
  - Cerrar cortos si `EnableShortExits` es true.
  - Abrir una posición larga cuando `EnableLongEntries` es true **y** `TSI[SignalBar] ≤ Trigger[SignalBar]`, señalando un movimiento alcista después del retroceso.
- **Contexto bajista**: `TSI[SignalBar + 1] < Trigger[SignalBar + 1]` ⇒ la barra anterior mostró momentum bajista.
  - Cerrar largos si `EnableLongExits` es true.
  - Abrir una posición corta cuando `EnableShortEntries` es true **y** `TSI[SignalBar] ≥ Trigger[SignalBar]`.
- Las señales de entrada se codifican por el tiempo de la barra analizada más un marco temporal completo; cada señal puede activar como máximo un trade gracias a las guardas `_lastLongEntryTime` / `_lastShortEntryTime`.
- Todas las acciones se ejecutan con órdenes de mercado. Las posiciones opuestas existentes se cierran antes de las reversiones.

## Parámetros
| Parámetro | Descripción | Por defecto |
|-----------|-------------|-------------|
| `CandleType` | Stream de datos usado para el análisis. Soporta cualquier `DataType` (velas de tiempo, tick, volumen). | Marco temporal H4 |
| `Volume` | Tamaño de orden fijo que reemplaza los bloques de gestión monetaria del EA. Debe ser > 0. | 0.1 |
| `FirstMethod`, `FirstLength`, `FirstPhase` | Primera etapa de suavizado para momentum y momentum absoluto. | SMA, 12, 15 |
| `SecondMethod`, `SecondLength`, `SecondPhase` | Segunda etapa de suavizado. | SMA, 12, 15 |
| `PriceMode` | Opción de precio aplicado que alimenta el oscilador. | Close |
| `SignalBar` | Desplazamiento de barra usado para evaluar señales (1 = última barra cerrada). | 1 |
| `TriggerShift` | Retraso aplicado a la línea de trigger (1 reproduce el indicador original). | 1 |
| `EnableLongEntries` / `EnableShortEntries` | Permitir apertura de trades largos/cortos. | true |
| `EnableLongExits` / `EnableShortExits` | Permitir cerrar posiciones en contexto opuesto. | true |
| `StopLossPoints` | Distancia de stop-loss en puntos de precio (convertida con el `PriceStep` del instrumento). | 1000 |
| `TakeProfitPoints` | Distancia de take-profit en puntos de precio. | 2000 |

## Gestión de riesgo
- El EA original dependía de funciones auxiliares de `TradeAlgorithms.mqh` para la colocación de SL/TP. La versión C# llama a `StartProtection` con las distancias seleccionadas convertidas a `UnitTypes.Point`.
- Si cualquier distancia se establece en 0, la orden de protección correspondiente se omite.
- No se implementan trailing stops ni escalonamiento de posición; estos coinciden con el comportamiento de MetaTrader para este asesor.

## Diferencias con la versión MetaTrader
- El sizing de lote basado en margen (`MM` y `MMMode`) se reemplaza por un parámetro `Volume` fijo. Esto mantiene el comportamiento determinista entre brokers y evita replicar la lógica de apalancamiento específica de cada cuenta.
- El deslizamiento (`Deviation_`) no se emula porque las órdenes de mercado de StockSharp no exponen un parámetro de deslizamiento.
- El suavizado del indicador se reconstruye completamente usando indicadores de StockSharp (incluido el manejo de fase Jurik a través de reflexión), por lo que los valores de señal son consistentes con los buffers originales.
- La implementación de Python se omite intencionalmente según lo solicitado.

## Notas de uso
- Asegurarse de que el instrumento seleccionado proporcione el tipo de vela solicitado por `CandleType`. Para marcos temporales estándar usar `TimeSpan.FromHours(x).TimeFrame()`.
- `SignalBar` debe ser ≥ `TriggerShift` para obtener valores de trigger válidos; de lo contrario, las señales se omiten hasta que se acumule suficiente historial.
- Como la estrategia reacciona en velas terminadas, habilitar el registro de órdenes en tiempo real solo después de que `IsFormedAndOnlineAndAllowTrading()` se vuelva true.
- El área de gráfico visualiza velas de precio y trades ejecutados; los indicadores se reconstruyen internamente y no se trazan automáticamente.
- Para reproducir los valores por defecto de MetaTrader: mantener todos los ajustes de suavizado en SMA con longitud 12, mantener ambos toggles de entrada y salida habilitados, y usar las distancias de stop/take por defecto.
