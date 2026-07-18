# Estratégia Harami de alta e baixa Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Harami Stochastic de alta e baixa** é a versão StockSharp do MetaTrader Expert Advisor `expert_abh_bh_stoch.mq5` da pasta `MQL/310`. O especialista original usa reconhecimento de padrão de velas para configurações Bullish Harami e Bearish Harami e requer uma confirmação do oscilador estocástico. A versão C# mantém a mesma lógica usando o StockSharp API de alto nível e adiciona registro detalhado e saída de gráfico para facilitar o monitoramento.

## Ideias Centrais

- Detecte os padrões de velas Bullish Harami e Bearish Harami usando as duas velas concluídas anteriores.
- Confirme as configurações de alta com a linha estocástica %D abaixo de um limite de sobrevenda e as configurações de baixa com %D acima de um limite de sobrecompra.
- Feche as posições curtas quando a linha estocástica %D subir acima dos limites de saída inferior ou superior e feche as posições longas quando %D cair abaixo desses limites.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Prazo da série de velas usada para reconhecimento de padrões. | `1 Hour` |
| `StochasticKPeriod` | Período de lookback para o cálculo estocástico de %K. | `47` |
| `StochasticDPeriod` | Período de suavização para a linha %D. | `9` |
| `StochasticSlowing` | Suavização adicional aplicada a %K (MT5 “desaceleração”). | `13` |
| `MovingAveragePeriod` | Número de velas usadas para calcular a média do tamanho do corpo para validação do padrão. | `5` |
| `OversoldLevel` | Limite de Stochastic%D para confirmar sinais de alta. | `30` |
| `OverboughtLevel` | Limite de Stochastic%D para confirmar sinais de baixa. | `70` |
| `ExitLowerLevel` | Nível estocástico inferior que aciona saídas. | `20` |
| `ExitUpperLevel` | Nível estocástico superior que aciona saídas. | `80` |

## Regras de negociação

### Entrada longa
1. Um padrão Harami de alta é detectado nas duas velas concluídas mais recentemente (uma pequena vela de alta engolfada por uma vela de baixa mais longa em uma tendência de baixa).
2. A linha %D estocástica da vela de confirmação está igual ou inferior a `OversoldLevel`.
3. Nenhuma posição longa está aberta no momento (`Position <= 0`).
4. A estratégia compra a mercado para o `Volume` configurado, adicionando qualquer exposição curta para inverter a posição, se necessário.

### Entrada curta
1. Um padrão Bearish Harami é detectado (pequena vela de baixa dentro de uma longa vela de alta durante uma tendência de alta).
2. O valor %D estocástico é igual ou superior a `OverboughtLevel`.
3. Não existe exposição curta (`Position >= 0`).
4. A estratégia vende no mercado, cobrindo primeiro qualquer posição longa, se necessário.

### Saídas
- **Cover Shorts:** Quando o %D estocástico cruza para cima através de `ExitLowerLevel` ou `ExitUpperLevel`, o algoritmo cobre toda a posição curta.
- **Fechar posições compradas:** Quando o estocástico %D cruza para baixo através de `ExitUpperLevel` ou `ExitLowerLevel`, a posição longa é fechada.

## Arquivos

- `CS/BullishBearishHaramiStochasticStrategy.cs` — implementação StockSharp de alto nível da estratégia.
- `README.md` — Documentação em inglês (este arquivo).
- `README_ru.md` — Documentação russa.
- `README_zh.md` — Documentação chinesa.

> **Observação:** A versão Python não está incluída nas instruções de conversão.
