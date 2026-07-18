# Seguidor de tendências FT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
FT Trend Follower é uma versão StockSharp do MetaTrader 4 consultor especialista `FT_TrendFollower.mq4`. A estratégia acompanha as tendências de médio prazo empilhando um ventilador Guppy Multiple Moving Average (GMMA) com um gatilho oscilador Laguerre, um cruzamento EMA rápido/lento e um filtro MACD. As entradas só disparam depois que o mercado mergulha no pacote GMMA, se recupera de um extremo de Laguerre e a maioria das linhas GMMA volta a se inclinar na direção da negociação. O gerenciamento de lucros reflete o EA original: um stop opcional baseado em swing, um stop de distância fixa e três módulos de saída escalonados mutuamente exclusivos, impulsionados por níveis de pivô diários ou médias de canal.

## Lógica estratégica
### Estrutura GMMA e detecção de tendências
* O leque GMMA vai de `StartGmmaPeriod` a `EndGmmaPeriod`. Os períodos são distribuídos em cinco grupos de `BandsPerGroup` linhas cada, replicando a lógica original `CountLine`.
* A direção da tendência compara o grupo GMMA mais lento (índice `CountLine + CountLine` do final) com o grupo mais rápido de longo prazo (índice `CountLine` do final). O aumento das médias de longo prazo define uma tendência de alta; os em queda definem uma tendência de baixa.
* A confirmação da inclinação conta quantas linhas GMMA de curto, médio e longo prazo aumentaram ou diminuíram em relação à barra anterior. Uma negociação exige que a contagem de inclinação ascendente (ou descendente) exceda metade do total de linhas GMMA, imitando o limite `controlvverh`/`controlvverhS` em MetaTrader.

### Preparação de sinal
* **Close reset** – Quando a vela anterior fecha abaixo da linha GMMA mais lenta, o módulo longo se arma; quando fecha acima da linha mais lenta, o módulo curto se arma. Cruzar de volta acima (ou abaixo) do GMMA mais rápido limpa os sinalizadores de armamento, assim como a lógica `CloseOk` original.
* **Gatilho Laguerre** – Um filtro Laguerre (`LaguerreGamma`) deve primeiro cair abaixo de `LaguerreOversold` (configuração longa) ou subir acima de `LaguerreOverbought` (configuração curta) enquanto a vela ainda respeita o GMMA de longo prazo. Somente depois que o oscilador recua através do limite é que uma entrada pode disparar.
* **EMA crossover** – O EMA rápida (`FastSignalLength`) deve mergulhar abaixo do EMA lenta (`SlowSignalLength`) para armar o módulo longo e, em seguida, cruzar novamente acima dele para liberar a entrada. Shorts revertem a desigualdade.
* **MACD filtro** – A linha principal MACD (5/35/5 como em EA) deve ser positiva para posições compradas e negativa para posições vendidas.

### Regras de entrada
Uma negociação longa é executada quando:
1. A detecção de tendências relata uma tendência de alta e a votação da inclinação GMMA excede metade das linhas disponíveis.
2. O gatilho Laguerre foi armado anteriormente e o valor atual fecha acima de `LaguerreOversold`.
3. O EMA rápida está acima do EMA lenta depois de anteriormente estar abaixo.
4. MACD é maior que zero.

Entradas curtas requerem condições simétricas com o oscilador cruzando abaixo de `LaguerreOverbought` e MACD negativo. Ao reverter uma posição existente, o tamanho do pedido compensa automaticamente a exposição anterior, de modo que a posição líquida final seja igual a `Volume`.

### Gestão de riscos e saídas
* **Stops** – Escolha o stop swing (`UseSwingStop`) posicionado abaixo (acima) da vela anterior em `SwingStopPips` pontos ou o stop de distância fixa (`UseFixedStop`) deslocado em `FixedStopPips` pontos. Se ambos estiverem habilitados ao mesmo tempo, a estratégia será abortada no início, reproduzindo as regras de validação EA.
* **Módulo de saída dinâmica (Quit)** – Quando ativado, o primeiro fechamento parcial (50% de `Volume`) é acionado quando o preço cruza o pivô R1/S1 do dia anterior com lucro não realizado. O restante fecha assim que o Hull MA produz um valor válido, correspondendo à verificação de buffer `hma1` de MetaTrader.
* **Módulo de saída da faixa de pivô (Quit1)** – O fechamento parcial inicial ainda ocorre em R1/S1. O restante sai em R2/S2 assim que a negociação permanecer lucrativa.
* **Módulo de saída de canal (Quit2)** – O primeiro fechamento parcial ocorre em R1/S1. A estratégia fecha o restante quando a vela reabre abaixo do canal SMA inferior (`ChannelPeriod`) para posições compradas ou acima do canal SMA superior para posições vendidas, refletindo o filtro de volatilidade original.

Apenas um módulo de saída pode estar ativo por vez, assim como a validação de parâmetro do EA.

## Parâmetros
* **Volume** – Tamanho do pedido para novas negociações.
* **StartGmmaPeriod / EndGmmaPeriod** – Limites para o fã do GMMA.
* **BandsPerGroup** – Número de linhas GMMA amostradas por grupo (CountLine em MT4).
* **FastSignalLength / SlowSignalLength** – EMA comprimentos usados para a confirmação de cruzamento.
* **TradeShift** – Mantido para compatibilidade; a implementação opera em velas finalizadas, portanto valores diferentes de 0 ou 1 são rejeitados.
* **UseSwingStop / SwingStopPips** – Habilita e configura a parada de proteção baseada em oscilação.
* **UseFixedStop / FixedStopPips** – Habilita a parada de distância fixa medida em faixas de preço.
* **EnablePivotExit / EnablePivotRangeExit / EnableChannelExit** – Módulos de saída preparados mutuamente exclusivos.
* **LaguerreOversold / LaguerreOverbought / LaguerreGamma** – Limites de disparo de Laguerre e fator de suavização.
* **HmaPeriod** – Comprimento MA do casco usado pelo módulo de saída pivô.
* **ChannelPeriod** – Duração do canal alto/baixo SMA para Quit2.
* **CandleType** – Período que conduz os cálculos da estratégia (padrão: velas de 1 hora).

## Notas adicionais
* Os níveis de pivô diários são calculados a partir da última vela diária concluída fornecida por uma assinatura secundária.
* Os pontos de preço e as conversões de pip dependem do `PriceStep` do título. Símbolos com diferentes tamanhos de ticks se adaptam automaticamente.
* A estratégia subscreve apenas indicadores de alto nível e evita leituras diretas de buffer, aderindo às diretrizes de alto nível API do projeto.
* Nenhuma implementação Python é fornecida neste pacote.
