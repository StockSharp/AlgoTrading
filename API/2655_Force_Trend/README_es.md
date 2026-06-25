# Estrategia de Tendencia Forzada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del asesor experto MetaTrader 5 **Exp_ForceTrend.mq5** ubicado en `MQL/18817`.
- Usa el oscilador ForceTrend propietario para detectar transiciones entre momentum alcista y bajista.
- Implementa la lógica con la API de alto nivel de StockSharp, basándose en suscripciones de velas e indicadores integrados en lugar del acceso directo a series.

## Indicador ForceTrend
- El indicador mira atrás `Length` velas y mide la distancia entre el máximo más alto y el mínimo más bajo dentro de esa ventana.
- El precio medio de la vela actual se normaliza dentro de ese rango y se suaviza dos veces:
  - La primera etapa produce un valor `force` intermedio con coeficientes `0.66` y `0.67`.
  - La segunda etapa aplica una transformación logarítmica combinada con suavizado de vida media para obtener el valor final de ForceTrend.
- Los valores por encima de cero se tratan como alcistas (originalmente renderizados en azul) y los valores por debajo de cero son bajistas (renderizados en magenta).

## Parámetros
- `Length` – tamaño de la ventana de lookback de ForceTrend; debe permanecer positivo.
- `SignalBar` – cuántas velas finalizadas se desplaza la señal. `0` reacciona a la barra cerrada más reciente, `1` imita la configuración MT5 por defecto esperando una barra extra, y valores mayores retrasan más la ejecución.
- `EnableLongEntry` – si está deshabilitado, la estrategia no abrirá posiciones largas en transiciones alcistas.
- `EnableShortEntry` – si está deshabilitado, la estrategia no abrirá posiciones cortas en transiciones bajistas.
- `EnableLongExit` – controla si las señales alcistas pueden cerrar posiciones cortas existentes.
- `EnableShortExit` – controla si las señales bajistas pueden cerrar posiciones largas existentes.
- `CandleType` – marco temporal de las velas usadas para los cálculos del indicador.

## Reglas de trading
1. La salida de ForceTrend se convierte en una dirección discreta (`+1`, `0`, `-1`).
2. Las direcciones se almacenan en un historial de longitud fija para que la estrategia pueda comparar la barra en el offset `SignalBar` con la barra inmediatamente anterior.
3. Una señal alcista (`direction > 0`) activa:
   - Cerrar cualquier posición corta abierta si `EnableShortExit` es `true`.
   - Abrir o revertir a una posición larga (orden de mercado de tamaño `Volume + |Position|`) cuando la dirección anterior no era alcista y `EnableLongEntry` es `true`.
4. Una señal bajista (`direction < 0`) activa las acciones simétricas para posiciones largas cuando `EnableLongExit`/`EnableShortEntry` están habilitados.
5. Las lecturas neutras de ForceTrend heredan la última dirección conocida para que el sistema no oscile entre estados planos.
6. Las órdenes se envían solo cuando la estrategia está completamente formada, en línea y el trading está permitido por el runtime de StockSharp.

## Notas de implementación
- Las velas se reciben a través de `SubscribeCandles(CandleType)`; el procesamiento del indicador se realiza en el callback `ProcessCandle`.
- Los precios más altos y más bajos se obtienen mediante los indicadores `Highest` y `Lowest` de StockSharp, asegurando que no se requiera gestión manual de buffers ni operaciones LINQ.
- El historial de dirección se almacena en un pequeño array fijo dimensionado según `SignalBar` para reproducir el comportamiento MT5 original sin recrear colecciones para cada tick.
- Las reversiones de posición usan una sola orden de mercado con volumen igual a la suma de la exposición deseada y la posición absoluta actual, emulando los helpers `BuyPositionOpen`/`SellPositionOpen` de la versión MQL.
- Los parámetros de gestión monetaria del asesor experto (dimensionamiento de lotes, stop-loss y take-profit en puntos) se omiten intencionalmente; la estrategia StockSharp depende del `Volume` configurado por el usuario y módulos de protección externos opcionales.
- Los interruptores booleanos reflejan las entradas MT5 (`BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose`).

## Consejos de uso
- Configurar la propiedad `Volume` antes de iniciar la estrategia para controlar el tamaño de la orden.
- Elegir un tipo de vela que coincida con el marco temporal usado durante las pruebas en MT5 (por defecto son velas de cuatro horas).
- Combinar con componentes de riesgo/protección de StockSharp si se requiere automatización de stop-loss o take-profit.

## Archivos
- Implementación de la estrategia: `CS/ForceTrendStrategy.cs`
- Archivos MQL originales: `MQL/18817/mql5/Experts/Exp_ForceTrend.mq5` y `MQL/18817/mql5/Indicators/ForceTrend.mq5`
