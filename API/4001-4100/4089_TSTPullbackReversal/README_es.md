# Estrategia de reversión de retroceso de TST
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de reversión de retroceso TST** busca reversiones agresivas dentro de la barra. Se convirtió del MetaTrader 4 asesor experto original `TST.mq4` y se reconstruyó utilizando el StockSharp API de alto nivel. El método busca velas en las que el precio se ha alejado bruscamente de la apertura de la vela después de establecer un extremo intradiario, luego desvanece ese movimiento esperando una reversión a la media. La estrategia opera tanto a largo como a corto y utiliza niveles estáticos de stop-loss y take-profit medidos en pasos de precios.

## Lógica de señal
- **Configuración larga**
  1. La vela cierra por debajo de su apertura (`Open > Close`).
  2. La distancia entre el máximo de la vela y el cierre es mayor que `GapPoints * PriceStep`.
  3. No se ejecutó ninguna operación anteriormente en la misma barra.
Cuando está satisfecho, la estrategia cierra cualquier exposición corta y compra `OrderVolume` unidades (más el tamaño necesario para pasar de una posición corta a una larga).

- **Configuración corta**
  1. La vela cierra por encima de su apertura (`Close > Open`).
  2. La distancia entre el cierre y el mínimo de la vela es mayor que `GapPoints * PriceStep`.
  3. No se ejecutó ninguna operación anteriormente en la misma barra.
Cuando está satisfecho, la estrategia cierra cualquier exposición larga y vende `OrderVolume` unidades (más el tamaño necesario para pasar de una posición larga a una corta).

## Gestión de Puestos
- Una nueva operación asigna inmediatamente niveles estáticos de stop-loss y take-profit calculados a partir del precio de ejecución y los parámetros `StopLossPoints`/`TakeProfitPoints`.
- En cada vela terminada, la estrategia verifica el máximo/mínimo de la vela para ver si se tocó el stop o el objetivo y sale de la posición si se activa. Los controles de limitación de pérdidas tienen prioridad sobre los controles de obtención de beneficios.
- Después de una salida, los niveles de riesgo almacenados se borran, pero la estrategia aún recuerda el tiempo de la barra para evitar volver a entrar durante la misma vela (reproduciendo la guardia `NevBar()` de la versión MQL4).

## Parámetros
- `StopLossPoints` (por defecto `500`): distancia desde la entrada hasta el stop de protección, expresada en incrementos de precio.
- `TakeProfitPoints` (predeterminado `100`): distancia desde la entrada hasta el objetivo de ganancias, expresada en incrementos de precio.
- `GapPoints` (predeterminado `500`): retroceso mínimo entre el extremo de la vela y el cierre requerido para generar una señal.
- `OrderVolume` (predeterminado `0.1`): cantidad enviada con cada nueva orden de mercado.
- `CandleType` (predeterminado `1 hour`): período de tiempo de las velas suministradas a través de `SubscribeCandles`.

Todas las configuraciones basadas en la distancia se multiplican por el `PriceStep` del instrumento. Si el valor no informa un paso, la estrategia vuelve a `1`.

## Notas de implementación
- La conversión utiliza el nivel alto API de StockSharp y no crea colecciones de indicadores personalizados.
- Sólo se procesan velas terminadas para seguir siendo compatibles con el Diseñador de estrategias; esto aproxima las decisiones intrabarra del robot MT4 utilizando datos de barra completos.
- Una bandera dedicada `_lastSignalBarTime` replica la protección `NevBar()` del código MQL4 para que solo se pueda abrir una orden por vela.
- El manejo del volumen de órdenes refleja el comportamiento de MT4: las posiciones opuestas existentes se aplanan antes de establecer la nueva dirección en una orden de mercado única.
- Las órdenes de stop-loss y take-profit se simulan dentro de la lógica de la estrategia (en lugar de órdenes del lado del servidor) para igualar las distancias originales mientras se mantiene el control dentro de StockSharp.

## Consejos de uso
- Elija `GapPoints` en relación con la volatilidad del instrumento negociado; los valores más grandes reducen la frecuencia del comercio pero filtran retrocesos más pequeños.
- Debido a que las comprobaciones de parada y objetivo dependen de velas terminadas, considere usar velas de menor duración si necesita una ejecución más estricta.
- Combine la estrategia con filtros adicionales (tendencia, hora del día, volumen) cuando la implemente en mercados reales para reducir las operaciones irregulares.
