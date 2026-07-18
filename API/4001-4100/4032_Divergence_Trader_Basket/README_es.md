# Estrategia de cesta de comerciantes de divergencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un puerto StockSharp del asesor experto "Divergence Trader" MetaTrader. Compara dos medias móviles simples.
se calcula sobre fuentes de precios configurables y mide su diferencia (divergencia). Cuando la distancia entre lo rápido y lo lento
promedio cae dentro de un corredor neutral, el algoritmo asume que el impulso está a punto de reanudarse y abre una posición en el
dirección del sesgo predominante. La implementación utiliza sólo velas completadas de un período de tiempo seleccionado y se basa en el
API de alto nivel con enlaces de indicadores.

## Parámetros
- **Tamaño del lote**: volumen de operaciones enviado con cada nueva posición. El valor está alineado con el paso de volumen del instrumento.
- **Período/precio rápido SMA**: duración y fuente de precio para el promedio móvil rápido.
- **Período/precio lento SMA**: duración y fuente de precio para el promedio móvil lento.
- **Umbral de compra**: se requiere una divergencia positiva mínima antes de abrir una posición larga.
- **Umbral de permanencia fuera**: divergencia máxima permitida para nuevas entradas; los valores fuera de este rango desactivan el comercio.
- **Take Profit (pips)** – objetivo de ganancias expresado en pips. Deshabilitado cuando se establece en cero.
- **Stop Loss (pips)** – tolerancia a la pérdida en pips. Deshabilitado cuando se establece en cero.
- **Trailing Stop (pips)**: distancia de seguimiento activada después de que la operación se vuelve rentable. Deshabilitado cuando es cero.
- **Activador/búfer de punto de equilibrio (pips)**: se requiere ganancia de pip antes de proteger la posición en el punto de equilibrio y un búfer opcional para
compensar el punto de equilibrio del precio de entrada.
- **Beneficio de la cesta/Pérdida de la cesta**: umbrales basados en el capital de la cuenta que aplanan todas las posiciones cuando se alcanzan. El control de pérdidas es
deshabilitado de forma predeterminada.
- **Hora de inicio/hora de finalización**: ventana de negociación en hora local. Cuando ambos valores son iguales la estrategia opera todo el día.
- **Tipo de vela**: período de tiempo utilizado tanto para la generación de señales como para la gestión de riesgos.

## Lógica de trading
1. Suscríbase a la serie de velas configuradas y calcule las medias móviles simples rápidas y lentas.
2. Trabaje solo con velas terminadas para evitar el ruido dentro de la barra y mantenerse cerca del comportamiento original EA.
3. Realice un seguimiento de la divergencia (rápida menos lenta) calculada en la vela previamente terminada:
   - Si la divergencia es positiva y permanece entre el **Umbral de compra** y el **Umbral de permanencia fuera**, envíe una orden de compra de mercado.
   - Si la divergencia es negativa y su valor absoluto permanece dentro del corredor, envíe una orden de venta de mercado.
4. Las operaciones se ignoran fuera del horario permitido o cuando la estrategia ya tiene una posición abierta.

## Gestión de Puestos
- **Control de equilibrio**: cuando la ganancia flotante alcanza el nivel de activación, la estrategia almacena un nivel de parada de equilibrio (opcionalmente
desplazado por el buffer). Una vela que toca este nivel cierra la posición.
- **Trailing stop**: una vez que el beneficio supera la distancia de seguimiento, el nivel de stop sigue el precio más favorable, siempre
permaneciendo detrás de él por el número configurado de pips.
- **Take Profit / Stop Loss** – salidas fijas calculadas a partir del precio de entrada en unidades de pips.
- **Protección de la cesta**: el capital de la cartera se compara con los límites de pérdidas y ganancias configurados. Golpear cualquiera de los límites
cierra la posición actual y cancela las órdenes activas, emulando la rutina "CloseEverything" de la versión MQL.

## Notas de uso
- El corredor de divergencia es simétrico: ampliar el **umbral de permanencia** permite que las operaciones permanezcan abiertas por más tiempo, mientras que lo estrecha
aumenta la frecuencia de las señales.
- Las opciones de fuente de precios corresponden a valores StockSharp `CandlePrice`, lo que permite utilizar valores de apertura, cierre, mediana o típico.
precios como en MetaTrader.
- La estrategia traza velas, tanto promedios móviles como órdenes ejecutadas en un área del gráfico para monitorear y depurar.
- Las funciones de administración del dinero dependen de los datos de la cartera. Cuando se ejecuta en un sandbox sin estadísticas de cartera, los controles de la cesta se
ignorado automáticamente.
