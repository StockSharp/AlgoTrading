# Estratégia GG-RSI-CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o expert advisor **GG-RSI-CCI** do MetaTrader usando a API de alto nível do StockSharp. Combina os indicadores Índice de Força Relativa (RSI) e Índice de Canal de Commodities (CCI), cada um suavizado por duas médias móveis. Uma posição é aberta quando ambos os indicadores apontam na mesma direção.

## Lógica

1. **Indicadores**
   - Calcular RSI e CCI com o mesmo período.
   - Suavizar cada indicador com uma média móvel rápida e uma lenta.
2. **Sinais**
   - **Comprar** quando o RSI rápido está acima do RSI lento **e** o CCI rápido está acima do CCI lento.
   - **Vender** quando o RSI rápido está abaixo do RSI lento **e** o CCI rápido está abaixo do CCI lento.
   - Se o modo estiver definido como `Flat`, qualquer estado neutro fechará a posição atual.
3. **Gestão de risco**
   - A estratégia chama `StartProtection` uma vez na inicialização. Os níveis de stop loss e take profit podem ser configurados através do gerenciador de risco da plataforma.

## Parâmetros

| Nome            | Descrição                                            |
|-----------------|------------------------------------------------------|
| `CandleType`    | Período de tempo usado para os cálculos.              |
| `Length`        | Período do RSI e CCI.                                |
| `FastPeriod`    | Período de suavização rápido.                         |
| `SlowPeriod`    | Período de suavização lento.                          |
| `Volume`        | Volume da ordem.                                      |
| `AllowBuyOpen`  | Habilitar abertura de posições compradas.             |
| `AllowSellOpen` | Habilitar abertura de posições vendidas.              |
| `AllowBuyClose` | Habilitar fechamento de posições vendidas.            |
| `AllowSellClose`| Habilitar fechamento de posições compradas.           |
| `Mode`          | `Trend` fecha apenas em sinais opostos; `Flat` fecha também em sinais neutros. |

## Observações

A estratégia processa apenas candles completados e usa auxiliares de ordens de alto nível (`BuyMarket` / `SellMarket`). Evita acesso direto aos buffers de indicadores e armazena o estado internamente.
