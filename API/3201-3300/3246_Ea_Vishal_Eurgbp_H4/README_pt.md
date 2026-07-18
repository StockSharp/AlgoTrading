# Estratégia de EA Vishal EURGBP H4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de EA Vishal EURGBP H4** replica o consultor especialista original do MetaTrader que combina um filtro de entrada de cruzamento estocástico com saídas baseadas em envelopes. A lógica opera em velas H4 por padrão e usa ferramentas virtuais de gerenciamento de risco (stop-loss, take-profit e trailing stop opcional) definidas em pips, espelhando de perto o comportamento do MT4.

## Lógica de trading
- **Entrada** – a estratégia aguarda um cruzamento estocástico avaliado nas duas velas concluídas mais recentes. Uma posição comprada é aberta quando %K cruza abaixo de %D entre a barra *n-2* e *n-1*. Uma posição vendida é aberta no cruzamento oposto. Apenas uma posição pode estar ativa por vez.
- **Saída** – as posições ativas são gerenciadas em três camadas:
  1. **Rompimento de envelope** – se a próxima barra abrir além da banda de envelope anterior enquanto a barra anterior abriu dentro, a posição é fechada imediatamente.
  2. **Stop-loss / take-profit virtual** – os preços-alvo são calculados a partir do preço de entrada usando as distâncias de pip configuradas.
  3. **Trailing stop opcional** – quando habilitado e um stop-loss está definido, o nível de stop segue o valor mais alto (para comprados) ou mais baixo (para vendidos) da vela anterior menos/mais a distância de stop.

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------ | --------- |
| `Volume` | 0.5 | Volume da ordem em lotes para cada operação. |
| `StopLossPips` | 0 | Distância de hard stop-loss em pips (0 desabilita o stop). |
| `TakeProfitPips` | 22 | Distância de take-profit em pips (0 desabilita o alvo). |
| `UseTrailingStop` | false | Habilita o trailing stop virtual que segue o extremo da vela anterior. Requer `StopLossPips` &gt; 0. |
| `StochasticKPeriod` | 6 | Período de lookback para o cálculo do %K estocástico. |
| `StochasticDPeriod` | 3 | Período de suavização para a linha %D. |
| `StochasticSlowing` | 1 | Fator de desaceleração aplicado ao %K. |
| `EnvelopePeriod` | 32 | Comprimento do SMA usado como base do envelope. |
| `EnvelopeDeviationPercent` | 0.3 | Desvio em porcentagem aplicado acima/abaixo do SMA para construir os envelopes. |
| `CandleType` | Período H4 | Série de velas que alimenta a estratégia (padrão: velas de quatro horas). |

## Notas
- Todos os parâmetros estão expostos para otimização no StockSharp Studio.
- Os níveis protetores são rastreados internamente e executados com ordens de mercado quando o intervalo da vela os perfura, correspondendo ao comportamento do consultor especialista original em eventos de nova barra.
- A estratégia depende exclusivamente de velas concluídas, garantindo backtests determinísticos e comportamento em produção.
