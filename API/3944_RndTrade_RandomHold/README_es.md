# Estrategia RndTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Conversión del asesor experto original MQL4 "RndTrade" en una estrategia StockSharp de alto nivel que realiza entradas de mercado totalmente aleatorias y las sale después de un período de tenencia fijo.

## Lógica principal

1. Suscríbase al tipo de vela configurado (velas de 1 minuto por defecto) y espere a que se completen las barras.
2. Siempre que la estrategia sea plana, genere un número aleatorio. Un valor superior a 0,5 desencadena una compra en el mercado; de lo contrario, una venta en el mercado, ambos utilizando el volumen comercial configurado.
3. Registre el tiempo de vela de la entrada y mantenga la posición abierta durante el tiempo de tenencia seleccionado (cuatro horas por defecto).
4. Una vez transcurrido el cronómetro de retención, cierre toda la posición con la orden de mercado correspondiente.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Tipo de datos de velas que activan la lógica de decisión aleatoria. | velas de 1 minuto |
| `TradeVolume` | Volumen utilizado para cada orden de mercado aleatoria. | 1 |
| `HoldDuration` | Lapso de tiempo para mantener activa cualquier posición aleatoria abierta antes de cerrarla. | 4 horas |

## Notas adicionales

- El generador aleatorio se reinicia automáticamente cuando la estrategia comienza a imitar el comportamiento MQL4 de usar la hora local como semilla.
- Sólo se utilizan órdenes de mercado, lo que refleja el asesor experto original que ejecutó inmediatamente operaciones sin órdenes pendientes.
- No se requieren indicadores adicionales ni reservas históricas; la estrategia sólo se basa en las marcas de tiempo de las velas entrantes y el temporizador interno.
