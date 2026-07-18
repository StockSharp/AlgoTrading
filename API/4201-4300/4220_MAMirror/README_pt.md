# Estratégia de espelho MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia MA Mirror é uma conversão do especialista MetaTrader *MA_MirrorEA*. O sistema compara duas médias móveis simples
calculado no mesmo período, mas usando fontes de preços diferentes: fechamento de vela versus abertura de vela. Quando a média móvel de
os preços de fechamento ficam acima da média móvel dos preços de abertura que a estratégia deseja que seja comprada; quando cai abaixo da abertura
média, a estratégia quer ser curta. Um parâmetro de mudança configurável permite que as médias móveis sejam lidas a partir de velas mais antigas
portanto, a porta StockSharp pode reproduzir o deslocamento visual aplicado no indicador MetaTrader original.

A implementação StockSharp mantém o comportamento original de “espelho”: apenas uma posição de mercado pode existir a qualquer momento, e uma
a mudança de sinal primeiro fecha a posição anterior e depois abre uma nova na direção oposta. Assim como o MetaTrader
código, a estratégia começa com um sinal curto virtual, o que significa que a primeira negociação real acontece somente após a média de fechamento
move-se acima da média aberta.

## Lógica de negociação
1. Assine a série de velas definida por `CandleType` e processe apenas velas concluídas para evitar decisões prematuras.
2. Alimente duas médias móveis simples com os preços de fechamento e abertura da vela. Ambos os indicadores compartilham o mesmo `MovingPeriod` então seus
os valores podem ser comparados diretamente.
3. Armazene os valores recentes da média móvel em buffers circulares. Os buffers permitem recuperar o valor de `MovingShift`
velas atrás, emulando o parâmetro shift MetaTrader sem chamar métodos de indicador proibidos.
4. Quando a média de fechamento deslocada estiver acima da média de abertura deslocada, defina o sinal desejado para **compra**. Quando estiver abaixo, defina o
sinal desejado para **vender**. Se ambas as médias forem iguais, o sinal anterior é preservado.
5. Se este for o primeiro sinal e não for de alta, permaneça estável. Caso contrário, se o sinal desejado for diferente do último sinal executado
sinal, feche qualquer exposição existente e abra uma nova posição de mercado com `TradeVolume` lotes na nova direção.
6. Atualize o sinal armazenado para que as velas posteriores ignorem instruções duplicadas enquanto a direção da posição permanece inalterada.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 minuto | Período primário processado pela estratégia. |
| `MovingPeriod` | `int` | `20` | Comprimento das médias móveis simples utilizadas nos preços de fechamento e abertura. |
| `MovingShift` | `int` | `0` | Número de velas concluídas em que os valores da média móvel são deslocados para trás. |
| `TradeVolume` | `decimal` | `1` | Quantidade usada para cada ordem de mercado. |

## Diferenças do especialista MetaTrader original
- Os auxiliares de gerenciamento de dinheiro (stop loss, takeprofit, trailing stop) contidos no arquivo de inclusão MQL não são portados. O
A versão StockSharp sempre negocia um `TradeVolume` fixo e depende de controles de risco externos, se necessário.
- MetaTrader armazena pedidos individuais, enquanto StockSharp trabalha com posições líquidas. A conversão fecha a posição líquida existente
antes de abrir um novo para que a exposição resultante corresponda ao comportamento do bilhete único do EA.
- O processamento do indicador é feito por meio da assinatura de vela de StockSharp API junto com os indicadores de `SimpleMovingAverage` e
buffers internos em vez de chamar `iMA` diretamente.

## Dicas de uso
- Ajuste `TradeVolume` ao passo do lote do instrumento antes de iniciar a estratégia. O construtor também atribui o mesmo valor a
`Strategy.Volume`, então os métodos auxiliares emitem pedidos com o tamanho esperado.
- Aumente `MovingShift` se quiser ler as médias móveis de velas mais antigas, por exemplo, para alinhar com a forma como o MetaTrader
os gráficos da plataforma mudaram os indicadores.
- Adicione a estratégia a um gráfico para visualizar velas juntamente com médias móveis e negociações executadas, o que torna mais fácil
para confirmar que as reversões ocorrem exatamente quando a média de fechamento cruza a média de abertura.

## Indicadores
- Duas médias móveis simples (preço de fechamento e preço de abertura) com comprimentos idênticos e deslocamento para trás opcional.
