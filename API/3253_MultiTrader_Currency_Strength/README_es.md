# Estrategia de MultiTrader Currency Strength (3253)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un puerto de alto nivel de StockSharp del panel público "MultiTrader" de MQL (código base #24786). El Asesor Experto original era un panel discrecional que mostraba la fortaleza relativa de las ocho divisas principales, activaba alertas visuales/auditivas cuando una divisa se volvía extremadamente fuerte o débil, y sugería qué par de Forex operar. La versión de StockSharp automatiza el mismo flujo de trabajo analítico y opcionalmente ejecuta trades en el par más fuerte vs. más débil.

La lógica calcula la posición porcentual del cierre de cada símbolo dentro de su rango de vela actual. Promediar los cruces relevantes produce una puntuación de fortaleza para AUD, CAD, CHF, EUR, GBP, JPY, NZD y USD. Cuando una divisa sube por encima del umbral de compra configurable y otra cae por debajo del umbral de venta, la estrategia recomienda el par construido a partir de esas divisas. Si el par existe en el universo configurado, la estrategia puede colocar automáticamente una orden de mercado en esa dirección.

## Modelo de fortaleza de divisas
La puntuación porcentual de un símbolo se calcula como:

```
percent = 100 * (Close - Low) / (High - Low)
```

La fortaleza de cada divisa se deriva de siete cruces, reflejando la implementación de MQL. Se aplica una inversión `100 - percent` cuando la divisa aparece como divisa cotizada en el par:

| Divisa | Componentes |
| --- | --- |
| AUD | AUDJPY, AUDNZD, AUDUSD, 100-EURAUD, 100-GBPAUD, AUDCHF, AUDCAD |
| CAD | CADJPY, 100-NZDCAD, 100-USDCAD, 100-EURCAD, 100-GBPCAD, 100-AUDCAD, CADCHF |
| CHF | CHFJPY, 100-NZDCHF, 100-USDCHF, 100-EURCHF, 100-GBPCHF, 100-AUDCHF, 100-CADCHF |
| EUR | EURJPY, EURNZD, EURUSD, EURCAD, EURGBP, EURAUD, EURCHF |
| GBP | GBPJPY, GBPNZD, GBPUSD, GBPCAD, 100-EURGBP, GBPAUD, GBPCHF |
| JPY | 100-AUDJPY, 100-CHFJPY, 100-CADJPY, 100-EURJPY, 100-GBPJPY, 100-NZDJPY, 100-USDJPY |
| NZD | NZDJPY, 100-GBPNZD, NZDUSD, NZDCAD, 100-EURNZD, 100-AUDNZD, NZDCHF |
| USD | 100-AUDUSD, USDCHF, USDCAD, 100-EURUSD, 100-GBPUSD, USDJPY, 100-NZDUSD |

La estrategia almacena la última vela completada por par, mantiene el porcentaje más reciente y actualiza las fortalezas de las divisas después de cada actualización.

## Trading y alertas
1. Cuando las ocho divisas tienen datos válidos, la estrategia registra una instantánea (de la más fuerte a la más débil).
2. Si el valor más fuerte es **≥ BuyLevel** y el valor más débil es **≤ SellLevel**, se genera una sugerencia de trading.
3. La estrategia intenta encontrar el par directo (divisa fuerte como base, divisa débil como cotización). Si no existe, verifica la orientación inversa y finalmente recurre a pares que involucran USD.
4. El par detectado y la dirección se registran. Si `EnableAutoTrading` es `true` y `OrderVolume` es positivo, la estrategia emite una orden de mercado en la dirección sugerida. Las posiciones opuestas se aplanan automáticamente aumentando el tamaño de la orden.

Las señales se limitan recordando el último par sugerido y el lado, evitando alertas duplicadas hasta que el mercado salga de la zona de umbral.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Universe` | Lista de objetos `Security` que representan los pares de FX (se recomiendan 28 principales). | Requerido |
| `CandleType` | Especificación de vela utilizada para los cálculos (Diario, Semanal, Mensual, etc.). | Velas diarias |
| `BuyLevel` | Umbral por encima del cual una divisa se trata como sobrecomprada. | 90 |
| `SellLevel` | Umbral por debajo del cual una divisa se trata como sobrevendida. | 10 |
| `EnableAutoTrading` | Habilita o deshabilita la colocación automática de órdenes. | false |
| `OrderVolume` | Volumen para enviar con órdenes de mercado cuando el trading automático está habilitado. | 1 |
| `SymbolPrefix` | Prefijo opcional utilizado por el broker/exchange (p.ej., `m.`). | "" |
| `SymbolSuffix` | Sufijo opcional utilizado por el broker/exchange (p.ej., `.FX`). | "" |

## Pasos de configuración
1. **Configuración del universo.** Añada los 28 cruces de Forex principales al universo de la estrategia. Los códigos deben coincidir con los nombres canónicos de los pares (p.ej., `EURUSD`). Use `SymbolPrefix`/`SymbolSuffix` si su broker añade decoraciones.
2. **Selección de temporalidad.** Elija el `CandleType` deseado. Las velas diarias, semanales y mensuales reproducen los modos del panel original.
3. **Ajuste de umbrales.** Ajuste `BuyLevel`/`SellLevel` para controlar cuán extrema debe ser la fortaleza antes de generar una señal.
4. **Trading automático (opcional).** Establezca `EnableAutoTrading` en true y defina `OrderVolume`. Deje la bandera en false para recibir solo registros informativos.

## Notas de migración
- La capa GUI completa del panel MQL original se omite intencionalmente. Toda la salida está disponible a través del registro de la estrategia.
- Las alertas se emiten como entradas `LogInfo`; las notificaciones push/email/escritorio no fueron portadas.
- Los cálculos automáticos de stop-loss/objetivo de la versión MQL no son compatibles; los traders deben gestionar el riesgo usando los módulos de protección de StockSharp o controles de riesgo externos.
- El helper de licencias basado en DES incrustado en el script MQL fue eliminado.

## Uso recomendado
- Despliegue la estrategia dentro de una sesión de conector que proporcione velas en tiempo real e históricas para todos los pares relevantes.
- Combine con un widget de gráfico para visualizar el par sugerido y monitorear las series de velas subyacentes.
- Use los parámetros `StartProtection` de StockSharp o estrategias de riesgo separadas para hacer cumplir stops/objetivos globales.

## Consideraciones de prueba
- Verifique que su fuente de datos entregue velas completadas para la temporalidad seleccionada; la estrategia ignora las barras no terminadas.
- Si algunos pares faltan en el universo, la divisa correspondiente no puede calcularse y no se producirá ninguna señal.
- Al evaluar el rendimiento histórico, asegúrese de que el universo permanezca estático durante todo el backtest para evitar brechas de fortaleza.
