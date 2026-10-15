# Estrategia de Fractals Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta carpeta contiene el port de StockSharp del asesor experto de MetaTrader "Fractals Martingale". La estrategia mezcla fractales de Bill Williams, un filtro de tendencia basado en Ichimoku y una confirmación mensual de MACD. El dimensionamiento de posición sigue una secuencia martingala clásica que multiplica el volumen de operación después de cada ciclo perdedor, mientras que un enfriamiento opcional previene exposiciones descontroladas.

## Lógica de trading

1. **Detección de fractales en el marco temporal de trabajo** – las velas finalizadas se almacenan en búfer para detectar máximos y mínimos locales separados por `FractalDepth` vecinos. Se registra una configuración alcista cuando la siguiente vela abre por encima del máximo fractal, mientras que una configuración bajista requiere la siguiente apertura por debajo del mínimo fractal. Los niveles detectados permanecen válidos durante `FractalLookback` velas procesadas.
2. **Filtro de tendencia Ichimoku** – el fractal debe alinearse con la tendencia de Ichimoku calculada en el marco temporal superior definido por `IchimokuCandleType`. Las operaciones long requieren que Tenkan-sen esté por encima de Kijun-sen; las operaciones short requieren que Tenkan-sen esté por debajo de Kijun-sen.
3. **Confirmación mensual de MACD** – el EA original usaba un MACD mensual para decidir si dominan compradores o vendedores. El port se suscribe a la serie `MacdCandleType` (velas de 30 días por defecto) y solo acepta señales long cuando la línea MACD está por encima de la línea de señal; las señales short necesitan la condición opuesta.
4. **Filtro de sesión** – las órdenes se colocan solo entre `StartHour` (inclusive) y `EndHour` (exclusive). Se admite una ventana nocturna para sesiones de trading nocturnas.
5. **Escala de volumen martingala** – el tamaño base de la orden proviene de `TradeVolume`. Después de cada ronda perdedora, el siguiente volumen de orden se multiplica por `Multiplier` y se alinea al paso de volumen del instrumento. Las operaciones ganadoras restablecen la secuencia. Cuando se supera `MaxConsecutiveLosses`, el algoritmo hace una pausa de `PauseMinutes` antes de reanudarse con el volumen base.
6. **Cambio de dirección** – siempre que se envía una nueva operación, la estrategia compensa automáticamente cualquier posición opuesta antes de abrir exposición en la dirección solicitada.

## Gestión de riesgos

- `StopLossPips` y `TakeProfitPips` se convierten a distancias de precio absolutas usando el tamaño de pip detectado y se aplican a través de `StartProtection`. Esto refleja el EA original donde ambos stops se definían en pips.
- La implementación original exponía trailing stops opcionales basados en dinero. El port de StockSharp depende del bloque de protección incorporado porque el manejo de la moneda del portfolio real es específico del broker.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `TradeVolume` | Tamaño base de la orden usado para la primera entrada de una secuencia. |
| `Multiplier` | Factor aplicado al próximo volumen de operación después de una pérdida. |
| `StopLossPips`, `TakeProfitPips` | Distancias de stop de protección y objetivo medidas en pips. |
| `FractalDepth` | Número de velas en cada lado requeridas para confirmar un máximo/mínimo fractal. |
| `FractalLookback` | Número máximo de velas procesadas para las cuales un fractal detectado permanece válido. |
| `StartHour`, `EndHour` | Ventana de trading expresada en horas del intercambio. Cuando ambos valores coinciden el filtro se deshabilita. |
| `MaxConsecutiveLosses` | Número de operaciones perdedoras antes de que la estrategia haga una pausa. |
| `PauseMinutes` | Duración del período de enfriamiento activado después de superar el límite de pérdidas. |
| `TenkanPeriod`, `KijunPeriod`, `SenkouPeriod` | Longitudes de Ichimoku Kinko Hyo usadas en el marco temporal superior. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Longitudes de EMA para la confirmación MACD del marco temporal superior. |
| `CandleType` | Serie de velas primaria donde se evalúan los fractales y las ejecuciones. |
| `IchimokuCandleType` | Marco temporal superior utilizado para calcular las líneas Tenkan y Kijun. |
| `MacdCandleType` | Marco temporal utilizado para calcular el filtro MACD (mensual por defecto). |

## Notas de uso

1. **Cálculo del tamaño de pip** – el valor del pip se deriva de `Security.PriceStep`. Las cotizaciones forex de cinco dígitos se escalan automáticamente para coincidir con la definición de MetaTrader usada en el EA fuente.
2. **Suscripciones de indicadores** – la estrategia consume hasta tres series de velas. Asegúrese de que el feed de datos pueda suministrar todos los marcos temporales solicitados para mantener los filtros sincronizados.
3. **Precauciones de martingala** – doblar el volumen aumenta rápidamente la exposición. Use los parámetros de enfriamiento o reduzca el multiplicador si la cuenta no puede soportar rachas de pérdidas prolongadas.
4. **Diferencias vs. el EA de MT4** – las alertas de correo/notificación, los trailing stops basados en balance y las verificaciones de margen explícitas fueron eliminadas porque StockSharp ya maneja la conectividad, la seguridad del portfolio y la ejecución de órdenes. La lógica de entrada/salida central coincide con la implementación MQL.

## Archivos

- `CS/FractalsMartingaleStrategy.cs` – implementación en C# usando la API de estrategia de alto nivel.
- `README.md` – documentación en inglés (este archivo).
- `README_zh.md` – traducción al chino simplificado.
- `README_ru.md` – traducción al ruso.
