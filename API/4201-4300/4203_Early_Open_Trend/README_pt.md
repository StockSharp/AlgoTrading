# Estratégia de Tendência Aberta Antecipada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Porto do MetaTrader 4 consultor especialista `earlyOpenTrend.mq4` localizado em `MQL/9826`.
- Negocia uma vez por direção por dia, comparando o preço atual com a abertura diária após uma confirmação baseada no pavio.
- Imita a lógica original da janela de tempo, incluindo o deslocamento do horário de verão que altera a sessão do corretor em uma ou duas horas.
- Usa StockSharp API de alto nível com assinaturas de velas, proteção de posição automatizada e manipulação de sessão integrada.

## Lógica de Mercado
1. Construa uma série de velas intradiárias (padrão 15 minutos) e reconstrua os valores de abertura, máximo e mínimo do dia atual.
2. Determine o deslocamento do horário de verão ativo: entre `SummerTimeStartDay` e `WinterTimeStartDay` a estratégia subtrai duas horas dos tempos de sessão configurados; caso contrário, uma hora será subtraída. Isso reproduz a variável `ZD` original.
3. Avalie os sinais apenas quando o horário de início da vela estiver dentro de `[StartHour, EndHour)` após a correção do horário de verão e a estratégia estiver plana.
4. Configuração longa:
   - A última vela fechou acima do preço de abertura diário.
   - A distância entre a abertura diária e o mínimo do dia atual excede `RangeFilterPips` (convertido em preço absoluto usando o tamanho do pip do instrumento).
   - Nenhuma negociação longa foi aberta anteriormente durante o mesmo dia de negociação.
5. Configuração curta:
   - A última vela fechou abaixo do preço de abertura diário.
   - A distância entre a máxima do dia atual e a abertura diária excede `RangeFilterPips`.
   - Nenhuma negociação a descoberto foi aberta anteriormente durante o mesmo dia de negociação.
6. Quando um sinal é acionado, a estratégia emite uma ordem de mercado com volume `OrderVolume`. O carimbo de data e hora da negociação é armazenado para suportar saídas de tempo de espera.

## Regras de sessão e saída
- `EndHour` evita novas entradas após o horário especificado (ajustado pelo deslocamento do horário de verão).
- `ClosingHour` força o fechamento da posição assim que a hora corrigida do servidor atingir o valor configurado.
- `HoldingHours` impõe uma duração máxima de retenção adicional; uma vez excedida, a posição é fechada independentemente do tempo da sessão.
- Cada direção de negociação pode ser executada no máximo uma vez por dia corrido. Os sinalizadores diários são redefinidos quando a estratégia detecta o início de uma nova sessão.

## Gestão de risco
- `StopLossPips` e `TakeProfitPips` são transformados em compensações de preço absoluto usando o tamanho do pip derivado de `Security.PriceStep` (símbolos de 5 dígitos multiplicam automaticamente o passo por 10).
- Se qualquer um dos parâmetros for maior que zero, a estratégia permite `StartProtection` a execução de mercado, replicando a lógica original pós-entrada `OrderModify`.
- Fora das saídas forçadas descritas acima, nenhuma lógica adicional é aplicada.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `OrderVolume` | 0,1 | Tamanho de cada ordem de mercado. |
| `OrderType` | 0 | Filtro de direção: `0` = ambos, `1` = apenas longo, `2` = apenas curto. |
| `RangeFilterPips` | 1 | Distância mínima do pavio entre a abertura diária e o extremo oposto antes de entrar. |
| `TakeProfitPips` | 100 | Distância de lucro em pips (0 desabilita). |
| `StopLossPips` | 1000 | Distância de stop-loss em pips (0 desativa). |
| `StartHour` | 7 | Hora de início da sessão antes da subtração do horário de verão. |
| `EndHour` | 18 | Hora de término da sessão antes da subtração do horário de verão. |
| `ClosingHour` | 20 | Hora usada para nivelar negociações abertas. |
| `HoldingHours` | 0 | Tempo máximo de retenção em horas (0 desabilita). |
| `SummerTimeStartDay` | 87 | Primeiro dia do ano que ativa o deslocamento de horário de verão de duas horas. |
| `WinterTimeStartDay` | 297 | Dia do ano em que o deslocamento retorna para uma hora. |
| `CandleType` | Período de 15 minutos | Série de velas usadas para cálculos. |

## Notas de uso
1. Anexe a estratégia a um título e certifique-se de que o tipo de vela corresponda à granularidade do feed de dados que você deseja negociar.
2. Ajuste o horário da sessão para corresponder ao relógio do servidor do corretor. Os parâmetros do horário de verão podem ser ajustados se o regime de horário de verão local for diferente do horário europeu padrão.
3. Configure paradas e alvos baseados em pip de acordo com o tamanho do tick do instrumento; a estratégia converte automaticamente os pips usando o valor do pip detectado.
4. Comece a estratégia. Ele atualizará o perfil do dia em cada vela concluída, avaliará os critérios de entrada dentro da janela da sessão e imporá a restrição de negociação única por direção.

## Diferenças vs. Especialista MQL original
- Usa velas finalizadas em vez de verificações de nível de tick `Bid`/`Ask`, o que atrasa um pouco as entradas, mas mantém a lógica determinística dentro de StockSharp.
- As ordens de proteção são implementadas por meio de `StartProtection` em vez de chamadas manuais `OrderModify`.
- Objetos gráficos e comentários de status do gráfico MetaTrader (retângulos, rótulos, exibição espelhada) são omitidos.
- As saídas forçadas no fechamento da sessão fecham a posição imediatamente, em vez de mudar para uma meta de ponto de equilíbrio quando estiver debaixo d'água.

## Recomendações de teste
- Backtest com dados intradiários que cobrem toda a sessão de negociação para que os máximos/mínimos diários correspondam ao ambiente ao vivo.
- Valide a configuração do horário de verão simulando datas nos períodos de verão e inverno.
- Experimente diferentes limites de pavio e horas de sessão para alinhar o comportamento com o perfil de volatilidade da sua corretora.
