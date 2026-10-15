# Estrategia GG-RSI-CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto **GG-RSI-CCI** de MetaTrader usando la API de alto nivel de StockSharp. Combina los indicadores Índice de Fuerza Relativa (RSI) e Índice de Canal de Materias Primas (CCI), cada uno suavizado por dos medias móviles. Se abre una posición cuando ambos indicadores apuntan en la misma dirección.

## Lógica

1. **Indicadores**
   - Calcular RSI y CCI con el mismo período.
   - Suavizar cada indicador con una media móvil rápida y una lenta.
2. **Señales**
   - **Comprar** cuando el RSI rápido está por encima del RSI lento **y** el CCI rápido está por encima del CCI lento.
   - **Vender** cuando el RSI rápido está por debajo del RSI lento **y** el CCI rápido está por debajo del CCI lento.
   - Si el modo está configurado en `Flat`, cualquier estado neutral cerrará la posición actual.
3. **Gestión de riesgos**
   - La estrategia llama a `StartProtection` una vez al inicio. Los niveles de stop loss y take profit se pueden configurar a través del gestor de riesgos de la plataforma.

## Parámetros

| Nombre          | Descripción                                          |
|-----------------|------------------------------------------------------|
| `CandleType`    | Marco temporal usado para los cálculos.               |
| `Length`        | Período de RSI y CCI.                                |
| `FastPeriod`    | Período de suavizado rápido.                          |
| `SlowPeriod`    | Período de suavizado lento.                           |
| `Volume`        | Volumen de la orden.                                  |
| `AllowBuyOpen`  | Habilitar apertura de posiciones largas.              |
| `AllowSellOpen` | Habilitar apertura de posiciones cortas.              |
| `AllowBuyClose` | Habilitar cierre de posiciones cortas.                |
| `AllowSellClose`| Habilitar cierre de posiciones largas.                |
| `Mode`          | `Trend` cierra solo en señales opuestas; `Flat` cierra también en señales neutras. |

## Notas

La estrategia procesa solo velas completadas y usa ayudantes de órdenes de alto nivel (`BuyMarket` / `SellMarket`). Evita el acceso directo a los búferes de indicadores y almacena el estado internamente.
