# Diagrama da estratégia de cruzamento do TRIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Aqui o TRIX não é um indicador pronto, e sim uma série montada dentro do diagrama, exatamente como a estratégia original a monta: uma média exponencial tripla e sua variação relativa de uma barra. O gatilho é a série rápida cruzando o zero, a série lenta precisa se mover no mesmo sentido acima de um limiar, e um alvo e um stop percentuais encerram a operação.

![schema](schema.svg)

## Visão geral da estratégia

- A matéria-prima são duas médias exponenciais triplas do preço de fechamento, de 9 e 21 barras; blocos de valor anterior guardam cada uma delas um candle atrás.
- O TRIX lento é um bloco de fórmula: a média menos o seu valor anterior, dividida por esse mesmo valor anterior, que é a variação relativa por barra calculada no código original.
- O cruzamento do zero pelo TRIX rápido é desenhado como o cruzamento da média rápida com o seu próprio valor anterior. Como a média de preços é positiva, o sinal da variação relativa é o sinal da diferença, então o bloco de cruzamento é um substituto exato e dispensa a divisão.
- O limiar do TRIX lento é o que mantém o diagrama fora do mercado lateral: a virada da série rápida só é aceita enquanto a lenta se move mais de 0,05 por cento por barra no mesmo sentido.
- O original roda em candles de quatro horas com alvo de 1500 e stop de 500 em unidades absolutas de preço; o diagrama foi reduzido para cinco minutos por causa do histórico de amostra incluído, e as duas distâncias viraram porcentagens do preço de entrada na mesma proporção de três para um.
- O indicador Trix embutido é deliberadamente evitado: ele é uma cadeia de três suavizações sucessivas com um fator de escala, portanto seus valores e sinais diferem da média exponencial tripla sobre a qual a estratégia foi escrita.

## Regras de entrada e saída

- **Entrada comprada**: O TRIX rápido cruza o zero para cima, ou seja, a média tripla rápida vira para cima depois de cair, o TRIX lento está acima do limiar e a posição não está comprada. A ordem compra um lote a mercado: a partir do zero abre uma compra, contra uma venda de mesmo tamanho a encerra.
- **Entrada vendida**: O TRIX rápido cruza o zero para baixo, ou seja, a média tripla rápida vira para baixo depois de subir, o TRIX lento está abaixo do limiar negativo e a posição não está vendida. A ordem vende um lote a mercado: a partir do zero abre uma venda, contra uma compra de mesmo tamanho a encerra.
- **Saída**: O bloco de proteção encerra a operação no alvo ou no stop, ambos medidos em porcentagem do preço de entrada; de resto a posição é mantida até o sinal contrário, que a fecha porque todas as ordens usam o mesmo volume.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Fast TEMA length | 9 | Período da média exponencial tripla rápida sobre a qual se constrói a série do gatilho. |
| Slow TEMA length | 21 | Período da média exponencial tripla lenta sobre a qual se constrói a série de confirmação. |
| Volume | 1 | Volume da ordem, em lotes; a mesma constante alimenta os dois blocos de ordem. |
| Take profit, % | 1.5 | Distância do alvo, em porcentagem do preço de entrada. |
| Stop loss, % | 0.5 | Distância do stop, em porcentagem do preço de entrada. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- Um conversor retira o preço de fechamento do candle e alimenta os dois blocos de indicador; o mesmo valor chega ao bloco de proteção como preço atual.
- Atrás de cada média há um bloco de valor anterior: o par rápido entra num bloco de cruzamento e o lento num bloco de fórmula que divide a diferença pelo valor anterior.
- O bloco de cruzamento sinaliza a virada para cima e um bloco NÃO a inverte na virada para baixo; duas comparações colocam a série lenta diante das constantes de limiar positiva e negativa.
- Cada E lógico une a virada, a confirmação e a checagem de posição e aciona um bloco de modificação de posição; ambos enviam o seu negócio ao bloco de proteção.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
