# Estratégia de Alerta de Linha de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia monitora duas linhas de tendência definidas pelo usuário e reage quando o preço as rompe. As linhas superior e inferior representam níveis de resistência e suporte. Quando o preço de fechamento cruza acima da linha superior, uma posição comprada é aberta. Quando o preço cai abaixo da linha inferior, uma posição vendida é aberta. A lógica opcional de trailing stop protege posições abertas movendo o nível do stop na direção do trade.

## Parâmetros

- `Breakout Points` – pontos adicionais somados aos níveis da linha de tendência para definir o limiar de rompimento.
- `Upper Line` – nível de preço para o rompimento de alta.
- `Lower Line` – nível de preço para o rompimento de baixa.
- `Start Hour` – horário de início do trading em horas.
- `End Hour` – horário de fim do trading em horas.
- `Use Trailing Stop` – ativa o gerenciamento do trailing stop.
- `Trailing Stop Points` – distância em pontos para o trailing stop.
- `Candle Type` – período de velas usado para análise.

## Como Funciona

1. A estratégia se inscreve na série de velas selecionada.
2. Para cada vela fechada verifica se o horário está dentro da janela de trading especificada.
3. Um rompimento é detectado quando o fechamento da vela cruza acima da linha superior ou abaixo da linha inferior, ajustado pelo limiar de pontos de rompimento.
4. Quando um rompimento ocorre, uma ordem a mercado é enviada na direção do rompimento se não houver posição existente.
5. Se o trailing stop estiver ativado, o nível do stop segue o preço até ser acionado.

## Notas

- A estratégia é uma conversão simplificada do assessor especialista original TrendlineAlert do MetaTrader. O desenho manual de linhas de tendência é substituído por níveis de preço fixos definidos por parâmetros.
- Nenhuma ordem é colocada fora dos horários de trading especificados.
