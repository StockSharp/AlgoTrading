# Estratégia de ruptura de intervalo baseada no tempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma porta direta do MetaTrader 4 consultor especialista `Tttttt_www_forex-instruments_info.mq4`. Ele cria níveis de ruptura intradiários uma vez por dia em um horário configurável. Sempre que o preço fecha além desses níveis, a estratégia abre uma posição na direção do rompimento. As saídas são gerenciadas por distâncias dinâmicas de lucros e perdas derivadas de uma média de intervalos históricos de dias.

## Lógica principal
1. **Tempo de snapshot diário** – Em `CheckHour:CheckMinute` a estratégia congela a máxima e a mínima do dia atual e fecha quaisquer posições abertas.
2. **Cálculo do intervalo médio** – O algoritmo agrega as últimas estatísticas `DaysToCheck`:
   - *CheckMode = 1*: usa toda a faixa máxima/baixa de cada dia concluído.
   - *CheckMode = 2*: utiliza a diferença absoluta entre os fechamentos de check-time de dias consecutivos.
3. **Construção de nível** – O valor médio é dividido por `OffsetFactor` para criar uma faixa de rompimento superior e inferior em torno da máxima/mínima do dia atual. A mesma média é dividida por `ProfitFactor` e `LossFactor` para obter lucro dinâmico e distâncias de parada.
4. **Janela de entrada** – Após o snapshot diário a estratégia observa o fechamento da vela até as 23h. Se um preço de fechamento ultrapassar a banda superior e nenhuma posição estiver aberta, ele compra; se a faixa inferior estiver quebrada, ele vende. O número de inscrições por dia é limitado por `TradesPerDay`.
5. **Gerenciamento de saída** – Enquanto está em uma posição, a estratégia compara o preço de fechamento com o preço médio de entrada (`Strategy.PositionPrice`). Uma vez que o movimento a favor ou contra atinja as distâncias de lucro ou perda configuradas, a posição é fechada a mercado. Se `CloseMode = 2`, qualquer posição restante também será fechada no início do próximo dia de negociação.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CheckHour` | Hora (0-23) em que o instantâneo do intervalo diário é obtido. | `8` |
| `CheckMinute` | Minuto (0-59) quando o instantâneo é tirado. | `0` |
| `DaysToCheck` | Número de dias históricos usados para cálculo da média. | `7` |
| `CheckMode` | `1` = usar intervalo máximo/mínimo diário, `2` = usar diferença absoluta entre fechamentos de horário de verificação consecutivos. | `1` |
| `ProfitFactor` | Divide o valor médio para obter a distância alvo do lucro. | `2` |
| `LossFactor` | Divide o valor médio para obter a distância de perda. | `2` |
| `OffsetFactor` | Divide o valor médio para obter o deslocamento do rompimento em torno de alto/baixo. | `2` |
| `CloseMode` | `1` = manter as posições durante a noite, `2` = estabilizar quando o dia do calendário muda. | `1` |
| `TradesPerDay` | Número máximo de entradas permitidas por dia. | `1` |
| `CandleType` | Série de velas usada para todos os cálculos (o padrão é velas de 15 minutos). | `15m` período de tempo |

Todos os parâmetros são criados por meio de `Strategy.Param` para que ofereçam suporte à otimização pronta para uso.

## Diferenças da versão MQL
- MetaTrader rastreia o lucro flutuante diretamente; a porta StockSharp o reconstrói de `Position` e `PositionPrice` ao avaliar saídas.
- O código MT4 contou pedidos ativos por meio de loops de tickets. A porta usa `TradesPerDay` junto com a posição agregada para manter o número de negociações no mesmo dia sob controle.
- O script original dependia de buffers históricos (por exemplo, `Highest`, `Lowest`). A versão StockSharp armazena estatísticas diárias internamente, evitando buffers de indicadores explícitos e respeitando as diretrizes de alto nível do API.
- As ordens protetoras de stop-loss e take-profit foram enviadas juntamente com a entrada no mercado no MT4. A porta realiza controle de risco equivalente, monitorando o fechamento das velas e enviando ordens de saída do mercado quando os limites são atingidos.

## Notas de uso
- Use uma série de velas que corresponda ao tamanho da barra da configuração original do MQL (barras de 15 minutos foram usadas no arquivo de referência).
- Forneça pelo menos `DaysToCheck` dias completos de dados históricos antes de iniciar a estratégia, caso contrário, os níveis de breakout permanecerão inativos.
- Ao otimizar, mantenha os fatores positivos para manter limites significativos de ruptura e risco.
