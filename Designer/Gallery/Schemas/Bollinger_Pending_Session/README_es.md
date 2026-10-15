# Ruptura Bollinger con ciclo de órdenes por sesión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama convierte una ruptura Bollinger bilateral en un ciclo visible de órdenes pendientes. Un cierre terminado fuera de Bollinger Bands(20, 1) registra la entrada en la banda rota; su ejecución crea un límite opuesto en la media móvil, Order replacing lo sigue y el fin de la sesión 07:00-20:00 cancela todo y deja la posición plana.

![schema](schema.svg)

## Resumen de la estrategia

- Velas terminadas de cinco minutos alimentan Bollinger Bands con periodo 20 y ancho 1, además del cierre comparado con ambas bandas.
- Working time solo permite entradas de 07:00 a 20:00 y la puerta común Position == 0 simplifica expresamente el diagrama a entradas desde plano.
- Una ruptura superior confirmada registra un límite de compra en la banda superior; la inferior registra la venta simétrica.
- El Trade.Volume real de la entrada dimensiona la salida opuesta en la media, sin sustituir ejecuciones parciales o no estándar por una constante.
- Combination conserva la orden más reciente devuelta por Order replacing; una salida ejecutada o el final del horario elimina toda orden viva restante.

## Reglas de entrada y salida

- **Entrada en largo**: Dentro de Working time, un cierre terminado sobre la banda superior con Position == 0 registra una compra limitada en el valor de esa banda.
- **Entrada en corto**: Dentro de Working time, un cierre terminado bajo la banda inferior con Position == 0 registra una venta limitada en el valor de esa banda.
- **Salida**: Tras ejecutarse la entrada se registra en la media Bollinger un límite opuesto por el volumen exacto y se mueve con cada nuevo valor. Su ejecución cancela los restos. Fuera de 07:00-20:00 se cancelan todas las órdenes y Modify position cierra a mercado cualquier largo o corto.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo terminado del diagrama, adaptado de cuatro horas en C# para el replay mensual. |
| Bollinger Period | 20 | Length de Bollinger y parámetro real de la estrategia C#. |
| Bollinger Width | 1 | Multiplicador de desviación estándar de Bollinger y parámetro real C#. |
| Session Start | 07:00:00 | Inicio de Working time añadido desde el README, no desde el constructor C#. |
| Session End | 20:00:00 | Fin de Working time; fuera se cancelan órdenes y se aplana la posición. |
| Order Volume | 1 | Volumen de cada entrada; la salida usa el volumen realmente ejecutado. |

## Detalles del diagrama

- El C# ejecutable usa velas de cuatro horas. Cinco minutos es una adaptación replay explícita para obtener suficientes valores formados y rupturas durante el mes de aceptación.
- Solo BandPeriod, BandWidth y CandleType son parámetros del constructor C#. Session Start y Session End proceden del README y se implementan con Working time; el diagrama también expone el volumen.
- C# admite Position <= 0 para largo y Position >= 0 para corto, cerrando y revirtiendo la posición contraria a mercado. Este ejemplo entra deliberadamente solo con Position == 0 y no revierte.
- También cambia la ejecución: la fuente entra y sale a mercado, mientras el diagrama coloca la entrada en la banda rota y mantiene un límite de salida en la media. La entrada puede esperar un retroceso.
- Order replacing devuelve un objeto nuevo; las órdenes inicial y reemplazada convergen por lado en Combination<Order>, sin realimentar la salida al propio reemplazo.
- Se desactiva el ajuste al paso de precio porque el instrumento replay no lo aporta; los niveles del indicador no cambian.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
