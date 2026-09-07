# Diagrama da estratégia com entrada MACD e preço médio combinados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama combina um novo cruzamento da diferença entre EMAs e um evento de compra para preço médio em um único fluxo de entradas longas. Cada compra tem uma unidade, cada ciclo admite no máximo cinco entradas e uma recuperação de dois por cento acima da última execução fecha todas as unidades contabilizadas.

![schema](schema.svg)

## Visão geral da estratégia

- Velas concluídas de cinco minutos alimentam EMA(12), EMA(26) formadas e o preço de fechamento.
- A fórmula `Fast EMA - Slow EMA` cria a linha MACD usada no diagrama; Crossing detecta sua passagem ascendente pelo zero.
- O cruzamento ascendente abre a primeira posição longa somente quando Position não é positiva e o contador de entradas é zero.
- Durante uma posição longa, um fechamento pelo menos cinco por cento abaixo da última execução acrescenta uma unidade se houver menos de cinco entradas contabilizadas.
- Combination reúne exatamente esses dois eventos booleanos e aciona um único bloco Buy a mercado.
- Um fechamento pelo menos dois por cento acima da última execução vende todo o tamanho contabilizado. Em seguida, o contador é zerado para o próximo ciclo.

## Regras de entrada e saída

- **Entrada longa inicial**: EMA(12) menos EMA(26) cruza o zero para cima, Position é menor ou igual a zero e Entries in Current Long é zero. Compra de uma unidade a mercado.
- **Entrada para preço médio**: Position é positiva, o contador está abaixo de Maximum Entries e a vela concluída fecha em ou abaixo de `Latest Entry Fill × (1 - Averaging Drop / 100)`. Compra de mais uma unidade a mercado.
- **Saída**: com Position positiva, uma vela concluída fecha em ou acima de `Latest Entry Fill × (1 + Take Profit / 100)`. Toda a quantidade contabilizada é vendida a mercado e o contador é zerado.
- **Escopo**: o diagrama opera somente comprado e administra as unidades abertas pelo próprio fluxo de entradas. Não há entrada vendida nem stop-loss.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Série de velas | 00:05:00 | Velas concluídas de cinco minutos usadas em todas as decisões. |
| Período da EMA rápida | 12 | Período da média móvel exponencial rápida. |
| Campo da EMA rápida | Não definido | Nenhum campo alternativo de entrada do indicador foi selecionado. |
| Período da EMA lenta | 26 | Período da média móvel exponencial lenta. |
| Campo da EMA lenta | Não definido | Nenhum campo alternativo de entrada do indicador foi selecionado. |
| Máximo de entradas | 5 | Número máximo de compras de uma unidade em um ciclo longo. |
| Queda para preço médio | 5 | Queda percentual desde a última execução necessária para outra compra. |
| Alvo de lucro | 2 | Alta percentual desde a última execução necessária para a saída completa. |
| Volume de entrada | 1 | Volume fixo a mercado de cada compra inicial ou adicional. |

## Detalhes do diagrama

- Candles emite apenas valores concluídos e pode construir a série de cinco minutos com velas armazenadas de intervalo menor.
- Os dois blocos EMA emitem somente valores formados. A primeira diferença MACD utilizável aparece após o aquecimento da EMA lenta.
- A fórmula `a - b` recebe as EMAs rápida e lenta, e Crossing compara o resultado com a Variable de zero.
- Position fornece as verificações de direção, enquanto Entries in Current Long aplica o requisito de contador zero e o limite de cinco entradas.
- Cada execução Buy envia o preço médio da ordem para Latest Entry Fill. Como cada ordem de entrada é executada uma vez, esse é o preço usado pelos dois níveis percentuais.
- Um caminho atrasado soma o volume executado após cada Buy. A execução da saída completa envia zero ao mesmo contador antes da decisão seguinte.
- Combination é booleano e possui duas entradas conectadas: Fresh MACD Entry e Averaging Entry Below Step Five. Entry Volume é publicado depois dos dois ramos para consumir o resultado atual.
- O gráfico recebe velas, as duas EMAs, a linha MACD, o último preço de entrada, o nível de compra adicional, o alvo de lucro e todas as execuções.

## Uso

Importe `Three_Signals_Combined.json` no Designer, forneça histórico suficiente para formar EMA(26) e teste a configuração de cinco minutos no instrumento escolhido. Avalie o risco de cinco entradas e a ausência de stop-loss antes da negociação ao vivo.
