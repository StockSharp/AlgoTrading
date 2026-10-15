# Estrategia Stochastic Momentum Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Stochastic Momentum Filter** es un port de StockSharp del asesor experto de MetaTrader `Stochastic.mq4` (carpeta `MQL/23473`). El robot original combina dos osciladores estocásticos, medias móviles ponderadas linealmente (LWMA), un filtro de desviación de momentum y una verificación de tendencia MACD de marco temporal superior. Esta versión en C# recrea los mismos bloques constructivos sobre la API de alto nivel de StockSharp y mantiene el flujo de trabajo de confirmación multicapa:

1. **Filtro de tendencia** – una LWMA rápida debe estar por encima (o debajo) de una LWMA lenta antes de que se permitan trades largos (o cortos).
2. **Confirmación del oscilador** – tanto un estocástico rápido (5/2/2) como un estocástico lento (21/4/10) deben coincidir en zonas de sobreventa/sobrecompra.
3. **Desviación de momentum** – al menos una de las tres lecturas de momentum más recientes debe desviarse de la línea base 100 en más de un umbral configurable, coincidiendo con el uso de la función MT4 `iMomentum` del experto.
4. **MACD de marco temporal superior** – la línea principal de MACD en un marco temporal superior configurable debe mantenerse por encima de la línea de señal para largos (y por debajo para cortos). El marco temporal predeterminado de 30 días aproxima el filtro mensual original.
5. **Lógica de riesgo** – stop loss, take profit y trailing opcional se manejan a través de `StartProtection`, reflejando los parámetros protectores del EA. Los giros de posición cierran la exposición opuesta automáticamente antes de establecer la nueva posición neta.

La estrategia se suscribe a dos flujos de velas: el marco temporal de trading y el marco temporal superior que alimenta el filtro MACD. Todos los cálculos se realizan con indicadores de StockSharp y se procesan a través de los helpers de alto nivel `Bind`.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `StochasticBuyLevel` | `30` | Nivel de sobreventa que ambos osciladores estocásticos deben romper para configuraciones largas. |
| `StochasticSellLevel` | `80` | Nivel de sobrecompra que ambos osciladores estocásticos deben alcanzar para configuraciones cortas. |
| `FastMaPeriod` | `6` | Longitud del filtro de tendencia LWMA rápido. |
| `SlowMaPeriod` | `85` | Longitud del filtro de tendencia LWMA lento. |
| `FastStochasticPeriod` | `5` | Período `%K` del oscilador estocástico rápido. |
| `FastStochasticSignal` | `2` | Período de suavizado `%D` del estocástico rápido. |
| `FastStochasticSmoothing` | `2` | Suavizado adicional aplicado al estocástico rápido (coincide con el "slowing" de MT4). |
| `SlowStochasticPeriod` | `21` | Período `%K` del oscilador estocástico lento. |
| `SlowStochasticSignal` | `4` | Período de suavizado `%D` del estocástico lento. |
| `SlowStochasticSmoothing` | `10` | Suavizado adicional aplicado al estocástico lento. |
| `MomentumPeriod` | `14` | Lookback del oscilador de momentum (igual que `iMomentum` de MT4). |
| `MomentumThreshold` | `0.3` | Desviación absoluta mínima desde la línea base 100 requerida dentro de los últimos tres valores de momentum. |
| `MacdFastPeriod` | `12` | Período EMA rápido para el MACD de marco temporal superior. |
| `MacdSlowPeriod` | `26` | Período EMA lento para el MACD de marco temporal superior. |
| `MacdSignalPeriod` | `9` | Período EMA de señal para el MACD de marco temporal superior. |
| `TakeProfitPoints` | `50` | Distancia de take-profit (en puntos de precio). Establecer en `0` para deshabilitar. |
| `StopLossPoints` | `20` | Distancia de stop-loss (en puntos de precio). Establecer en `0` para deshabilitar. |
| `EnableTrailing` | `true` | Habilita el trailing de StockSharp de la protección de stop. |
| `TradeVolume` | `1` | Tamaño de posición neta objetivo en cada señal. |
| `MaxNetPositions` | `1` | Limita la exposición neta apilada (multiplica `TradeVolume`). |
| `CandleType` | Marco temporal de `15m` | Marco temporal de trading principal. |
| `HigherTimeframe` | Marco temporal de `30d` | Marco temporal usado para confirmación MACD. |

## Lógica de trading
1. **Preparación de indicadores** – la estrategia vincula ambas LWMAs, ambos osciladores estocásticos, el indicador de momentum y el MACD a sus respectivos flujos de velas.
2. **Historial de momentum** – la distancia absoluta del oscilador de momentum desde 100 se almacena para las últimas tres barras finalizadas. Esto replica los arrays `MomLevelB/MomLevelS` del EA.
3. **Reglas de entrada**
   - **Largo**: LWMA rápida por encima de la LWMA lenta, valores de `%K` y `%D` de ambos estocásticos por debajo de `StochasticBuyLevel`, desviación de momentum por encima de `MomentumThreshold`, y línea principal de MACD por encima de la línea de señal.
   - **Corto**: LWMA rápida por debajo de la LWMA lenta, valores de `%K` y `%D` de ambos estocásticos por encima de `StochasticSellLevel`, desviación de momentum por encima del umbral, y línea principal de MACD por debajo de la línea de señal.
4. **Manejo de posiciones** – las órdenes se envían con `BuyMarket`/`SellMarket`. Cuando aparece una señal de reversión, la estrategia cierra automáticamente cualquier exposición neta opuesta antes de establecer la nueva dirección.
5. **Protección** – `StartProtection` aplica las distancias configuradas de take-profit y stop-loss (en puntos). Cuando `EnableTrailing` es true, StockSharp gestiona el trailing de stop de manera similar a la rutina de trailing del EA.

## Diferencias con la versión MQL
- **Escalado de volumen**: el EA escala tamaños de lote usando `LotExponent` y permite múltiples tickets simultáneos. El port de StockSharp se centra en la exposición neta y apunta a un único `TradeVolume` por dirección (limitado por `MaxNetPositions`).
- **Gestión de margen**: las verificaciones de margen, las paradas de equity y las funciones de notificación del script original no se reproducen porque dependen de APIs de cuenta de MT4.
- **Niveles de congelación**: las verificaciones específicas de congelación de nivel bajo del bróker se omiten; el enrutamiento de órdenes de StockSharp maneja las restricciones del exchange.
- **Toggle de break-even**: el helper "move to breakeven" de MT4 es reemplazado por la protección de trailing de StockSharp.

## Notas de uso
1. Asignar un instrumento y conector, luego iniciar la estrategia. Se suscribirá automáticamente tanto al marco temporal de trading como al marco temporal superior requerido por el filtro MACD.
2. Si su fuente de datos no admite un tipo de vela de 30 días, ajuste `HigherTimeframe` a un intervalo admitido (p. ej., semanal o diario). La lógica de confirmación de tendencia aún espera que la línea principal de MACD permanezca en el mismo lado de su línea de señal.
3. Establecer `TradeVolume` para que coincida con sus unidades de cartera.
4. Establecer `TakeProfitPoints`/`StopLossPoints` en cero si las órdenes protectoras deben estar deshabilitadas.
5. Todos los comentarios dentro del código están escritos en inglés, y la indentación usa tabulaciones.

## Archivos
- `CS/StochasticMomentumFilterStrategy.cs` – implementación de StockSharp de la lógica de la estrategia.
- `README.md` – documentación en inglés (este archivo).
- `README_ru.md` – documentación en ruso.
- `README_zh.md` – documentación en chino.
