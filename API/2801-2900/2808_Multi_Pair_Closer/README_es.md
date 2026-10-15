# Estrategia Cierre de Múltiples Pares
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Cierre de Múltiples Pares** refleja el script original de MetaTrader que supervisa una cesta de pares de divisas y liquida todas las posiciones abiertas una vez que el beneficio flotante combinado alcanza un objetivo o la pérdida acumulada supera un umbral de seguridad. La conversión aprovecha la API de alto nivel de StockSharp para rastrear beneficios, aplicar un tiempo mínimo de mantenimiento y cerrar posiciones en varios instrumentos en una acción.

## Lógica

1. Resolver los instrumentos observados del parámetro `WatchedSymbols` separado por comas. Si la lista está vacía, se usa el `Security` principal.
2. Suscribirse al tipo de vela seleccionado (por defecto: marco temporal de 1 minuto) para cada instrumento. Cada vela terminada activa una evaluación de beneficios.
3. Para cada instrumento la estrategia almacena:
   - El último beneficio calculado (`Positions[i].PnL`).
   - El timestamp cuando una posición se volvió no nula por primera vez para respetar el requisito `MinAgeSeconds`.
4. Después de cada actualización se calcula el beneficio neto en todos los instrumentos observados:
   - Si se alcanza `ProfitTarget`, todas las posiciones más antiguas que la edad mínima se aplanan usando órdenes `BuyMarket` / `SellMarket`.
   - Si el beneficio neto cae por debajo de `-MaxLoss`, se aplica la misma lógica de liquidación como stop protector.
5. Los registros detallados resumen el beneficio por instrumento y el resultado actual de la cesta después de cada evaluación.

## Parámetros

| Parámetro | Descripción | Por defecto |
|-----------|-------------|-------------|
| `WatchedSymbols` | Lista de identificadores de instrumentos separada por comas para supervisar. Cuando está vacía, la estrategia recurre al `Security` asignado. | `"GBPUSD,USDCAD,USDCHF,USDSEK"` |
| `ProfitTarget` | Beneficio neto (en moneda de cartera) requerido para activar un cierre global de todas las posiciones observadas. | `60` |
| `MaxLoss` | Pérdida máxima aceptable (en moneda de cartera) antes de que la estrategia cierre forzosamente la cesta. | `60` |
| `Slippage` | Parámetro de compatibilidad que refleja el deslizamiento permitido del script original. Se usan órdenes de mercado para las salidas, por lo que el valor es informativo. | `10` |
| `MinAgeSeconds` | Tiempo de vida mínimo de una posición antes de que la estrategia pueda cerrarla. | `60` |
| `CandleType` | Tipo de vela usado para supervisión periódica (por defecto: velas de 1 minuto). | `1 minute` |

## Notas

- La estrategia depende de `Positions[i].PnL` proporcionado por StockSharp para medir el beneficio flotante. No extrae historial de operaciones ni calcula precios manualmente.
- Las posiciones abiertas antes de que la estrategia empiece heredan el tiempo de inicio como su primer timestamp visto. Se cerrarán solo después de que transcurra el intervalo `MinAgeSeconds` desde el inicio de la estrategia.
- Las salidas se ejecutan con órdenes de mercado para maximizar la probabilidad de liquidación inmediata. `Slippage` se registra por paridad con la versión MQL pero no se aplica a los cálculos de precio.
- La salida del registro replica la ventana "Comment" de MetaTrader imprimiendo el beneficio de cada símbolo seguido del total de la cesta general.

## Requisitos

- Asignar un `SecurityProvider` válido o asegurarse de que los identificadores solicitados estén disponibles a través del conector.
- Proporcionar suficiente configuración de volumen por instrumento para que las órdenes de mercado puedan aplanar la posición completamente.
