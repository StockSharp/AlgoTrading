# Estratégia Métrica Euclidiana Estatística
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia reproduz o comportamento do MetaTrader consultor especialista `Stat_Euclidean_Metric.mq4`. Ele monitora MACD reversões em um único instrumento e período. Quando a linha MACD forma um ponto de inflexão local, a estratégia abre uma posição imediatamente (modo de treinamento) ou valida a configuração com um classificador k-vizinhos mais próximos (k-NN) que compara a estrutura atual do mercado com vetores de recursos históricos armazenados em arquivos binários.

## Lógica de negociação
1. Assine o tipo de vela configurado e calcule o indicador MACD no preço típico ((High + Low + Close) / 3).
2. Detecte uma reversão de baixa quando os últimos três valores MACD concluídos satisfizerem `MACD[2] <= MACD[1]` e `MACD[1] > MACD[0]`.
3. Detecte uma reversão de alta quando `MACD[2] >= MACD[1]` e `MACD[1] < MACD[0]`.
4. Dependendo do modo selecionado:
   - **Modo de treinamento (`TrainingMode = true`)** – abra uma ordem de mercado na direção da reversão após, opcionalmente, fechar a posição atual. Isso imita o comportamento original EA ao coletar novas amostras.
   - **Modo classificador (`TrainingMode = false`)** – calcule cinco proporções de médias móveis simples do preço típico e avalie a probabilidade de sucesso com um modelo k-NN. Faça pedidos somente se a probabilidade ultrapassar os limites configurados.
5. Aplique o módulo `StartProtection` integrado para anexar níveis de stop-loss e take-profit nas etapas do instrumento.

## Vetor de recursos para classificação
O modelo k-NN usa as seguintes proporções calculadas na vela recém-fechada:
- SMA(89) / SMA(144)
- SMA(144) / SMA(233)
- SMA(21) / SMA(89)
- SMA(55) / SMA(89)
- SMA(2) / SMA(55)

Cada amostra armazenada nos arquivos do conjunto de dados contém seis valores `double`: as cinco proporções acima e um rótulo (`0` para um resultado desfavorável, `1` para uma negociação bem-sucedida). Durante a avaliação, a estratégia seleciona as amostras `NeighborCount` mais próximas, calcula a média de seus rótulos e interpreta o resultado como a probabilidade de sucesso.

## Arquivos de conjunto de dados
- `BuyDatasetPath` – caminho para o arquivo binário com vetores coletados após negociações de alta.
- `SellDatasetPath` – caminho para o arquivo binário com vetores coletados após negociações de baixa.

Se um caminho for relativo, ele será resolvido em `Environment.CurrentDirectory`. Os arquivos ausentes são relatados no log e tratados como um conjunto de dados vazio. Esta implementação lê conjuntos de dados, mas não atualiza nem acrescenta novas amostras automaticamente; a exportação de novos vetores deve ser tratada externamente durante a execução no modo de treinamento.

## Parâmetros
- **TrainingMode** – alterne entre negociação MACD pura e negociação assistida por classificador.
- **BuyThreshold / SellThreshold** – probabilidade mínima retornada pelo classificador para abrir negociações na direção primária.
- **AllowInverseEntries** – permite negociações contrárias quando a probabilidade é extremamente baixa.
- **InverseBuyThreshold / InverseSellThreshold** – probabilidade máxima que ainda desencadeia uma negociação na direção oposta.
- **FastLength / SlowLength / SignalLength** – MACD EMA comprimentos.
- **TakeProfitPoints / StopLossPoints** – níveis de proteção expressos em etapas do instrumento.
- **ClosePositionsOnSignal** – fecha a posição líquida atual antes de enviar uma nova ordem.
- **BuyDatasetPath / SellDatasetPath** – arquivos binários que armazenam vetores históricos.
- **NeighborCount** – número de vizinhos usados na votação k-NN.
- **CandleType** – série de velas usada para todos os indicadores.

## Recomendações de uso
- Forneça caminhos absolutos ou relativos ao diretório de trabalho para os arquivos do conjunto de dados antes de ativar o modo classificador.
- Colete amostras de alta qualidade executando a estratégia no modo de treinamento em dados históricos e exportando vetores manualmente.
- Otimize os limites e a contagem de vizinhos para adaptar o classificador a novos mercados ou instrumentos.
- Mantenha o parâmetro `Volume` do instrumento alinhado ao modelo de risco, pois a estratégia sempre abre `Volume + |Position|` lotes para reverter a posição líquida quando necessário.

## Diferenças da versão MQL4
- Os conjuntos de dados do classificador são apenas lidos; o EA original grava novas amostras durante a desinicialização. Aqui o usuário deve atualizar os arquivos manualmente após analisar o histórico de negociações.
- Todas as ordens de proteção são anexadas por meio de StockSharp `StartProtection` em vez de parâmetros manuais `OrderSend`.
- O fechamento de ordem no modo classificador sempre sai de toda a posição quando `ClosePositionsOnSignal` está habilitado, enquanto o script MQL4 fecha apenas ordens lucrativas antes de receber novos sinais.
