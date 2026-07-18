# Estratégia de tempo fechado FitFul 13
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **FitFul 13 Time Gated Strategy** é uma versão StockSharp do MetaTrader 4 consultor especialista "FitFul_13". A estratégia constrói uma escada dinâmica semanal (PP, R0.5, R1, R1.5, R2, R2.5, R3 e os níveis de suporte correspondentes) usando a máxima, a mínima e o fechamento da semana anterior. As decisões comerciais são tomadas no prazo principal (padrão 1 hora) e são opcionalmente confirmadas por um prazo mais rápido (padrão 15 minutos). Novas posições são permitidas apenas em minutos intradiários específicos para imitar o comportamento original EA.

## Lógica de sinal
1. **Cálculo de pivô semanal**
   - No final de cada vela semanal, a escada pivô é recalculada.
   - Os preços stop-loss e take-profit são compensados dos níveis base por uma distância configurável expressa em pontos de preço.
2. **Primárias condições de prazo**
   - A última vela primária concluída deve ser de alta para procurar entradas longas ou de baixa para procurar entradas curtas.
   - A vela primária anterior deve abranger um dos níveis de pivô (abrir abaixo e fechar acima para posições compradas, abrir acima e fechar abaixo para posições vendidas).
3. **Condições de prazo de confirmação**
   - Se a vela de confirmação atual for de alta, os mínimos das duas velas de confirmação anteriores deverão perfurar e fechar acima do mesmo nível de pivô para confirmar um sinal longo.
   - Se a vela de confirmação atual for de baixa, as máximas das duas velas de confirmação anteriores deverão perfurar e fechar abaixo de um nível de pivô para confirmar um sinal curto.
4. **Tempo de entrada**
   - Uma negociação é realizada somente quando o minuto de abertura da vela primária finalizada for igual a um dos quatro minutos configurados (0, 15, 30 ou 45 por padrão).
   - A exposição líquida é limitada por `MaxNetPositions × Volume` para emular a restrição de "máximo de três pedidos abertos" da versão MetaTrader.

## Gestão de risco
- **Stops e metas** – Cada posição recebe um stop-loss e um take-profit derivados do pivô imediatamente após a entrada.
- **Trailing Stop** – Uma vez que o preço avança pelo número configurado de pontos, o stop é seguido na direção da negociação.
- **Tempo máximo de retenção** – As negociações lucrativas são fechadas quando o tempo de retenção excede a duração configurada (48 horas por padrão).
- **Regra fixa de sexta-feira** – Às sextas-feiras, qualquer posição aberta é fechada entre os minutos configurados da hora especificada (padrão 21h50–21h59).

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `PrimaryCandleType` | Prazo usado para as principais verificações cruzadas do pivô. |
| `ConfirmationCandleType` | Prazo mais rápido que valida reações pivot. |
| `Volume` | Volume líquido de ordens de mercado. |
| `MaxNetPositions` | Exposição máxima medida em múltiplos de `Volume`. |
| `OffsetPoints` | Distância do ponto de preço aplicada a stops e alvos em torno de cada pivô. |
| `TrailingStopPoints` | Distância do trailing stop em faixas de preço. |
| `CloseAfter` | Tempo máximo de manutenção para posições lucrativas. |
| `CloseHour`, `CloseMinuteFrom`, `CloseMinuteTo` | Janela horária de sexta-feira para saídas forçadas. |
| `EntryMinute0..3` | Minutos permitidos (a cada hora) para abertura de novas posições. |

## Notas
- A conversão mantém a dependência do EA original na escada dinâmica da semana anterior e nas janelas de execução de um quarto de hora.
- O gerenciamento de dinheiro foi simplificado: o parâmetro StockSharp `Volume` controla o tamanho do pedido diretamente em vez de reimplementar o cálculo dinâmico do lote de MetaTrader.
- Todos os comentários dentro do código são escritos em inglês, conforme exigido pelas diretrizes do projeto.
