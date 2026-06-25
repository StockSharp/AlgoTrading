# Estratégia de Verificação OHLC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Verificação OHLC replica o clássico assessor especializado do MetaTrader que inspeciona a estrutura de abertura, máximo, mínimo e fechamento da vela anterior. A estratégia avalia o corpo da vela em um deslocamento histórico configurável e abre uma nova posição na direção desse corpo, opcionalmente espelhando o sinal. É projetada para execução simples baseada em ação de preço sem depender de osciladores ou médias móveis.

## Lógica de trading
1. A estratégia se inscreve na série de velas configurada e aguarda o fechamento da barra antes de processar.
2. Para cada vela finalizada, o motor armazena o preço de abertura e fechamento para que o deslocamento definido pelo usuário ("SignalShift") possa referenciar barras mais antigas.
3. Um corpo altista (fechamento acima da abertura) gera um sinal comprado, enquanto um corpo baixista (fechamento abaixo da abertura) gera um sinal vendido. Se o corpo for plano, nenhuma operação é criada.
4. O sinalizador `ReverseSignals` pode inverter a direção da operação, reproduzindo o modo de negociação reversa do assessor especializado original.
5. Quando não há posição ativa, a estratégia tenta abrir uma ordem de mercado na direção detectada, desde que o spread atual esteja dentro do limite permitido de `SpreadLimitPips`. O spread é monitorado usando o feed do livro de ordens.
6. Quando já existe uma posição, o sinal oposto aciona o fechamento da posição em vez de uma reversão completa, seguindo a lógica MQL.
7. Proteções opcionais de stop-loss e take-profit são iniciadas na inicialização usando distâncias em pips convertidas para o passo de preço do instrumento, recriando o comportamento de gestão de dinheiro MQL.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `CandleType` | Período de 5 minutos | Série de dados usada para avaliação OHLC. |
| `StopLossPips` | 50 | Distância do stop-loss medida em pips; `0` desativa o stop. |
| `TakeProfitPips` | 100 | Distância do take-profit medida em pips; `0` desativa o alvo. |
| `ReverseSignals` | `false` | Inverte a direção dos sinais comprado e vendido. |
| `SpreadLimitPips` | 1 | Spread máximo, em pips, permitido ao abrir uma nova posição. |
| `SignalShift` | 1 | Número de velas completadas para trás usadas para cálculo do sinal (1 = vela anterior). |
| `OrderVolume` | 1 | Volume enviado com cada ordem de mercado. |

## Notas de uso
- A estratégia usa os metadados do instrumento para converter valores de pips em distâncias de passo de preço. Instrumentos com 3 ou 5 casas decimais recebem automaticamente o ajuste padrão de dez pontos por pip.
- A subscrição do livro de ordens deve estar habilitada no feed de dados para que as verificações de spread funcionem corretamente. Se não houver cotações bid/ask disponíveis, a estratégia ignorará a abertura de novas operações.
- Os stops de proteção são iniciados uma vez durante `OnStarted`. Alterar os parâmetros de stop posteriormente requer reiniciar a estratégia para aplicar novas proteções.
- Como a estratégia reage apenas ao corpo da vela, os valores de máximo e mínimo são ignorados exatamente como na versão original do MetaTrader.

## Passos de implantação
1. Anexe a estratégia a um instrumento que forneça tanto velas quanto cotações do livro de ordens.
2. Configure os parâmetros de acordo com o estilo de negociação desejado (período, distâncias em pips e volume).
3. Lance a estratégia. Ela aguardará a próxima vela completada antes de realizar qualquer ação.
4. Monitore o registro em busca de rejeições por spread ou operações executadas e ajuste os parâmetros conforme necessário.
