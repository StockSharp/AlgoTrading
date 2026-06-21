# Estrategia BlauTvi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el experto MQL5 `Exp_BlauTVI` en una estrategia de alto nivel de StockSharp. Utiliza el **Blau True Volume Index (TVI)** para detectar reversiones en el flujo de volumen de ticks.

## Idea

El True Volume Index separa los up-ticks y down-ticks y los suaviza con tres medias móviles exponenciales. El valor final oscila entre -100 y +100 y representa el dominio de compradores o vendedores. La estrategia abre una posición larga cuando el indicador gira hacia arriba tras un descenso y abre una posición corta cuando el indicador gira hacia abajo tras una subida. Las posiciones existentes en la dirección opuesta se cierran.

## Parámetros

- `Length1` – primer período de suavizado para up-ticks y down-ticks.
- `Length2` – segundo período de suavizado.
- `Length3` – período de suavizado final aplicado al TVI.
- `CandleType` – tipo de velas utilizado para los cálculos (por defecto: marco temporal de 4 horas).
- `Allow Buy Open` – habilitar apertura de posiciones largas.
- `Allow Sell Open` – habilitar apertura de posiciones cortas.
- `Allow Buy Close` – habilitar cierre de posiciones largas cuando aparece una señal de venta.
- `Allow Sell Close` – habilitar cierre de posiciones cortas cuando aparece una señal de compra.
- `Enable Stop Loss` – usar protección de stop-loss en puntos.
- `Stop Loss` – valor del stop-loss en puntos.
- `Enable Take Profit` – usar protección de take-profit en puntos.
- `Take Profit` – valor del take-profit en puntos.
- `Volume` – volumen de la orden en lotes.

## Señales

1. **Compra** – cuando el valor anterior del TVI es menor que el de antes y el valor actual del TVI es mayor que el anterior. Si está habilitado, se cierran las posiciones cortas existentes.
2. **Venta** – cuando el valor anterior del TVI es mayor que el de antes y el valor actual del TVI es menor que el anterior. Si está habilitado, se cierran las posiciones largas existentes.

Solo se procesan velas finalizadas y todos los cálculos usan el volumen de ticks de la vela. El stop-loss y el take-profit son opcionales y se expresan en puntos de precio.

## Notas

La estrategia usa la API de alto nivel: se suscribe a velas, calcula el indicador internamente con instancias de `ExponentialMovingAverage` y gestiona posiciones con los métodos `BuyMarket` y `SellMarket`. El gráfico muestra el indicador TVI junto con las operaciones ejecutadas por la estrategia.
