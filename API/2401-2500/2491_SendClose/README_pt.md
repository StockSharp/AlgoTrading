# Estratégia SendClose
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
SendClose é uma estratégia de rompimento baseada em fractais que recria o comportamento do consultor especialista original do MT5. O algoritmo constrói continuamente linhas dinâmicas de suporte e resistência ligando pivôs fractais alternantes e reage no momento em que o preço revisita esses níveis projetados. O port do StockSharp mantém as mecânicas centrais intactas: as linhas de tendência são geradas a partir de sequências alternantes de fractais para cima/para baixo, os rompimentos acionam entradas de mercado, e linhas de deslocamento separadas são usadas para forçar a liquidação de posições.

## Fluxo de detecção de fractais
1. **Janela de cinco velas** – a estratégia mantém um buffer rolante das últimas cinco velas concluídas. Assim que a janela estiver cheia, avalia a vela do meio em relação aos dois vizinhos mais antigos e dois mais novos.
2. **Regra de fractal ascendente** – a vela central forma um fractal ascendente quando sua máxima é maior do que as máximas das duas velas mais novas e estritamente maior do que as máximas das duas velas mais antigas. Isso corresponde à lógica `iFractals` do MT5 (>= no lado mais novo, > no lado mais antigo).
3. **Regra de fractal descendente** – similarmente, a vela central é um fractal descendente se sua mínima for menor ou igual em comparação com as velas mais novas e estritamente menor do que as duas velas mais antigas.
4. **Fila de fractais** – cada fractal recém-confirmado é inserido em uma fila FIFO de seis elementos ordenada do mais recente para o mais antigo. Esta fila é posteriormente escaneada para encontrar os padrões alternantes necessários.

## Construção de linhas de tendência
* **Linha de venda** – o algoritmo procura a sequência mais recente *fractal ascendente → fractal descendente → fractal ascendente*. A linha é traçada pelo primeiro e último fractal ascendente, conectando efetivamente dois topos de oscilação separados por um fundo de oscilação.
* **Linha de compra** – simetricamente, procura uma cadeia *fractal descendente → fractal ascendente → fractal descendente* e conecta os fundos de oscilação circundantes.
* **Projeção** – os pontos finais armazenados (tempo e preço) são usados para interpolar ou extrapolar o valor da linha para qualquer timestamp posterior. Quando o mercado alcança a projeção no fechamento da vela atual, uma decisão de negociação é tomada.
* **Linhas de fechamento** – dois níveis auxiliares são calculados deslocando a linha de venda para cima e a linha de compra para baixo por `LineOffsetSteps * PriceStep`. Eles atuam como gatilhos de saída forçada assim como as linhas Close1/Close2 originais.

## Lógica de trading
* **Condições de entrada**
  * Vender quando o preço toca a linha de venda e não há exposição comprada conflitante. A exposição vendida existente pode ser aumentada até o limite `MaxPositions` ser atingido.
  * Comprar quando o preço toca a linha de compra e não há exposição vendida conflitante. A exposição comprada existente pode ser aumentada até o mesmo limite.
* **Condições de saída**
  * O preço tocando qualquer linha de fechamento fecha imediatamente a posição aberta, emulando o comportamento do MT5 onde tocar Close1/Close2 emite uma saída completa.
  * Os sinais de entrada tentam achatar posições opostas antes de colocar a nova ordem, refletindo a adaptação de cobertura para neteo dentro do StockSharp.
* **Detecção de toque** – a precisão de tick do MT5 é aproximada com dados de velas. Um nível é considerado "tocado" quando está entre a máxima e a mínima da vela.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `EnableSellLine` | Habilita ou desabilita ordens baseadas na linha fractal superior (de venda). |
| `EnableBuyLine` | Habilita ou desabilita ordens baseadas na linha fractal inferior (de compra). |
| `EnableCloseSellLine` | Ativa o nível Close1 que fecha posições quando o preço sobe acima da linha de venda mais o deslocamento. |
| `EnableCloseBuyLine` | Ativa o nível Close2 que fecha posições quando o preço cai abaixo da linha de compra menos o deslocamento. |
| `MaxPositions` | Número máximo de lotes que podem permanecer abertos em uma direção. Entradas adicionais além deste limite são ignoradas. |
| `OrderVolume` | Volume de cada ordem de mercado. O valor deve corresponder ao tamanho do contrato do instrumento. |
| `LineOffsetSteps` | Deslocamento, medido em passos de preço, usado ao calcular os níveis Close1/Close2. O padrão 15 replica o deslocamento `15*Point()` do MT5. |
| `CandleType` | Série de velas usada para análise. Escolha um período de tempo que corresponda ao gráfico que planeja negociar (ex., M15, H1). |

## Notas de implementação
* A estratégia executa em velas concluídas para respeitar o EA original, que dependia de barras MT5 confirmadas antes de avaliar os fractais.
* A igualdade em nível de tick com bid/ask é aproximada com intervalos de velas. Se maior precisão for necessária, alimentar dados de tick em vez de velas.
* O parâmetro `MaxPositions` opera sobre a posição líquida do StockSharp. Portanto é adequado para contas de neteo; contas de cobertura ainda podem simular escalonamento aumentando `MaxPositions`.
* As linhas de fechamento são avaliadas antes das entradas. Se tanto uma saída quanto uma entrada forem acionadas na mesma vela, a saída tem prioridade, evitando ordens conflitantes.

## Diretrizes de uso
1. Configure o símbolo e o período de tempo desejados no seu terminal StockSharp e certifique-se de que o instrumento forneça informações de `PriceStep`. A lógica de deslocamento depende disso.
2. Ajuste `CandleType` para corresponder ao período de tempo que deseja analisar. O padrão é 30 minutos, que oferece um equilíbrio entre ruído e capacidade de resposta.
3. Defina `OrderVolume` como o tamanho da posição que deseja enviar por operação. Para futuros, use contagens de contratos; para CFDs de câmbio, use tamanhos de lote.
4. Ajuste `LineOffsetSteps` para se alinhar com a volatilidade do instrumento. Deslocamentos maiores requerem um movimento mais forte para acionar as saídas Close1/Close2.
5. Monitore o número de lotes abertos ao aumentar `MaxPositions`. A estratégia não excederá este limite, mas ainda pode piramidear posições em mercados de tendência.

## Diferenças da versão MT5
* O StockSharp opera com posições líquidas, portanto o código achata a exposição oposta antes de abrir uma nova operação em vez de manter tickets de compra/venda simultâneos.
* Os objetos de gráfico não são desenhados automaticamente. Se precisar de visualização no gráfico, conecte um módulo de gráfico e plote os valores de linha gerados manualmente.
* A detecção de toque baseada em velas pode disparar ligeiramente mais tarde do que as verificações de tick do MT5, especialmente em mercados rápidos com velas amplas.

## Gerenciamento de risco
A estratégia coloca ordens de mercado sem stop-losses integrados. Sempre complemente-a com controles de risco externos como stops de capital, filtros de horário de negociação ou supervisão manual. Faça backtesting extensivo no instrumento e período de tempo alvo antes de implantar ao vivo.
