# Estrategia Bull vs Medved
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bull vs Medved es una estrategia de ruptura con órdenes límite originalmente publicada para MetaTrader 5. Observa las últimas tres velas completadas y solo permite operaciones durante seis ventanas de cinco minutos distribuidas uniformemente a lo largo del día. Cuando aparece una secuencia específica de velas alcistas o bajistas, la estrategia coloca una orden límite pendiente desfasada del spread actual y protege la posición con objetivos simétricos de stop-loss y take-profit.

## Lógica de Trading

1. **Ventanas de trading** – las órdenes se evalúan solo si la hora del día actual está dentro de una de las seis ventanas configurables (por defecto 00:05, 04:05, 08:05, 12:05, 16:05, 20:05) y dentro de la duración configurada (5 minutos por defecto). Salir de la ventana reinicia la protección de una orden por ventana.
2. **Datos de velas requeridos** – la estrategia espera tres velas terminadas antes de generar señales. Los cálculos siempre usan las tres velas completadas más recientes.
3. **Setups alcistas**:
   - **Bull regular**: la vela de hace tres períodos cierra por encima de la apertura de la segunda vela, la segunda vela tiene al menos un cuerpo alcista de 1 pip, y la vela más reciente tiene un cuerpo alcista mayor que el umbral `CandleSizePips` configurado.
   - **Filtro bad bull**: si las tres velas tienen grandes cuerpos alcistas, la señal se ignora para evitar movimientos parabólicos.
   - **Cool bull**: después de una fuerte retroceso bajista (segunda vela cierra al menos 2 pips por debajo de su apertura), la vela más reciente debe envolver el retroceso e imprimir al menos el 40% del cuerpo normal `CandleSizePips`. Un bull regular (sin el filtro bad-bull) o un patrón cool bull activa un setup largo.
   - Con una señal larga válida, la estrategia coloca una orden **buy limit** por debajo del mejor ask en `IndentUpPips` (convertido a unidades de precio del instrumento).
4. **Setup bajista**:
   - Si la vela más reciente tiene un cuerpo bajista mayor que `CandleSizePips`, la estrategia coloca una orden **sell limit** por encima del mejor bid en `IndentDownPips`.
5. **Gestión de riesgos** – una vez abierta una posición, la estrategia adjunta automáticamente objetivos absolutos de stop-loss y take-profit usando las distancias en pips configuradas.
6. **Gestión de órdenes** – solo se puede enviar una orden por ventana y no se coloca ninguna orden nueva mientras otra orden límite para el mismo símbolo permanezca activa.

## Parámetros

- `OrderVolume` – volumen de trading para órdenes límite.
- `CandleSizePips` – tamaño mínimo del cuerpo alcista/bajista para la última vela.
- `StopLossPips` – distancia del stop de protección desde el precio de entrada.
- `TakeProfitPips` – distancia del objetivo de beneficio desde el precio de entrada.
- `IndentUpPips` – desplazamiento del buy limit por debajo del mejor ask.
- `IndentDownPips` – desplazamiento del sell limit por encima del mejor bid.
- `EntryWindowMinutes` – duración de cada ventana de trading permitida.
- `CandleType` – marco temporal de velas usado para evaluar patrones.
- `StartTime0` … `StartTime5` – horas de inicio de las seis ventanas de trading.

## Notas Adicionales

- La estrategia se suscribe al libro de órdenes para mantener los precios bid/ask más recientes para colocación precisa de límites. Si no hay datos del libro disponibles, recae al último cierre de vela.
- Los offsets de precio se calculan en unidades de tamaño pip que se adaptan automáticamente a cotizaciones de 3 y 5 dígitos.
- El stop-loss y take-profit se aplican a través de `StartProtection` para que los objetivos sigan el precio de ejecución real de la orden límite.
