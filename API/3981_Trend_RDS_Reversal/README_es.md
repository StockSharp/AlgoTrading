# Estrategia RDS de tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Trend RDS es una estrategia de inversión basada en sesiones escrita originalmente para MetaTrader. Busca una formación de impulso de tres barras al comienzo de una ventana comercial específica y desvanece el movimiento ingresando en la dirección opuesta. El puerto StockSharp mantiene la lógica de administración de dinero original, incluida la inversión opcional de las señales, niveles fijos de stop-loss y take-profit, protección de equilibrio y un trailing stop con tamaño de paso ajustable.

## Lógica de trading
1. **Ventana de señal**: en el `Start Time` configurado, la estrategia inspecciona hasta 100 velas cerradas recientemente.
2. **Detección de patrones**: busca la primera secuencia de tres barras consecutivas donde:
   - Los máximos aumentan mientras que los mínimos aumentan (`High[n] < High[n+1] < High[n+2]` y `Low[n] > Low[n+1] > Low[n+2]`).
   - Los máximos caen mientras que los mínimos caen (`High[n] > High[n+1] > High[n+2]` y `Low[n] < Low[n+1] < Low[n+2]`).
Una expansión simétrica en ambas direcciones se trata como un conflicto y se ignora. La dirección de la señal se invierte opcionalmente cuando `Reverse Signals` está habilitado.
3. **Entradas**: la estrategia envía una orden de mercado con el `Trade Volume` configurado si no hay ninguna posición abierta. Si la posición opuesta todavía está abierta, se cierra primero.
4. **Ventana de salida forzada** – Entre `Close Time` y quince minutos después se liquida cualquier posición residual.
5. **Protección** – Una vez abierta la posición, la estrategia registra:
   - Una orden de stop-loss y take-profit a las distancias de pips solicitadas.
   - Un disparador de equilibrio que mueve el stop al precio de entrada después de alcanzar `Break-Even (pips)`.
   - Un trailing stop que mantiene una distancia de `Trailing Stop (pips)` del precio actual y avanza sólo después de un movimiento adicional de `Trailing Step (pips)`.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| **Volumen comercial** | Tamaño de la orden de mercado expresado en lotes o contratos. |
| **Detener pérdidas (pips)** | Distancia al tope de protección. Establezca en cero para desactivar. |
| **Obtener ganancias (pips)** | Distancia al objetivo de ganancias. Establezca en cero para desactivar. |
| **Hora de inicio** | Hora del día (hora de intercambio) en la que comienza la búsqueda de patrones. |
| **Hora de cierre** | Hora del día (hora de cambio) en la que todas las operaciones abiertas se cierran en 15 minutos. |
| **Señales inversas** | Invierte entradas largas y cortas. |
| **Parada dinámica (pips)** | Distancia de seguimiento de la base. Zero desactiva el seguimiento. |
| **Paso final (pips)** | Se necesita movimiento adicional antes de que el trailing stop se actualice nuevamente. |
| ** Punto de equilibrio (pips) ** | Umbral de beneficio para mover el stop al precio de entrada. Zero desactiva la función. |
| **Tipo de vela** | Serie de velas utilizadas para el análisis. |

## Notas prácticas
- La estrategia se basa en el paso del precio del instrumento para calcular las distancias de los pips. Asegúrese de que la seguridad exponga un valor `PriceStep` o `MinPriceStep` válido.
- Sólo se procesan velas terminadas, por lo que la señal puede aparecer como máximo una vez al día por período de tiempo.
- Las órdenes stop y take-profit se actualizan cada vez que cambia el tamaño de la posición, lo que garantiza que las ejecuciones parciales mantengan una protección constante.
- La lógica de seguimiento y de equilibrio se activa sólo mientras una posición está abierta y se conoce un precio de entrada válido.

## Archivos
- `CS/TrendRdsStrategy.cs` – StockSharp Implementación C# de la estrategia.
- `README_zh.md` – Documentación china.
- `README_ru.md` – Documentación rusa.
