# Estrategia de canales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un puerto directo del asesor experto MetaTrader 4 "Canales" incluido en la biblioteca pública de Gordago. Combina una media móvil exponencial muy rápida (EMA) con tres envolventes basadas en EMA para detectar momentos en los que el precio escapa de las zonas comprimidas. Una vez que se abre una sola posición, la estrategia se basa en órdenes de parada y paradas dinámicas opcionales para gestionar las salidas, al igual que la implementación original MQL.

## Lógica comercial

- La estrategia se suscribe a velas horarias de forma predeterminada y calcula:
  - Un EMA rápido (longitud 2) utilizando precios de **cierre** de velas.
  - Un segundo EMA rápido (longitud 2) utilizando precios de vela **apertura**, requeridos por las reglas de entrada corta del asesor experto.
  - Un EMA lento (longitud 220) en cierres que sirve como base para tres desviaciones de envolvente: ±1,0%, ±0,7% y ±0,3%.
- Se abre una posición **larga** cuando el cierre EMA rápida satisface cualquiera de las seis verificaciones cruzadas históricas:
  1. Cruza hacia arriba a través de la envoltura exterior inferior del 1%.
  2. Cruza hacia arriba a través de la envolvente inferior del 0,7%.
  3. Pasa dos barras consecutivas por debajo del límite inferior del 0,3% (condición de sobreventa).
  4. Cruza hacia arriba a través del EMA lenta.
  5. Cruza hacia arriba a través de la envolvente superior del 0,3%.
  6. Cruza hacia arriba a través de la envolvente superior del 0,7%.
- Se abre una posición **corta** cuando el EMA rápido basado en apertura activa cualquiera de las reglas cortas simétricas:
  1. Cruza hacia abajo a través de la envoltura superior exterior del 1%.
  2. Cruza hacia abajo a través de la envolvente superior del 0,7%.
  3. Cruza hacia abajo a través de la envolvente superior del 0,3%.
  4. Cruza hacia abajo por el EMA lenta.
  5. Cruza hacia abajo a través de la envolvente inferior del 0,3%.
  6. Cruza hacia abajo a través de la envolvente inferior del 0,7%.
- Sólo puede existir una posición de mercado a la vez. Una nueva señal se ignora mientras una operación está activa, coincidiendo con el comportamiento del experto MetaTrader.

## Gestión de riesgos

- Se pueden configurar distancias individuales de stop-loss y take-profit para operaciones largas y cortas. Cuando se establece en cero, esas órdenes de protección se omiten, lo que replica el estado deshabilitado por defecto de la fuente original.
- Los trailingstops opcionales refuerzan la orden de protección una vez que el precio se mueve a favor de la posición en más que la distancia de seguimiento medida en puntos.
- Todas las órdenes de protección se cancelan automáticamente cuando la posición se aplana o la estrategia se detiene.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `Candle Type` | Plazo utilizado para el análisis de precios (predeterminado: 1 hora). |
| `Volume` | Tamaño del pedido utilizado para todas las entradas. |
| `Fast EMA` / `Slow EMA` | Períodos para las EMA rápidas y lentas. |
| `Envelope 1%`, `Envelope 0.7%`, `Envelope 0.3%` | Ancho porcentual de las tres bandas envolventes. |
| `Buy Stop-Loss`, `Sell Stop-Loss` | Distancia en puntos entre el precio de entrada y el stop-loss inicial para operaciones largas o cortas. |
| `Buy Take-Profit`, `Sell Take-Profit` | Distancia en puntos para los niveles de toma de ganancias fijos opcionales. |
| `Buy Trailing`, `Sell Trailing` | Distancia del trailing stop en puntos para posiciones largas o cortas. |
| `Use Trading Hours` | Habilita el filtro de ventana de tiempo. |
| `From Hour`, `To Hour` | Límites horarios inclusivos para la apertura de nuevas posiciones. La ventana cierra alrededor de la medianoche si `From` es mayor que `To`. |

## Notas de uso

1. Debido a que las distancias de parada se definen en puntos, se multiplican internamente por la seguridad `PriceStep`. Asegúrese de que este paso coincida con el instrumento utilizado para operar.
2. La longitud rápida EMA es intencionalmente muy corta para reflejar al experto en MT4. Aumentarlo cambiará drásticamente la frecuencia de la señal.
3. El asesor original también permitía la inclusión en listas blancas de cuentas y alertas sonoras. Se omitieron porque son específicos de la plataforma y no afectan la lógica de los pedidos.
