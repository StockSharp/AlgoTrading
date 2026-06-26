# Estrategia Hans Indicator Sistema de Nube
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia porta el asesor experto MQL5 `Exp_Hans_Indicator_Cloud_System` a la API de alto nivel de StockSharp. Reproduce los
rangos de "nube" del indicador Hans que dividen cada día de trading en dos sesiones de referencia y opera cuando el indicador reporta un
breakout por encima o por debajo de esos rangos dinámicos. La implementación consume una serie de velas configurable (predeterminado: M30), procesa
solo velas finalizadas, y refleja la lógica de ejecución retardada del script original actuando en la siguiente barra después de un cambio
de color.

## Recreación del indicador Hans
El indicador original desplaza todas las marcas de tiempo desde la zona horaria del broker (`LocalTimeZone`) a una zona horaria objetivo (`DestinationTimeZone`).
El port de StockSharp aplica el mismo desplazamiento antes de dividir cada día en dos sesiones:

1. **Sesión 1 (04:00–08:00 hora objetivo)** – la estrategia registra el máximo más alto y el mínimo más bajo de todas las velas que caen dentro
   de esta ventana. Una vez que la ventana termina, la zona se considera completa.
2. **Sesión 2 (08:00–12:00 hora objetivo)** – el proceso se repite para la segunda ventana. Cuando esta sesión termina, sus valores alto/bajo
   supersedan la primera zona por el resto del día.

Un buffer configurable (`PipsForEntry`) expresado en pasos de precio se agrega por encima del máximo y por debajo del mínimo de la zona activa. El
mapa de colores del indicador se reproduce de la siguiente manera:

- `0` – el cierre está por encima de la zona superior y el cuerpo de la vela es alcista.
- `1` – el cierre está por encima de la zona superior y el cuerpo de la vela es bajista.
- `3` – el cierre está por debajo de la zona inferior y el cuerpo de la vela es alcista.
- `4` – el cierre está por debajo de la zona inferior y el cuerpo de la vela es bajista.
- `2` – sin breakout (estado neutral).

Estos valores se almacenan para emular las búsquedas de `CopyBuffer` realizadas por el experto MQL5.

## Lógica de trading
- La estrategia mantiene un historial rodante de códigos de color y mira atrás `SignalBar` barras (predeterminado 1) más una barra extra, coincidiendo con
  la llamada `CopyBuffer(..., SignalBar, 2, ...)` del fuente.
- **Abrir largo**: la barra más antigua (`SignalBar + 1`) reporta color `0` o `1` y la barra más reciente (`SignalBar`) no está coloreada
  `0`/`1`. Cualquier exposición corta existente se cierra antes de abrir un nuevo largo de `TradeVolume` unidades.
- **Abrir corto**: la barra más antigua reporta color `3` o `4` y la barra más reciente no está coloreada `3`/`4`. Cualquier exposición larga
  existente se aplana primero y luego se abre un nuevo corto.
- **Cerrar largo**: cuando la barra más antigua está coloreada `3` o `4` y los cierres largos están habilitados.
- **Cerrar corto**: cuando la barra más antigua está coloreada `0` o `1` y los cierres cortos están habilitados.

Las salidas se procesan antes que las entradas exactamente como las funciones auxiliares dentro de `TradeAlgorithms.mqh`, asegurando que las posiciones opuestas
se cierren antes de emitir nuevas órdenes.

## Parámetros
- **Tipo de vela** (`CandleType`): marco temporal de las velas procesadas.
- **Barra de señal** (`SignalBar`): cuántas velas finalizadas atrás inspeccionar para un cambio de color.
- **Zona horaria local** (`LocalTimeZone`): zona horaria del broker/servidor en horas.
- **Zona horaria de destino** (`DestinationTimeZone`): zona horaria objetivo que define las ventanas de sesión.
- **Buffer de breakout** (`PipsForEntry`): número de pasos de precio agregados por encima/por debajo del rango de sesión detectado.
- **Habilitar entradas/salidas largas** (`BuyPosOpen`, `BuyPosClose`): interruptores para gestionar posiciones largas.
- **Habilitar entradas/salidas cortas** (`SellPosOpen`, `SellPosClose`): interruptores para gestionar posiciones cortas.
- **Volumen de trading** (`TradeVolume`): tamaño de orden usado para cada nueva posición; también sincronizado con `Strategy.Volume` al inicio.

## Notas
- La traducción de Python se omite intencionalmente según lo solicitado.
- Los auxiliares de gestión de dinero de `TradeAlgorithms.mqh` (modos de margen, dimensionamiento de posición dinámico, colocación de stop-loss/take-profit)
  se simplifican a un volumen de trading fijo y reglas de salida explícitas.
- Cuando el valor no expone `PriceStep`, el buffer de breakout se interpreta como unidades de precio absolutas, coincidiendo con la mejor
  aproximación disponible sin información sobre el tamaño del tick.
