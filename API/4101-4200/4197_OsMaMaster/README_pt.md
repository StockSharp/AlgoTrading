# Estratégia OsMaMaster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia OsMaMaster reproduz o comportamento do especialista **OsMaSter_V0** MetaTrader 4 original, contando com o histograma MACD (OsMA) para detectar reversões de impulso. A estratégia assina uma única série de velas e avalia o ponto de viragem mais recente do OsMA assim que uma vela é fechada, o que se alinha com a diretriz do repositório de trabalhar apenas em barras acabadas.

## Lógica de negociação
- **Pilha de indicadores** – um indicador `MovingAverageConvergenceDivergence` é processado em cada vela finalizada. Os períodos rápido, lento e de sinal espelham os parâmetros de entrada MQL e o padrão é 26/09/5, respectivamente.
- **Preço aplicado** – o parâmetro `AppliedPrice` mapeia as constantes clássicas MetaTrader `PRICE_*` (0 = fechamento, 1 = abertura, 2 = alto, 3 = baixo, 4 = mediana, 5 = típico, 6 = ponderado). O preço selecionado é inserido diretamente no indicador MACD.
- **Detecção de sinal** – quatro leituras OsMA são comparadas de acordo com os deslocamentos `Shift1`–`Shift4` fornecidos. A configuração padrão (0,1,2,3) procura um mínimo ou máximo local do histograma:
  - Configuração longa: `OsMA[shift4] > OsMA[shift3]`, `OsMA[shift3] < OsMA[shift2]`, `OsMA[shift2] < OsMA[shift1]`.
  - Configuração curta: `OsMA[shift4] < OsMA[shift3]`, `OsMA[shift3] > OsMA[shift2]`, `OsMA[shift2] > OsMA[shift1]`.
- **Política de posição única** – uma nova negociação é enviada somente quando nenhuma posição está aberta no momento, correspondendo ao EA original que verificou pedidos existentes via `ExistPositions`.

## Gerenciamento de posição
- **Stop-loss** – `StopLossPips` define a distância opcional (em pips) entre o preço de preenchimento e o stop de proteção. Um valor de `0` desativa a parada.
- **Take-profit** – `TakeProfitPips` reflete o parâmetro de take-profit de EA. Quando definido como `0`, nenhum destino fixo é usado.
- **Modelo de execução** – tanto o stop quanto o alvo são avaliados em relação aos extremos da vela (`HighPrice`/`LowPrice`). Se um limite for ultrapassado dentro de uma vela, a posição será fechada no fechamento da vela usando ordens de mercado.
- **Reset de estado** – sempre que a posição é fechada, todas as referências de stop/alvo pendentes são limpas para que a próxima entrada possa configurá-las novamente.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Período da série de velas usado para todos os cálculos. | 1 hora |
| `FastEmaPeriod` | Comprimento EMA rápido dentro do indicador MACD. | 9 |
| `SlowEmaPeriod` | Comprimento EMA lento dentro do indicador MACD. | 26 |
| `SignalPeriod` | Comprimento do sinal EMA usado para construir o histograma. | 5 |
| `AppliedPrice` | Código MetaTrader `PRICE_*` que define qual preço da vela alimenta o MACD. | 0 (fechar) |
| `Shift1` | Primeira mudança OsMA (geralmente a barra atual). | 0 |
| `Shift2` | Segunda mudança OsMA. | 1 |
| `Shift3` | Terceira mudança OsMA. | 2 |
| `Shift4` | Quarta mudança OsMA. | 3 |
| `StopLossPips` | Distância de parada protetora em pips. | 50 |
| `TakeProfitPips` | Distância alvo de lucro em pips. | 50 |

## Notas de conversão
- A implementação StockSharp mantém um buffer de anel compacto de valores OsMA recentes em vez de solicitar repetidamente o histórico do indicador, garantindo a conformidade com a regra do repositório sobre como evitar coletas de dados personalizadas.
- Todas as decisões de negociação usam velas acabadas para evitar trabalhar com valores de indicadores incompletos.
- A lógica stop-loss e take-profit emula a colocação de pedidos MQL monitorando os máximos e mínimos das velas e fechando posições com ordens de mercado.
- O volume da estratégia padrão é definido como **0,01**, refletindo o tamanho de lote padrão do EA.

## Dicas de uso
- Ajuste os períodos `CandleType` e MACD para corresponder à volatilidade do instrumento. Mercados mais rápidos podem se beneficiar de comprimentos EMA mais curtos.
- Considere desativar o take-profit definindo `TakeProfitPips` como `0` se quiser acompanhar tendências estendidas e gerenciar saídas manualmente.
- Ao experimentar diferentes valores de `Shift`, certifique-se de que a maior mudança não seja excessivamente grande; a estratégia mantém apenas os valores do histograma necessários para o deslocamento máximo.
- Como as saídas são avaliadas com base em dados de velas, o uso de prazos mais curtos reduz o atraso entre a violação real do limite e a execução da saída.
