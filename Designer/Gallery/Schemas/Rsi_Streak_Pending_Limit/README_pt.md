# Diagrama de limites pendentes por sequência de RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama espera o RSI permanecer em uma zona extrema antes de colocar uma ordem limitada de retração. Ele confirma o RSI atual e os dois valores finalizados anteriores, permite uma ordem pendente por permanência contínua na zona, cancela a ordem quando o RSI sai dela e protege cada execução com saídas percentuais.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de cinco minutos alimentam um RSI de comprimento 14 e fornecem o fechamento usado no cálculo das ordens pendentes.
- A zona inferior fica abaixo de 30 e a superior acima de 70. Um bloco N values agenda a avaliação após três atualizações finalizadas do RSI.
- Na avaliação, blocos Formula verificam juntos o RSI atual, os dois valores anteriores e a restrição de posição. Uma sequência interrompida não gera entrada.
- A compra limitada é colocada 0,2% abaixo do fechamento do sinal; a venda limitada, 0,2% acima.
- Blocos Flag separados permitem apenas uma ordem durante cada permanência contínua em uma zona extrema.
- Entradas executadas recebem alvo de 1,5% e stop de 1%; ambos enviam uma ordem a mercado quando ativados.

## Regras de entrada e saída

- **Entrada comprada**: Após a avaliação de três atualizações, os três valores do RSI devem estar abaixo de 30 e Position deve ser menor ou igual a zero. Registra-se uma compra limitada em `Close × (1 − Pending Offset / 100)`.
- **Entrada vendida**: Após a avaliação de três atualizações, os três valores do RSI devem estar acima de 70 e Position deve ser maior ou igual a zero. Registra-se uma venda limitada em `Close × (1 + Pending Offset / 100)`.
- **Ordem pendente**: Uma compra não executada é cancelada quando o RSI sobe acima de 30; uma venda não executada é cancelada quando o RSI cai abaixo de 70. O mesmo evento reinicia o Flag desse lado para uma visita posterior.
- **Saída**: Depois que uma ordem pendente é executada, Position protection fecha sua exposição com um movimento favorável de 1,5% ou desfavorável de 1%, usando uma ordem a mercado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:05:00 | Período dos candles finalizados usados nos sinais e preços. |
| Comprimento do RSI | 14 | Comprimento de média do índice de força relativa. |
| Campo de entrada do RSI | Não definido | Nenhum campo de entrada alternativo está selecionado para o indicador. |
| Sobrevenda | 30 | Limite superior estrito de uma sequência comprada de três valores. |
| Sobrecompra | 70 | Limite inferior estrito de uma sequência vendida de três valores. |
| Número de confirmações (N) | 3 | Quantidade de atualizações finalizadas do RSI no intervalo de confirmação. |
| Afastamento pendente, % | 0.2 | Distância entre o limite e o fechamento do candle de sinal. |
| Volume | 1 | Tamanho de cada ordem pendente de entrada. |
| Alvo, % | 1.5 | Movimento favorável desde a entrada executada que ativa a proteção. |
| Stop, % | 1 | Movimento desfavorável desde a entrada executada que ativa a proteção. |
| Stop móvel | false | Mantém fixo o limite de stop. |
| Usar ordens a mercado | true | Envia as saídas de proteção ativadas como ordens a mercado. |

## Detalhes do diagrama

- O RSI atual alimenta dois blocos Previous value com deslocamentos 1 e 2. Os três valores vêm somente de candles finalizados.
- Um bloco N values compartilhado é armado por qualquer zona extrema e conta três atualizações do RSI. Sua saída captura as duas pontuações de entrada no mesmo ponto de avaliação.
- A pontuação comprada só é negativa quando os três valores do RSI estão abaixo de 30 e Position não é positiva. A pontuação vendida só é negativa quando os três valores estão acima de 70 e Position não é negativa.
- Se o RSI sair da zona durante o intervalo de confirmação, a pontuação correspondente não será negativa e nenhum disparo de registro será produzido. Uma visita extrema posterior pode iniciar um novo intervalo.
- Blocos Formula calculam os dois limites a partir do fechamento, e blocos Order registering enviam ordens limitadas sem arredondamento de uma unidade.
- Order cancellation mantém a ordem mais recente de cada lado e atua assim que o RSI cruza de volta o limite dessa zona.
- Uma ordem de uma unidade contra uma posição oposta de uma unidade primeiro zera a exposição; outra visita confirmada é necessária para criar exposição na nova direção.
- O gráfico mostra candles de cinco minutos, RSI com os dois níveis, ambos os fluxos de ordens pendentes e todas as execuções ou saídas da estratégia.

## Uso

Importe o arquivo `.json` no Designer, execute-o com dados históricos no backtester e ajuste os níveis do RSI, o afastamento pendente, as distâncias de proteção e o volume ao instrumento antes de operar ao vivo.
