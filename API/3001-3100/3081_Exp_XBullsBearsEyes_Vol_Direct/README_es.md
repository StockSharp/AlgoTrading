# Estrategia Exp XBullsBearsEyes Vol Directa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión en C# del experto de MetaTrader **Exp_XBullsBearsEyes_Vol_Direct**. Recrea el oscilador personalizado
construido a partir de Bulls Power y Bears Power, lo multiplica por una fuente de volumen configurable y aplica un suavizado adaptativo
idéntico al del indicador original. Las decisiones de trading se basan exclusivamente en el búfer de dirección del indicador: el algoritmo
reacciona a los cambios de momentum en lugar de cruzar niveles, abriendo o cerrando posiciones cuando el histograma suavizado cambia de pendiente.

A diferencia de muchas conversiones, la versión de StockSharp conserva la etapa de ponderación por volumen y el filtro gamma de cuatro
niveles utilizado en el código MQL. El oscilador se suaviza dos veces con el mismo método de media móvil —una pasada para el histograma
y otra para el flujo de volumen—, por lo que las señales aparecen solo cuando ambos componentes están completamente formados. La estrategia
procesa únicamente velas cerradas y admite volumen de ticks o volumen real negociado, lo que la hace portable entre diferentes mercados.

## Lógica del indicador
1. Calcular Bulls Power y Bears Power con una media móvil exponencial del precio de cierre sobre `Period` velas.
2. Aplicar el filtro gamma de cuatro etapas original (parámetros `Gamma`, `L0`–`L3`) para combinar las dos fuerzas en un histograma
   normalizado entre -50 y +50.
3. Multiplicar el histograma por la fuente de volumen seleccionada (número de ticks o volumen negociado).
4. Suavizar el histograma y el volumen bruto con la misma familia de medias móviles (`Method`, `SmoothingLength`, `SmoothingPhase`).
5. Derivar un búfer de dirección: color `0` cuando el histograma suavizado sube, color `1` cuando baja. Esto imita el
   `ColorDirectBuffer` de la implementación de MetaTrader.

Los búferes de umbral superior/inferior del indicador se calculan internamente pero no se usan para filtros de operaciones, lo que
reproduce el comportamiento del experto original que solo dependía de los cambios de dirección.

## Reglas de trading
- **Cerrar cortos** cuando la dirección de la barra anterior era alcista (`olderColor = 0`).
- **Abrir largos** si las entradas largas están permitidas, una barra alcista es seguida por una bajista (`currentColor = 1`), y la estrategia
  no está ya en largo.
- **Cerrar largos** cuando la dirección de la barra anterior era bajista (`olderColor = 1`).
- **Abrir cortos** si las entradas cortas están permitidas, una barra bajista es seguida por una alcista (`currentColor = 0`), y no hay
  posición larga activa.
- Las reversiones de posición cierran primero el lado opuesto y luego envían una orden de mercado con el `OrderVolume` configurado.

Las señales se evalúan con un desplazamiento de barra configurable (`SignalBar`). El valor predeterminado de `1` emula al experto MQL que
esperaba una vela completamente cerrada antes de reaccionar al cambio de dirección.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `CandleType` | Tipo/marco temporal de vela suscrito por la estrategia (predeterminado: velas de 2 horas). |
| `Period` | Período de lookback utilizado para Bulls/Bears Power. |
| `Gamma` | Factor de suavizado (0…1) del filtro gamma adaptativo. |
| `VolumeMode` | Fuente de volumen: número de ticks o volumen negociado. |
| `Method` | Familia de medias móviles para suavizar histograma y volumen (SMA, EMA, SMMA, LWMA, Jurik; los tipos heredados no soportados vuelven a SMA). |
| `SmoothingLength` | Longitud de ambas etapas de suavizado. |
| `SmoothingPhase` | Parámetro de fase Jurik (mantenido por compatibilidad). |
| `SignalBar` | Número de barras atrás para leer al evaluar el búfer de dirección. |
| `AllowBuyOpen` / `AllowSellOpen` | Habilitar o deshabilitar la apertura de posiciones largas/cortas. |
| `AllowBuyClose` / `AllowSellClose` | Habilitar o deshabilitar salidas forzadas en señales opuestas. |
| `OrderVolume` | Tamaño de la orden de mercado para nuevas entradas. |
| `StopLossPoints` | Stop de protección opcional en pasos de precio (0 deshabilita el stop). |
| `TakeProfitPoints` | Objetivo de protección opcional en pasos de precio (0 deshabilita el objetivo). |

## Notas de uso
- La estrategia opera sobre un único instrumento devuelto por `GetWorkingSecurities()` y funciona mejor en símbolos que proporcionan un
  flujo de volumen estable.
- Se recomienda el volumen de ticks para símbolos de FX al contado donde el volumen real negociado no está disponible. Establezca
  `VolumeMode` en `Real` para bolsas que publiquen volumen ejecutado.
- Las distancias de stop-loss y take-profit se expresan en pasos de precio y se convierten en unidades de precio absolutas usando el
  `PriceStep` del instrumento.
- Debido a que la lógica se basa en cambios de dirección, valores del histograma consecutivamente iguales mantienen la dirección previa
  hasta que aparece una nueva pendiente, exactamente como en la versión de MetaTrader.
- La salida del gráfico muestra solo velas de precio de forma predeterminada. Puede agregar gráficos personalizados para el histograma
  si se requiere confirmación visual.
