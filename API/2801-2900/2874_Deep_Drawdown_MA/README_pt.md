# Estratégia de MA com Proteção de Drawdown Profundo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de MA com Proteção de Drawdown Profundo é uma conversão direta do consultor especialista MetaTrader 5 "Deep Drawdown MA (barabashkakvn's edition)" para a API de alto nível do StockSharp. A estratégia negocia cruzamentos de médias móveis enquanto aplica um mecanismo de ponto de equilíbrio projetado para proteger operações que entraram em drawdown. A versão StockSharp mantém os parâmetros configuráveis de média móvel, a capacidade de limitar o número de entradas agregadas e a opção de liquidar imediatamente as operações perdedoras em uma inversão de sinal.

## Lógica de operação
- **Indicadores**: Duas médias móveis com períodos individuais, fontes de preço e deslocamentos históricos. Ambas as médias compartilham o mesmo método de suavização (SMA, EMA, SMMA ou LWMA).
- **Condições de entrada**:
  - **Comprado**: A média rápida deslocada sobe acima da média lenta deslocada. A estratégia adiciona o volume de ordem configurado (e cobre qualquer exposição vendida) quando a última entrada não foi comprada e o limite máximo de posição não é excedido.
  - **Vendido**: A média rápida deslocada cai abaixo da média lenta deslocada. A estratégia vende o volume configurado (e cobre qualquer exposição comprada) quando a entrada anterior não foi vendida e o limite máximo de posição permite.
- **Condições de saída**:
  - **Comprados**: Quando a média rápida cruza de volta abaixo da média lenta, a posição é fechada imediatamente (`CloseLosses = true`) ou marcada para uma saída de ponto de equilíbrio. Durante uma saída de ponto de equilíbrio, a estratégia aguarda até que o fechamento da vela retorne ao preço de entrada médio antes de zerar.
  - **Vendidos**: Comportamento espelhado — em um cruzamento altista, a posição é fechada instantaneamente ou armada com um alvo de ponto de equilíbrio que é acionado quando o preço retorna à entrada média.
- **Rastreamento de posição**: O preço de entrada médio e a última direção aberta são reconstruídos a partir das próprias operações para que a API de alto nível possa reproduzir o comportamento MQL.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `OrderVolume` | Tamanho de ordem para cada operação de mercado. | 0.1 |
| `MaxPositions` | Número máximo de lotes agregados por direção (exposição líquida). | 5 |
| `CloseLosses` | Fechar operações perdedoras imediatamente em uma inversão em vez de esperar o ponto de equilíbrio. | false |
| `FastMaPeriod` / `SlowMaPeriod` | Comprimento das médias móveis rápida e lenta. | 10 / 30 |
| `FastMaShift` / `SlowMaShift` | Deslocamento histórico aplicado a cada média móvel (emula o argumento shift do MT5). | 3 / 0 |
| `FastPriceType` / `SlowPriceType` | Fonte de preço usada por cada média móvel (Close, Open, High, Low, Median, Typical, Weighted). | Close |
| `MaMethod` | Método de suavização compartilhado por ambas as médias (SMA, EMA, SMMA, LWMA). | SMA |
| `CandleType` | Série de velas usada para os cálculos. | Velas de 15 minutos |

## Notas de conversão
- O robô original do MetaTrader poderia manter posições compradas e vendidas cobertas simultaneamente. As estratégias do StockSharp operam sobre posições líquidas; portanto, a versão convertida aplica exposição agregada respeitando ainda a contagem máxima de posições.
- A proteção de ponto de equilíbrio é implementada com sinalizadores internos em vez de modificações de ordens do MT5. A estratégia monitora os fechamentos de velas e sai ao preço de entrada médio reconstruído.
- Os parâmetros de "deslocamento" de média móvel são reproduzidos mantendo uma fila curta de valores de indicador recentes, que espelha o argumento `shift` do MT5 sem chamar buffers de indicador de baixo nível.

## Uso
1. Anexe a estratégia ao instrumento desejado e defina `OrderVolume`, tipo de vela e parâmetros de média móvel para corresponder ao seu mercado alvo.
2. Habilite a negociação quando a estratégia estiver em execução e a assinatura de velas estiver ativa.
3. Monitore os sinalizadores de ponto de equilíbrio nos registros: as operações serão zeradas automaticamente quando o preço retornar à entrada média.

## Gestão de risco
- Use `CloseLosses = true` para forçar liquidação rápida de operações perdedoras quando as médias se invertem.
- Ajuste `MaxPositions` para limitar a exposição agregada após entradas alternativas consecutivas.
- Combine a estratégia com os controles de risco em nível de conta disponíveis no StockSharp (por exemplo, `StartProtection`) para salvaguardas adicionais.

## Arquivos
- `CS/DeepDrawdownMaStrategy.cs` – Implementação em C# usando a API de alto nível do StockSharp.
- `README.md`, `README_ru.md`, `README_zh.md` – Documentação multilíngue do comportamento e parâmetros da estratégia.
