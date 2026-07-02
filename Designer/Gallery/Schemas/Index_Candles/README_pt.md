# Diagrama de Criação de Índice Composto a partir de Múltiplas Séries de Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este arquivo de diagrama ilustra uma estratégia para criar um índice composto a partir de séries de velas de diferentes instrumentos financeiros, utilizando a Galeria de Estratégias da plataforma Designer. A estratégia agrega dados de vários ativos para formar um índice unificado, que pode ser usado para avaliar o sentimento geral do mercado ou o desempenho de um setor.

![schema](schema.png)

## Visão Geral da Estratégia

A estratégia consiste em combinar dados de preços de múltiplos ativos em um único [índice](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/index.html). Esse processo normalmente utiliza técnicas de normalização ou ponderação para garantir que cada ativo contribua proporcionalmente para o valor final do índice.

## Componentes do Diagrama

- **Nós de Coleta de Dados**: são responsáveis por buscar os [dados de velas](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) de cada ativo selecionado.
- **Nós de Normalização**: aplicam normalização aos dados de velas para garantir um impacto uniforme no [cálculo do índice](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/index.html) final, mitigando os efeitos de escalas de preços diferentes.
- **Nós de Ponderação**: atribuem pesos a cada ativo com base em critérios predefinidos, como capitalização de mercado ou volatilidade histórica.
- **Nó de Cálculo do Índice**: agrega os dados de preços normalizados e ponderados para calcular o valor final do índice.

## Pontos de Entrada e Saída

- **Pontos de Entrada**: normalmente não há pontos de entrada tradicionais, pois esta estratégia não envolve diretamente decisões de negociação.
- **Saída**: a principal saída é o valor do índice em tempo real, que reflete o movimento coletivo dos ativos incluídos.

## Uso

Traders e analistas podem usar este diagrama para:
- monitorar o desempenho geral de um setor ou mercado específico criando um índice personalizado;
- comparar ativos individuais com o índice de mercado mais amplo para identificar desempenho acima ou abaixo da média;
- usar o índice personalizado como referência para o desempenho do portfólio.

## Valor Educacional

Este diagrama de estratégia é especialmente valioso para fins educacionais, fornecendo insights sobre:
- a mecânica do cálculo de índices e a importância da normalização e ponderação de dados na análise financeira;
- a aplicação de dados combinados de múltiplas fontes para criar métricas financeiras significativas.

Os usuários podem importar este diagrama para a plataforma Designer para explorar e modificar a abordagem, adaptá-la a diferentes conjuntos de ativos ou aumentar a complexidade da metodologia de cálculo do índice.

Este arquivo faz parte de uma coleção diversificada de estratégias disponíveis na plataforma Designer, destinada a aprimorar a compreensão dos usuários sobre agregação de dados financeiros e construção de índices.
