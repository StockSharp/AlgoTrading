# Estratégia de Padrão Borboleta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Padrão Borboleta** converte a lógica de padrão harmônico original MetaTrader "Cypher EA" em StockSharp de alto nível API. A estratégia verifica uma série de velas configuráveis ​​em busca de formações borboleta de alta e baixa, valida as proporções harmônicas e abre posições de mercado com três metas de lucro em estágios. Recursos opcionais de gerenciamento de risco refletem o especialista MetaTrader: bloqueio de ponto de equilíbrio e atualizações de trailing stop estão disponíveis após saídas parciais.

## Como funciona

1. As velas são armazenadas em buffer até que um ponto de pivô possa ser confirmado usando a janela `PivotLeft`/`PivotRight`.
2. Quando cinco pivôs alternados estão disponíveis, a estratégia verifica as proporções Fibonacci necessárias para um padrão borboleta.
3. As configurações qualificadas são revalidadas (opcional) e avaliadas por um índice de qualidade harmônica (`MinPatternQuality`).
4. Assim que um padrão for confirmado em uma vela fechada:
   - Uma ordem de mercado é colocada usando volume fixo ou dimensionamento baseado em risco.
   - O volume da posição é dividido entre três níveis de take-profit (`TP1/TP2/TP3`).
   - Um stop-loss geométrico é derivado da estrutura do padrão.
5. Durante a vida útil da posição, a estratégia monitora velas para acionar saídas parciais, bloqueio de ponto de equilíbrio e ajustes finais de acordo com os limites configurados.

> **Dica:** A versão MetaTrader funciona com vários intervalos de tempo simultaneamente. Para replicar esse comportamento em StockSharp, inicie várias instâncias da estratégia com valores `CandleType` diferentes.

## Parâmetros principais

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Prazo usado para detectar pivôs e padrões. |
| `PivotLeft` / `PivotRight` | Número de velas à esquerda/direita necessárias para confirmar um pivô alto/baixo. |
| `Tolerance` | Desvio máximo da relação harmônica permitido na validação do padrão borboleta. |
| `AllowTrading` | Ativa ou desativa a geração de pedidos após uma confirmação de padrão. |
| `UseFixedVolume` / `FixedVolume` | Força um volume de comércio constante. Quando desativada, a estratégia dimensiona as posições via `RiskPercent`. |
| `RiskPercent` | Porcentagem do valor do portfólio arriscado por negociação (usado somente quando `UseFixedVolume` é falso). |
| `AdjustLotsForTakeProfits` | Normaliza os volumes parciais para garantir que a soma corresponda ao tamanho da entrada. |
| `Tp1Percent` / `Tp2Percent` / `Tp3Percent` | Distribuição do volume total entre os três níveis de take-profit. |
| `MinPatternQuality` | Pontuação harmônica mínima (0–1) necessária para aceitar um padrão detectado. |
| `UseSessionFilter`, `SessionStartHour`, `SessionEndHour` | Restrinja a negociação a uma janela específica da sessão de câmbio. |
| `RevalidatePattern` | Força uma verificação secundária do preço antes de abrir uma posição. |
| `UseBreakEven`, `BreakEvenAfterTp`, `BreakEvenTrigger`, `BreakEvenProfit` | Controla a ativação do ponto de equilíbrio após o nível de lucro especificado e o buffer de lucro adicional. |
| `UseTrailingStop`, `TrailAfterTp`, `TrailStart`, `TrailStep` | Permite trailing stops quando um nível de lucro for atingido e a excursão mínima favorável for alcançada. |

## Gestão de risco

- Os níveis de stop-loss, ponto de equilíbrio e trailing são gerenciados internamente sem a criação de pedidos adicionais. Saídas parciais e fechamentos de stop são acionados com ordens de mercado para emular a lógica MetaTrader.
- Quando `UseFixedVolume` está desativado, o tamanho da posição é calculado a partir da distância de parada, do valor do tick do instrumento e da configuração `RiskPercent`.

## Notas de uso

- Certifique-se de que o instrumento conectado suporta o `CandleType` configurado e a etapa de preço, caso contrário a lógica de validação pode rejeitar sinais devido a verificações de distância mínima.
- Os recursos de ponto de equilíbrio e trailing exigem que os respectivos níveis de lucro sejam preenchidos (`BreakEvenAfterTp` e `TrailAfterTp`).
- Várias instâncias de estratégia podem ser executadas simultaneamente em diferentes títulos ou prazos para reproduzir a varredura de vários prazos do EA original.
