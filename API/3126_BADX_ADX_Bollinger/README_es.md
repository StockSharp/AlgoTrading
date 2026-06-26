# Estrategia de BADX ADX Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general

Esta estrategia reproduce el asesor experto BADX de MetaTrader usando la API de alto nivel de StockSharp. Combina el **Average Directional Index (ADX)** con **Bollinger Bands** para operar en condiciones de rango: cuando el ADX cae por debajo de un umbral configurable y el precio toca la banda exterior, la estrategia desvanece el movimiento esperando una reversión a la media. Todas las órdenes de protección, incluyendo stop-loss, take-profit y trailing stop opcional, son gestionadas automáticamente por `StartProtection`.

## Cómo Funciona

1. Se suscribe a la serie de velas configurada y alimenta un indicador `AverageDirectionalIndex` y un `BollingerBands` a través de bindings de alto nivel.
2. Para cada vela terminada el callback recibe el valor ADX así como los envolventes superior e inferior de Bollinger.
3. Si el ADX está por debajo de `AdxLevel`, el mercado se considera sin tendencia:
   - Cuando el precio de cierre está por debajo de la banda inferior y no hay posición abierta, la estrategia compra a mercado.
   - Cuando el precio de cierre está por encima de la banda superior y no hay posición abierta, la estrategia vende a mercado.
4. La gestión de riesgo convierte distancias en pips a offsets de precio absoluto. Stop-loss, take-profit y parámetros de trailing (si están habilitados) se aplican inmediatamente después de las entradas mediante el gestor de protección.
5. Solo puede haber una posición activa a la vez. Las salidas se producen a través de órdenes de protección o ajustes de trailing stop.

## Parámetros

- **CandleType** (`DataType`): Marco temporal usado para los cálculos del indicador. Por defecto velas de 15 minutos.
- **AdxPeriod** (`int`): Período de promediado para el indicador ADX. Por defecto 30.
- **AdxLevel** (`decimal`): Valor ADX máximo que aún califica como mercado en rango. Por defecto 20.
- **BollingerPeriod** (`int`): Período para la media móvil de las Bollinger Bands. Por defecto 10.
- **BollingerDeviation** (`decimal`): Multiplicador de desviación estándar para las Bollinger Bands. Por defecto 1.5.
- **StopLossPips** (`decimal`): Distancia de stop-loss medida en pips. Por defecto 50.
- **TakeProfitPips** (`decimal`): Distancia de take-profit medida en pips. Por defecto 50.
- **TrailingStopPips** (`decimal`): Distancia del trailing stop en pips. Por defecto 5.
- **TrailingStepPips** (`decimal`): Mejora mínima de precio en pips antes de que se ajuste el trailing stop. Por defecto 5.

## Uso

1. Adjuntar la estrategia a un instrumento y configurar los parámetros deseados.
2. Iniciar la estrategia. Se suscribirá automáticamente al stream de velas requerido, construirá los indicadores y configurará las órdenes de protección.
3. Monitorear los trades en el área del gráfico: las velas, las Bollinger Bands y las órdenes ejecutadas se visualizan cuando la plataforma soporta gráficos.
4. Ajustar los parámetros de riesgo (stop-loss, take-profit, distancias de trailing) para adaptarse a la volatilidad del instrumento o preferencias personales.

## Notas

- Solo se procesan velas terminadas para evitar entradas prematuras.
- El tamaño de pip se deriva del `PriceStep` del instrumento; cuando el instrumento usa 3 o 5 dígitos decimales, el pip se ajusta por un factor de diez, imitando al asesor experto original.
- La estrategia mantiene `Volume` en `1` por defecto. Modificar la propiedad `Volume` de la clase base para ajustarse al tamaño de trade preferido.
- Todos los comentarios en línea en el código fuente están escritos en inglés de acuerdo con las directrices del repositorio.
