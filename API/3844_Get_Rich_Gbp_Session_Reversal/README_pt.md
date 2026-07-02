# Estratégia de reversão de sessão Get Rich GBP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Fique Rico ou Morra Tentando GBP** é um sistema de reversão à média de alta frequência que portou o MetaTrader 4 consultor especialista "Fique Rico ou Morra Tentando GBP" para o StockSharp API de alto nível. A lógica monitora um curto histórico contínuo de velas de minuto e abre negociações perto de dois horários predefinidos do dia, quando as velas mais recentes fecharam principalmente contra a direção esperada. Esta abordagem tenta capturar uma retração rápida imediatamente após a sobreposição das sessões de Londres e Nova York.

## Lógica de negociação
1. A estratégia assina velas de 1 minuto por padrão (o tipo de vela pode ser personalizado).
2. Uma janela contínua das últimas velas finalizadas *Lookback* é mantida. Cada vela é categorizada como:
   - `+1` se fechou abaixo de sua abertura (vela de baixa).
   - `-1` se fechou acima de sua abertura (vela de alta).
   - `0` se a vela for neutra.
3. A soma cumulativa dessas classificações é usada para decidir a direção do comércio:
   - Uma soma positiva significa que as velas de baixa dominam e a estratégia se prepara para uma entrada **longa**.
   - Uma soma negativa significa que as velas de alta dominam e a estratégia se prepara para uma entrada **curta**.
4. Os pedidos podem ser feitos apenas durante os primeiros *EntryWindowMinutes* minutos após a hora em que o horário atual do servidor corresponde a um dos dois horários alvo:
   - `FirstEntryHour + HourShift` (padrão: meia-noite de Londres após a correção GMT+2).
   - `SecondEntryHour + HourShift` (padrão: 21h, horário do servidor para a sobreposição de fechamento de Nova York).
5. Se nenhuma posição estiver aberta e todas as condições forem satisfeitas, a estratégia envia uma ordem de mercado com tamanho de lote fixo ou tamanho dinâmico calculado a partir do bloco de gestão de dinheiro.
6. Enquanto estiver numa posição, a estratégia aplica três regras de saída independentes:
   - Um **obter lucro parcial** fecha a negociação assim que o preço de fechamento move o preço de *PartialTakeProfitPoints* a favor.
   - Um **stop-loss rígido** é acionado quando o preço se move em direção ao preço de *StopLossPoints* contra a negociação.
   - Um **trailing stop** bloqueia o lucro depois que o mercado ultrapassa as etapas de preço *TrailingStopPoints*, usando a máxima mais alta (para posições compradas) ou a mínima mais baixa (para posições vendidas) observada desde a entrada.
7. Um nível final de lucro igual às etapas de preço *TakeProfitPoints* também é monitorado como uma rede de segurança.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `TakeProfitPoints` | 100 | Distância máxima de lucro (em etapas de preço) monitorada após a lógica de rastreamento. |
| `PartialTakeProfitPoints` | 40 | Distância primária de lucro (em etapas de preço) que replica a saída antecipada do EA original. |
| `StopLossPoints` | 100 | Distância de stop-loss (em etapas de preço). |
| `TrailingStopPoints` | 30 | Distância do trailing stop (em etapas de preço). |
| `FixedVolume` | 1 | Volume base do pedido em lotes quando o gerenciamento de dinheiro está desativado. |
| `UseMoneyManagement` | falso | Permite o dimensionamento dinâmico da posição com base no valor da conta e no risco configurado. |
| `RiskPercent` | 10 | Percentagem do valor da carteira em relação ao risco por negociação quando a gestão de dinheiro está ativa. |
| `Lookback` | 18 | Número de velas finalizadas usadas na contagem de alta/baixa. |
| `FirstEntryHour` | 22 | Primeira hora de negociação antes da correção da mudança horária. |
| `SecondEntryHour` | 19 | Segunda hora de negociação antes da correção da mudança horária. |
| `HourShift` | 2 | Correção de fuso horário aplicada a ambos os horários de negociação. |
| `EntryWindowMinutes` | 5 | Largura da janela de entrada (minutos desde o início da hora de qualificação). |
| `CandleType` | Período de 1 minuto | Tipo de vela para assinar; pode ser substituído por qualquer outro tipo de vela periódica. |

## Gestão de capital
Quando `UseMoneyManagement` está habilitado, a estratégia estima o volume do pedido arriscando `RiskPercent` do valor do portfólio atual sobre o `StopLossPoints` configurado. O cálculo respeita o passo do lote e o volume mínimo do instrumento para permanecer compatível com o câmbio.

## Notas de uso
- As janelas de negociação são avaliadas usando o horário de troca/servidor das velas recebidas. Ajuste `HourShift` para que `FirstEntryHour + HourShift` e `SecondEntryHour + HourShift` correspondam aos limites de sessão desejados.
- `Lookback` deve permanecer maior que 1 para evitar decisões ruidosas. Aumentá-lo suaviza a medição do sentimento ao custo de reações mais lentas.
- A lógica de proteção depende de velas acabadas. Se for necessária precisão intrabarra, reduza a duração da vela de acordo.
- O especialista MQL original permitiu múltiplas posições simultâneas; esta porta limita a exposição a uma única posição aberta para corresponder às StockSharp práticas recomendadas.

## Limitações
- O trailing stop é virtual e é executado enviando uma saída de mercado na próxima vela concluída após o preço cruzar o limite móvel.
- O dimensionamento da gestão financeira pressupõe que `Security.StepPrice` representa corretamente o valor monetário de uma etapa de preço. Valide este mapeamento para cada instrumento antes de negociar ao vivo.

## Requisitos
- StockSharp ambiente API de alto nível (solução AlgoTrading).
- Velas de minutos históricas e em tempo real para o instrumento negociado em GBP.

## Referências
- Consultor especialista original MetaTrader 4: `MQL/7690/Get_rich_or_die_trying_any_gbp.mq4`.
