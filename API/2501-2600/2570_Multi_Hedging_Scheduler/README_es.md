# Estrategia de Planificador de Cobertura Múltiple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Planificador de Cobertura Múltiple** es una conversión directa de StockSharp del asesor experto original MetaTrader 5 `MultiHedg_1.mq5`. La estrategia está diseñada para cuentas que permiten cobertura y puede gestionar hasta diez instrumentos diferentes simultáneamente. Abre posiciones de la misma dirección durante una ventana de trading configurable y proporciona lógica de salida a nivel de portafolio basada en tiempo o umbrales de porcentaje de equity.

En lugar de depender de indicadores, la estrategia usa un flujo de velas de un minuto (configurable) puramente como fuente de temporización. Cada vela terminada activa verificaciones para abrir operaciones, cerrar todo cuando la ventana de trading expira y hacer cumplir reglas de riesgo basadas en equity. La estrategia es por lo tanto adecuada para portafolios donde la ejecución está impulsada por horario en lugar de patrones de precio.

## Lógica de trading
1. **Selección de instrumentos** – Se pueden habilitar hasta diez símbolos. Para cada entrada habilitada la estrategia resuelve el ticker a través del `SecurityProvider`, se suscribe a velas del tipo configurado y usa la misma lógica en todos los instrumentos.
2. **Ventana de trading** – cuando la marca de tiempo de la vela entra en la ventana `TradeStartTime` (que dura `TradeDuration`), la estrategia abre una posición de mercado en la dirección configurada (`TradeDirection`) para cada símbolo habilitado que no tenga ya una posición abierta en esa dirección. Si existe una posición opuesta, el volumen se aumenta para voltear al lado deseado.
3. **Protección de equity** – si `CloseByEquityPercent` está habilitado y el equity del portafolio se desvía del saldo inicial en `PercentProfit` o `PercentLoss`, cada posición abierta gestionada por la estrategia se cierra.
4. **Salida basada en tiempo** – si `UseTimeClose` está habilitado, la estrategia cierra todas las posiciones rastreadas cuando el reloj alcanza la ventana `CloseTime` (que dura `TradeDuration`).
5. **Registro** – acciones como entradas, salidas basadas en equity y salidas basadas en tiempo se registran a través de llamadas `LogInfo` para trazabilidad.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `TradeDirection` | Dirección de todas las órdenes (`Buy` o `Sell`). | Buy |
| `TradeStartTime` | Hora local cuando se abre la ventana de entrada. | 19:51 |
| `TradeDuration` | Duración de las ventanas de entrada y cierre. | 00:05:00 |
| `UseTimeClose` | Habilita la ventana de cierre basada en tiempo. | true |
| `CloseTime` | Hora local cuando se abre la ventana de cierre. | 20:50 |
| `CloseByEquityPercent` | Habilita el cierre de todas las posiciones en umbrales de equity. | true |
| `PercentProfit` | Porcentaje de ganancia en equity que activa un cierre global. | 1.0 |
| `PercentLoss` | Porcentaje de drawdown en equity que activa un cierre global. | 55.0 |
| `CandleType` | Tipo de vela usado como controlador de programación. | Marco temporal de 1 minuto |
| `UseSymbol0..9` | Alterna el trading para el símbolo correspondiente. | true para los símbolos 0–5, false para 6–9 |
| `Symbol0..9` | Ticker para cada slot, resuelto vía `SecurityProvider.LookupById`. | Ver predeterminados abajo |
| `Volume0..9` | Volumen de orden para cada slot (lotes en el EA original). | 0.1–1.0 |

**Configuración de símbolos predeterminada**

| Slot | Habilitado | Símbolo | Volumen |
|------|---------|--------|--------|
| 0 | ✔ | EURUSD | 0.1 |
| 1 | ✔ | GBPUSD | 0.2 |
| 2 | ✔ | GBPJPY | 0.3 |
| 3 | ✔ | EURCAD | 0.4 |
| 4 | ✔ | USDCHF | 0.5 |
| 5 | ✔ | USDJPY | 0.6 |
| 6 | ✖ | USDCHF | 0.7 |
| 7 | ✖ | GBPUSD | 0.8 |
| 8 | ✖ | EURUSD | 0.9 |
| 9 | ✖ | USDJPY | 1.0 |

## Notas de uso
- Asegurarse de que la cuenta soporte cobertura si se planea replicar el comportamiento original de MetaTrader. En cuentas netting la estrategia automáticamente compensará posiciones opuestas al cambiar de dirección.
- Proporcionar identificadores de instrumentos en los parámetros `SymbolX` exactamente como se conocen en el `SecurityProvider` de StockSharp (por ejemplo `EURUSD@FXCM`).
- El flujo de velas solo se usa para impulsar la lógica de programación. Ajustar `CandleType` si la fuente de datos proporciona un intervalo de agregación diferente.
- La protección de equity compara el equity en vivo contra el saldo capturado en `OnStarted`. Reiniciar la estrategia resetea el saldo de referencia.
- La estrategia no incluye órdenes de stop protector o take-profit. Las salidas globales son controladas únicamente por los porcentajes de equity y la ventana de cierre.

## Notas de conversión
- El experto MT5 original usaba `OnTick`. En la versión StockSharp, las velas terminadas sustituyen a los eventos de tick para evaluar ventanas de tiempo de manera de alto nivel impulsada por eventos.
- El filtrado por número mágico es innecesario porque la estrategia opera dentro del contenedor de estrategias de StockSharp; por lo tanto `CloseAllManagedPositions` solo itera a través de los símbolos configurados.
- Las alertas sonoras y los comentarios en el gráfico fueron omitidos, pero la estrategia registra todas las acciones críticas vía `LogInfo` para una auditoría más fácil.
