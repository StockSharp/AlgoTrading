# Diagrama da estratégia Envelope Multi Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma média rápida e uma média lenta não se encontram em um único ponto limpo: elas se tocam, se separam e se tocam de novo em torno da mesma zona. Este diagrama aceita isso e transforma essa zona em três níveis — uma banda estreita traçada acima e abaixo da média lenta, e a própria média lenta. Cada nível ganha seu próprio bloco de cruzamento, e dois blocos Combination canalizam seis cruzamentos independentes em um único fluxo comprador e um único fluxo vendedor, de modo que toda uma escada de sinais chega a um só par de blocos de ordem.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de um instrumento alimentam duas médias móveis exponenciais, uma rápida e uma lenta, e um conversor que extrai o preço de fechamento do mesmo candle.
- Duas fórmulas pegam a média lenta e a escalam para cima e para baixo por uma fração fixa, produzindo um envelope superior e um inferior; com a média lenta entre eles, o diagrama passa a ter três níveis em vez de um.
- Três blocos de cruzamento observam a média rápida subir através do envelope inferior, através da média lenta e através do envelope superior, cada um em seu próprio par de entradas.
- Outros três blocos de cruzamento levam os mesmos três níveis com as entradas trocadas — o nível acima, a média rápida abaixo — de modo que relatam a média rápida afundando através desses níveis.
- Um bloco Combination junta os três cruzamentos de alta em um único fluxo e um segundo junta os três de baixa; a partir daí toda a escada vira um sinal por lado.
- Cada fluxo é confirmado por uma comparação do preço de fechamento com a média rápida, de modo que uma perfuração só conta como entrada enquanto o preço estiver do mesmo lado da linha rápida.
- As entradas são ordens a mercado de volume fixo abertas a partir de posição zerada; o fluxo oposto, usado em estado bruto, aciona sozinho um bloco de fechamento de posição.
- Position protection assume cada execução de entrada e a conduz com um take-profit percentual e um stop-loss móvel.

## Regras de entrada e saída

- **Entrada comprada**: O fluxo comprador dispara: a média rápida cruzou acima do envelope inferior, acima da média lenta ou acima do envelope superior. A comparação confirma que o candle fechou acima da média rápida, as duas respostas se encontram em uma condição lógica, e o bloco Position modify compra o volume da ordem a mercado. A configuração de abertura de posição deixa essa ordem passar somente enquanto a posição estiver zerada.
- **Entrada vendida**: O fluxo vendedor dispara da mesma maneira, nos cruzamentos espelhados: a média rápida afundou através do envelope superior, através da média lenta ou através do envelope inferior. A comparação confirma que o candle fechou abaixo da média rápida, e o bloco Position modify vende o volume da ordem a mercado a partir de posição zerada.
- **Saída**: Duas coisas independentes encerram a operação. O fluxo oposto, tomado sem a confirmação do preço, aciona um bloco de fechamento de posição: qualquer perfuração para baixo zera uma posição comprada, qualquer perfuração para cima zera uma vendida. Enquanto isso, Position protection acompanha as execuções de entrada, lê o preço de fechamento e encerra a operação no take-profit ou no stop móvel — o que for atingido primeiro.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 00:15:00 | Time frame da série de candles sobre a qual todo o resto é calculado. |
| Fast EMA Length | 10 | Período da média móvel exponencial rápida — a linha que faz a perfuração. |
| Slow EMA Length | 30 | Período da média móvel exponencial lenta — a linha em torno da qual a banda é traçada. |
| Envelope Buffer | 0.003 | Metade da largura da banda como fração da média lenta: 0.003 coloca os envelopes 0.3% acima e abaixo dela. |
| Order Volume | 1 | Tamanho da ordem, em lotes, para as duas direções de entrada. |
| Take Profit, % | 1.5 | Distância do take-profit, em percentual do preço de entrada. |
| Stop Loss, % | 0.8 | Distância do stop-loss, em percentual do preço de entrada; ela acompanha a posição que se move a favor. |

## Detalhes do diagrama

- Os dois blocos Combination são o que torna a escada legível. Sem eles, cada um dos seis cruzamentos precisaria de sua própria ligação até os blocos de ordem, e acrescentar um quarto nível significaria redesenhar todo o lado direito do diagrama.
- Um bloco de cruzamento informa a direção em que cruzou, e um cruzamento para baixo chega como um sinal negativo que um gatilho de ordem simplesmente ignora. É por isso que o conjunto de baixa é construído como três blocos separados com as entradas trocadas, e não pela negação do conjunto de alta.
- Os candles são assinados apenas como finalizados. A atualização de um candle ainda em formação carrega o horário de abertura da barra, e uma ordem marcada com horário anterior ao momento atual é recusada.
- As entradas usam a condição de abertura de posição, de modo que um sinal que chega enquanto uma operação já está em curso não custa nada: o bloco informa um volume inválido e nenhuma ordem é enviada. Essa única configuração faz o trabalho de um filtro de posição explícito dentro da condição de entrada.
- A banda é deliberadamente estreita. Ela é uma margem em torno da média lenta, não um canal de volatilidade, de modo que os dois níveis extras disparam perto do cruzamento simples e adensam o sinal em vez de substituí-lo.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
