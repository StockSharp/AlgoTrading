# Diagrama de Uso Básico da Fonte de Dados e do Bloco Chart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama fornece uma demonstração simples de como usar a fonte de dados "Candles" e o bloco "Chart" dentro da plataforma Designer. Foi projetado para ajudar os usuários a entender os fundamentos da obtenção de dados de mercado e sua visualização em formato de gráfico.

![schema](schema.png)

## Visão geral

O diagrama mostra a configuração básica necessária para recuperar dados de candles de um instrumento financeiro específico e exibi-los em um gráfico. Serve como exemplo fundamental para aqueles que são novos no uso do Designer ou que desejam começar com técnicas simples de visualização de dados.

## Componentes do diagrama

- **Fonte de dados Candles**: Este é o nó principal que obtém [dados de candles](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) do instrumento financeiro selecionado. Os usuários podem especificar o instrumento, o intervalo de dados e o período do candle (por exemplo, candles de 1 minuto, 5 minutos).
- **Bloco Chart**: Este nó é usado para [plotar](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) os dados obtidos em uma interface gráfica. Ele pode exibir vários atributos dos candles, como preços de abertura, máximo, mínimo e fechamento.

## Funcionalidade

- **Recuperação de dados**: O diagrama começa recuperando dados de candles usando os parâmetros especificados no bloco Fonte de Dados Candles.
- **Visualização de dados**: Os dados recuperados são então passados para o bloco Chart, que plota os candles em um gráfico dentro do ambiente Designer.

## Caso de uso

Este diagrama é particularmente útil para:
- Novos usuários aprendendo a configurar a recuperação e visualização de dados no Designer.
- Traders e analistas que desejam visualizar rapidamente dados de mercado para análise.
- Propósitos educacionais, demonstrando a interação básica entre nós de fonte de dados e ferramentas de visualização dentro da plataforma.

## Aplicação prática

Ao entender e usar esta configuração básica, os usuários podem:
- Configurar rapidamente representações visuais de dados de mercado para análise em tempo real ou histórica.
- Estender o diagrama básico incorporando ferramentas analíticas adicionais ou indicadores disponíveis no Designer.
- Usar o gráfico como bloco de construção para estratégias de negociação mais complexas ou estudos de dados.

Este diagrama faz parte de um conjunto mais amplo de recursos educacionais disponíveis na plataforma Designer, com o objetivo de aprimorar a proficiência dos usuários em manipulação e visualização de dados.
