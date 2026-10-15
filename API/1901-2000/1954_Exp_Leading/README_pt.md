# Estratégia Exp Leading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um sistema de cruzamento baseado no indicador personalizado **Leading** descrito por John F. Ehlers em *Cybernetics Analysis for Stock and Futures*. O indicador calcula duas linhas:

1. **NetLead** – filtro líder suavizado controlado pelos coeficientes `Alpha1` e `Alpha2`.
2. **EMA** – uma média móvel exponencial simples com fator constante de 0.5.

A estratégia opera em velas fechadas do período selecionado. Quando a linha NetLead cruza **abaixo** da linha EMA, uma reversão de alta é antecipada e uma posição comprada é aberta. Inversamente, quando NetLead cruza **acima** da linha EMA, uma posição vendida é aberta. A posição anterior, se houver, é fechada implicitamente quando uma ordem oposta é enviada.

## Parâmetros

- `Alpha1` – coeficiente para o cálculo intermediário do líder. Padrão: `0.25`.
- `Alpha2` – fator de suavização aplicado ao resultado líder. Padrão: `0.33`.
- `CandleType` – tipo de dados de vela usado para cálculos. Padrão: período de 4 horas.
- `StopLoss` – stop loss em unidades absolutas de preço. Padrão: `1000`.
- `TakeProfit` – take profit em unidades absolutas de preço. Padrão: `2000`.

## Lógica de Trading

1. Cada vela fechada atualiza os valores de NetLead e EMA.
2. Se a barra anterior mostrou NetLead acima de EMA e a última barra mostra NetLead abaixo de EMA, uma ordem de mercado de **compra** é enviada.
3. Se a barra anterior mostrou NetLead abaixo de EMA e a última barra mostra NetLead acima de EMA, uma ordem de mercado de **venda** é enviada.
4. `StartProtection` é usado para aplicar automaticamente as regras de stop-loss e take-profit.

Este exemplo é destinado a fins educativos para demonstrar como uma estratégia MetaTrader pode ser portada para a API de alto nível do StockSharp.
