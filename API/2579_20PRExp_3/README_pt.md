# Estratégia de Rompimento 20PRExp-3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia 20PRExp-3 é um sistema de rompimento que compara a sessão de trading atual com os extremos de preço do dia anterior. Ela reconstrói o canal diário em cada vela de cinco minutos concluída, confirma o momentum com uma expansão do volume de ticks de 30 minutos e entra apenas quando o preço rompe além do máximo ou mínimo de sessão atualizado. Uma vez no mercado, espelha o especialista original do MetaTrader 5 usando saídas de Parabolic SAR, stops trailing dinâmicos e dimensionamento fixo de risco baseado na distância ao stop de proteção.

## Conceito
- **Canal diário**: Rastrear o máximo em andamento, mínimo e ponto médio do dia de trading atual.
- **Confirmação de rompimento**: Exigir que o preço feche além do limite do canal com um filtro de intervalo diário mínimo (`GapPoints`).
- **Expansão de volume**: Comparar as duas últimas velas de 30 minutos concluídas e exigir pelo menos um aumento de 1,5× no volume de ticks para evitar rompimentos fracos.
- **Filtro de tempo**: Permitir novas posições apenas após a hora de início de sessão configurada (`SessionStartHour`) para evitar o intervalo noturno de baixa liquidez.
- **Simetria de risco**: Operações compradas usam o mínimo diário como stop loss, operações vendidas usam o máximo diário. Os deslocamentos de take profit e trailing são medidos em pontos de preço.

## Dados de mercado
- Velas de cinco minutos para o sinal primário e o cálculo do Parabolic SAR.
- Velas de trinta minutos para o filtro de taxa de volume de ticks.
- As estatísticas de máximo/mínimo diário são derivadas em tempo real dos dados de cinco minutos, portanto nenhuma subscrição diária separada é necessária.

## Lógica de entrada
1. Aguardar uma vela de cinco minutos terminada após a hora de início configurada.
2. Calcular o máximo/mínimo/ponto médio do dia atual e a largura do canal.
3. Verificar se a largura do canal excede `GapPoints * PriceStep`.
4. Calcular a taxa de volume de ticks = (último volume de 30 minutos concluído) / (volume de 30 minutos anterior) e garantir que seja maior que 1,5.
5. **Configuração comprada**: a vela fecha em ou acima do máximo diário atual → comprar.
6. **Configuração vendida**: a vela fecha em ou abaixo do mínimo diário atual → vender.
7. Pular novas entradas enquanto uma posição estiver ativa (máximo uma operação aberta).

## Gestão de saída
- **Stop inicial**: operações compradas usam o mínimo diário, operações vendidas usam o máximo diário capturado na entrada.
- **Take profit**: opcional; colocado a `TakeProfitPoints * PriceStep` da entrada em ambos os lados do mercado.
- **Reversão Parabolic SAR**: fecha a posição se o valor de SAR cruzar o fechamento da vela anterior (comportamento do EA original).
- **Stop trailing**: ativa quando o lucro excede `TrailingStopPoints * PriceStep` e se move pelo menos `TrailingStepPoints * PriceStep`.
- **Take trailing espelho**: sempre que o stop trailing for atualizado, o nível de take-profit é reposicionado simetricamente em torno do fechamento atual.

## Dimensionamento de posição
- O volume de posição é derivado de `RiskPercent`: a estratégia arrisca uma porcentagem do valor atual do portfólio baseado na distância entre entrada e stop.
- Se a avaliação do portfólio não estiver disponível, o algoritmo recorre a `Volume + |Position|` e, como último recurso, opera um único contrato.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `CandleType` | Velas de 5 minutos | Período primário para sinais e Parabolic SAR. |
| `VolumeCandleType` | Velas de 30 minutos | Período usado para avaliar a expansão do volume de ticks. |
| `TakeProfitPoints` | 20 | Distância ao alvo de lucro expressa em pontos de preço. Definir como 0 para desabilitar. |
| `TrailingStopPoints` | 10 | Distância em pontos para a ativação do stop trailing. |
| `TrailingStepPoints` | 10 | Progresso adicional mínimo (em pontos) antes que o stop trailing se mova novamente. |
| `RiskPercent` | 5 | Porcentagem do capital do portfólio arriscada por operação. |
| `GapPoints` | 50 | Largura mínima do canal diário em pontos necessária para habilitar um rompimento. |
| `SessionStartHour` | 7 | Hora (0–23) após a qual a estratégia pode abrir novas posições. |

## Notas
- Os parâmetros do Parabolic SAR (passo 0,005, máx 0,01) correspondem à estratégia MQL original.
- Os valores do ponto médio diário são calculados para completude e podem ser plotados para referência visual se desejado.
- Como a expansão de volume é avaliada em velas de 30 minutos concluídas, a confirmação do rompimento usa as informações mais recentes disponíveis de fechamento a fechamento, o que é robusto tanto para testes históricos quanto para trading ao vivo.
- Todos os comentários no código estão em inglês para alinhar com as diretrizes do repositório.
