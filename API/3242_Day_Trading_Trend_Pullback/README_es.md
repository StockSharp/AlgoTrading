# Estrategia de Day Trading Trend Pullback
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Day Trading es un sistema de seguimiento de tendencia que entra en retrocesos dentro de una dirección establecida. El asesor experto original (entrada MQL `MQL/24298/Day Trading.mq4`) mezcla un filtro de tendencia EMA de 100 períodos con momentum y una confirmación MACD de marco temporal superior. El port de StockSharp mantiene la misma idea mientras expone cada entrada importante como un parámetro de estrategia.

La estrategia opera en un único instrumento y un tipo de vela configurable. Nunca coloca órdenes pendientes – todas las operaciones se ejecutan a mercado una vez que las condiciones en la última vela terminada se satisfacen. Los niveles protectores de stop-loss y take-profit se adjuntan inmediatamente después de la entrada.

## Lógica de trading
1. **Calificación de tendencia** – El mínimo de cada una de las últimas `TrendConfirmationCount` velas debe cerrar por encima de la EMA de 100 períodos para permitir configuraciones largas. Para cortos, los máximos de la ventana de lookback deben permanecer por debajo de la EMA. Esto reproduce el helper `candles()` del EA original.
2. **Verificación de retroceso** – Una operación solo puede ocurrir si al menos una de las tres velas anteriores retrocedió a la EMA de 20 períodos. Para operaciones largas el mínimo debe perforar por debajo de la EMA, mientras que las cortas requieren que el mínimo se mantenga por encima de la EMA (el código MQL usaba `Low > EMA20` para filtros cortos y la misma comparación se mantiene aquí).
3. **Filtro de momentum** – El Momentum (período `MomentumPeriod`) debe desviarse del valor neutro de 100 por más de `MomentumThreshold` en cualquiera de las tres últimas velas completadas. La desviación se mide como `abs(momentum - 100)`.
4. **Confirmación MACD mensual** – El port abre posiciones solo cuando la línea principal del MACD mensual está por encima de la línea de señal para largos o por debajo para cortos. El MACD se evalúa en la suscripción `MacdCandleType` (mensual por defecto) y reutiliza la configuración clásica 12/26/9.
5. **Dimensionamiento de posición** – Cada nueva orden usa `Volume` lotes. El tamaño neto de la posición nunca supera `Volume * MaxPositions`. Cuando la señal se invierte mientras hay una posición abierta, la estrategia invierte la posición combinando los volúmenes de cierre y apertura en una sola orden de mercado.
6. **Gestión de riesgo** – Justo después de una ejecución, la estrategia almacena precios fijos de stop-loss y take-profit calculados desde `StopLossPips` y `TakeProfitPips`. Cada vela terminada verifica si algún nivel ha sido alcanzado y cierra la posición si es necesario.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Volume` | Tamaño base de la orden. El valor se normaliza al paso de volumen del instrumento. | `1` |
| `CandleType` | Marco temporal de trabajo. | `TimeSpan.FromMinutes(15).TimeFrame()` |
| `MacdCandleType` | Marco temporal usado por la confirmación MACD. | `TimeSpan.FromDays(30).TimeFrame()` |
| `TrendConfirmationCount` | Número de velas que deben permanecer en el lado correcto de la EMA de 100. Refleja el input `Count` del EA. | `10` |
| `MomentumPeriod` | Período del indicador de momentum. | `14` |
| `MomentumThreshold` | Distancia absoluta mínima del momentum desde 100 para permitir entradas. | `0.3` |
| `StopLossPips` | Distancia de stop-loss en pips. | `20` |
| `TakeProfitPips` | Distancia de take-profit en pips. | `50` |
| `MaxPositions` | Número máximo de lotes base que pueden acumularse en una dirección. | `10` |

## Notas de implementación
- Los bindings de indicadores se realizan con la API de alto nivel. La suscripción principal de velas proporciona valores de EMA20/60/100 y momentum, mientras que la suscripción mensual alimenta el filtro MACD via `BindEx`.
- Todas las colecciones que replican los lookbacks de MQL (flags de retroceso, flags de tendencia EMA, desviaciones de momentum) se implementan como colas rodantes para que no se acceda directamente al historial crudo del indicador.
- Los stops y objetivos se verifican en cada vela terminada. El helper que convierte pips a precios adapta el tamaño del pip del instrumento `PriceStep`, reproduciendo el cálculo de `pips` usado en el EA.
- La estrategia usa `StartProtection()` en `OnStarted` para que el bloque de protección integrado esté habilitado antes de que se envíen órdenes.

## Diferencias de conversión
- El experto original realizaba numerosas tareas de gestión de balance (equity stop, interruptores de break-even, trailing personalizado). Solo se portaron las partes deterministas de la lógica de entrada/salida. Los usuarios de StockSharp pueden extender la clase si se requieren esas reglas de gestión de dinero.
- Las notificaciones de correo, push y las anotaciones de gráfico presentes en el archivo MQL se omiten intencionalmente.
- Debido a que StockSharp trabaja con posiciones agregadas, `MaxPositions` limita la exposición neta absoluta en lugar del conteo de órdenes en bruto.

## Uso
1. Adjuntar la estrategia a un conector que proporcione el instrumento deseado y datos de velas para el marco temporal de trading y el de confirmación MACD.
2. Ajustar los parámetros según la volatilidad del activo y la tolerancia al riesgo. Aumentar `TrendConfirmationCount` o `MomentumThreshold` hace las entradas más selectivas.
3. Iniciar la estrategia. Las órdenes se generarán automáticamente una vez que todos los filtros se alineen en una vela terminada.

## Archivos
- `CS/DayTradingStrategy.cs` – Implementación StockSharp.
- `README_ru.md` – Descripción en ruso.
- `README_zh.md` – Descripción en chino.
