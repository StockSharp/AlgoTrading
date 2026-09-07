# Diagrama da estratégia de varredura do dia anterior com alerta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia rompimentos falsos da faixa do dia UTC anterior. À meia-noite, ele fixa a máxima e a mínima daquele dia, espera uma vela de quinze minutos ultrapassar um limite e fechar novamente dentro da faixa, entra contra a varredura e grava uma mensagem no log para cada vela de sinal aceita.

![schema](schema.svg)

## Visão geral da estratégia

- Velas concluídas de quinze minutos fornecem máxima, mínima e fechamento usados em todas as decisões.
- Highest(96) e Lowest(96) cobrem um dia completo. Blocos Previous value com deslocamento 1 excluem a nova vela da meia-noite antes da captura da faixa.
- A máxima e a mínima capturadas permanecem fixas das 00:00 UTC até o fim daquele dia civil. A avaliação de entradas começa às 00:15 UTC.
- Um rompimento falso acima da máxima mantida gera uma configuração vendida; um rompimento falso abaixo da mínima mantida gera uma configuração comprada.
- Entradas são permitidas somente com posição zerada e usam ordens a mercado de uma unidade. Se as duas condições ocorrerem na mesma vela, a varredura da máxima tem prioridade.
- Cada entrada executada recebe take-profit fixo de 1% e stop-loss de 1%, ativados como saídas a mercado pelos fechamentos de velas concluídas.

## Regras de entrada e saída

- **Entrada comprada**: Das 00:15 às 23:59:59 UTC, a mínima da vela deve ficar abaixo da mínima mantida do dia anterior, o fechamento deve ficar acima desse nível, não pode haver varredura simultânea da máxima e Position deve ser zero. Compra-se uma unidade a mercado.
- **Entrada vendida**: Na mesma janela, a máxima da vela deve ficar acima da máxima mantida do dia anterior, o fechamento deve ficar abaixo desse nível e Position deve ser zero. Vende-se uma unidade a mercado.
- **Alerta**: Cada sinal comprado ou vendido aceito passa uma vez por um Flag por vela. String Formatter inclui o fechamento da vela de sinal em uma mensagem que Notification envia ao log da estratégia.
- **Saída**: Position protection fecha a entrada executada após um movimento favorável de 1% ou adverso de 1%. As duas saídas protetoras são ordens a mercado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Velas | 00:15:00 | Período das velas concluídas para construção da faixa e decisões. |
| Comprimento de Highest | 96 | Quantidade de máximas de quinze minutos em uma faixa diária completa. |
| Fonte de Highest | Não definida | Nenhum campo alternativo de entrada do indicador foi selecionado. |
| Comprimento de Lowest | 96 | Quantidade de mínimas de quinze minutos em uma faixa diária completa. |
| Fonte de Lowest | Não definida | Nenhum campo alternativo de entrada do indicador foi selecionado. |
| Volume | 1 | Tamanho de cada ordem de entrada a mercado. |
| Take-profit | 1% | Movimento favorável desde a execução que ativa a proteção. |
| Stop-loss | 1% | Movimento adverso desde a execução que ativa a proteção. |
| Stop-loss móvel | false | Mantém fixo o limite do stop. |
| Usar ordens a mercado | true | Envia saídas protetoras ativadas como ordens a mercado. |

## Detalhes do diagrama

- Conversores HighPrice e LowPrice alimentam os dois indicadores móveis de 96 valores; ClosePrice fornece os testes de retorno à faixa, o texto do alerta e as verificações da proteção.
- Cada resultado móvel passa por um bloco Previous value concluído com deslocamento 1. Das 00:00 às 00:14:59 UTC, Variables de captura recebem o valor anterior e Variables de retenção o publicam em cada vela.
- Quatro comparações detectam a perfuração da máxima com fechamento abaixo da máxima mantida ou a perfuração da mínima com fechamento acima da mínima mantida. Working time restringe as duas combinações a 00:15-23:59:59 UTC.
- O valor da posição é amostrado em cada vela e comparado com zero. A porta vendida combina a varredura da máxima com a verificação de posição zerada; a porta comprada exige também o sinal invertido da varredura da máxima para preservar a prioridade.
- As duas portas aceitas acionam Buy e Sell nos blocos Modify position e são reunidas para o aviso. Um Flag reiniciado por cada vela impede mensagens duplicadas dentro de um evento de sinal sem suprimir sinais posteriores no mesmo dia.
- Execuções de entrada armam o bloco Position protection compartilhado. Seu preço de referência é atualizado pelo fechamento concluído; assim, toques intravela que recuam antes do fechamento não são observados.
- A primeira faixa utilizável exige 96 velas anteriores de quinze minutos. Os níveis são atualizados somente na captura da meia-noite e permanecem inalterados até o próximo dia UTC.
- O gráfico mostra velas, limites diários móveis e mantidos, execuções de compra e venda e todas as execuções da estratégia, inclusive saídas protetoras.

## Uso

Importe o arquivo `.json` no Designer, execute-o no backtester com histórico suficiente para formar a primeira faixa diária e ajuste o período, os comprimentos da faixa, as distâncias de proteção e o volume ao instrumento antes da negociação ao vivo.
