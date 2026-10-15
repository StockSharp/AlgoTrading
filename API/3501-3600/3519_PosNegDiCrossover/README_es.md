# PosNegDiCrossoverEstrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
**PosNegDiCrossoverStrategy** es un puerto StockSharp del MetaTrader experto `_HPCS_PosNegDIsCrossOver_Mt4_EA_V01_WE`. el
El sistema original escucha los cruces entre las líneas +DI y -DI del índice direccional promedio (ADX) e inmediatamente
abre una posición en la dirección del nuevo líder. Cada posición está protegida por stop-loss y take-profit simétricos.
Los umbrales se miden en pips y las operaciones perdedoras desencadenan un ciclo de recuperación estilo martingala que vuelve a entrar con un volumen multiplicado.
hasta que se alcanza un número fijo de intentos o se produce una salida rentable.

## Lógica comercial
1. **Detección de señal**: cuando la vela terminada entrega nuevos valores ADX, la estrategia compara el +DI y el -DI actuales
lecturas con las anteriores. Aparece una señal alcista cuando +DI cruza por encima de -DI, mientras que se genera una señal bajista cuando
+DI cruza por debajo de -DI. Sólo se permite una entrada inicial por barra para reflejar la protección MQL que evitó operaciones duplicadas en
la misma vela.
2. **Filtro de tiempo**: las entradas solo se permiten dentro de una ventana diaria definida por el usuario. Fuera de la ventana la estrategia sigue gestionando
posiciones activas (stops virtuales y toma de ganancias) pero no abre nuevos ciclos ni continúa una secuencia de martingala.
3. **Colocación de órdenes**: se envía una orden de mercado en la dirección detectada con el volumen base configurado. Después del llenado el
La estrategia convierte `TakeProfitPips` y `StopLossPips` en precios absolutos usando el paso del instrumento (un multiplicador de 10x es
aplicado para instrumentos cotizados con 3 o 5 decimales) y almacena esos niveles para controles de salida manuales.
4. **Manejo de protección** – se inspecciona cada vela terminada: una posición larga se cierra si el mínimo perfora el stop o si el
alto alcanza el objetivo; Las posiciones cortas utilizan las condiciones simétricas. Las salidas se ejecutan con órdenes de mercado por lo que el ciclo
Puede evaluar el resultado antes de decidir el siguiente paso.
5. **Martingale bucle** – después de una pérdida, la estrategia multiplica el volumen actual por `MartingaleMultiplier`, incrementa el ciclo
contador e inmediatamente vuelve a entrar en la misma dirección (respetando la ventana de negociación). Cuando se produce una salida rentable o la
El número de intentos llega a `MartingaleCycleLimit`, el ciclo se restablece al volumen base y espera el siguiente cruce ADX.

## Parámetros
| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `CandleType` | plazo de 15 minutos | Serie de velas utilizadas para cálculos ADX y monitoreo de parada/objetivo. |
| `AdxPeriod` | 14 | Longitud del indicador del índice direccional promedio. |
| `UseTimeFilter` | `true` | Habilita la ventana de negociación diaria. |
| `StartTime` | 00:00 | Inicio de la sesión de negociación (hora de cambio). |
| `StopTime` | 23:59 | Fin de la sesión de negociación (hora de cambio). |
| `OrderVolume` | 0.1 | Volumen inicial de órdenes de mercado para cada ciclo. |
| `TakeProfitPips` | 10 | Distancia al objetivo de ganancias en pips (convertida a precio mediante el paso del instrumento). |
| `StopLossPips` | 10 | Distancia al tope de protección en pips. |
| `MartingaleMultiplier` | 2 | Multiplicador de volumen aplicado después de cada operación perdedora durante el ciclo de martingala. |
| `MartingaleCycleLimit` | 5 | Número máximo de reingresos de martingala permitidos para la misma señal. |

## Notas
- La estrategia verifica `IsFormedAndOnlineAndAllowTrading()` antes de enviar cualquier orden, asegurando una inicialización y riesgo adecuados.
controles desde el marco.
- El manejo virtual de stop-loss y take-profit imita el comportamiento MetaTrader donde las órdenes de protección se adjuntan directamente al
posición. Se evalúan en velas terminadas para que sigan siendo compatibles con el StockSharp API de alto nivel.
- Cuando la ventana de negociación está deshabilitada (ya sea por parámetro o estableciendo tiempos de inicio y finalización idénticos), la estrategia se comporta como
un sistema 24 horas al día, 5 días a la semana, idéntico al experto original con `is_start` y `is_stop` cubriendo todo el día.
