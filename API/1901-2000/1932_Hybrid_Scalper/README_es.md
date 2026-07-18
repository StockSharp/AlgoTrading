# Estrategia Hybrid Scalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Hybrid Scalper** es un algoritmo de trading a corto plazo convertido desde el script MQL4 `hybrid_Scalper.mq4`. Opera sobre la API de alto nivel de StockSharp y está diseñada para el marco temporal de 1 minuto. La estrategia combina múltiples indicadores técnicos para identificar oportunidades de ruptura rápida, evitando al mismo tiempo periodos de volatilidad excesiva o insuficiente.

## Lógica de la estrategia

1. **Filtro de tendencia** – Una EMA rápida (21) y una EMA lenta (89) determinan la dirección del mercado. Las operaciones largas solo se permiten cuando la EMA rápida está por encima de la EMA lenta; las operaciones cortas requieren la condición opuesta.
2. **Filtro de momentum** – El Oscilador Estocástico (5,3,3) genera señales de entrada. Se activa una compra cuando %K está por debajo de 20 y por debajo de %D. Se activa una venta cuando %K está por encima de 80 y sigue por encima de %D.
3. **Confirmación RSI** – El Índice de Fuerza Relativa con período 7 confirma el momentum. Las entradas largas requieren RSI por debajo de 25, mientras que las entradas cortas requieren RSI por encima de 85.
4. **Filtro de volatilidad** – Las Bandas de Bollinger (50, desviación 4) miden el ancho actual del mercado. La estrategia opera solo cuando la diferencia entre las bandas superior e inferior está entre 0.00045 y 0.00262, evitando tanto mercados tranquilos como inestables.
5. **Días de trading** – Los parámetros permiten habilitar o deshabilitar el trading para cada día de la semana de forma individual (lunes–viernes).

## Parámetros

| Nombre | Descripción |
| ------ | ----------- |
| `RsiPeriod` | Período del indicador RSI. |
| `EmaFastPeriod` | Período de la EMA rápida para la detección de tendencia. |
| `EmaSlowPeriod` | Período de la EMA lenta para la detección de tendencia. |
| `BbPeriod` | Período utilizado en las Bandas de Bollinger. |
| `BbDeviation` | Multiplicador de desviación para las Bandas de Bollinger. |
| `TradeMonday`–`TradeFriday` | Habilitar el trading en días de la semana específicos. |
| `CandleType` | Tipo de vela/marco temporal, por defecto velas de 1 minuto. |

## Notas

- La estrategia utiliza la API de alto nivel `BindEx` para conectar múltiples indicadores en una sola suscripción.
- `StartProtection()` se invoca una vez al inicio para activar la protección de posición integrada (sin parámetros explícitos de stop-loss o take-profit).
- Todos los comentarios en el código se proporcionan en inglés de acuerdo con las directrices del repositorio.

## Cómo ejecutar

1. Agregue el archivo de estrategia a un proyecto StockSharp.
2. Configure los conectores de datos de mercado y ejecución requeridos.
3. Compile y lance la estrategia; asegúrese de que el instrumento seleccionado proporcione velas de 1 minuto.
4. Ajuste los parámetros a través de la interfaz `StrategyParam` según sea necesario.
