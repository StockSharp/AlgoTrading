# Estrategia de Eugenio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen

La estrategia Eugene traslada el MetaTrader 4 asesor experto original "Eugene" al StockSharp alto nivel API. El algoritmo monitorea las velas horarias de forma predeterminada y busca rupturas de velas internas que se confirman mediante un retroceso a un tercio de la vela anterior. Una vez que se confirma una ruptura, la estrategia entra en la dirección de la ruptura y puede revertir las posiciones existentes cuando aparece una configuración opuesta.

## Lógica de trading

1. **Detección de vela interior**: la vela anterior debe estar completamente dentro del rango de la vela anterior. Su dirección de cierre determina si se clasifica como insider negro (bajista) o blanco (alcista).
2. **Filtros de pájaros**: una vela interior confirmada por otra vela del mismo color detrás de ella está marcada como "pájaro". Los pájaros negros bloquean las operaciones largas, los pájaros blancos bloquean las operaciones cortas. Esto refleja el filtro protector de la versión MQL.
3. **Niveles de confirmación en zigzag**: se calculan dos precios de confirmación en un tercio del cuerpo o mecha de la vela anterior:
   - El nivel de confirmación largo está un tercio por debajo del cierre anterior (cuerpo para velas alcistas, mecha para velas bajistas).
   - El nivel de confirmación corta está un tercio por encima del cierre anterior (cuerpo para velas bajistas, mecha para velas alcistas).
4. **Filtro de sesión**: si la vela actual se abre a las 08:00 o más tarde, las confirmaciones se consideran satisfechas incluso sin un retroceso.
5. **Condición de ruptura**: una señal de compra requiere que la vela actual alcance un máximo más alto que la vela anterior mientras mantiene un mínimo más alto y se superpone al rango de la vela dos barras atrás. Una señal de venta utiliza condiciones simétricas con mínimos y máximos más bajos.
6. **Gestión de posiciones**: antes de abrir una nueva operación, la estrategia cierra cualquier exposición opuesta. Solo se puede emitir una entrada larga y una corta por vela, replicando las restricciones `Counter_buy` y `Counter_sell` del asesor experto original.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Trade Volume` | Tamaño de la orden para órdenes de mercado. | `0.1` |
| `Candle Type` | Marco temporal de la serie de velas procesadas. | `1 hour` |

## Trazar

Cuando hay un área del gráfico disponible, la estrategia traza las velas procesadas junto con las operaciones ejecutadas, lo que ayuda a visualizar el comportamiento de ruptura.

## Notas

- La versión StockSharp mantiene el filtro de sesión por hora del experto MQL. Ajuste el tipo de vela cuando opere en otros mercados o zonas horarias.
- La gestión de stop-loss y take-profit no está incluida en el archivo fuente MQL. Por lo tanto, el puerto deja la gestión de riesgos al entorno de alojamiento.
