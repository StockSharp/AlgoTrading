# Estratégia Above Below MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Above Below MA replica o consultor especialista MetaTrader *Above Below MA (barabashkakvn's edition)*. Ela monitora o quão longe os preços atuais negociam em relação a uma média móvel configurável e permite operações apenas quando o preço está no lado "errado" da média por pelo menos uma distância definida, enquanto a própria média segue a direção antecipada. A lógica foi portada para a API de alto nível do StockSharp e é executada exclusivamente em velas completadas.

## Visão geral

- **Regime de mercado**: Funciona melhor em instrumentos que frequentemente retestam uma média móvel antes de retomar a tendência.
- **Instrumentos**: Qualquer instrumento suportado pela sua conexão StockSharp. Pares de Forex se beneficiam mais porque o script original media a distância em pips.
- **Período**: Ajustável através do parâmetro *Candle Type* (período de 1 minuto por padrão).
- **Direção da posição**: Tanto operações compradas quanto vendidas são suportadas, mas apenas uma posição líquida pode existir em um dado momento.

## Lógica da estratégia

1. Calcula-se uma média móvel na série de velas selecionada. O método de média (SMA, EMA, SMMA, WMA), o preço aplicado (close, open, high, low, median, typical, weighted) e o deslocamento para frente replicam as entradas do MetaTrader.
2. A distância mínima expressa em pips é convertida em um deslocamento de preço real usando o `PriceStep` do instrumento. Se a corretora não publicar um passo de preço, o filtro de distância é ignorado automaticamente.
3. Em cada vela finalizada:
   - **Configuração comprada**:
     - A abertura e o fechamento da vela devem estar pelo menos à distância configurada abaixo da média móvel deslocada.
     - A média móvel deve estar subindo em comparação com a vela anterior.
   - **Configuração vendida**:
     - A abertura e o fechamento da vela devem estar pelo menos à distância configurada acima da média móvel deslocada.
     - A média móvel deve estar caindo em comparação com a vela anterior.
4. A estratégia fecha qualquer posição oposta antes de enviar uma nova ordem a mercado na direção do sinal. Nenhuma exposição simultânea comprada/vendida é permitida.

Todas as decisões de negociação são tomadas em velas completadas para evitar entradas repetidas dentro de uma barra em formação. As ordens são executadas via `BuyMarket` ou `SellMarket` com o volume configurado.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `MaPeriod` | Comprimento da média móvel. Padrão 6.
| `MaShift` | Número de velas para deslocar a média móvel para frente. Um valor de 0 usa a barra atual, `n` usa o valor de `n` barras atrás. Padrão 0.
| `MaMethod` | Tipo de média móvel: `Simple`, `Exponential`, `Smoothed` ou `Weighted`. Padrão `Exponential`.
| `AppliedPrice` | Fonte de preço: close, open, high, low, median, typical ou weighted. Padrão `Typical`.
| `MinimumDistancePips` | Distância requerida em pips entre os preços da vela e a média móvel. Convertida usando `PriceStep`. Padrão 5.
| `CandleType` | Tipo de vela que impulsiona as atualizações do indicador. Período de 1 minuto por padrão.
| `TradeVolume` | Volume da ordem para novas entradas. Padrão 1.

## Notas adicionais

- Nenhuma lógica de stop-loss ou take-profit está incluída. A gestão de risco deve ser implementada via configurações do portfólio ou módulos externos.
- O buffer de deslocamento da média móvel é mantido mínimo e respeita a diretriz de "sem coleções" armazenando apenas os valores necessários para o deslocamento especificado.
- Quando `PriceStep` não está disponível, o filtro de distância mínima não pode ser avaliado, portanto as entradas dependem exclusivamente das condições da média móvel.
- A estratégia desenha a série de velas, o indicador de média móvel e suas operações na área do gráfico padrão quando um contêiner de gráfico está disponível.
