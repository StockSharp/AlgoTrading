# Estratégia DreamBot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
DreamBot é uma versão StockSharp do MetaTrader 4 consultor especialista "DreamBot". A estratégia monitora o oscilador do Índice de Força em velas horárias e espera que o impulso ultrapasse os limites de alta ou baixa. Quando o Índice de Força cruza acima do nível de alta depois de estar abaixo dele na barra anterior, a estratégia abre uma posição longa. Quando o Índice de Força cruza abaixo do nível de baixa depois de estar acima dele, a estratégia abre uma posição curta. A negociação ocorre apenas quando não existe posição, refletindo a lógica de posição única do robô original.

## Lógica de negociação
- Assine as velas H1 e calcule um Índice de Força suavizado (comprimento 13 por padrão).
- Acompanhe os dois últimos valores completos do Índice de Força. Os sinais são gerados usando os valores da barra *anteriores*, exatamente como a implementação MT4 (`iForce` com deslocamento 1 e 2).
- Insira comprado quando o Índice de Força na vela anterior estiver acima de `BullsThreshold` e o valor de duas velas atrás estiver abaixo do limite, desde que nenhuma posição esteja aberta.
- Digite short quando o índice de força na vela anterior estiver abaixo de `BearsThreshold` e o valor de duas velas atrás estiver acima do limite, desde que nenhuma posição esteja aberta.
- O trailing stop opcional replica o EA original: quando o lucro excede `TrailingStepPoints`, um nível de stop é puxado para `TrailingStartPoints` longe do preço e segue avanços adicionais.

## Gestão de risco
- `StartProtection` anexa ordens clássicas de stop-loss e take-profit usando a distância de MetaTrader "pontos" convertida por meio da etapa de preço do instrumento.
- A proteção móvel é baseada no mercado: quando o nível móvel calculado é violado, a estratégia envia uma ordem de mercado para fechar a posição imediatamente.
- O rastreamento de posição captura o preço de entrada ponderado pelo volume para que a lógica final se alinhe com preenchimentos parciais e reversões.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `ForcePeriod` | Período de suavização do índice de força (padrão 13). |
| `TakeProfitPoints` | Distância de lucro em MetaTrader pontos. |
| `StopLossPoints` | Distância de stop-loss em MetaTrader pontos. |
| `BullsThreshold` | Limite do Índice de Força de Alta que permite entradas longas. |
| `BearsThreshold` | Limite do Índice de Força de Baixa que permite entradas curtas. |
| `EnableTrailing` | Ativa a lógica de trailing stop. |
| `TrailingStartPoints` | Distância (em pontos) mantida entre o preço e o trailing stop uma vez ativado. |
| `TrailingStepPoints` | Lucro (em pontos) necessário antes da ativação do trailing stop. |
| `CandleType` | Prazo usado para cálculos do Índice de Força (o padrão é velas H1). |

## Notas
- A validação do parâmetro evita que o gatilho final (`TrailingStepPoints`) exceda a distância final (`TrailingStartPoints`), correspondendo à verificação de segurança MetaTrader.
- A aplicação do nível de parada do EA original (corretor `MODE_STOPLEVEL`) é aproximada por meio das conversões de etapas de preço de StockSharp. Dependendo das restrições do corretor, poderá ser necessária validação adicional.
- Todos os comentários e registros do código são fornecidos em inglês, conforme solicitado pelas diretrizes de conversão.
