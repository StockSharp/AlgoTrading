# Estratégia HarVesteR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia HarVesteR combina o momentum do MACD com duas médias móveis simples e um filtro opcional de força de tendência ADX.
Ela busca situações em que o preço se mantém próximo às médias móveis enquanto o MACD cruzou recentemente a linha zero, sinalizando um potencial rompimento da consolidação.
Os stops são colocados em máximos ou mínimos de swing, metade da posição é fechada em um múltiplo fixo de recompensa, e o restante é protegido com uma saída de break-even controlada pela média móvel rápida.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `MACD > 0 && MACD history contains negative value && Close < SlowSMA && Close + Indentation > FastSMA && Close + Indentation > SlowSMA && ADX ≥ AdxBuyLevel (if enabled)`
  - Vendido: `MACD < 0 && MACD history contains positive value && Close > SlowSMA && Close - Indentation < FastSMA && Close - Indentation < SlowSMA && ADX ≥ AdxSellLevel (if enabled)`
- **Stop Loss**: Último mínimo/máximo de swing nos `StopLookback` candles concluídos.
- **Saída parcial**: Fecha metade da posição quando o preço se move `HalfCloseRatio` vezes a distância entre entrada e stop, depois move o stop para break-even.
- **Saída final**:
  - Comprado: fecha o restante se o preço cair abaixo de `FastSMA + Indentation` após o stop estar em break-even.
  - Vendido: fecha o restante se o preço subir acima de `FastSMA + Indentation` após o stop estar em break-even.
- **Comprado/Vendido**: Ambas as direções suportadas.
- **Filtros**: Filtro opcional de força de tendência ADX; defina `UseAdxFilter` como `false` para desativá-lo.
- **Gestão de posição**: Reverte a posição compensando o volume do sinal oposto mais a exposição atual.

## Parâmetros

| Nome | Padrão | Descrição |
|------|--------|-----------|
| `MacdFast` | 12 | Período EMA rápido para a linha de diferença do MACD. |
| `MacdSlow` | 24 | Período EMA lento para a linha de diferença do MACD. |
| `MacdSignal` | 9 | Período EMA de sinal para suavização do MACD. |
| `MacdLookback` | 6 | Número de candles recentemente concluídos verificados para uma mudança de sinal do MACD. |
| `SmaFastLength` | 50 | Comprimento da média móvel simples rápida. |
| `SmaSlowLength` | 100 | Comprimento da média móvel simples lenta. |
| `MinIndentation` | 10 | Deslocamento em pips aplicado em torno das médias móveis antes de entrar ou sair. |
| `StopLookback` | 6 | Retrocesso de máximo/mínimo de swing usado para inicializar o nível de stop inicial. |
| `UseAdxFilter` | false | Habilita o filtro de força ADX para ambas as direções. |
| `AdxBuyLevel` | 50 | Nível mínimo de ADX necessário para permitir entradas compradas quando o filtro está habilitado. |
| `AdxSellLevel` | 50 | Nível mínimo de ADX necessário para permitir entradas vendidas quando o filtro está habilitado. |
| `AdxPeriod` | 14 | Período usado para o cálculo do ADX. |
| `HalfCloseRatio` | 2 | Multiplicador aplicado à distância entrada-stop antes de realizar lucros parciais. |
| `Volume` | 1 | Volume de ordem para novas entradas (compensando qualquer exposição oposta). |
| `CandleType` | 1 hour | Período principal usado para construir candles e indicadores. |

## Notas

- `MinIndentation` é convertido em distância de preço usando o tamanho do tick do instrumento. Instrumentos cotados com três ou cinco decimais recebem um ajuste de dez vezes para aproximar as unidades de pip.
- Quando `UseAdxFilter` está desativado, a estratégia aceita sinais em ambas as direções sem verificar o valor do ADX.
- A realização de lucros parciais e as saídas de break-even são executadas em cada candle concluído para proteger posições abertas mesmo quando novas negociações não são permitidas.
