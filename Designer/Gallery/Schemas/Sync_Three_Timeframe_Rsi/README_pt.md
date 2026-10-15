# Diagrama da estratégia de concordância RSI em três períodos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama espera valores RSI formados em velas de cinco, quinze e trinta minutos, coleta os três em cada fechamento do período lento e os avalia como um grupo sincronizado. A concordância abaixo de 30 abre ou reverte para comprado; a concordância acima de 70 abre ou reverte para vendido.

![schema](schema.svg)

## Visão geral da estratégia

- Três fluxos de velas concluídas calculam valores RSI(14) independentes nos períodos de 5, 15 e 30 minutos.
- O evento RSI de trinta minutos coleta o último valor de cada fluxo. Sync agrupa as três amostras com intervalo de 30 minutos e as limpa após a liberação.
- Uma decisão é tomada para cada valor RSI formado de trinta minutos, somente depois que as três saídas sincronizadas atualizam suas comparações.
- Os três RSI abaixo do limiar de compra geram uma configuração comprada. Os três acima do limiar de venda geram uma configuração vendida.
- As portas de posição impedem outra ordem na direção já mantida. Uma configuração contrária envia uma reversão a mercado de duas unidades; uma entrada com posição zerada usa uma unidade.
- Não há stop-loss, take-profit, saída por tempo nem pausa independentes. A próxima configuração contrária qualificada é a única saída e estabelece imediatamente a nova direção.

## Regras de entrada e saída

- **Entrada comprada**: Fast RSI, Middle RSI e Slow RSI sincronizados devem estar estritamente abaixo de 30, e Position deve ser menor ou igual a zero. Compra-se `Base Volume + abs(sign(Position))` a mercado: uma unidade com posição zerada ou duas unidades a partir da posição vendida de uma unidade criada pelo diagrama.
- **Entrada vendida**: Os três RSI sincronizados devem estar estritamente acima de 70, e Position deve ser maior ou igual a zero. Vende-se a mesma quantidade calculada a mercado: uma unidade com posição zerada ou duas unidades a partir da posição comprada de uma unidade criada pelo diagrama.
- **Saída**: Uma posição comprada só é fechada por uma configuração vendida qualificada, e uma posição vendida apenas por uma configuração comprada qualificada. A ordem de reversão fecha a exposição de uma unidade e abre uma unidade na nova direção.
- **Escopo da posição**: A fórmula normalizada destina-se às posições criadas por este diagrama. Se uma posição externa tiver módulo superior a uma unidade base, uma reversão de duas unidades não garante atingir o alvo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Série de velas rápidas | 00:05:00 | Velas concluídas de cinco minutos usadas pelo Fast RSI. |
| Série de velas médias | 00:15:00 | Velas concluídas de quinze minutos usadas pelo Middle RSI. |
| Série de velas lentas | 00:30:00 | Velas concluídas de trinta minutos que agendam decisões sincronizadas. |
| Comprimento do Fast RSI | 14 | Comprimento de média do RSI no fluxo rápido. |
| Fonte do Fast RSI | Não definida | Nenhum campo alternativo de entrada do indicador foi selecionado. |
| Comprimento do Middle RSI | 14 | Comprimento de média do RSI no fluxo médio. |
| Fonte do Middle RSI | Não definida | Nenhum campo alternativo de entrada do indicador foi selecionado. |
| Comprimento do Slow RSI | 14 | Comprimento de média do RSI no fluxo lento. |
| Fonte do Slow RSI | Não definida | Nenhum campo alternativo de entrada do indicador foi selecionado. |
| Limiar de compra | 30 | Limite superior estrito da concordância que permite entrada comprada. |
| Limiar de venda | 70 | Limite inferior estrito da concordância que permite entrada vendida. |
| Volume base | 1 | Exposição-alvo e tamanho da entrada zerada; reversões usam o dobro do padrão. |

## Detalhes do diagrama

- Cada bloco Candles emite somente valores concluídos e pode construir seu período a partir de velas menores armazenadas. Cada RSI começa a emitir após completar seu aquecimento de 14 valores.
- Fast RSI e Middle RSI ficam prontos antes do Slow RSI. Três Variables do tipo indicator value são acionadas por cada evento Slow RSI, evitando que intervalos incompletos de aquecimento permaneçam no início da fila de Sync.
- Sync tem exatamente três pares conectados de entrada e saída, Interval `00:30:00` e Clear Sockets ativado. Suas saídas carregam o último RSI rápido, o último RSI médio e o RSI lento atual com um horário de decisão comum.
- Seis blocos Comparison aplicam testes estritos `< Buy Threshold` e `> Sell Threshold`. Um pulso booleano de liberação chega às duas portas AND de cinco entradas somente depois que as seis comparações processam o grupo atual.
- Current Position é comparada com zero usando `<=` para a rota comprada e `>=` para a rota vendida. Esses testes permitem entrada zerada ou reversão da posição contrária e suprimem entradas repetidas na mesma direção.
- Formula calcula `Base Volume + abs(sign(Position))`. Com a exposição mantida, o resultado é 1 com posição zerada e 2 em uma posição, permanecendo limitado mesmo com várias assinaturas de dados.
- Os blocos Buy e Sell Modify position usam ações a mercado NoCondition porque direção e permissão já foram determinadas pelas portas externas. Não há bloco de proteção nem de gráfico.
- Os limiares são deliberadamente separados dos comprimentos RSI e das séries de velas. Alterar um período ou comprimento muda o aquecimento; Sync ainda espera uma nova tripla de amostras antes da avaliação.

## Uso

Importe o arquivo `.json` no Designer, execute-o no backtester com histórico suficiente para formar o RSI de trinta minutos e ajuste as três séries de velas, comprimentos RSI, limiares e volume base ao instrumento antes da negociação ao vivo.
