# Estrategia Alligator Fractal Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el Expert Advisor de MetaTrader "Alligator(barabashkakvn's edition)" a la API de alto nivel de StockSharp. Combina el indicador Alligator de Bill Williams con confirmación de ruptura fractal, una escalera de promediado Martingale y trailing stops adaptativos. La lógica está diseñada para ejecución tipo cobertura donde la primera orden se abre a mercado y las entradas adicionales se programan a distancias predefinidas cuando el precio se mueve en contra de la posición.

## Lógica de trading

- **Expansión de la boca del Alligator** – las medias móviles suavizadas de los labios (verde), dientes (rojo) y mandíbula (azul) se procesan sobre el precio mediano. Un sesgo largo se activa cuando los labios suben por encima de la mandíbula al menos `EntrySpread`, mientras que un sesgo corto requiere la alineación opuesta. Cuando el diferencial se contrae por debajo de `ExitSpread`, el sesgo respectivo se desactiva.
- **Filtro fractal (opcional)** – las velas terminadas se escanean en busca de fractales de Bill Williams. Una señal larga se acepta solo si un fractal alcista dentro de los últimos `FractalLookback` barras permanece al menos `FractalBuffer` por encima del cierre. Las señales cortas requieren un fractal bajista por debajo del mercado. Deshabilite el filtro mediante `UseFractalFilter` para entrar solo con señales del Alligator.
- **Promediado Martingale** – después de la orden inicial de mercado, la estrategia puede pre-construir `MartingaleSteps` niveles de promediado separados por `MartingaleStepDistance`. Cada nivel multiplica el volumen previo por `MartingaleMultiplier` (limitado por `MaxVolume`) y se ejecuta una vez que el precio toca el nivel.
- **Gestión de salida con trailing** – cada posición larga o corta llena recibe un stop-loss sintético y take-profit basado en `StopLossDistance` y `TakeProfitDistance`. Cuando `EnableTrailing` está activado, los stops se adelantan al menos `TrailingStep` a medida que el mercado se mueve a favor del trade.
- **Salidas por Alligator (opcional)** – cuando `UseAlligatorExit` es verdadero, la posición se cierra en cuanto la boca del Alligator se cierra (el sesgo cambia de activo a inactivo).

## Gestión de riesgo y órdenes

- La estrategia usa el parámetro `Volume` para la primera orden de mercado. Cada nivel martingale reutiliza el volumen redondeado y lo multiplica por el factor configurado mientras mantiene el resultado por debajo de `MaxVolume`.
- Los stops y objetivos se evalúan internamente en cada vela terminada en lugar de depender de órdenes nativas de la bolsa. Cuando el rango de la vela cruza el stop o el objetivo sintético, la posición se cierra inmediatamente.
- Las posiciones opuestas se cierran antes de abrir una nueva dirección para evitar exposición cubierta dentro de StockSharp.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `Volume` | Tamaño base de la orden para la primera entrada a mercado. |
| `JawLength`, `TeethLength`, `LipsLength` | Longitud de las medias móviles suavizadas que forman la mandíbula, dientes y labios del Alligator. |
| `JawShift`, `TeethShift`, `LipsShift` | Desplazamiento hacia adelante (en barras) aplicado al leer los buffers del Alligator. |
| `EntrySpread`, `ExitSpread` | Diferencial mínimo para habilitar trades y umbral de contracción para deshabilitarlos. |
| `UseAlligatorEntry`, `UseAlligatorExit` | Activar entradas y salidas basadas en el Alligator. |
| `UseFractalFilter` | Habilitar o deshabilitar la capa de confirmación fractal. |
| `FractalLookback`, `FractalBuffer` | Ventana de retroceso y margen de seguridad para fractales válidos. |
| `EnableMartingale`, `MartingaleSteps`, `MartingaleMultiplier`, `MartingaleStepDistance`, `MaxVolume` | Controlan la escalera de promediado. |
| `StopLossDistance`, `TakeProfitDistance`, `EnableTrailing`, `TrailingStep` | Configuran la gestión sintética de riesgo. |
| `AllowMultipleEntries` | Permitir entradas repetidas a mercado mientras una posición está abierta. |
| `ManualMode` | Cuando es verdadero, el algoritmo solo gestiona trades abiertos y no crea nuevos. |
| `CandleType` | Serie de velas fuente para los cálculos de indicadores. |

## Notas de uso

1. Asegúrese de que el instrumento seleccionado admita los pasos de precio y volumen configurados; la estrategia redondea los valores usando `Security.MinPriceStep` y `Security.VolumeStep` cuando están disponibles.
2. La escalera martingale se simula internamente. Si prefiere usar órdenes límite reales en la bolsa, deshabilite la función y gestione el escalado externamente.
3. Inicie la estrategia en una cartera compatible con cobertura. Aunque StockSharp agrega la posición neta, la lógica original asume la capacidad de añadir múltiples tramos en la misma dirección.
4. Revise las distancias predeterminadas basadas en pips (`0.008` ≈ 80 pips para cotizaciones FX de cuatro dígitos) y ajústelas al instrumento que se esté operando.
