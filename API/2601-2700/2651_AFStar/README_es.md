# Estrategia AFStar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia AFStar detecta cambios de impulso a corto plazo combinando un amplio
rango de cruces de EMA rápida/lenta con un filtro de ruptura de canal basado en
Williams %R. Solo cuando ambos componentes coinciden en dirección la estrategia genera
señales accionables.

Se produce una flecha de compra cuando al menos una EMA rápida (dentro del intervalo
configurado) cruza hacia arriba una EMA lenta compatible mientras el oscilador basado
en Williams %R escapa de la banda inferior tras haber permanecido dentro de la zona
neutral. Una flecha de venta se genera por las condiciones simétricas para cruces
bajistas y una salida desde la banda superior. Las señales se ejecutan tras el número
configurado de barras definido por el parámetro **Signal Bar**, igual que en el experto
MetaTrader original.

Una vez abierta una posición, la estrategia puede adjuntar opcionalmente niveles
protectores de stop loss y take profit expresados en pasos de precio. Dichas
protecciones se verifican en cada vela cerrada. Todas las operaciones utilizan el
parámetro constante **Order Volume**, por lo que las complejas reglas de gestión
monetaria de la versión MQL5 son reemplazadas por un enfoque de tamaño fijo más simple.

## Lógica de entrada

- **Largo:**
  - Al menos una EMA rápida dentro de `[Start Fast, End Fast]` sube por encima de una
    EMA lenta dentro de `[Start Slow, End Slow]` usando el incremento `Step Period`.
  - El canal Williams %R, evaluado con valores de riesgo en el rango
    `[Start Risk, End Risk]` y `Risk Step`, detecta una ruptura por encima del límite
    superior tras haber permanecido en la banda neutral.
  - Las posiciones cortas opcionales se cierran previamente cuando **Enable Sell Exits**
    está activado.
- **Corto:**
  - Cruce simétrico y ruptura de Williams %R en la dirección opuesta.
  - Los cierres de largos opcionales ocurren primero cuando **Enable Buy Exits** está
    habilitado.

## Lógica de salida

- Las flechas opuestas cierran posiciones cuando las flags de salida correspondientes
  están habilitadas (las flechas de compra cierran cortos, las de venta cierran largos).
- Niveles opcionales de stop loss y take profit medidos en pasos de precio pueden cerrar
  posiciones antes si el precio alcanza esos umbrales.

## Parámetros

- **Order Volume** – tamaño de operación usado para órdenes de mercado.
- **Candle Type** – marco temporal para los datos de mercado (por defecto velas de 4 horas).
- **Start Fast / End Fast / Step Period** – rango de EMA rápida para el escaneo de cruces.
- **Start Slow / End Slow** – rango de EMA lenta emparejada con los valores de EMA rápida.
- **Start Risk / End Risk / Risk Step** – límites del escaneo de riesgo de Williams %R.
- **Signal Bar** – número de barras finalizadas a esperar antes de ejecutar una señal.
- **Stop Loss (pips)** – distancia opcional de stop loss en pasos de precio.
- **Take Profit (pips)** – distancia opcional de take profit en pasos de precio.
- **Enable Buy Entries / Enable Sell Entries** – permitir entradas largas o cortas.
- **Enable Buy Exits / Enable Sell Exits** – habilitar el cierre en la dirección opuesta.

## Notas

- La estrategia mantiene hasta 512 velas recientes para evaluar la lógica AFStar.
- Si los pasos de precio no están disponibles para el instrumento, se usa el valor 1
  al calcular las distancias de stop-loss y take-profit.
- Las señales están en cola, de modo que **Signal Bar = 0** ejecuta inmediatamente,
  mientras que valores mayores retrasan la ejecución por ese número de barras completadas.
