# Estratégia de teste de padrões de velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de teste de padrões de velas** é uma conversão de alto nível StockSharp do consultor especialista original MetaTrader 5 *CandlePatternsTest EA*. A estratégia verifica as velas concluídas em busca de uma lista selecionada de formações clássicas de velas japonesas e reage entrando em posições longas ou curtas quando aparecem estruturas de alta ou baixa. A conversão se concentra na lógica de padrão discricionária do robô de origem enquanto aproveita StockSharp controles de risco e assinatura de dados API.

## Lógica de negociação

1. **Assinatura de vela** – a estratégia assina o tipo de vela configurado e aguarda as barras finalizadas antes de executar o reconhecimento de padrão.
2. **Filtro de corpo médio** – uma média móvel simples dos corpos das velas atua como normalização dinâmica. Somente os padrões cujas velas constituintes excedem esta média são considerados válidos, espelhando a função `AvgBody` da implementação `AvgBody`.
3. **Reconhecimento de padrões** – o detector verifica:
   - Três Soldados Brancos / Três Corvos Negros
   - Linha Piercing / Cobertura de Nuvem Escura
   - Estrela Doji da Manhã / Estrela Doji da Noite
   - Engolindo alta e baixa
   - Harami de alta e baixa
   - Linhas de Reunião
4. **Gerenciamento de entradas** – uma vez confirmado um padrão de alta, a estratégia abre uma ordem de compra de mercado; padrões de baixa acionam uma ordem de venda no mercado. Os sinais opostos invertem automaticamente a posição atual.
5. **Gerenciamento de saída** – os níveis de proteção de stop-loss e take-profit são derivados do corpo médio da vela e rastreados em cada vela finalizada. Se o preço atingir qualquer um dos limites, a posição será fechada.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Tipo de dados de velas para assinar (padrão: período de 1 hora). |
| `AverageBodyPeriod` | Número de velas usadas para o comprimento médio do corpo. Controla a normalização do padrão. |
| `EnableBullishPatterns` | Ativa ou desativa entradas longas. |
| `EnableBearishPatterns` | Ativa ou desativa entradas curtas. |
| `StopLossFactor` | Multiplicador aplicado ao corpo médio para distância de stop-loss. |
| `TakeProfitFactor` | Multiplicador aplicado ao corpo médio para a distância de take-profit. |

Todos os parâmetros são expostos por meio de `StrategyParam<T>` para oferecer suporte à configuração da GUI e às execuções do otimizador.

## Gráficos

Quando uma área do gráfico está disponível, a estratégia traça:

- As velas assinadas
- A média móvel de preço de fechamento usada para contexto de tendência
- Negociações executadas para verificação visual

## Diferenças do original EA

- Filtros de notícias, janelas de tempo, alternâncias de hedge e gerenciamento de grade de trilha presentes no arquivo MQ5 original são intencionalmente omitidos para focar no núcleo do padrão de velas.
- A gestão de risco é simplificada para um modelo stop/target simétrico derivado da volatilidade da vela.
- A versão StockSharp usa o gerenciamento de posição da estrutura e auxiliares `BuyMarket`/`SellMarket` em vez de tickets de pedidos manuais.

## Notas de uso

- Defina o parâmetro `CandleType` para alinhar com a sessão de mercado que você deseja analisar; prazos mais altos produzem menos sinais, mas mais fortes.
- Ajuste `AverageBodyPeriod` para que o corpo médio se aproxime da volatilidade recente. Um valor menor reage mais rápido, mas pode aumentar o ruído.
- `StopLossFactor` e `TakeProfitFactor` podem ser otimizados para corresponder ao perfil de risco do instrumento.

## Requisitos

- Ambiente StockSharp com feed de dados de mercado capaz de gerar o tipo de vela configurado.
- A estratégia espera séries de velas sequenciais e não sobrepostas. Certifique-se de que o quadro selecionado suporte atualizações regulares da barra.
