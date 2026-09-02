# Diagrama da estratégia de reversão no ponto pivô
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O pivô clássico do pregão é recalculado a cada candle sobre uma janela móvel: a máxima e a mínima dos últimos sessenta candles, junto com o fechamento atual, produzem o pivô P, o suporte S1 e a resistência R1. O diagrama opera contra o movimento nas bordas dessa faixa e realiza o lucro no próprio pivô.

![schema](schema.svg)

## Visão geral da estratégia

- Highest e Lowest sobre a mesma janela substituem a amplitude da sessão anterior, de modo que os níveis acompanham o mercado em vez de ficarem fixos uma vez por dia.
- P = (High + Low + Close) / 3, S1 = 2P - High, R1 = 2P - Low, e uma folga de dois por cento da amplitude da janela alarga as duas zonas.
- A entrada exige ainda que o candle concorde: de alta no suporte, de baixa na resistência.
- O alvo é o próprio pivô: a posição é encerrada assim que o fechamento passa para o outro lado de P.

## Regras de entrada e saída

- **Entrada comprada**: A mínima do candle entra na zona de S1 (mínima <= S1 + folga), o candle fecha acima da sua abertura e a posição está zerada. A ordem de compra abre uma posição comprada de um lote.
- **Entrada vendida**: A máxima do candle alcança a zona de R1 (máxima >= R1 - folga), o candle fecha abaixo da sua abertura e a posição está zerada. A ordem de venda abre uma posição vendida de um lote.
- **Saída**: A compra é encerrada quando o fechamento fica acima do pivô e a venda quando fica abaixo. Os dois blocos de saída trabalham em modo de encerramento de posição, portanto não fazem nada quando não há o que encerrar. O código original não tem stop nem alvo, e o diagrama mantém isso.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Highest Length | 60 | Tamanho da janela do indicador Highest, ou seja, quantos candles entram na máxima. |
| Lowest Length | 60 | Tamanho da janela do indicador Lowest; mantenha igual ao do Highest. |
| Zone Buffer | 0.02 | Largura das zonas de entrada como fração da amplitude da janela: 0,02 são dois por cento. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta os indicadores Highest e Lowest e também quatro conversores: abertura, máxima, mínima e fechamento.
- Três blocos de fórmula transformam esses cinco números no pivô, no suporte com folga e na resistência com folga; a folga é uma constante separada e por isso pode ser otimizada.
- Cada entrada é um E lógico de três comparações: toque no nível, direção do candle e posição zerada.
- Os dois blocos de saída são acionados por uma comparação simples entre o fechamento e o pivô e usam o modo de encerramento em vez de volume fixo.
- A estratégia original usa candles de um minuto e fica quinhentas barras em silêncio após cada operação; o diagrama trabalha em cinco minutos, que é o que o histórico incluído oferece, e não tem essa pausa.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
