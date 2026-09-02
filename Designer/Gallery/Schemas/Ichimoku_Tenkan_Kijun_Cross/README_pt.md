# Diagrama da estratégia de cruzamento Tenkan/Kijun do Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Aqui o sistema Ichimoku é usado por inteiro: o par de linhas rápidas dá o sinal e a nuvem decide se esse sinal é permitido. O cruzamento de Tenkan-sen com Kijun-sen é o gatilho, e a posição só é aberta quando o fechamento está do mesmo lado da nuvem Kumo para onde o cruzamento aponta.

![schema](schema.svg)

## Visão geral da estratégia

- Um único bloco Ichimoku constrói todas as linhas, e quatro conversores leem Tenkan-sen, Kijun-sen, Senkou Span A e Senkou Span B do seu valor composto.
- Dois blocos de fórmula dobram as duas linhas Senkou no topo e no fundo da nuvem, de modo que basta uma comparação por lado para situar o fechamento em relação à nuvem.
- As entradas só ocorrem a partir do zero, e isso é verificado duas vezes: comparando a posição com zero e pela condição de abertura do próprio bloco de ordem.
- As saídas são blocos separados: o cruzamento contrário ou um fechamento que volta a cair dentro da nuvem levam a posição de volta ao zero, e os blocos de fechamento tomam o tamanho da posição aberta.
- O original ignora qualquer sinal por 500 candles após uma execução, o que também atrasa suas saídas; não é possível montar um contador de barras com estes blocos, então essa pausa fica de fora e o diagrama opera com mais frequência que o original.

## Regras de entrada e saída

- **Entrada comprada**: Tenkan-sen cruza acima de Kijun-sen, o fechamento está acima do topo da nuvem e a posição está zerada. A ordem compra o volume fixo e abre a compra.
- **Entrada vendida**: Tenkan-sen cruza abaixo de Kijun-sen, o fechamento está abaixo do fundo da nuvem e a posição está zerada. A ordem vende o volume fixo e abre a venda.
- **Saída**: A compra é encerrada quando Tenkan-sen volta a cruzar abaixo de Kijun-sen ou o fechamento cai abaixo do fundo da nuvem; a venda, na imagem espelhada. A ordem de fechamento é dimensionada pela posição, então o diagrama volta ao zero em vez de inverter, e não há stop nem alvo, como no original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Tenkan period | 9 | Período de Tenkan-sen, o ponto médio entre a máxima e a mínima desse número de candles. |
| Kijun period | 26 | Período de Kijun-sen, construído da mesma forma sobre uma janela mais longa. |
| Senkou Span B period | 52 | Período de Senkou Span B, a mais lenta das duas bordas da nuvem. |
| Volume | 1 | Volume da ordem, em lotes, usado para abrir a posição; as saídas fecham o tamanho que estiver aberto. |
| Candles | 00:01:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o indicador Ichimoku e um conversor para o preço de fechamento.
- Tenkan-sen e Kijun-sen se encontram em um bloco de cruzamento cuja saída é o cruzamento de alta; um NÃO lógico dela dá o cruzamento de baixa.
- As duas comparações com a nuvem são compartilhadas entre entradas e saídas: acima da nuvem abre-se uma compra e fecha-se uma venda, abaixo dela ocorre o contrário.
- Cada entrada passa por um E lógico junto com a checagem de posição zerada, enquanto cada saída passa por um OU lógico, de modo que o cruzamento ou o rompimento da nuvem já basta para acionar um bloco de fechamento.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
