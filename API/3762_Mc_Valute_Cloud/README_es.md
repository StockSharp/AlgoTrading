# Estrategia de nube de McValute
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta carpeta contiene el puerto StockSharp del asesor experto MetaTrader "Mc_valute". El robot original combinó un corto
media móvil exponencial (EMA) con tres medias móviles suavizadas, un filtro de nube Ichimoku y múltiples instancias MACD mientras
escalando hacia la tendencia. La implementación StockSharp mantiene la pila de confirmación de tendencias principales pero simplifica la gestión de posiciones
a una sola exposición en cada dirección para que la lógica encaje naturalmente en el nivel alto API.

## Lógica comercial

1. **Filtro de precios EMA**: el `FilterMaLength` EMA debe ubicarse encima (para largos) o debajo (para cortos) de los dos movimientos suavizados.
promedios (`BlueMaLength` y `LimeMaLength`). Los promedios suavizados emulan las líneas "azul" y "lima" de la plantilla MT4.
2. **Ichimoku confirmación en la nube**: el EMA también tiene que estar fuera de la nube. Las operaciones largas requieren el filtro EMA encima de ambos
Senkou se extiende mientras que las operaciones cortas exigen que permanezca por debajo del fondo de la nube.
3. **MACD verificación de impulso**: la línea principal MACD debe estar por encima de su línea de señal para entradas largas y debajo de ella para entradas cortas.
Solo se conserva el primer conjunto MACD del EA original porque las copias restantes se desactivaron en la versión final MQL.
4. **Gestión de posición única**: cada vez que aparece una nueva señal, la estrategia compensa cualquier posición opuesta existente y abre una
comercio nuevo con el `Volume` configurado. Las órdenes de protección se actualizan inmediatamente después de enviarse la orden de mercado.
5. **Evaluación vela por vela**: todos los indicadores operan en el período definido por `CandleType`. Se toman decisiones comerciales
solo en velas terminadas para reflejar el controlador MT4 `start()` que procesó barras cerradas.

## Gestión de riesgos

- `TakeProfit` y `StopLoss` se miden en puntos de precio. Después de cada entrada, el ayudante `SetTakeProfit` y `SetStopLoss`
Las funciones se llaman utilizando el tamaño de posición resultante esperado, que refleja el comportamiento de MT4 donde se aplicaron paradas por
billete.
- El asesor experto original piramidó hasta tres órdenes adicionales utilizando la distancia `Step`. El puerto StockSharp mantiene un
posición única para permanecer dentro de los ayudantes de orden de alto nivel. Los usuarios que necesitan escalar pueden aumentar `Volume` o clonar el
estrategia en varias carteras.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `Volume` | Tamaño comercial base utilizado por las llamadas `BuyMarket`/`SellMarket` de alto nivel. |
| `CandleType` | Serie de velas primarias que impulsan los indicadores y la lógica comercial. |
| `FilterMaLength` | Longitud del filtro de tendencia EMA. |
| `BlueMaLength`, `LimeMaLength` | Longitudes de las dos medias móviles suavizadas que actúan como banda direccional. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | EMA longitudes para la confirmación MACD. |
| `TenkanLength`, `KijunLength`, `SenkouLength` | Ichimoku Configuración de Kinko Hyo para el filtro de nube. |
| `TakeProfit`, `StopLoss` | Distancias de protección expresadas en puntos de precio. |

## Notas de uso

1. **Cambios de indicador**: MetaTrader permitió parámetros de "desplazamiento" distintos de cero al crear los promedios móviles suavizados. StockSharp
Los indicadores funcionan en la barra actual, por lo tanto el puerto ignora esos cambios manteniendo los períodos originales.
2. **MACD variantes**: el código fuente declaró tres bloques MACD pero solo el primero participó en señales en vivo. el puerto
sigue ese comportamiento; Se pueden volver a habilitar filtros MACD adicionales duplicando los enlaces del indicador.
3. **Escalamiento de operaciones**: el robot MT4 envió hasta tres órdenes promedio separadas por `Step` puntos. Este comportamiento está documentado
pero se omite intencionalmente porque las estrategias de alto nivel operan con una única posición agregada.
4. **Bloque de protección**: `StartProtection()` se invoca una vez durante el inicio para que la infraestructura integrada supervise que se detenga
y objetivos de pedidos incluso después de las reconexiones.

## Archivos

- `CS/McValuteCloudStrategy.cs`: implementación de C# utilizando la estrategia de alto nivel API con enlaces de indicadores y detalles
comentarios.
- `README.md` – Documentación en inglés (este archivo).
- `README_zh.md` – Traducción al chino simplificado.
- `README_ru.md` – traducción al ruso.
