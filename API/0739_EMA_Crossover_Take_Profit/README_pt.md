# Estratégia de Cruzamento EMA com Take Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no cruzamento das médias móveis exponenciais (EMAs) de 20 e 50 períodos. Uma posição comprada é aberta quando a EMA rápida cruza acima da EMA lenta, e uma posição vendida é aberta no cruzamento oposto.

Após uma entrada, quatro níveis de take profit são calculados a partir do intervalo da vela de sinal. A posição é fechada quando o preço atinge qualquer um desses níveis ou quando o stop loss é acionado. As velas são destacadas em verde quando a EMA rápida está acima da EMA lenta e em vermelho quando está abaixo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA20 cruza acima de EMA50.
  - **Vendido**: EMA20 cruza abaixo de EMA50.
- **Take Profit**: Quatro alvos baseados em multiplicadores do intervalo anterior.
- **Stops**: Stop loss de 3% a partir do preço de entrada.
- **Indicadores**: EMA20, EMA50, EMA200.
- **Período**: Configurável via parâmetro.
- **Direção**: Comprado e Vendido.
