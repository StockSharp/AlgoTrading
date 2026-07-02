# Estrategia de tendencia de cesta vectorial (puerto MT4)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta carpeta contiene el puerto API de alto nivel de StockSharp del asesor experto MetaTrader 4 **Vector** (script original: `MQL/8305/Vector.mq4`). La estrategia coordina hasta cuatro pares de divisas principales (EURUSD (primario), GBPUSD, USDCHF y USDJPY) y los negocia en la misma dirección cuando aparece una alineación de promedio móvil suavizada compartida. La conversión mantiene las ideas centrales de Vector mientras las adapta a patrones idiomáticos StockSharp.

## Lógica comercial

1. **Promedios móviles suavizados (SMMA)**: cada instrumento rastrea un SMMA rápido (3 períodos) y lento (7 períodos) calculado sobre los precios medios del período de negociación configurable (15 minutos de forma predeterminada).
2. **Filtro de tendencia vectorial**: se suman las diferencias entre cada par rápido/lento. Una suma positiva indica un impulso alcista sincronizado en toda la canasta, mientras que una suma negativa implica una presión bajista colectiva.
3. **Reglas de entrada** – la estrategia abre o revierte posiciones con órdenes de mercado solo cuando:
   - La tendencia de la cesta es positiva y la SMMA rápida del instrumento se mantiene por encima de la SMMA lenta (entrada larga).
   - La tendencia de la cesta es negativa y la SMMA rápida está por debajo de la SMMA lenta (entrada corta).
4. **Objetivo de pip del rango H4**: para cada instrumento, una suscripción de vela de 4 horas separada mide el rango anterior. Una quinta parte de ese rango (con un límite de 13 pips) se convierte en el objetivo de ganancias por posición, reflejando la salida de pips fijos del código MT4.
5. **Global Equity Guard**: los umbrales de ganancias y retiros basados en porcentajes (tomados de las entradas originales `PrcProfit` y `PrcLose`) cierran todas las posiciones abiertas una vez activadas.

## Diferencias clave frente al EA original

- Las **suscripciones de velas de alto nivel y vinculación de indicadores** de StockSharp reemplazan el sondeo de bajo nivel que se encuentra en MT4 (`SubscribeCandles().Bind(...)`).
- El puerto admite **instrumentos secundarios opcionales**: deje los espacios GBPUSD / USDCHF / USDJPY vacíos para operar solo con el valor principal.
- El tamaño del lote dinámico vinculado al margen de la cuenta MT4 se reemplazó con un parámetro `BaseVolume` limpio que está normalizado para los valores `VolumeStep`, `MinVolume` y `MaxVolume` de cada valor.
- La gestión comercial almacena los precios de entrada a través de `OnNewMyTrade` devoluciones de llamada, evitando búsquedas de valores de indicadores directos no permitidas.

## Parámetros

| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromMinutes(15)` | Plazo utilizado para los cálculos de SMMA y las comprobaciones de entrada. |
| `RangeCandleType` | `TimeSpan.FromHours(4)` | Se utiliza un marco de tiempo más alto para derivar el objetivo del pip adaptativo. |
| `SecondSecurity` | `null` | Ranura GBPUSD opcional (establezca un `Security` antes del inicio). |
| `ThirdSecurity` | `null` | Ranura USDCHF opcional. |
| `FourthSecurity` | `null` | Ranura USDJPY opcional. |
| `BaseVolume` | `1` | Volumen comercial solicitado por orden, normalizado a los límites de intercambio. |
| `TakeProfitPercent` | `0.5` | Ganancia bursátil global (en %) que desencadena una salida de toda la cartera. |
| `MaxDrawdownPercent` | `30` | Reducción de capital máxima permitida (en %) antes de que se cierren todas las posiciones. |

## Notas de uso

- Asigne el mismo conector y cartera a cada valor referenciado por los parámetros antes de iniciar la estrategia.
- Asegúrese de que la fuente de datos proporcione tanto el período de negociación como el período de rango para todos los instrumentos.
- Cuando no se proporcionan los valores opcionales, el cálculo del vector se adapta automáticamente a los instrumentos disponibles.
- Las salidas siempre ocurren con órdenes de mercado para que coincidan con el comportamiento original de MT4.

## Archivos

- `CS/VectorStrategy.cs`: implementación de C# siguiendo las pautas de alto nivel de StockSharp.
- `README.md`, `README_ru.md`, `README_zh.md`: documentación de estrategia en inglés, ruso y chino.
