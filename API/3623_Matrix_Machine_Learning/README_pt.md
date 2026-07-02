# Estratégia de aprendizado de máquina matricial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Matrix Machine Learning é uma abordagem baseada em redes neurais publicada originalmente para MetaTrader 5 dentro do projeto educacional "MQL5Book". O script especialista coleta uma janela de preços de ticks, converte diferenças de preços consecutivas em uma sequência binária e treina uma rede neural recorrente Hopfield. A rede treinada é avaliada em um segmento dentro da amostra, validada em um segmento fora da amostra e finalmente usada para inferir a direção dos próximos movimentos. As posições são abertas quando o primeiro elemento do vetor binário previsto mostra uma direção de alta (`+1`) ou de baixa (`-1`).

Esta versão C# transporta a lógica original para o StockSharp API de alto nível e substitui o processamento de ticks por velas finalizadas para garantir um comportamento estável entre plataformas. Cada fechamento de vela atualiza o padrão de preço binário, treina novamente a rede Hopfield, avalia a precisão histórica e produz uma previsão online para as próximas etapas.

## Detalhes do algoritmo
1. Colete os últimos `HistoryDepth` fechamentos de velas. Os pontos `ForwardDepth` mais recentes formam o conjunto fora da amostra, enquanto os valores restantes criam o segmento de treinamento.
2. Converta diferenças consecutivas de fechamento a fechamento em uma sequência binária: deltas positivos ou zero tornam-se `+1`, deltas negativos tornam-se `-1`.
3. Treine uma matriz de pesos de Hopfield somando os produtos externos de cada par preditor/saída onde o comprimento do preditor é igual a `PredictorLength` e o comprimento da resposta é igual a `ForecastLength`.
4. Avalie a matriz treinada nos conjuntos de treinamento e encaminhamento. A métrica de precisão corresponde ao script original: o produto escalar entre os vetores de resposta previstos e reais é calculado e redimensionado para uma porcentagem.
5. Construa o padrão binário online mais recente e execute o loop de inferência Hopfield (ativação tanh com limite de convergência). O primeiro componente de previsão orienta a decisão comercial.

## Parâmetros
- **Profundidade do histórico** – número de fechamentos recentes de velas armazenados para a rede Hopfield. Deve ser maior que `ForwardDepth` e pelo menos `PredictorLength + ForecastLength + 1`.
- **Forward Depth** – tamanho da janela de validação reservada para verificações futuras. Requer pelo menos `ForecastLength + 1` fechamentos.
- **Comprimento do preditor** – comprimento do vetor de entrada binário usado pela rede neural.
- **Comprimento da previsão** – número de etapas futuras previstas pelo vetor de saída da rede.
- **Tipo de vela** – StockSharp `DataType` descrevendo a série de velas solicitada ao conector.
- **Log de depuração** – quando ativado, imprime vetores intermediários detalhados, comparações de amostras e previsões on-line.

## Lógica de negociação
- Se o primeiro elemento da previsão de Hopfield for positivo e a estratégia for plana ou curta, uma ordem de compra de mercado será enviada para `Volume + |Position|` passar para uma posição longa.
- Se o primeiro elemento for negativo e a estratégia for plana ou longa, uma ordem de venda a mercado é enviada para `Volume + |Position|` passar para uma posição curta.
- Previsões zero são ignoradas para evitar rotações desnecessárias.

A estratégia traça automaticamente velas e negociações próprias quando uma área do gráfico está disponível. A rede Hopfield treina novamente cada vela finalizada para manter os pesos neurais sincronizados com a estrutura de mercado mais recente.
