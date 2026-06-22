# Estratégia de Sinal de Vela JMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza duas médias móveis Jurik (JMA) calculadas sobre os preços de abertura e fechamento de cada vela. Um sinal de alta ocorre quando a JMA do preço de abertura cruza abaixo da JMA do preço de fechamento, gerando uma entrada comprada. Um sinal de baixa ocorre quando a JMA do preço de abertura cruza acima da JMA do preço de fechamento, gerando uma entrada vendida.

O período padrão são velas de quatro horas com um período JMA de sete. Os níveis de stop loss e take profit são definidos em pontos e aplicados através da gestão de risco integrada. A estratégia age apenas em velas concluídas e mantém no máximo uma posição aberta.

## Parâmetros
- **JMA Length** – período para ambas as JMAs.
- **Candle Type** – período das velas processadas.
- **Take Profit** – meta de lucro em pontos.
- **Stop Loss** – perda máxima em pontos.
