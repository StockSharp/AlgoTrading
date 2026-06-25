# Estrategia AO Relámpago
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

AO Relámpago reproduce el asesor experto MT5 "AO_Lightning" usando la API de alto nivel de StockSharp. El sistema monitorea la pendiente del Awesome Oscillator (AO) construido a partir de precios medianos. Cuando el oscilador disminuye, la estrategia acumula exposición larga, y cuando el oscilador aumenta, construye una posición corta. Las posiciones se piramidam hasta un límite configurable mientras las posiciones opuestas se cierran antes de cambiar de dirección.

## Lógica de trading

1. Suscribirse a la serie de velas seleccionada y calcular el Awesome Oscillator con período corto 5 y período largo 34 (los valores predeterminados tomados del código MQL original).
2. Esperar solo velas terminadas; la estrategia ignora las actualizaciones intermedias para evitar el doble conteo.
3. En la primera vela terminada, el valor AO se almacena como referencia.
4. Cuando el valor AO actual es **menor** que el valor anterior:
   - Si existe una posición corta abierta, enviar una orden de compra de mercado dimensionada para cerrar todo el corto e inmediatamente agregar una capa larga.
   - Si no hay corto presente y la exposición larga está por debajo del límite, comprar una capa adicional.
5. Cuando el valor AO actual es **mayor** que el valor anterior:
   - Si existe una posición larga abierta, enviar una orden de venta de mercado que cierre la exposición larga y simultáneamente abra una capa corta.
   - Si no hay largo presente y la exposición corta está por debajo del límite, vender una capa adicional.
6. Los valores de AO iguales al valor anterior dejan la posición sin cambios.
7. El `StartProtection()` integrado se habilita una vez al inicio para que los usuarios de Designer puedan adjuntar stops u otros módulos de riesgo si lo desean.

La lógica refleja el asesor experto original: la pendiente de AO define la dirección del trade, las operaciones opuestas se aplanan antes de una nueva entrada, y las órdenes incrementales siguen acumulándose hasta que se alcanza el límite.

## Gestión de posición

- **Volumen de trade** define el tamaño de cada capa adicional y corresponde al parámetro MT5 `LotFixed`.
- **Máximo de posiciones** coincide con la entrada MT5 `Orders`. Restringe cuántas capas pueden acumularse en cualquier lado.
- **Piramidación** es lineal: cada señal válida agrega exactamente una capa del tamaño de un lote siempre que no se haya alcanzado el límite.
- **Aplanamiento** envía órdenes combinadas (cierre + nueva dirección) para evitar estados planos intermedios al cambiar de corto a largo o viceversa.

## Parámetros

| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `TradeVolume` | Tamaño de la orden para cada nueva capa. | 1 |
| `MaxPositions` | Número máximo de capas largas o cortas que pueden estar activas simultáneamente. | 10 |
| `AoShortPeriod` | Longitud de la SMA rápida usada por el Awesome Oscillator (SMA de precio mediano). | 5 |
| `AoLongPeriod` | Longitud de la SMA lenta para el Awesome Oscillator. | 34 |
| `CandleType` | Fuente de datos de velas procesada por la estrategia. | Marco temporal de 5 minutos |

## Notas

- El experto MT5 original nombra las entradas `Period_sma_slow` y `Period_sma_fast` pero intercambia los valores (5 y 34). El puerto StockSharp mantiene el mapeo funcional exponiendo parámetros intuitivos `AoShortPeriod`/`AoLongPeriod`.
- No se proporciona versión Python aún, según la solicitud de tarea.
- No se incluyen pruebas; ejecute las validaciones necesarias a través de Designer o su propio arnés de backtesting antes de desplegar a producción.
