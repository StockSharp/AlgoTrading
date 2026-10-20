# Binomial Option Pricing Model

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Regras de avaliação

Módulo de avaliação de opções Cox–Ross–Rubinstein em dois passos, sem negociação ou envio de ordens. `CalculatePrice(spot, volatility, timeToExpiry)` retorna o preço para strike, call/put, estilo europeu/americano e classe de ativo configurados. Taxas e volatilidade são frações anuais: `0.05` significa 5%. O ano tem 365 dias; o prazo aceita minutos e horas.

Para `dt = T / 2`: `u = exp(volatility * sqrt(dt))`, `d = 1/u`, `p = (exp(b * dt) - d) / (u - d)`. `b = r - q` para `Stock`, `Index`, `Currency` (q: dividendo/taxa estrangeira); `b = 0` para `Future`. Cada passo inverso desconta por `exp(-r * dt)`. Europeu exerce no vencimento; americano compara continuação e valor intrínseco em cada nó. Prazo zero retorna intrínseco; volatilidade zero segue o caminho determinista com as mesmas regras. Classes/estilos não suportados e probabilidades inválidas falham explicitamente, sem limitar valores.

`EvaluatePrice(candle)` processa cada vela concluída uma vez. Após `Length` fechamentos, estima volatilidade anual por `SD populacional(close) / mean(close) * sqrt(365 dias / intervalo)`. Avalia `TimeToExpiry`, atualiza `LastOptionPrice`, registra o valor e desenha velas/desvio quando há gráfico. Trata-se de dispersão dos níveis de preço, não volatilidade de retornos nem sinal de negociação.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `StrikePrice` | `100` |
| `DividendYield` | `0` |
| `AssetClass` | `Stock` |
| `OptionStyle` | `European` |
| `OptionType` | `Call` |
| `TimeToExpiry` | `30d` |
| `Length` | `20` |
| `CandleType` | `1m` |
| `RiskFreeRate` | `0.05` |
