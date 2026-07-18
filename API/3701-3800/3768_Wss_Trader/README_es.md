# Comerciante WSS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Puerto del asesor experto "Wss_trader" MetaTrader 4 publicado en forex-instruments.info. El EA original combina niveles de reversión estilo Camarilla con distancias de pivote clásicas y abre una única operación por barra cada vez que el precio rompe las bandas configuradas durante la sesión de Londres.

## Lógica estratégica

1. Al comienzo de cada nuevo día de negociación, la estrategia lee el máximo, mínimo y cierre diario anterior para construir una escalera de pivote:
   - `Pivot = (High + Low + Close) / 3`
   - `Long entry = Pivot + Metric × point`
   - `Short entry = Pivot − Metric × point`
   - `Long stop = Short entry`
   - `Short stop = Long entry`
   - Los objetivos reflejan las fórmulas MetaTrader `Close ± (High − Low) × 1.1 / 2` con la misma abrazadera de seguridad que el código original.
2. Solo se permite operar entre `Start Hour` y `End Hour` (inclusive). Fuera de la ventana, todas las posiciones abiertas se cierran inmediatamente.
3. Cuando una vela terminada cruza por encima del nivel de entrada largo (cierre >= nivel y cierre anterior <nivel), la estrategia compra una vez con el volumen configurado, adjunta el stop y el objetivo precalculados y bloquea cualquier entrada adicional para esa barra. Se aplica una regla simétrica para los pantalones cortos.
4. Si la posición se mueve a favor al menos `Trailing Points` pasos de precio, se sigue el stop para mantener la misma distancia con respecto al precio de cierre. La parada nunca retrocede.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `Working Candle` | Tipo de vela principal utilizado para cálculos intradiarios. | `15 Minute` |
| `Daily Candle` | Tipo de vela utilizada para leer el día anterior para los niveles de pivote. | `1 Day` |
| `Start Hour` | Hora (0-23) en la que el comercio está habilitado. | `8` |
| `End Hour` | Hora (0-23) en la que la negociación deja de aceptar nuevas entradas. | `16` |
| `Metric Points` | Distancia desde el pivote hasta los niveles de ruptura medidos en pasos de precios. | `20` |
| `Trailing Points` | Distancia del trailing stop en pasos de precio. Establezca en `0` para deshabilitar el seguimiento. | `20` |
| `Order Volume` | Tamaño del pedido que refleja el parámetro `lots` original. | `0.1` |

## Notas

- La estrategia cierra la posición actual tan pronto como finaliza la ventana de negociación, coincidiendo con el comportamiento del EA original.
- El arrastre se procesa en velas terminadas. El seguimiento intrabar no se reproduce porque StockSharp opera con cierres de velas en este puerto.
- Solo se permite una operación por vela, replicando la bandera `tenb` de la versión MQL.
