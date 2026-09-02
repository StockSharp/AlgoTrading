# Diagrama da estratégia de virada do mês
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia um efeito de calendário em vez de um padrão de preço: carrega uma posição comprada sobre a fronteira entre dois meses e fica zerado no meio do mês. Não há indicador algum; a única entrada é a data de cada candle finalizado.

![schema](schema.svg)

## Visão geral da estratégia

- Um conversor extrai o número do dia do horário de abertura do candle, e uma fórmula curta o transforma na distância até a borda de mês mais próxima: min(day - 1, 31 - day).
- Um único limiar define toda a janela: enquanto a distância for menor ou igual a ele, a data conta como virada do mês; acima dele, como meio do mês.
- O original conta dias úteis e pula fins de semana; um diagrama não tem laços, então usam-se dias corridos e a janela fica simétrica em torno da fronteira do mês. Num mês de 31 dias ela cobre os seis primeiros e os seis últimos dias, num mês curto um ou dois a menos.
- A estratégia é apenas comprada, então a verificação da posição decide entre abrir e fechar, e não existe ramo vendido.
- A pausa de 10 barras entre operações do original foi omitida: com uma janela de vários dias e a entrada travada pela condição de posição, ela não muda nada.

## Regras de entrada e saída

- **Entrada comprada**: A distância até a borda do mês é menor ou igual à janela e a posição não está comprada. A ordem compra o volume fixo e abre a compra que deve atravessar a virada do mês.
- **Entrada vendida**: Não há entrada vendida. A estratégia só mantém uma compra ou fica zerada, exatamente como o original.
- **Saída**: A distância até a borda do mês é maior que a janela e a posição está comprada. O bloco de fechamento envia uma ordem a mercado do tamanho da posição aberta, de modo que o diagrama passa o meio do mês zerado. Não há stop nem alvo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Window, days | 5 | Semilargura da janela de calendário, em dias: a data conta como virada do mês enquanto não estiver mais distante que esse valor do primeiro ou do último dia. |
| Volume | 1 | Volume da ordem, em lotes, usado para abrir a compra; a saída fecha o tamanho que estiver aberto. |
| Candles | 00:30:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta um conversor tipado como candle cujo caminho de propriedade é OpenTime.Day, o que devolve o dia do calendário como um número simples.
- O bloco de fórmula dobra esse número na distância até a borda de mês mais próxima, então um único limiar cobre o fim de um mês e o início do seguinte.
- Dois blocos de comparação dividem o calendário na janela e no restante; outros dois comparam a posição com uma constante zero.
- Cada E lógico une uma condição de calendário a uma de posição: a primeira aciona um bloco de abertura e a segunda um bloco de fechamento que toma o tamanho da própria posição.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
