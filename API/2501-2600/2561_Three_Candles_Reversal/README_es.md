# Estrategia de Reversión de Tres Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port fiel de StockSharp del asesor experto MQL5 `Exp_ThreeCandles`. Busca una reversión clásica de tres velas:

1. Dos velas consecutivas en una dirección.
2. Una tercera vela que invierte la dirección y cierra más allá de la barra del medio.
3. Confirmación opcional de volumen a menos que la barra más antigua del patrón sea excepcionalmente grande.

Cuando aparece una configuración alcista el algoritmo cierra la exposición corta y puede entrar en una posición larga. Una configuración bajista hace lo contrario. Los niveles de stop-loss y take-profit protectores se aplican usando el paso de precio actual del instrumento.

## Detección del patrón

La estrategia mantiene una ventana deslizante de las `SignalBar + 3` velas terminadas más recientes. En cada nueva barra verifica la vela en el desplazamiento `SignalBar` (por defecto: 1 barra atrás) y las tres velas más antiguas:

- **Reversión alcista** (potencial compra):
  - Las dos velas más antiguas (`SignalBar + 3` y `SignalBar + 2`) son bajistas.
  - La vela del medio cierra por encima del mínimo de la barra más antigua.
  - La vela más reciente antes de la señal (`SignalBar + 1`) es alcista y cierra por encima de la apertura de la vela del medio.
- **Reversión bajista** (potencial venta):
  - Lógica espejo del caso alcista.

Un filtro de volumen refleja el indicador original. El filtro se omite cuando `MaxBarSize` (en pasos de precio) es superado por el rango de la vela más antigua o cuando `VolumeFilter` está configurado como `None`. De lo contrario, la reversión debe satisfacer `volumen antiguo < volumen medio` **O** `volumen reciente > volumen medio` **O** `volumen reciente > volumen más antiguo`. El volumen tick y real se mapean al volumen agregado de la vela porque StockSharp no distingue los dos en el flujo de velas de alto nivel.

## Gestión de operaciones

- Si `AllowSellExit` está habilitado, un patrón alcista cubre inmediatamente cualquier posición corta antes de considerar una entrada larga. `AllowBuyExit` se comporta igual para posiciones largas en patrones bajistas.
- Las nuevas posiciones solo se abren cuando la posición actual es plana y la bandera `Allow*Entry` correspondiente es verdadera. El tamaño de la orden usa la configuración de volumen estándar de la estrategia.
- Las distancias de stop-loss y take-profit (`StopLossPips`, `TakeProfitPips`) se expresan en pasos de precio y se monitorean en cada vela terminada.
- El último tiempo de señal alcista/bajista procesado se almacena en caché para evitar acciones duplicadas mientras una vela sigue generando ticks.

## Parámetros

| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `CandleType` | Marco temporal de 4 horas | Serie de velas procesada por la estrategia. |
| `SignalBar` | 1 | Cuántas barras atrás se evalúa la señal. Debe ser ≥ 0. |
| `MaxBarSize` | 300 | Si el rango de la barra más antigua (convertido con `PriceStep`) supera este valor, el filtro de volumen se omite. Configurar en 0 para siempre omitir. |
| `VolumeFilter` | `Tick` | Modo de volumen (`Tick`, `Real` o `None`). Tanto `Tick` como `Real` usan `TotalVolume` de las velas. |
| `AllowBuyEntry` | `true` | Habilitar entradas largas en patrones alcistas. |
| `AllowSellEntry` | `true` | Habilitar entradas cortas en patrones bajistas. |
| `AllowBuyExit` | `true` | Permitir cerrar posiciones largas en patrones bajistas. |
| `AllowSellExit` | `true` | Permitir cerrar posiciones cortas en patrones alcistas. |
| `StopLossPips` | 1000 | Distancia de stop-loss en pasos de precio (0 deshabilita). |
| `TakeProfitPips` | 2000 | Distancia de take-profit en pasos de precio (0 deshabilita). |

## Notas de conversión

- Las rutinas de gestión de dinero del archivo de inclusión original MQL5 fueron reemplazadas por llamadas `BuyMarket`/`SellMarket` de StockSharp. Por lo tanto, el tamaño de posición sigue el volumen predeterminado del motor.
- El tiempo de señal refleja el asesor experto evaluando la barra en el desplazamiento `SignalBar` y manteniendo la marca de tiempo de la señal anterior.
- Las alertas de correo electrónico, push y sonido del indicador MQL se omiten intencionalmente.
- Los modos de volumen se preservan pero ambos se mapean al volumen agregado de la vela porque los volúmenes tick y real separados no están disponibles en la API de alto nivel.
- Todos los comentarios fueron reescritos en inglés según lo requerido por las pautas del proyecto.

Esta implementación se mantiene cerca del comportamiento original mientras adhiere al modelo de suscripción de alto nivel de StockSharp.
