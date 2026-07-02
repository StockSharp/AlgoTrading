# Diagrama de Estratégia de Médias Móveis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este arquivo contém uma representação diagramática de uma estratégia de trading baseada em médias móveis, desenvolvida usando a Galeria de Estratégias da plataforma Designer. A estratégia utiliza o conceito de médias móveis para gerar sinais de compra e venda com base em seus cruzamentos, um método popular nos mercados financeiros para avaliar o momentum e confirmar tendências.

![schema](schema.png)

## Visão Geral da Estratégia

A estratégia incorpora duas médias móveis:

- **Média Móvel de Curto Prazo**: uma [média móvel](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) mais rápida que reage mais rapidamente às mudanças de preço.
- **Média Móvel de Longo Prazo**: uma [média móvel](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) mais lenta que fornece uma imagem mais suavizada das tendências de preços.

## Regras de Entrada e Saída

- **Sinal de Compra**: a estratégia gera um sinal de [compra](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) quando a média móvel de curto prazo [cruza](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) acima da média móvel de longo prazo, sugerindo uma tendência de alta.
- **Sinal de Venda**: por outro lado, um sinal de [venda](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) é emitido quando a média móvel de curto prazo [cruza](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) abaixo da média móvel de longo prazo, indicando uma possível tendência de baixa.

## Detalhes do Diagrama

O diagrama apresenta visualmente o fluxo lógico da estratégia:

- **Cálculo de Médias Móveis**: nós calculam as médias móveis com base em parâmetros definidos pelo usuário, como o período e o tipo de média móvel (por exemplo, simples, exponencial).
- **Nós de Comparação**: avaliam as condições de cruzamento para determinar se entrar ou sair de posições.
- **Ações de Trading**: nós que executam ordens de compra ou venda com base nos resultados da avaliação dos nós de comparação.

## Uso

Os traders podem importar este diagrama para a plataforma Designer para:
- testar a estratégia usando dados históricos para entender sua eficácia;
- modificar os parâmetros das médias móveis ou a lógica para melhor atender às necessidades específicas de trading ou às condições de mercado;
- implantar a estratégia em um ambiente de trading ao vivo após testes suficientes.

## Valor Educacional

Este diagrama de estratégia serve como uma ferramenta educacional para iniciantes entenderem os fundamentos da análise técnica e do design de estratégias. Também fornece uma base para o desenvolvimento de estratégias mais complexas para usuários avançados.

Este arquivo faz parte de uma coleção abrangente de estratégias de trading fornecidas na plataforma Designer, com o objetivo de aprimorar as habilidades de trading e as capacidades de desenvolvimento de estratégias dos usuários.
