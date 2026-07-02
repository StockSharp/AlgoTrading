# Exemplo de Estratégia com Fórmulas e Expressões Matemáticas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este arquivo contém um exemplo detalhado de uma estratégia de trading projetada usando a plataforma Designer do StockSharp. A estratégia integra expressões e fórmulas matemáticas para executar negociações com base em condições específicas atendidas por indicadores técnicos.

## Visão Geral da Estratégia

Este esquema demonstra a aplicação de dois indicadores técnicos populares para tomar decisões de trading:

### Estratégia de Bandas de Bollinger
- **Condição de Compra**: Uma ordem de compra é acionada quando o candle de preço cruza para cima a curva superior do indicador Bollinger Bands.
- **Condição de Venda**: Uma ordem de venda é executada quando o candle de preço cruza para baixo a curva inferior do indicador Bollinger Bands.

### Estratégia do Indicador MACD
- **Condição de Compra**: Inicia uma ordem de compra quando a curva MACD muda seu sinal de negativo para positivo.
- **Condição de Venda**: Aciona uma ordem de venda quando a curva MACD muda seu sinal de positivo para negativo.

## Recursos Adicionais

- **Comparação Visual**: O esquema permite uma comparação visual lado a lado dos resultados de ambas as estratégias.
- **Exportação de Resultados**: Inclui funcionalidade para exportar os resultados dos testes em um arquivo para análise posterior.

Este esquema fornece uma estrutura prática para entender e aplicar ferramentas matemáticas em estratégias de trading, aproveitando as capacidades da plataforma Designer.
