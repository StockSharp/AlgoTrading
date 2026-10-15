# Estrategia RPM5 BullsBearsEyes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia RPM5 BullsBearsEyes** es una adaptación de C# del experto MetaTrader 4 *Rpm5_mt4v1*. El asesor reconstruyó el oscilador BullsBearsEyes personalizado a partir de las lecturas de Bulls Power y Bears Power y abrió una posición única que siguió el sesgo predominante. Esta versión StockSharp reproduce el mismo comportamiento utilizando el nivel alto API manteniendo los parámetros de riesgo originales, la lógica de seguimiento y los umbrales de señal.

## Reconstrucción de indicadores
- Dos osciladores clásicos, **Bulls Power** y **Bears Power**, se calculan en la serie de velas configuradas.
- Su suma pasa a través del mismo suavizador IIR de cuatro etapas utilizado por el indicador MT4. El factor de suavizado (`Gamma`) controla la rapidez con la que reacciona el oscilador.
- La salida filtrada se transforma en un valor entre **0** y **1**. Los valores por encima del umbral central indican un dominio alcista, los valores por debajo de él indican un control bajista. Aparece cero o uno exacto cuando cualquiera de los lados está completamente agotado, coincidiendo con los casos extremos del indicador original.

## Reglas comerciales
1. La estrategia se suscribe al período de tiempo seleccionado (5 minutos por defecto) y espera únicamente a que se completen las velas.
2. Cuando está plano, evalúa la relación BullsBearsEyes:
   - **Entrada larga**: valor actual estrictamente por encima de `Threshold` (predeterminado 0,5).
   - **Entrada corta** – valor actual estrictamente por debajo de `Threshold`.
   - El algoritmo mantiene como máximo una posición abierta. Las señales opuestas se ignoran hasta que la gestión de riesgos cierre completamente la posición activa.
3. Una vez en una operación, la posición se deja intacta hasta que se produzca un evento de stop-loss, take-profit o trailing stop.

## Gestión de riesgos
- **Las distancias de stop-loss/take-profit** se recrean a partir de la configuración original de 25/150 pips. Se recalculan utilizando el instrumento `PriceStep` (pip) cada vez que se abre una nueva posición.
- **ATR final**: en cada vela terminada se evalúa el rango verdadero promedio (período `AtrPeriod`, predeterminado 5). La distancia de seguimiento equivale a un pip más `AtrMultiplier × ATR`. Cuando el cierre avanza más allá de esa distancia, el tope protector se aprieta para mantener la brecha, idéntica a la lógica MQL que repetidamente llamó a `OrderModify`.
- Los niveles de protección se verifican antes de procesar nuevas señales, lo que garantiza que las salidas siempre tengan prioridad sobre las nuevas entradas, como en la fuente EA.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Bulls/Bears Period` | 13 | Período promedio para los indicadores Bulls Power y Bears Power. |
| `Gamma` | 0,5 | Relación de suavizado IIR de cuatro etapas para el oscilador BullsBearsEyes. |
| `Threshold` | 0,5 | Divisor entre zonas alcistas (> umbral) y bajistas (< umbral). |
| `ATR Period` | 5 | Lookback utilizado para el trailing stop basado en ATR. |
| `ATR Multiplier` | 1.5 | Multiplicador aplicado a ATR al derivar la distancia de seguimiento. |
| `Stop Loss (pips)` | 25 | Distancia de parada protectora, convertida de pips a precio. |
| `Take Profit (pips)` | 150 | Distancia objetivo de ganancias, convertida de pips a precio. |
| `Trade Volume` | 1 | Volumen de órdenes de mercado utilizado para cada nueva posición. |
| `Candle Type` | velas de 5 minutos | Plazo procesado por la estrategia. |

## Notas
- El port no dibuja los objetos visuales del canal diario que estaban presentes en MT4 porque eran solo cosméticos.
- Todos los comentarios dentro del código están escritos en inglés según lo solicitado.
- Las pruebas no cambian; ejecute las comprobaciones de nivel de solución existentes si se requiere validación.
