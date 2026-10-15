# Estratégia JTPO SilverTrend V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
SilverTrend V3 é uma estratégia de acompanhamento de tendências traduzida da implementação original do MetaTrader 4. Ele avalia o indicador SilverTrend junto com o filtro estatístico J_TPO para identificar novas oscilações direcionais. A estratégia negocia um único instrumento por vez e impõe uma regra fixa na noite de sexta-feira para evitar riscos durante o fim de semana.

## Lógica de negociação
1. **Processamento de Indicadores**
   - A estratégia mantém um buffer contínuo de velas recentes e recalcula a direção SilverTrend em cada barra concluída.
   - SilverTrend usa uma janela de 9 barras e um fator de risco de 3 para determinar os limites adaptativos do canal. Se o preço de fechamento ultrapassar o limite superior, o sinal muda para alta; cruzar abaixo do limite inferior muda o sinal para baixa.
   - O cálculo J_TPO (comprimento 14) mede a assimetria da distribuição de preços. Somente valores positivos J_TPO confirmam entradas longas, enquanto leituras negativas são necessárias para curtas.
2. **Condições de entrada**
   - Uma negociação longa é aberta quando o sinal SilverTrend muda de baixa para alta e J_TPO está acima de zero.
   - Uma negociação curta é aberta quando o sinal SilverTrend muda de alta para baixa e J_TPO está abaixo de zero.
   - Novas posições são bloqueadas às sextas-feiras quando a hora do mercado ultrapassa o limite configurado.
3. **Gerenciamento de saídas**
   - Os sinais opostos do SilverTrend fecham as negociações abertas imediatamente.
   - Os níveis iniciais opcionais de stop loss e takeprofit são colocados em distâncias fixas (expressas em pontos).
   - Um trailing stop opcional segue o preço quando ele ultrapassa o buffer de lucro configurado.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `Volume` | Tamanho do pedido em lotes. | `1` |
| `TrailingStopPoints` | Distância do trailing stop em faixas de preço. `0` desativa o rastreamento. | `0` |
| `TakeProfitPoints` | Tire a distância do lucro em faixas de preço. `0` desativa o lucro. | `0` |
| `InitialStopPoints` | Distância inicial de stop loss em pontos de preço. `0` desativa a parada protetora. | `0` |
| `FridayCutoffHour` | Hora (horário de câmbio) após a qual novas negociações na sexta-feira não serão permitidas. | `16` |
| `CandleType` | Tipo de vela ou período de tempo usado para análise. | `1h` velas |

## Notas adicionais
- Apenas uma posição está aberta a qualquer momento, correspondendo ao comportamento de negociação única do consultor especialista original.
- A implementação usa StockSharp API de alto nível, então a estratégia assina velas e executa lógica apenas em barras finalizadas.
- Os trailing e os stops fixos são gerenciados internamente e fecharão a posição ao preço de mercado, uma vez acionados.
