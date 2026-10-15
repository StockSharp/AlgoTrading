# Estrategia de Creación de Mercado BitexOne
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La **Estrategia de Creación de Mercado BitexOne** reproduce el robot de cotización asíncrono del fuente original
`BITEX.ONE MarketMaker.mq5`. El algoritmo coloca continuamente pares de órdenes límite alrededor de un precio de referencia
y mantiene un número igual de niveles en los lados de oferta y demanda. La estrategia fue reescrita para StockSharp usando la
API de alto nivel: la gestión de cotizaciones está impulsada por las suscripciones al libro de órdenes y nivel 1, mientras que
la normalización de riesgos y volúmenes se basa en los metadatos del instrumento (`PriceStep`, `VolumeStep` y `MinVolume`).

## Lógica de Trading
1. Determinar el *precio líder* desde el `PriceSource` seleccionado. Por defecto la estrategia espera precios mark, pero
   puede usar el libro de órdenes principal o un instrumento auxiliar (índice o símbolo mark) via el parámetro `LeadSecurity`.
2. Calcular la distancia entre niveles de precio como `ShiftCoefficient * lead price` y crear una escalera simétrica de
   cotizaciones por encima y por debajo de la referencia.
3. Limitar la exposición total en cada lado a `MaxVolumePerLevel * LevelCount`. Las operaciones ejecutadas reducen
   inmediatamente el volumen disponible para que la cuadrícula siempre refleje la posición actual.
4. Normalizar precios y volúmenes usando el tamaño de tick del instrumento y el paso de volumen. El algoritmo cancela
   órdenes desactualizadas y registra nuevas cuando el precio o el volumen derivan más allá de la tolerancia heredada del
   código MQL original (umbral de precio del 0.05% y umbral de volumen de medio paso).
5. Todas las órdenes activas se cancelan durante eventos de stop/reset para mantener el libro limpio.

## Parámetros
- `MaxVolumePerLevel` – volumen máximo cotizado en cualquier nivel de precio único. Afecta ambos lados del libro y actúa
  como límite cuando crece la posición actual.
- `ShiftCoefficient` – desplazamiento relativo desde el precio líder aplicado para cada nivel incremental
  (`leadPrice ± shift * levelIndex`).
- `LevelCount` – número de niveles de cotización por lado. Cada nivel crea una orden límite de compra y una de venta.
- `PriceSource` – valor enumerado (`OrderBook`, `MarkPrice`, `IndexPrice`) que define de dónde se origina el precio de
  referencia.
- `LeadSecurity` – instrumento opcional usado cuando se requieren precios mark o de índice externos. Si se omite, el
  instrumento de estrategia principal proporciona la referencia.

## Notas de Conversión
- La gestión asíncrona de órdenes de MetaTrader (SendAsync/ModifyAsync/RemoveOrderAsync) se mapea a los helpers
  `BuyLimit`/`SellLimit` de StockSharp combinados con cancelación explícita cuando se exceden las tolerancias.
- La lógica de equilibrio de posición (`max_pos * level_count ± position`) se preserva para mantener la escalera centrada
  y consciente del riesgo.
- La selección del precio líder imita la lógica de sufijos del robot original (`symbol`, `symbolm`, `symboli`) permitiendo
  un `LeadSecurity` personalizado combinado con una pista `PriceSource`.
- Las comprobaciones periódicas impulsadas por temporizador en MQL son reemplazadas con actualizaciones reactivas
  desencadenadas por mensajes del libro de órdenes/nivel 1 y eventos de cartera.

## Notas de Uso
- Asegúrese de que el adaptador conectado proporcione profundidad de mercado o datos de nivel 1 tanto para el símbolo de
  trading como para el `LeadSecurity` opcional.
- Cuando use feeds mark o de índice, suscríbase a los instrumentos correspondientes antes de iniciar la estrategia para
  que el precio líder esté disponible inmediatamente.
- Considere habilitar protección de cartera o gestión de riesgo adicional en el entorno de hosting si el intercambio
  requiere ratios estrictos de cotización a operación.
- La estrategia no comienza a cotizar hasta que se recibe un precio líder positivo; verifique la conectividad si no
  aparecen órdenes después del inicio.
