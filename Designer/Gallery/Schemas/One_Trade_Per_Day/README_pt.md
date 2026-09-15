# Diagrama de estratégia de uma operação a cada 24 horas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama negocia cruzamentos estritos da EMA(10) com a EMA(30) em candles finalizados de quatro horas e no sentido inverso. Uma janela configurável de horário de trabalho controla quando candidatos são aceitos, enquanto Flag e um contador N values de seis candles permitem no máximo uma decisão de entrada em cada intervalo móvel de 24 horas.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de quatro horas alimentam EMA 10 e EMA 30. O estado do cruzamento evolui desde o início, mas candidatos de entrada só são habilitados depois de dez candles finalizados.
- Um cruzamento estrito para cima da EMA rápida pela lenta cria um candidato de venda. Um cruzamento estrito para baixo cria um candidato de compra.
- Time e Working time aceitam candidatos somente dentro do intervalo configurado. Combination reúne os dois fluxos direcionais, e Flag libera apenas o primeiro candidato aceito até ser reiniciado.
- A entrada aceita inicia N values. Depois de outros seis candles finalizados de quatro horas, o contador reinicia Flag e cria uma limitação móvel de 24 horas.
- Com posição zerada, Position modify envia uma ordem a mercado de Volume fixo. Contra uma posição unitária oposta, primeiro a fecha e então abre o novo lado com uma segunda ordem a mercado de Volume fixo.
- Position protection é o mecanismo de saída. Ele acompanha execuções diretas de entradas e reversões e pode fechar a posição no take-profit de 3% ou stop-loss fixo de 2%.

## Regras de entrada e saída

- **Entrada comprada**: Após um cruzamento estrito para baixo das EMA, dez candles finalizados de aquecimento, a janela Working time aberta e a trava móvel disponível, compra-se Volume. Partindo de zero abre comprado; partindo de uma posição vendida unitária, uma compra fecha e outra abre comprado.
- **Entrada vendida**: Após um cruzamento estrito para cima das EMA, dez candles finalizados de aquecimento, a janela Working time aberta e a trava móvel disponível, vende-se Volume. Partindo de zero abre vendido; partindo de uma posição comprada unitária, uma venda fecha e outra abre vendido.
- **Saída**: Position protection fecha a exposição acompanhada no take-profit de 3% ou stop-loss fixo de 2%. Um cruzamento oposto permitido mais tarde pode inverter a posição por meio de fechamento seguido de nova abertura.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 04:00:00 | Período de quatro horas; somente candles finalizados controlam EMA, aquecimento, contador móvel e verificações de preço da proteção. |
| Fast EMA Length | 10 | Comprimento da média móvel exponencial rápida. |
| Slow EMA Length | 30 | Comprimento da média móvel exponencial lenta. |
| Warmup Bars | 10 | Número de candles finalizados necessário antes de permitir entradas por cruzamento. |
| Session From | 00:00:00 | Início da janela permitida no horário de reprodução ou do servidor da estratégia. |
| Session Until | 23:59:59 | Fim da janela permitida no horário de reprodução ou do servidor da estratégia. |
| Rolling Cooldown Bars | 6 | Número de candles finalizados contado depois de uma entrada aceita antes de outra decisão ser permitida; seis candles de quatro horas equivalem a 24 horas. |
| Volume | 1 | Quantidade fixa usada por cada ação de abertura ou reversão. |
| Take Profit % | 3 | Movimento percentual favorável usado por Position protection. |
| Stop Loss % | 2 | Movimento percentual adverso usado pelo stop fixo, sem rastreamento. |

## Detalhes do diagrama

- O bloco [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) envia candles finalizados de quatro horas a dois blocos [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html). Crossing identifica mudanças entre EMA 10 e EMA 30; comparações estritas dos valores anteriores e atuais validam cada evento, e um ramo NOT cria o pulso para baixo.
- Uma porta de aquecimento de dez candles bloqueia candidatos iniciais. [Time](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/time.html) fornece o horário de reprodução ou do servidor a [Working time](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/working_time.html); a reprodução do histórico incluído usa UTC.
- Combination passa candidatos acionáveis de compra e venda a [Flag](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/flag.html). O primeiro candidato verdadeiro é liberado, inicia [N values](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/n_values.html) e bloqueia os seguintes até que outros seis candles finalizados reiniciem Flag.
- A posição atual seleciona o caminho de [Position modify](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html). Uma entrada a partir de zero usa uma ação a mercado de lado fixo; uma reversão usa duas ações consecutivas do mesmo lado, primeiro para zerar e depois para abrir. Assim, a limitação conta decisões de entrada aceitas, embora uma decisão possa gerar intencionalmente duas execuções.
- Todas as execuções diretas de abertura e reversão atualizam [Position protection](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). Fechamentos de candles finalizados conduzem suas verificações de preço; sua própria execução de fechamento não retorna à entrada de operações.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
